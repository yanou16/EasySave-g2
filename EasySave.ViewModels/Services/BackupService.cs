using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using EasyLog;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Models;

namespace EasySave.ViewModels.Services
{
    /// <summary>
    /// Handles v3 backup execution: parallel jobs, priority files, large-file locking,
    /// pause/resume/stop controls, state tracking and logging.
    /// </summary>
    public class BackupService
    {
        private Logger _logger;
        private readonly string _stateFilePath;
        private readonly List<BackupStateEntry> _allStates;
        private readonly SettingsService _settingsService;
        private readonly CryptoSoftService _cryptoSoftService;
        private readonly BusinessSoftwareWatcher _businessWatcher;
        private readonly ParallelCoordinator _coordinator;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private readonly object _stateLock = new();
        private readonly ConcurrentDictionary<int, JobControl> _controls = new();

        public event EventHandler<BackupProgressChangedEventArgs>? ProgressChanged;
        public event EventHandler<BackupStatusChangedEventArgs>? StatusChanged;

        public BackupService(
            Logger logger,
            string stateFilePath,
            List<BackupStateEntry> allStates,
            SettingsService settingsService,
            CryptoSoftService cryptoSoftService,
            BusinessSoftwareWatcher businessWatcher,
            ParallelCoordinator coordinator)
        {
            _logger = logger;
            _stateFilePath = stateFilePath;
            _allStates = allStates;
            _settingsService = settingsService;
            _cryptoSoftService = cryptoSoftService;
            _businessWatcher = businessWatcher;
            _coordinator = coordinator;
            _businessWatcher.BusinessSoftwareChanged += OnBusinessSoftwareChanged;
            _businessWatcher.Start();
        }

        public Task Start(BackupJob job)
        {
            JobControl control = GetControl(job);
            lock (control.Sync)
            {
                if (control.RunTask is { IsCompleted: false })
                    return control.RunTask;

                control.RunTask = Task.Run(() => Execute(job));
                return control.RunTask;
            }
        }

        /// <summary>Executes multiple jobs in parallel.</summary>
        public void ExecuteAll(IEnumerable<BackupJob> jobs, CancellationToken ct = default)
        {
            var tasks = jobs.Select(job => Task.Run(() => Execute(job, ct), ct)).ToArray();
            Task.WaitAll(tasks, ct);
        }

        public Task StartAll(IEnumerable<BackupJob> jobs)
        {
            var tasks = jobs.Select(Start).ToArray();
            return Task.WhenAll(tasks);
        }

        /// <summary>Executes a single backup job with runtime controls.</summary>
        public void Execute(BackupJob job, CancellationToken externalCancellation = default)
        {
            JobControl control = GetControl(job);
            control.BeginRun();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                externalCancellation,
                control.Cancellation.Token);
            CancellationToken ct = linkedCts.Token;

            ReportStatus(job, BackupRuntimeStatus.Running);

