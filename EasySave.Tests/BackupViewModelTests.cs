using EasyLog.Services;
using EasySave.Models;
using EasySave.ViewModels;
using EasySave.ViewModels.Services;
using System.Diagnostics;
using Xunit;

namespace EasySave.Tests
{
    /// <summary>
    /// Tests for BackupViewModel – job management, settings, validation.
    /// Each test gets an isolated temp directory so tests never interfere.
    /// </summary>
    public class BackupViewModelTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly BackupViewModel _vm;

        public BackupViewModelTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "EasySaveTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);

            // Build ViewModel exactly like ViewModelFactory but in a temp directory.
            var configService   = new ConfigService(_tempDir);
            var settingsService = new SettingsService(_tempDir);
            var languageService = new LanguageService();
            // Load minimal language strings needed by the ViewModel.
            languageService.LoadFromJson("""
            {
                "EmptyName":       "Name required",
                "EmptySource":     "Source required",
                "EmptyTarget":     "Target required",
                "JobNameExists":   "Name already exists",
                "JobAdded":        "Job added",
                "JobRemoved":      "Job removed",
                "InvalidJobIndex": "Invalid index",
                "InvalidLogFormat":"Invalid format",
                "SettingsSaved":   "Saved"
            }
            """);

            // Use stub BackupService (no real logger/crypto for ViewModel-level tests).
            var logsDir       = Path.Combine(_tempDir, "Logs");
            var stateFile     = Path.Combine(_tempDir, "state.json");
            var allStates     = new List<EasyLog.Models.BackupStateEntry>();
            var logger        = new Logger(logsDir, EasyLog.LogFormat.Json);
            var crypto        = new CryptoSoftService(_tempDir);
            var watcher       = new BusinessSoftwareWatcher(new List<string>()); // disabled
            var coordinator   = new ParallelCoordinator();
            var backupService = new BackupService(logger, stateFile, allStates, settingsService, crypto, watcher, coordinator);

            _vm = new BackupViewModel(configService, backupService, languageService, settingsService);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        // ── AddJob ────────────────────────────────────────────────────────────

        [Fact]
        public void AddJob_Valid_SucceedsAndAppearsInList()
        {
            var r = _vm.AddJob("MyJob", @"C:\src", @"C:\dst", BackupType.Full);

            Assert.True(r.Success);
            Assert.Single(_vm.Jobs);
            Assert.Equal("MyJob", _vm.Jobs[0].Name);
        }

        [Fact]
        public void AddJob_EmptyName_Fails()
        {
            var r = _vm.AddJob("", @"C:\src", @"C:\dst", BackupType.Full);
            Assert.False(r.Success);
            Assert.Empty(_vm.Jobs);
        }

        [Fact]
        public void AddJob_WhitespaceName_Fails()
        {
            var r = _vm.AddJob("   ", @"C:\src", @"C:\dst", BackupType.Full);
            Assert.False(r.Success);
        }

        [Fact]
        public void AddJob_EmptySource_Fails()
        {
            var r = _vm.AddJob("Job1", "", @"C:\dst", BackupType.Full);
            Assert.False(r.Success);
        }

        [Fact]
        public void AddJob_EmptyTarget_Fails()
        {
            var r = _vm.AddJob("Job1", @"C:\src", "", BackupType.Full);
            Assert.False(r.Success);
        }

        [Fact]
        public void AddJob_DuplicateName_Fails()
        {
            _vm.AddJob("Job1", @"C:\src", @"C:\dst", BackupType.Full);
            var r = _vm.AddJob("Job1", @"C:\src2", @"C:\dst2", BackupType.Full);

            Assert.False(r.Success);
            Assert.Single(_vm.Jobs); // still only one
        }

        [Fact]
        public void AddJob_DuplicateName_CaseInsensitive_Fails()
        {
            _vm.AddJob("myjob", @"C:\src", @"C:\dst", BackupType.Full);
            var r = _vm.AddJob("MYJOB", @"C:\src2", @"C:\dst2", BackupType.Full);

            Assert.False(r.Success);
        }

