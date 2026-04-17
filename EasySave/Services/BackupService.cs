using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EasyLog;
using EasyLog.Models;
using EasySave.Models;

namespace EasySave.Services
{
    public class BackupService
    {
        private readonly Logger _logger;
        private readonly StateManager _stateManager;
        private readonly List<BackupStateEntry> _states;

        public BackupService(Logger logger, StateManager stateManager, List<BackupStateEntry> states)
        {
            _logger = logger;
            _stateManager = stateManager;
            _states = states;
        }

        public void Execute(BackupJob job)
        {
            if (!Directory.Exists(job.SourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            Directory.CreateDirectory(job.TargetDirectory);

            string[] files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            int totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            BackupStateEntry state = GetOrCreateStateEntry(job.Name);
            state.TotalFiles = totalFiles;
            state.TotalSize = totalSize;
            state.RemainingFiles = totalFiles;
            state.RemainingSize = totalSize;
            state.Progress = 0;
            state.State = "Active";
            state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _stateManager.UpdateState(_states);

            int processed = 0;
            long processedSize = 0;

            foreach (string sourceFile in files)
            {
                string relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                string destFile = Path.Combine(job.TargetDirectory, relativePath);
                var fileInfo = new FileInfo(sourceFile);
                long fileSize = fileInfo.Length;

                if (job.Type == BackupType.Differential && File.Exists(destFile))
                {
                    if (fileInfo.LastWriteTime <= new FileInfo(destFile).LastWriteTime)
                    {
                        processed++;
                        processedSize += fileSize;
                        continue;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                state.CurrentSourceFile = sourceFile;
                state.CurrentDestFile = destFile;
                state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _stateManager.UpdateState(_states);

                long transferTimeMs = 0;
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    File.Copy(sourceFile, destFile, true);
                    stopwatch.Stop();
                    transferTimeMs = stopwatch.ElapsedMilliseconds;
                }
                catch (Exception)
                {
                    stopwatch.Stop();
                    transferTimeMs = -stopwatch.ElapsedMilliseconds;
                }

                _logger.Log(new LogEntry
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    BackupName = job.Name,
                    SourcePath = sourceFile,
                    DestinationPath = destFile,
                    FileSize = fileSize,
                    TransferTimeMs = transferTimeMs
                });

                processed++;
                processedSize += fileSize;
                state.RemainingFiles = totalFiles - processed;
                state.RemainingSize = totalSize - processedSize;
                state.Progress = totalFiles > 0 ? processed * 100 / totalFiles : 100;
                state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _stateManager.UpdateState(_states);
            }

            state.State = "Inactive";
            state.Progress = 100;
            state.RemainingFiles = 0;
            state.RemainingSize = 0;
            state.CurrentSourceFile = string.Empty;
            state.CurrentDestFile = string.Empty;
            state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _stateManager.UpdateState(_states);
        }

        private BackupStateEntry GetOrCreateStateEntry(string name)
        {
            var entry = _states.FirstOrDefault(s => s.Name == name);
            if (entry != null) return entry;

            entry = new BackupStateEntry { Name = name };
            _states.Add(entry);
            return entry;
        }
    }
}