            try
            {
                string? detectedAtLaunch = _businessWatcher.GetRunningBusinessSoftware();
                if (detectedAtLaunch is not null)
                {
                    _logger.LogBusinessSoftwareDetected(job.Name, detectedAtLaunch);
                    PauseAllFromBusiness();
                    control.WaitIfPaused(ct);
                }

                if (!Directory.Exists(job.SourceDirectory))
                    throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

                Directory.CreateDirectory(job.TargetDirectory);

                AppSettings settings = _settingsService.Load();
                var priorityExtensions = ParseExtensions(settings.PriorityExtensions);
                long largeFileLimitBytes = settings.MaxParallelFileSizeKb * 1024;

                string[] allFiles = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
                int totalFiles = allFiles.Length;
                long totalSize = allFiles.Sum(f => new FileInfo(f).Length);

                var priorityFiles = allFiles
                    .Where(f => priorityExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToArray();

                foreach (var _ in priorityFiles)
                    _coordinator.IncrementPendingPriority();

                BackupStateEntry state = GetOrCreateState(job.Name);
                InitializeState(state, totalFiles, totalSize);

                int processed = 0;
                long processedSize = 0;

                var parallelOptions = new ParallelOptions
                {
                    CancellationToken = ct,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                try
                {
                    Parallel.ForEach(allFiles, parallelOptions, sourceFile =>
                    {
                        ProcessFile(
                            job,
                            sourceFile,
                            state,
                            settings,
                            priorityExtensions,
                            largeFileLimitBytes,
                            totalFiles,
                            totalSize,
                            ref processed,
                            ref processedSize,
                            control,
                            ct);
                    });
                }
                catch (OperationCanceledException) when (control.IsStopRequested || externalCancellation.IsCancellationRequested)
                {
                    MarkStopped(job, state);
                    return;
                }

                MarkFinished(job, state);
            }
            catch (OperationCanceledException) when (control.IsStopRequested || externalCancellation.IsCancellationRequested)
            {
                BackupStateEntry state = GetOrCreateState(job.Name);
                MarkStopped(job, state);
            }
            catch
            {
                ReportStatus(job, control.IsStopRequested ? BackupRuntimeStatus.Stopped : BackupRuntimeStatus.Error);
                throw;
            }
            finally
            {
                control.EndRun();
            }
        }

        public void PauseJob(int jobId)
        {
            if (_controls.TryGetValue(jobId, out var control))
            {
                control.UserPause();
                ReportCurrentStatus(control);
            }
        }

        public void ResumeJob(int jobId)
        {
            if (_controls.TryGetValue(jobId, out var control))
            {
                control.UserResume();
                ReportCurrentStatus(control);
            }
        }

        public void StopJob(int jobId)
        {
            if (_controls.TryGetValue(jobId, out var control))
            {
                control.Stop();
                ReportStatus(control.Job, BackupRuntimeStatus.Stopped);
            }
        }

        public void PauseAll()
        {
            foreach (var control in _controls.Values)
            {
                control.UserPause();
                ReportCurrentStatus(control);
            }
        }

        public void ResumeAll()
        {
            foreach (var control in _controls.Values)
            {
                control.UserResume();
                ReportCurrentStatus(control);
            }
        }

        public void StopAll()
        {
            foreach (var control in _controls.Values)
            {
                control.Stop();
                ReportStatus(control.Job, BackupRuntimeStatus.Stopped);
            }
        }

        public BackupRuntimeStatus GetStatus(int jobId)
        {
            return _controls.TryGetValue(jobId, out var control)
                ? control.Status
                : BackupRuntimeStatus.Idle;
        }

        public void UpdateBusinessSoftware(string rawNames)
        {
            _businessWatcher.UpdateSoftwareNames(ParseList(rawNames));
            if (_businessWatcher.GetRunningBusinessSoftware() is null)
                ResumeAllFromBusiness();
        }

        /// <summary>Updates the logger format and destination at runtime (called when Settings are saved).</summary>
        public void UpdateLogFormat(string format)
        {
            string dir = Path.Combine(Path.GetDirectoryName(_stateFilePath)!, "Logs");
            LogFormat logFormat = format == "XML" ? LogFormat.Xml : LogFormat.Json;
            AppSettings settings = _settingsService.Load();
            var destination = Enum.TryParse<EasyLog.Models.LogDestination>(settings.LogDestination, true, out var dest)
                ? dest : EasyLog.Models.LogDestination.Local;
            _logger = new Logger(dir, logFormat, destination, settings.DockerLogUrl ?? string.Empty);
        }

        private void ProcessFile(
            BackupJob job,
            string sourceFile,
            BackupStateEntry state,
            AppSettings settings,
            HashSet<string> priorityExtensions,
            long largeFileLimitBytes,
            int totalFiles,
            long totalSize,
            ref int processed,
            ref long processedSize,
            JobControl control,
            CancellationToken ct)
        {
            bool isPriority = priorityExtensions.Contains(Path.GetExtension(sourceFile).ToLowerInvariant());
            bool priorityRegistered = isPriority;
            bool largeFileSlotTaken = false;

            try
            {
                control.WaitIfPaused(ct);

                if (!isPriority)
                {
                    while (_coordinator.HasPendingPriorityFiles())
                    {
                        control.WaitIfPaused(ct);
                        ct.ThrowIfCancellationRequested();
                        Thread.Sleep(100);
                    }
                }

                HandleBusinessSoftwareIfRunning(job, control, ct);

                string relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                string targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var info = new FileInfo(sourceFile);

                if (job.Type == BackupType.Differential && File.Exists(targetFile))
                {
                    if (info.LastWriteTime <= new FileInfo(targetFile).LastWriteTime)
                    {
                        UpdateProgress(job, state, totalFiles, totalSize, ref processed, ref processedSize, info.Length, control.Status);
                        return;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                SetCurrentFile(state, sourceFile, targetFile);

                bool isLargeFile = largeFileLimitBytes > 0 && info.Length > largeFileLimitBytes;
                if (isLargeFile)
                {
                    _coordinator.WaitForLargeFileSlot(ct);
                    largeFileSlotTaken = true;
                }

                long transferTimeMs = 0;
                long encryptionTimeMs = 0;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    File.Copy(sourceFile, targetFile, overwrite: true);
                    stopwatch.Stop();
                    transferTimeMs = stopwatch.ElapsedMilliseconds;

                    _coordinator.WaitForCryptoSlot(ct);
                    try
                    {
                        encryptionTimeMs = _cryptoSoftService.EncryptIfRequired(targetFile, settings.CryptoExtensions);
                    }
                    finally
                    {
                        _coordinator.ReleaseCryptoSlot();
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    stopwatch.Stop();
                    transferTimeMs = -stopwatch.ElapsedMilliseconds;
                    lock (_stateLock) { state.Error = ex.Message; }
                }

                _logger.WriteLog(job.Name, sourceFile, targetFile, info.Length, transferTimeMs, encryptionTimeMs);
                UpdateProgress(job, state, totalFiles, totalSize, ref processed, ref processedSize, info.Length, control.Status);
            }
            finally
            {
                if (largeFileSlotTaken)
                    _coordinator.ReleaseLargeFileSlot();

                if (priorityRegistered)
                    _coordinator.DecrementPendingPriority();
            }
        }

        private void HandleBusinessSoftwareIfRunning(BackupJob job, JobControl control, CancellationToken ct)
        {
            string? detected = _businessWatcher.GetRunningBusinessSoftware();
            if (detected is null)
                return;

            _logger.LogBusinessSoftwareDetected(job.Name, detected);
            PauseAllFromBusiness();
            control.WaitIfPaused(ct);
        }

        private void OnBusinessSoftwareChanged(object? sender, BusinessSoftwareChangedEventArgs e)
        {
            if (e.IsRunning)
                PauseAllFromBusiness();
            else
                ResumeAllFromBusiness();
        }

        private void PauseAllFromBusiness()
        {
            foreach (var control in _controls.Values)
            {
                control.BusinessPause();
                ReportCurrentStatus(control);
            }
        }

        private void ResumeAllFromBusiness()
        {
            foreach (var control in _controls.Values)
            {
                control.BusinessResume();
                ReportCurrentStatus(control);
            }
        }

        private void InitializeState(BackupStateEntry state, int totalFiles, long totalSize)
        {
            lock (_stateLock)
            {
                state.State = "Running";
                state.TotalFiles = totalFiles;
                state.TotalSize = totalSize;
                state.RemainingFiles = totalFiles;
                state.RemainingSize = totalSize;
                state.Progress = 0;
                state.CurrentSourceFile = string.Empty;
                state.CurrentTargetFile = string.Empty;
                state.Error = string.Empty;
                state.LastActionTimestamp = Timestamp();
                WriteAllStates();
            }
        }

        private void SetCurrentFile(BackupStateEntry state, string sourceFile, string targetFile)
        {
            lock (_stateLock)
            {
                state.CurrentSourceFile = sourceFile;
                state.CurrentTargetFile = targetFile;
                state.LastActionTimestamp = Timestamp();
                WriteAllStates();
            }
        }

        private void UpdateProgress(
            BackupJob job,
            BackupStateEntry state,
            int totalFiles,
            long totalSize,
            ref int processed,
            ref long processedSize,
            long fileSize,
            BackupRuntimeStatus status)
        {
            double progress;
            lock (_stateLock)
            {
                processed++;
                processedSize += fileSize;
                state.RemainingFiles = totalFiles - processed;
                state.RemainingSize = totalSize - processedSize;
                state.Progress = totalFiles > 0 ? (double)processed / totalFiles * 100 : 100;
                state.State = status.ToString();
                state.LastActionTimestamp = Timestamp();
                state.Error = string.Empty;
                progress = state.Progress;
                WriteAllStates();
            }

            ProgressChanged?.Invoke(this, new BackupProgressChangedEventArgs(job.Id, job.Name, progress, status));
        }

        private void MarkFinished(BackupJob job, BackupStateEntry state)
        {
            lock (_stateLock)
            {
                state.State = "Finished";
                state.Progress = 100;
                state.RemainingFiles = 0;
                state.RemainingSize = 0;
                state.CurrentSourceFile = string.Empty;
                state.CurrentTargetFile = string.Empty;
                state.LastActionTimestamp = Timestamp();
                WriteAllStates();
            }

            ReportStatus(job, BackupRuntimeStatus.Finished);
            ProgressChanged?.Invoke(this, new BackupProgressChangedEventArgs(job.Id, job.Name, 100, BackupRuntimeStatus.Finished));
        }

        private void MarkStopped(BackupJob job, BackupStateEntry state)
        {
            lock (_stateLock)
            {
                state.State = "Stopped";
                state.CurrentSourceFile = string.Empty;
                state.CurrentTargetFile = string.Empty;
                state.LastActionTimestamp = Timestamp();
                WriteAllStates();
            }

            ReportStatus(job, BackupRuntimeStatus.Stopped);
        }

        private void ReportStatus(BackupJob job, BackupRuntimeStatus status)
        {
            JobControl control = GetControl(job);
            control.Status = status;
            StatusChanged?.Invoke(this, new BackupStatusChangedEventArgs(job.Id, job.Name, status));
        }

        private void ReportCurrentStatus(JobControl control)
        {
            StatusChanged?.Invoke(this, new BackupStatusChangedEventArgs(
                control.Job.Id,
                control.Job.Name,
                control.Status));
        }

        private void WriteAllStates()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
                File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(_allStates, JsonOptions));
            }
            catch { }
        }