        [Fact]
        public void AddJob_SpecialCharsInName_Succeeds()
        {
            // Special chars in the name should be allowed (stored as string, not used as path).
            var r = _vm.AddJob("Job #1 – été", @"C:\src", @"C:\dst", BackupType.Full);
            Assert.True(r.Success);
        }

        [Fact]
        public void AddJob_VeryLongName_Succeeds()
        {
            string longName = new string('A', 300);
            var r = _vm.AddJob(longName, @"C:\src", @"C:\dst", BackupType.Full);
            Assert.True(r.Success);
        }

        [Fact]
        public void AddJob_MultipleJobs_IdsAreSequential()
        {
            _vm.AddJob("A", @"C:\a", @"C:\da", BackupType.Full);
            _vm.AddJob("B", @"C:\b", @"C:\db", BackupType.Differential);
            _vm.AddJob("C", @"C:\c", @"C:\dc", BackupType.Full);

            Assert.Equal(1, _vm.Jobs[0].Id);
            Assert.Equal(2, _vm.Jobs[1].Id);
            Assert.Equal(3, _vm.Jobs[2].Id);
        }

        [Fact]
        public void AddJob_DifferentialType_StoredCorrectly()
        {
            _vm.AddJob("DiffJob", @"C:\src", @"C:\dst", BackupType.Differential);
            Assert.Equal(BackupType.Differential, _vm.Jobs[0].Type);
        }

        // ── RemoveJob ─────────────────────────────────────────────────────────

        [Fact]
        public void RemoveJob_ValidIndex_SucceedsAndListShrinks()
        {
            _vm.AddJob("Job1", @"C:\src", @"C:\dst", BackupType.Full);
            var r = _vm.RemoveJob(0);

            Assert.True(r.Success);
            Assert.Empty(_vm.Jobs);
        }

        [Fact]
        public void RemoveJob_NegativeIndex_Fails()
        {
            _vm.AddJob("Job1", @"C:\src", @"C:\dst", BackupType.Full);
            var r = _vm.RemoveJob(-1);

            Assert.False(r.Success);
            Assert.Single(_vm.Jobs);
        }

        [Fact]
        public void RemoveJob_IndexOutOfBounds_Fails()
        {
            _vm.AddJob("Job1", @"C:\src", @"C:\dst", BackupType.Full);
            var r = _vm.RemoveJob(5);

            Assert.False(r.Success);
        }

        [Fact]
        public void RemoveJob_EmptyList_Fails()
        {
            var r = _vm.RemoveJob(0);
            Assert.False(r.Success);
        }

        [Fact]
        public void RemoveJob_Middle_ReNumbersIds()
        {
            _vm.AddJob("A", @"C:\a", @"C:\da", BackupType.Full);
            _vm.AddJob("B", @"C:\b", @"C:\db", BackupType.Full);
            _vm.AddJob("C", @"C:\c", @"C:\dc", BackupType.Full);

            _vm.RemoveJob(1); // remove B

            Assert.Equal(2, _vm.Jobs.Count);
            Assert.Equal(1, _vm.Jobs[0].Id); // A
            Assert.Equal(2, _vm.Jobs[1].Id); // C re-numbered
        }

        // ── v1.1 : 5-job limit (enforced in ConsoleApp, not ViewModel) ────────

        [Fact]
        public void AddJob_v20_NoLimit_CanAddMoreThan5()
        {
            // v2.0 ViewModel has no limit – the GUI accepts unlimited jobs.
            for (int i = 1; i <= 7; i++)
                _vm.AddJob($"Job{i}", $@"C:\src{i}", $@"C:\dst{i}", BackupType.Full);

            Assert.Equal(7, _vm.Jobs.Count);
        }

        // ── Settings ──────────────────────────────────────────────────────────

        [Fact]
        public void ChangeLogFormat_Json_Succeeds()
        {
            var r = _vm.ChangeLogFormat("JSON");
            Assert.True(r.Success);
            Assert.Equal("JSON", _vm.GetSettings().LogFormat);
        }

