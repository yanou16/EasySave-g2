using System.Text.Json;
using System.Xml.Linq;
using EasyLog.Models;

namespace EasyLog.Services
{
    /// <summary>
    /// Writes file transfer events to a daily log file (JSON or XML) with chained SHA-256 integrity hashing.
    /// Thread-safe for parallel backup jobs (v3.0).
    /// Compatible with all EasySave versions (v1.0 and above).
    /// </summary>
    public class Logger
    {
        private readonly string _logDirectory;
        private readonly LogFormat _format;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        // Thread-safety: one write at a time per logger instance
        private readonly object _fileLock = new();

        public Logger(string logDirectory) : this(logDirectory, LogFormat.Json) { }

        public Logger(string logDirectory, LogFormat format)
        {
            _logDirectory = logDirectory;
            _format = format;
            Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// Appends a file transfer event to today's log file.
        /// Thread-safe — can be called from multiple parallel jobs simultaneously.
        /// </summary>
        public void WriteLog(
            string backupName,
            string sourcePath,
            string targetPath,
            long fileSize,
            long transferTimeMs,
            long encryptionTimeMs = 0)
        {
            sourcePath = SecurityHelper.NormalizePath(sourcePath);
            targetPath = SecurityHelper.NormalizePath(targetPath);
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            lock (_fileLock)
            {
                if (_format == LogFormat.Xml)
                    WriteXml(backupName, sourcePath, targetPath, fileSize, transferTimeMs, encryptionTimeMs, timestamp);
                else
                    WriteJson(backupName, sourcePath, targetPath, fileSize, transferTimeMs, encryptionTimeMs, timestamp);
            }
        }

        private void WriteJson(
            string backupName,
            string sourcePath,
            string targetPath,
            long fileSize,
            long transferTimeMs,
            long encryptionTimeMs,
            string timestamp)
        {
            string logFilePath = Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.json");
            List<LogEntry> logs = LoadExistingLogs(logFilePath);

            string previousHash = logs.Count > 0 ? logs[^1].Hash : "GENESIS";

            var entry = new LogEntry
            {
                Timestamp = timestamp,
                BackupName = backupName,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                FileSize = fileSize,
                TransferTimeMs = transferTimeMs,
                EncryptionTimeMs = encryptionTimeMs,
                PreviousHash = previousHash
            };

            entry.Hash = SecurityHelper.BuildLogSignature(
                entry.Timestamp,
                entry.BackupName,
                entry.SourcePath,
                entry.TargetPath,
                entry.FileSize,
                entry.TransferTimeMs,
                entry.EncryptionTimeMs,
                entry.PreviousHash);

            logs.Add(entry);
            File.WriteAllText(logFilePath, JsonSerializer.Serialize(logs, JsonOptions));
        }

        private void WriteXml(
            string backupName,
            string sourcePath,
            string targetPath,
            long fileSize,
            long transferTimeMs,
            long encryptionTimeMs,
            string timestamp)
        {
            string logFilePath = Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.xml");

            XDocument doc;
            string previousHash;

            if (File.Exists(logFilePath))
            {
                doc = XDocument.Load(logFilePath);
                previousHash = doc.Root?
                    .Elements("LogEntry")
                    .LastOrDefault()
                    ?.Element("Hash")?.Value ?? "GENESIS";
            }
            else
            {
                doc = new XDocument(new XElement("Logs"));
                previousHash = "GENESIS";
            }

            string hash = SecurityHelper.BuildLogSignature(
                timestamp, backupName, sourcePath, targetPath,
                fileSize, transferTimeMs, encryptionTimeMs, previousHash);

            doc.Root!.Add(new XElement("LogEntry",
                new XElement("Timestamp", timestamp),
                new XElement("BackupName", backupName),
                new XElement("SourcePath", sourcePath),
                new XElement("TargetPath", targetPath),
                new XElement("FileSize", fileSize),
                new XElement("TransferTimeMs", transferTimeMs),
                new XElement("EncryptionTimeMs", encryptionTimeMs),
                new XElement("PreviousHash", previousHash),
                new XElement("Hash", hash)
            ));

            doc.Save(logFilePath);
        }

        private static List<LogEntry> LoadExistingLogs(string logFilePath)
        {
            if (!File.Exists(logFilePath)) return new List<LogEntry>();
            try
            {
                string json = File.ReadAllText(logFilePath);
                return string.IsNullOrWhiteSpace(json)
                    ? new List<LogEntry>()
                    : JsonSerializer.Deserialize<List<LogEntry>>(json) ?? new List<LogEntry>();
            }
            catch { return new List<LogEntry>(); }
        }

        public void LogBusinessSoftwareDetected(string backupName, string softwareName)
        {
            WriteLog(backupName, $"BLOCKED:{softwareName}", softwareName, 0, 0, 0);
        }
    }
}