using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasyLog.Models;

namespace EasyLog
{
    public class Logger
    {
        private readonly string _logDirectory;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public Logger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public void Log(LogEntry entry)
        {
            string filePath = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");

            List<LogEntry> entries = new();

            if (File.Exists(filePath))
            {
                try
                {
                    entries = JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(filePath)) ?? new List<LogEntry>();
                }
                catch
                {
                    entries = new List<LogEntry>();
                }
            }

            entries.Add(entry);
            File.WriteAllText(filePath, JsonSerializer.Serialize(entries, JsonOptions));
        }
    }
}
