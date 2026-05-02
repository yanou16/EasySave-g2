using System.Diagnostics;
using System.Text.Json;
using EasyLog;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Models;

namespace EasySave.ViewModels.Services
{
    /// <summary>
    /// Handles file copying, real-time state tracking and logging for a single backup job.
    /// Uses EasyLog.Logger for daily log files.
    /// Writes all jobs states to a single state.json (spec requirement).
    /// </summary>
    public class BackupService
    {
        private Logger _logger;
        private readonly string _stateFilePath;
        private readonly List<BackupStateEntry> _allStates;
        private readonly SettingsService _settingsService;
        private readonly CryptoSoftService _cryptoSoftService;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private readonly BusinessSoftwareWatcher _businessWatcher;

        public BackupService(
            Logger logger,
            string stateFilePath,
            List<BackupStateEntry> allStates,
            SettingsService settingsService,
            CryptoSoftService cryptoSoftService,
            BusinessSoftwareWatcher businessWatcher)
        {
            _logger = logger;
            _stateFilePath = stateFilePath;
            _allStates = allStates;
            _settingsService = settingsService;
            _cryptoSoftService = cryptoSoftService;
            _businessWatcher = businessWatcher;
        }

        public void Execute(BackupJob job)
        {
            if (!Directory.Exists(job.SourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            Directory.CreateDirectory(job.TargetDirectory);

            string[] files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            int totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            BackupStateEntry state = GetOrCreateState(job.Name);
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

            int processed = 0;
            long processedSize = 0;
            AppSettings settings = _settingsService.Load();

            foreach (string sourceFile in files)
            {
                var detected = _businessWatcher.GetRunningBusinessSoftware();

                if (detected != null)
                {
                    _logger.LogBusinessSoftwareDetected(job.Name, detected);
                    throw new BusinessSoftwareDetectedException(detected);
                }
                string relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                string targetFile = Path.Combine(job.TargetDirectory, relativePath);
                var info = new FileInfo(sourceFile);

                if (job.Type == BackupType.Differential && File.Exists(targetFile))
                {
                    if (info.LastWriteTime <= new FileInfo(targetFile).LastWriteTime)
                    {
                        processed++;
                        processedSize += info.Length;
                        continue;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

                state.CurrentSourceFile = sourceFile;
                state.CurrentTargetFile = targetFile;
                state.LastActionTimestamp = Timestamp();
                WriteAllStates();

                long transferTimeMs = 0;
                long encryptionTimeMs = 0;
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    File.Copy(sourceFile, targetFile, overwrite: true);
                    stopwatch.Stop();
                    transferTimeMs = stopwatch.ElapsedMilliseconds;

                    // Encrypt only after a successful copy, so the source file is never modified.
                    encryptionTimeMs = _cryptoSoftService.EncryptIfRequired(targetFile, settings.CryptoExtensions);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    transferTimeMs = -stopwatch.ElapsedMilliseconds;
                    state.Error = ex.Message;
                }

                _logger.WriteLog(job.Name, sourceFile, targetFile, info.Length, transferTimeMs, encryptionTimeMs);

                processed++;
                processedSize += info.Length;
                state.RemainingFiles = totalFiles - processed;
                state.RemainingSize = totalSize - processedSize;
                state.Progress = totalFiles > 0 ? (double)processed / totalFiles * 100 : 100;
                state.LastActionTimestamp = Timestamp();
                state.Error = string.Empty;
                WriteAllStates();
            }

            state.State = "Inactive";
            state.Progress = 100;
            state.RemainingFiles = 0;
            state.RemainingSize = 0;
            state.CurrentSourceFile = string.Empty;
            state.CurrentTargetFile = string.Empty;
            state.LastActionTimestamp = Timestamp();
            WriteAllStates();
        }

        /// <summary>
        /// Updates the logger format at runtime (called when user changes log format in settings).
        /// </summary>
        public void UpdateLogFormat(string format)
        {
            string dir = Path.Combine(Path.GetDirectoryName(_stateFilePath)!, "Logs");
            LogFormat logFormat = format == "XML" ? LogFormat.Xml : LogFormat.Json;
            _logger = new Logger(dir, logFormat);
        }

        // Writes ALL jobs states into one single state.json (spec: "fichier unique")
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

        private static string Timestamp() =>
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
