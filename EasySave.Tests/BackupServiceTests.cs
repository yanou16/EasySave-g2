using EasyLog.Services;
using EasySave.Models;
using EasySave.ViewModels.Services;
using Xunit;

namespace EasySave.Tests
{
    /// <summary>
    /// Integration tests for BackupService – real file I/O in temp directories.
    /// Tests verify actual file copy behaviour, differential logic, and error handling.
    /// </summary>
    public class BackupServiceTests : IDisposable
    {
        private readonly string _root;
        private readonly string _src;
        private readonly string _dst;
        private readonly string _logs;
        private readonly BackupService _svc;

        public BackupServiceTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "EasySaveServiceTests_" + Guid.NewGuid());
            _src  = Path.Combine(_root, "source");
            _dst  = Path.Combine(_root, "target");
            _logs = Path.Combine(_root, "logs");

            Directory.CreateDirectory(_src);
            Directory.CreateDirectory(_logs);

            var settingsService = new SettingsService(_root);
            var allStates       = new List<EasyLog.Models.BackupStateEntry>();
            var logger          = new Logger(_logs, EasyLog.LogFormat.Json);
            var crypto          = new CryptoSoftService(_root); // crypto disabled (no extensions set)
            var watcher         = new BusinessSoftwareWatcher(new List<string>()); // disabled
            var coordinator     = new ParallelCoordinator();

            _svc = new BackupService(logger, Path.Combine(_root, "state.json"),
                                     allStates, settingsService, crypto, watcher, coordinator);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); } catch { }
        }

        private BackupJob MakeJob(BackupType type = BackupType.Full) =>
            new BackupJob { Id = 1, Name = "Test", SourceDirectory = _src, TargetDirectory = _dst, Type = type };

        // ── Full backup ───────────────────────────────────────────────────────

        [Fact]
        public void Execute_FullBackup_CopiesAllFiles()
        {
            File.WriteAllText(Path.Combine(_src, "a.txt"), "hello");
            File.WriteAllText(Path.Combine(_src, "b.txt"), "world");

            _svc.Execute(MakeJob(BackupType.Full));

            Assert.True(File.Exists(Path.Combine(_dst, "a.txt")));
            Assert.True(File.Exists(Path.Combine(_dst, "b.txt")));
        }

        [Fact]
        public void Execute_FullBackup_PreservesSubdirectories()
        {
            string sub = Path.Combine(_src, "subdir");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "nested.txt"), "data");

            _svc.Execute(MakeJob(BackupType.Full));

            Assert.True(File.Exists(Path.Combine(_dst, "subdir", "nested.txt")));
        }

        [Fact]
        public void Execute_FullBackup_OverwritesExistingTarget()
        {
            File.WriteAllText(Path.Combine(_src, "file.txt"), "new content");
            Directory.CreateDirectory(_dst);
            File.WriteAllText(Path.Combine(_dst, "file.txt"), "old content");

            _svc.Execute(MakeJob(BackupType.Full));

            Assert.Equal("new content", File.ReadAllText(Path.Combine(_dst, "file.txt")));
        }

        [Fact]
        public void Execute_EmptySource_CompletesWithoutError()
        {
            // Empty directory → no files to copy, should succeed silently.
            _svc.Execute(MakeJob(BackupType.Full));
            Assert.True(Directory.Exists(_dst));
        }

        [Fact]
        public void Execute_SourceNotFound_ThrowsDirectoryNotFoundException()
        {
            var job = new BackupJob
            {
                Id = 1, Name = "Bad", Type = BackupType.Full,
                SourceDirectory = @"C:\this_path_does_not_exist_xyz",
                TargetDirectory = _dst
            };

            Assert.Throws<DirectoryNotFoundException>(() => _svc.Execute(job));
        }

        [Fact]
        public void Execute_CreatesTargetDirectory_IfNotExists()
        {
            File.WriteAllText(Path.Combine(_src, "x.txt"), "x");
            Assert.False(Directory.Exists(_dst));

            _svc.Execute(MakeJob());

            Assert.True(Directory.Exists(_dst));
        }

        [Fact]
        public void Execute_WritesStateJson()
        {
            File.WriteAllText(Path.Combine(_src, "z.txt"), "z");
            _svc.Execute(MakeJob());

            Assert.True(File.Exists(Path.Combine(_root, "state.json")));
        }

        // ── Differential backup ───────────────────────────────────────────────

        [Fact]
        public void Execute_Differential_CopiesNewFiles()
        {
            File.WriteAllText(Path.Combine(_src, "new.txt"), "new");

            _svc.Execute(MakeJob(BackupType.Differential));

            Assert.True(File.Exists(Path.Combine(_dst, "new.txt")));
        }

        [Fact]
        public void Execute_Differential_SkipsUnchangedFiles()
        {
            // Copy once (full), then verify differential skips the unchanged file.
            File.WriteAllText(Path.Combine(_src, "same.txt"), "content");
            _svc.Execute(MakeJob(BackupType.Full));

            // Touch the source BEFORE the target → not newer → skip.
            string srcFile = Path.Combine(_src, "same.txt");
            string dstFile = Path.Combine(_dst, "same.txt");
            File.SetLastWriteTime(srcFile, DateTime.Now.AddMinutes(-10));
            File.SetLastWriteTime(dstFile, DateTime.Now); // target is newer

            // Replace target content to detect if it gets overwritten.
            File.WriteAllText(dstFile, "TARGET_CONTENT");

            _svc.Execute(MakeJob(BackupType.Differential));

            Assert.Equal("TARGET_CONTENT", File.ReadAllText(dstFile)); // not overwritten
        }

        [Fact]
        public void Execute_Differential_UpdatesModifiedFiles()
        {
            File.WriteAllText(Path.Combine(_src, "mod.txt"), "v1");
            _svc.Execute(MakeJob(BackupType.Full));

            // Update source to be newer than target.
            string srcFile = Path.Combine(_src, "mod.txt");
            File.WriteAllText(srcFile, "v2");
            File.SetLastWriteTime(srcFile, DateTime.Now.AddMinutes(5));

            _svc.Execute(MakeJob(BackupType.Differential));

            Assert.Equal("v2", File.ReadAllText(Path.Combine(_dst, "mod.txt")));
        }

        // ── Log creation ──────────────────────────────────────────────────────

        [Fact]
        public void Execute_CreatesLogFile()
        {
            File.WriteAllText(Path.Combine(_src, "log.txt"), "data");
            _svc.Execute(MakeJob());

            string[] logFiles = Directory.GetFiles(_logs);
            Assert.NotEmpty(logFiles);
        }
    }
}
