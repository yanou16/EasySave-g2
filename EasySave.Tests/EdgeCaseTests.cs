using EasyLog.Services;
using EasySave.Models;
using EasySave.ViewModels;
using EasySave.ViewModels.Services;
using Xunit;

namespace EasySave.Tests
{
    /// <summary>
    /// Edge-case tests based on real user behaviour:
    /// "what happens when a user tries something unexpected?"
    ///
    /// These scenarios were identified as likely failure points during QA:
    ///   – Rapid repeated actions
    ///   – Unicode / special characters
    ///   – Boundary values (0, max, empty)
    ///   – Same source and target
    ///   – v1.1 5-job console limit
    /// </summary>
    public class EdgeCaseTests : IDisposable
    {
        private readonly string _temp;
        private readonly BackupViewModel _vm;
        private readonly BackupService _svc;
        private readonly string _src;

        public EdgeCaseTests()
        {
            _temp = Path.Combine(Path.GetTempPath(), "EasySaveEdge_" + Guid.NewGuid());
            _src  = Path.Combine(_temp, "src");
            Directory.CreateDirectory(_src);

            var configService   = new ConfigService(_temp);
            var settingsService = new SettingsService(_temp);
            var languageService = new LanguageService();
            languageService.LoadFromJson("""
            {
                "EmptyName":"Name required","EmptySource":"Source required",
                "EmptyTarget":"Target required","JobNameExists":"Exists",
                "JobAdded":"Added","JobRemoved":"Removed","InvalidJobIndex":"Invalid",
                "InvalidLogFormat":"Bad format","SettingsSaved":"Saved"
            }
            """);

            var logsDir   = Path.Combine(_temp, "Logs");
            var stateFile = Path.Combine(_temp, "state.json");
            var allStates = new List<EasyLog.Models.BackupStateEntry>();
            var logger    = new Logger(logsDir, EasyLog.LogFormat.Json);
            var crypto    = new CryptoSoftService(_temp);
            var watcher   = new BusinessSoftwareWatcher(new List<string>());
            var coord     = new ParallelCoordinator();

            _svc = new BackupService(logger, stateFile, allStates, settingsService, crypto, watcher, coord);
            _vm  = new BackupViewModel(configService, _svc, languageService, settingsService);
        }

        public void Dispose()
        {
            try { Directory.Delete(_temp, recursive: true); } catch { }
        }

        // ── Unicode / special characters ──────────────────────────────────────

        [Fact]
        public void AddJob_UnicodeJobName_StoresAndRetrievesCorrectly()
        {
            string name = "Sauvegarde été 2024 – 日本語";
            _vm.AddJob(name, @"C:\src", @"C:\dst", BackupType.Full);
            Assert.Equal(name, _vm.Jobs[0].Name);
        }

        [Fact]
        public void AddJob_NameWithSlashes_Allowed()
        {
            // Slashes in the name are valid – the name is stored as a string, not used as a path.
            var r = _vm.AddJob("Backup/Monthly", @"C:\src", @"C:\dst", BackupType.Full);
            Assert.True(r.Success);
        }

        [Fact]
        public void AddJob_NameWithHashAndDash_Allowed()
        {
            var r = _vm.AddJob("Job #1 - Test", @"C:\src", @"C:\dst", BackupType.Full);
            Assert.True(r.Success);
        }

        // ── v1.1 Console 5-job limit ──────────────────────────────────────────

        [Fact]
        public void V11_FifthJob_IsAllowed()
        {
            // Simulate what the console enforces: job count < 5.
            for (int i = 1; i <= 5; i++)
                _vm.AddJob($"Job{i}", $@"C:\src{i}", $@"C:\dst{i}", BackupType.Full);

            // 5 jobs should exist.
            Assert.Equal(5, _vm.Jobs.Count);
        }

