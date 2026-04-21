using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using EasyLog.Models;

namespace EasyLog.Services
{
    public class Logger
    {
        private readonly string _logDirectory;

        public Logger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public void WriteLog(string backupName, string sourcePath, string targetPath, long fileSize, long transferTimeMs)
        {
            sourcePath = SecurityHelper.NormalizePath(sourcePath);
            targetPath = SecurityHelper.NormalizePath(targetPath);

            string logFilePath = Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.json");

            List<LogEntry> logs = new();

            if (File.Exists(logFilePath))
            {
                try
                {
                    string json = File.ReadAllText(logFilePath);

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        logs = JsonSerializer.Deserialize<List<LogEntry>>(json) ?? new List<LogEntry>();
                    }
                }
                catch
                {
                    logs = new List<LogEntry>();
                }
            }

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string previousHash = logs.Count > 0 ? logs[^1].Hash : "GENESIS";

            var entry = new LogEntry
            {
                Timestamp = timestamp,
                BackupName = backupName,
                SourcePath = sourcePath,
                TargetPath = targetPath,
                FileSize = fileSize,
                TransferTimeMs = transferTimeMs,
                PreviousHash = previousHash
            };

            entry.Hash = SecurityHelper.BuildLogSignature(
                entry.Timestamp,
                entry.BackupName,
                entry.SourcePath,
                entry.TargetPath,
                entry.FileSize,
                entry.TransferTimeMs,
                entry.PreviousHash
            );

            logs.Add(entry);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            File.WriteAllText(logFilePath, JsonSerializer.Serialize(logs, options));
        }
    }
}
