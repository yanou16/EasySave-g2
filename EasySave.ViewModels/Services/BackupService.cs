using System.Diagnostics;
using System.Text.Json;
using EasyLog;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Models;

namespace EasySave.ViewModels.Services
{
    /// <summary>
    /// Handles file copying in parallel with priority file management,
    /// large file mutual exclusion, real-time state tracking and logging.
    /// v3.0 — full parallel rewrite using ParallelCoordinator.
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
        }

        /// <summary>
        /// Executes multiple jobs in parallel (v3.0).
        /// </summary>
        public void ExecuteAll(IEnumerable<BackupJob> jobs, CancellationToken ct = default)
        {
            var tasks = jobs.Select(job => Task.Run(() => Execute(job, ct), ct)).ToArray();
            Task.WaitAll(tasks, ct);
        }

        /// <summary>
        /// Executes a single backup job with parallel file copying,
        /// priority file management and large file mutual exclusion.
        /// </summary>
        public void Execute(BackupJob job, CancellationToken ct = default)
        {
            // Block if business software is running
            var detectedAtLaunch = _businessWatcher.GetRunningBusinessSoftware();
            if (detectedAtLaunch != null)
            {
                _logger.LogBusinessSoftwareDetected(job.Name, detectedAtLaunch);
                throw new BusinessSoftwareDetectedException(detectedAtLaunch);
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

            // Register priority files with the coordinator
            var priorityFiles = allFiles
                .Where(f => priorityExtensions.Contains(
                    Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            foreach (var _ in priorityFiles)
                _coordinator.IncrementPendingPriority();

            // Initialize state
            BackupStateEntry state = GetOrCreateState(job.Name);
            lock (_stateLock)
            {
                state.State = "Active";
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

            int processed = 0;
            long processedSize = 0;

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(allFiles, parallelOptions, sourceFile =>
            {
                ct.ThrowIfCancellationRequested();

                bool isPriority = priorityExtensions.Contains(
                    Path.GetExtension(sourceFile).ToLowerInvariant());

                // Non-priority files wait until no priority files are pending
                if (!isPriority)
                {
                    while (_coordinator.HasPendingPriorityFiles() && !ct.IsCancellationRequested)
                        Thread.Sleep(100);
                }

                ct.ThrowIfCancellationRequested();

                // Check business software during execution
                var detected = _businessWatcher.GetRunningBusinessSoftware();
                if (detected != null)
                {
                    _logger.LogBusinessSoftwareDetected(job.Name, detected);
                    throw new BusinessSoftwareDetectedException(detected);
                }

                string relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                string targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var info = new FileInfo(sourceFile);

                // Skip unchanged files in differential mode
                if (job.Type == BackupType.Differential && File.Exists(targetFile))
                {
                    if (info.LastWriteTime <= new FileInfo(targetFile).LastWriteTime)
                    {
                        if (isPriority) _coordinator.DecrementPendingPriority();
                        lock (_stateLock)
                        {
                            processed++;
                            processedSize += info.Length;
                        }
                        return;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

                lock (_stateLock)
                {
                    state.CurrentSourceFile = sourceFile;
                    state.CurrentTargetFile = targetFile;
                    state.LastActionTimestamp = Timestamp();
                    WriteAllStates();
                }

                long transferTimeMs = 0;
                long encryptionTimeMs = 0;
                bool isLargeFile = largeFileLimitBytes > 0 && info.Length > largeFileLimitBytes;

                // Acquire large file slot if needed
                if (isLargeFile) _coordinator.WaitForLargeFileSlot(ct);
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        File.Copy(sourceFile, targetFile, overwrite: true);
                        stopwatch.Stop();
                        transferTimeMs = stopwatch.ElapsedMilliseconds;

                        // CryptoSoft mono-instance — one encryption at a time
                        _coordinator.WaitForCryptoSlot(ct);
                        try
                        {
                            encryptionTimeMs = _cryptoSoftService.EncryptIfRequired(
                                targetFile, settings.CryptoExtensions);
                        }
                        finally
                        {
                            _coordinator.ReleaseCryptoSlot();
                        }
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        transferTimeMs = -stopwatch.ElapsedMilliseconds;
                        lock (_stateLock) { state.Error = ex.Message; }
                    }
                }
                finally
                {
                    if (isLargeFile) _coordinator.ReleaseLargeFileSlot();
                }

                // Decrement priority counter after processing
                if (isPriority) _coordinator.DecrementPendingPriority();

                _logger.WriteLog(
                    job.Name, sourceFile, targetFile,
                    info.Length, transferTimeMs, encryptionTimeMs);

                lock (_stateLock)
                {
                    processed++;
                    processedSize += info.Length;
                    state.RemainingFiles = totalFiles - processed;
                    state.RemainingSize = totalSize - processedSize;
                    state.Progress = totalFiles > 0 ? (double)processed / totalFiles * 100 : 100;
                    state.LastActionTimestamp = Timestamp();
                    state.Error = string.Empty;
                    WriteAllStates();
                }
            });

            lock (_stateLock)
            {
                state.State = "Inactive";
                state.Progress = 100;
                state.RemainingFiles = 0;
                state.RemainingSize = 0;
                state.CurrentSourceFile = string.Empty;
                state.CurrentTargetFile = string.Empty;
                state.LastActionTimestamp = Timestamp();
                WriteAllStates();
            }
        }

        /// <summary>Updates the logger format at runtime.</summary>
        public void UpdateLogFormat(string format)
        {
            string dir = Path.Combine(Path.GetDirectoryName(_stateFilePath)!, "Logs");
            LogFormat logFormat = format == "XML" ? LogFormat.Xml : LogFormat.Json;
            _logger = new Logger(dir, logFormat);
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
            var entry = _allStates.FirstOrDefault(s => s.BackupName == jobName);
            if (entry != null) return entry;
            entry = new BackupStateEntry { BackupName = jobName, State = "Inactive" };
            _allStates.Add(entry);
            return entry;
        }

        private static HashSet<string> ParseExtensions(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new HashSet<string>();
            return raw.Split(';', StringSplitOptions.RemoveEmptyEntries)
                      .Select(e => e.Trim().ToLowerInvariant())
                      .ToHashSet();
        }

        private static string Timestamp() =>
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}