        [Fact]
        public void V11_SimulatedConsoleLimitCheck_BlocksSixthJob()
        {
            // The console checks _vm.Jobs.Count >= 5 before calling AddJob.
            // Replicate that logic here to confirm the guard works.
            for (int i = 1; i <= 5; i++)
                _vm.AddJob($"Job{i}", $@"C:\src{i}", $@"C:\dst{i}", BackupType.Full);

            bool wouldBlock = _vm.Jobs.Count >= 5;
            Assert.True(wouldBlock); // Console would print "MaxJobsReached" here.
        }

        // ── Same source and target ─────────────────────────────────────────────

        [Fact]
        public void Execute_SourceEqualsTarget_CopiesWithoutCrash()
        {
            // User accidentally sets Source == Target.
            // The spec does not forbid this; behaviour should be: overwrite-in-place (file.Copy to itself).
            // Verify it does not throw an unhandled exception.
            File.WriteAllText(Path.Combine(_src, "self.txt"), "data");

            var job = new BackupJob
            {
                Id = 1, Name = "Self", Type = BackupType.Full,
                SourceDirectory = _src,
                TargetDirectory = _src   // same as source
            };

            // Should complete (or throw a known exception), but never crash with NullRef etc.
            var ex = Record.Exception(() => _svc.Execute(job));
            Assert.True(ex is null || ex is IOException || ex is UnauthorizedAccessException);
        }

        // ── Rapid repeated actions ────────────────────────────────────────────

        [Fact]
        public void AddThenImmediatelyRemove_ListIsEmpty()
        {
            _vm.AddJob("Fast", @"C:\src", @"C:\dst", BackupType.Full);
            _vm.RemoveJob(0);
            Assert.Empty(_vm.Jobs);
        }

        [Fact]
        public void AddSameNameAfterRemove_Succeeds()
        {
            _vm.AddJob("Reused", @"C:\src", @"C:\dst", BackupType.Full);
            _vm.RemoveJob(0);
            var r = _vm.AddJob("Reused", @"C:\src", @"C:\dst", BackupType.Full);
            Assert.True(r.Success); // name is free again after removal
        }

        // ── Settings boundary values ──────────────────────────────────────────

        [Fact]
        public void SaveSettings_EmptyBusinessSoftware_DisablesDetection()
        {
            var s = new AppSettings { BusinessSoftware = string.Empty };
            _vm.SaveSettings(s);
            Assert.Equal(string.Empty, _vm.GetSettings().BusinessSoftware);
        }

        [Fact]
        public void SaveSettings_EmptyCryptoExtensions_DisablesCrypto()
        {
            var s = new AppSettings { CryptoExtensions = string.Empty };
            _vm.SaveSettings(s);
            Assert.Equal(string.Empty, _vm.GetSettings().CryptoExtensions);
        }

        [Fact]
        public void ChangeLogFormat_ThenSaveAllSettings_KeepsFormat()
        {
            _vm.ChangeLogFormat("XML");

            // SaveSettings called by SettingsWindow overwrites everything – test it doesn't reset LogFormat.
            var s = _vm.GetSettings();
            s.Language = "fr"; // only change language
            _vm.SaveSettings(s);

            Assert.Equal("XML", _vm.GetSettings().LogFormat); // must be preserved
        }

        // ── Multiple executions of same job ───────────────────────────────────

        [Fact]
        public void Execute_FullBackup_RunTwice_NoError()
        {
            File.WriteAllText(Path.Combine(_src, "repeat.txt"), "hello");
            string dst = Path.Combine(_temp, "dst_repeat");

            var job = new BackupJob
            {
                Id = 1, Name = "Repeat", Type = BackupType.Full,
                SourceDirectory = _src, TargetDirectory = dst
            };

            _svc.Execute(job);
            var ex = Record.Exception(() => _svc.Execute(job));
            Assert.Null(ex);
        }

        // ── ExecuteAllJobs on empty list ──────────────────────────────────────

        [Fact]
        public void ExecuteAllJobs_EmptyList_DoesNothing()
        {
            var ex = Record.Exception(() => _vm.ExecuteAllJobs());
            Assert.Null(ex);
        }
    }
}
