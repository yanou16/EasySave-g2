using System.Text.Json;
using EasyLog.Models;

namespace EasyLog.Services
{
    /// <summary>
    /// Writes file transfer events to a daily JSON log file with chained SHA-256 integrity hashing.
    /// One log file is created per calendar day under the configured directory.
    /// This class is the primary public API of the EasyLog library.
    /// Compatible with all EasySave versions (v1.0 and above).
    /// </summary>
    public class Logger
    {
        private readonly string _logDirectory;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Initialises the logger and ensures the log directory exists.
        /// </summary>
        /// <param name="logDirectory">
        /// Absolute path of the directory where daily log files are stored.
        /// Recommended: a sub-folder of the application's LocalApplicationData root.
        /// Paths such as "C:\temp" must not be used in production deployments.
        /// </param>
        public Logger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// Appends a file transfer event to today's JSON log file.
        /// The entry is chained to the previous one using SHA-256 so the log
        /// can be verified for tampering by <see cref="LogIntegrityVerifier"/>.
        /// </summary>
        /// <param name="backupName">Name of the backup job that triggered the transfer.</param>
        /// <param name="sourcePath">Absolute source path (normalised to UNC format).</param>
        /// <param name="targetPath">Absolute destination path (normalised to UNC format).</param>
        /// <param name="fileSize">File size in bytes.</param>
        /// <param name="transferTimeMs">
        /// Transfer duration in milliseconds.
        /// Pass a negative value when the transfer failed (absolute value = time before failure).
        /// </param>
        public void WriteLog(
            string backupName,
            string sourcePath,
            string targetPath,
            long fileSize,
            long transferTimeMs)
        {
            sourcePath = SecurityHelper.NormalizePath(sourcePath);
            targetPath = SecurityHelper.NormalizePath(targetPath);

            string logFilePath = Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.json");

            List<LogEntry> logs = LoadExistingLogs(logFilePath);

            string previousHash = logs.Count > 0 ? logs[^1].Hash : "GENESIS";
            string timestamp    = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            var entry = new LogEntry
            {
                Timestamp     = timestamp,
                BackupName    = backupName,
                SourcePath    = sourcePath,
                TargetPath    = targetPath,
                FileSize      = fileSize,
                TransferTimeMs = transferTimeMs,
                PreviousHash  = previousHash
            };

            entry.Hash = SecurityHelper.BuildLogSignature(
                entry.Timestamp,
                entry.BackupName,
                entry.SourcePath,
                entry.TargetPath,
                entry.FileSize,
                entry.TransferTimeMs,
                entry.PreviousHash);

            logs.Add(entry);
            File.WriteAllText(logFilePath, JsonSerializer.Serialize(logs, JsonOptions));
        }

        /// <summary>Reads the existing log entries for today, or returns an empty list on failure.</summary>
        private static List<LogEntry> LoadExistingLogs(string logFilePath)
        {
            if (!File.Exists(logFilePath))
                return new List<LogEntry>();

            try
            {
                string json = File.ReadAllText(logFilePath);
                return string.IsNullOrWhiteSpace(json)
                    ? new List<LogEntry>()
                    : JsonSerializer.Deserialize<List<LogEntry>>(json) ?? new List<LogEntry>();
            }
            catch
            {
                return new List<LogEntry>();
            }
        }
    }
}