        [Fact]
        public void ChangeLogFormat_Xml_Succeeds()
        {
            var r = _vm.ChangeLogFormat("XML");
            Assert.True(r.Success);
            Assert.Equal("XML", _vm.GetSettings().LogFormat);
        }

        [Fact]
        public void ChangeLogFormat_Invalid_Fails()
        {
            var r = _vm.ChangeLogFormat("PDF");
            Assert.False(r.Success);
        }

        [Fact]
        public void ChangeLogFormat_Lowercase_Fails()
        {
            // Must be exact uppercase per spec ("JSON" or "XML").
            var r = _vm.ChangeLogFormat("json");
            Assert.False(r.Success);
        }

        [Fact]
        public void SaveSettings_Persists()
        {
            var settings = new AppSettings
            {
                LogFormat        = "XML",
                BusinessSoftware = "calc",
                CryptoExtensions = ".txt;.pdf",
                Language         = "fr"
            };

            _vm.SaveSettings(settings);
            var loaded = _vm.GetSettings();

            Assert.Equal("XML",        loaded.LogFormat);
            Assert.Equal("calc",       loaded.BusinessSoftware);
            Assert.Equal(".txt;.pdf",  loaded.CryptoExtensions);
            Assert.Equal("fr",         loaded.Language);
        }

        [Fact]
        public void ExecuteJob_InvalidIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _vm.ExecuteJob(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _vm.ExecuteJob(-1));
        }

        [Fact]
        public async Task StartJob_BusinessSoftwareRunning_PausesThenAutoResumes()
        {
            string source = Path.Combine(_tempDir, "source");
            string target = Path.Combine(_tempDir, "target");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "a.txt"), "data");

            _vm.AddJob("Controlled", source, target, BackupType.Full);
            var processName = Process.GetCurrentProcess().ProcessName;
            _vm.SaveSettings(new AppSettings { BusinessSoftware = processName });

            Task run = _vm.StartJob(0);
            await WaitUntilAsync(() => _vm.GetJobStatus(0) == BackupRuntimeStatus.Paused);

            _vm.SaveSettings(new AppSettings { BusinessSoftware = string.Empty });
            await run.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(BackupRuntimeStatus.Finished, _vm.GetJobStatus(0));
            Assert.True(File.Exists(Path.Combine(target, "a.txt")));
        }

        [Fact]
        public async Task StopJob_CancelsPausedJob()
        {
            string source = Path.Combine(_tempDir, "source_stop");
            string target = Path.Combine(_tempDir, "target_stop");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "a.txt"), "data");

            _vm.AddJob("StopMe", source, target, BackupType.Full);
            var processName = Process.GetCurrentProcess().ProcessName;
            _vm.SaveSettings(new AppSettings { BusinessSoftware = processName });

            Task run = _vm.StartJob(0);
            await WaitUntilAsync(() => _vm.GetJobStatus(0) == BackupRuntimeStatus.Paused);

            _vm.StopJob(0);
            await run.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(BackupRuntimeStatus.Stopped, _vm.GetJobStatus(0));
        }

        [Fact]
        public async Task UserPauseSurvivesBusinessResume_UntilUserResumes()
        {
            string source = Path.Combine(_tempDir, "source_user_pause");
            string target = Path.Combine(_tempDir, "target_user_pause");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "a.txt"), "data");

            _vm.AddJob("PauseMe", source, target, BackupType.Full);
            var processName = Process.GetCurrentProcess().ProcessName;
            _vm.SaveSettings(new AppSettings { BusinessSoftware = processName });

            Task run = _vm.StartJob(0);
            await WaitUntilAsync(() => _vm.GetJobStatus(0) == BackupRuntimeStatus.Paused);

            _vm.PauseJob(0);
            _vm.SaveSettings(new AppSettings { BusinessSoftware = string.Empty });
            await Task.Delay(250);
            Assert.Equal(BackupRuntimeStatus.Paused, _vm.GetJobStatus(0));

            _vm.ResumeJob(0);
            await run.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(BackupRuntimeStatus.Finished, _vm.GetJobStatus(0));
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!condition())
            {
                await Task.Delay(50, cts.Token);
            }
        }
    }
}
