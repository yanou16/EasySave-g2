using System.Diagnostics;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Models;

namespace EasySave.ViewModels.Services
{
    public class BackupService
    {
        private readonly Logger _logger;
        private readonly StateManager _stateManager;

        public BackupService(Logger logger, StateManager stateManager)
        {
            _logger = logger;
            _stateManager = stateManager;
        }

        public void Execute(BackupJob job)
        {
            if (!Directory.Exists(job.SourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            Directory.CreateDirectory(job.TargetDirectory);

            string[] files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            int totalFiles = files.Length;
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            var state = new BackupStateEntry
            {
                BackupName = job.Name,
                State = "Active",
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                RemainingFiles = totalFiles,
                RemainingSize = totalSize,
                Progress = 0,
                LastActionTimestamp = Timestamp()
            };
            _stateManager.WriteState(state);

            int processed = 0;
            long processedSize = 0;

            foreach (string sourceFile in files)
            {
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
                _stateManager.WriteState(state);

                long transferTimeMs = 0;
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    File.Copy(sourceFile, targetFile, overwrite: true);
                    stopwatch.Stop();
                    transferTimeMs = stopwatch.ElapsedMilliseconds;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    transferTimeMs = -stopwatch.ElapsedMilliseconds;
                    state.Error = ex.Message;
                }

                // Adapted to EasyLog's actual API (Rayan Lowst)
                _logger.WriteLog(job.Name, sourceFile, targetFile, info.Length, transferTimeMs);

                processed++;
                processedSize += info.Length;
                state.RemainingFiles = totalFiles - processed;
                state.RemainingSize = totalSize - processedSize;
                state.Progress = totalFiles > 0 ? (double)processed / totalFiles * 100 : 100;
                state.LastActionTimestamp = Timestamp();
                state.Error = string.Empty;
                _stateManager.WriteState(state);
            }

            state.State = "Inactive";
            state.Progress = 100;
            state.RemainingFiles = 0;
            state.RemainingSize = 0;
            state.CurrentSourceFile = string.Empty;
            state.CurrentTargetFile = string.Empty;
            state.LastActionTimestamp = Timestamp();
            _stateManager.WriteState(state);
        }

        private static string Timestamp() =>
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