        private BackupStateEntry GetOrCreateState(string jobName)
        {
            lock (_stateLock)
            {
                var entry = _allStates.FirstOrDefault(s => s.BackupName == jobName);
                if (entry != null) return entry;
                entry = new BackupStateEntry { BackupName = jobName, State = "Inactive" };
                _allStates.Add(entry);
                return entry;
            }
        }

        private JobControl GetControl(BackupJob job)
        {
            return _controls.AddOrUpdate(
                job.Id,
                _ => new JobControl(job),
                (_, existing) =>
                {
                    existing.Job = job;
                    return existing;
                });
        }

        private static HashSet<string> ParseExtensions(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new HashSet<string>();
            return raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .Select(e => e.Trim().ToLowerInvariant())
                      .ToHashSet();
        }

        private static List<string> ParseList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        private static string Timestamp() =>
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        private sealed class JobControl
        {
            private bool _userPaused;
            private bool _businessPaused;

            public JobControl(BackupJob job)
            {
                Job = job;
                PauseGate.Set();
            }

            public object Sync { get; } = new();
            public BackupJob Job { get; set; }
            public ManualResetEventSlim PauseGate { get; } = new(false);
            public CancellationTokenSource Cancellation { get; private set; } = new();
            public Task? RunTask { get; set; }
            public BackupRuntimeStatus Status { get; set; } = BackupRuntimeStatus.Idle;
            public bool IsStopRequested => Cancellation.IsCancellationRequested;

            public void BeginRun()
            {
                lock (Sync)
                {
                    Cancellation.Dispose();
                    Cancellation = new CancellationTokenSource();
                    _userPaused = false;
                    _businessPaused = false;
                    Status = BackupRuntimeStatus.Running;
                    PauseGate.Set();
                }
            }

            public void EndRun()
            {
                lock (Sync)
                {
                    _userPaused = false;
                    _businessPaused = false;
                    PauseGate.Set();
                }
            }

            public void UserPause()
            {
                lock (Sync)
                {
                    if (!CanAcceptRuntimeControl())
                        return;

                    _userPaused = true;
                    Status = BackupRuntimeStatus.Paused;
                    PauseGate.Reset();
                }
            }

            public void UserResume()
            {
                lock (Sync)
                {
                    if (!CanAcceptRuntimeControl())
                        return;

                    _userPaused = false;
                    if (!_businessPaused)
                    {
                        Status = BackupRuntimeStatus.Running;
                        PauseGate.Set();
                    }
                }
            }

            public void BusinessPause()
            {
                lock (Sync)
                {
                    if (!CanAcceptRuntimeControl())
                        return;

                    _businessPaused = true;
                    Status = BackupRuntimeStatus.Paused;
                    PauseGate.Reset();
                }
            }

            public void BusinessResume()
            {
                lock (Sync)
                {
                    if (!CanAcceptRuntimeControl())
                        return;

                    _businessPaused = false;
                    if (!_userPaused)
                    {
                        Status = BackupRuntimeStatus.Running;
                        PauseGate.Set();
                    }
                }
            }

            public void Stop()
            {
                lock (Sync)
                {
                    if (!CanAcceptRuntimeControl())
                        return;

                    Status = BackupRuntimeStatus.Stopped;
                    Cancellation.Cancel();
                    PauseGate.Set();
                }
            }

            public void WaitIfPaused(CancellationToken ct)
            {
                while (!PauseGate.Wait(100, ct))
                {
                }
            }

            private bool CanAcceptRuntimeControl() =>
                Status is BackupRuntimeStatus.Running or BackupRuntimeStatus.Paused;
        }
    }
}
