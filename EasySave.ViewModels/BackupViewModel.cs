using EasySave.Models;
using EasySave.ViewModels.Services;

namespace EasySave.ViewModels
{
    /// <summary>
    /// Main ViewModel. Orchestrates jobs, services and language.
    /// The Views layer only talks to this class.
    /// </summary>
    public class BackupViewModel
    {
        private readonly ConfigService  _configService;
        private readonly BackupService  _backupService;
        private readonly LanguageService _language;
        private readonly SettingsService _settingsService;
        private List<BackupJob> _jobs;

        /// <summary>Jobs exposed to the View (read-only).</summary>
        public IReadOnlyList<BackupJob> Jobs => _jobs.AsReadOnly();

        public event EventHandler<BackupProgressChangedEventArgs>? ProgressChanged
        {
            add => _backupService.ProgressChanged += value;
            remove => _backupService.ProgressChanged -= value;
        }

        public event EventHandler<BackupStatusChangedEventArgs>? StatusChanged
        {
            add => _backupService.StatusChanged += value;
            remove => _backupService.StatusChanged -= value;
        }

        public BackupViewModel(
            ConfigService   configService,
            BackupService   backupService,
            LanguageService language,
            SettingsService settingsService)
        {
            _configService   = configService;
            _backupService   = backupService;
            _language        = language;
            _settingsService = settingsService;
            _jobs            = configService.LoadJobs();
            ApplyLogFormat();
        }

        // ── Settings ──────────────────────────────────────────────────────────

        /// <summary>Returns current saved settings.</summary>
        public AppSettings GetSettings() => _settingsService.Load();

        /// <summary>Saves all settings at once and applies the log format immediately.</summary>
        public (bool Success, string Message) SaveSettings(AppSettings settings)
        {
            if (settings.LogFormat != "JSON" && settings.LogFormat != "XML")
                return (false, _language.Get("InvalidLogFormat"));

            _settingsService.Save(settings);
            ApplyLogFormat();
            _backupService.UpdateBusinessSoftware(settings.BusinessSoftware);
            return (true, _language.Get("SettingsSaved"));
        }

        /// <summary>Updates only the log format and applies it immediately.</summary>
        public (bool Success, string Message) ChangeLogFormat(string format)
        {
            if (format != "JSON" && format != "XML")
                return (false, _language.Get("InvalidLogFormat"));

            AppSettings settings = _settingsService.Load();
            settings.LogFormat   = format;
            _settingsService.Save(settings);
            ApplyLogFormat();
            return (true, _language.Get("SettingsSaved"));
        }

        /// <summary>Updates the semicolon-separated list of file extensions encrypted through CryptoSoft.</summary>
        public (bool Success, string Message) ChangeCryptoExtensions(string extensions)
        {
            AppSettings settings = _settingsService.Load();
            settings.CryptoExtensions = extensions.Trim();
            _settingsService.Save(settings);
            return (true, _language.Get("SettingsSaved"));
        }

        public void ApplyLogFormat()
        {
            AppSettings settings = _settingsService.Load();
            _backupService.UpdateLogFormat(settings.LogFormat);
        }

        // ── Jobs ──────────────────────────────────────────────────────────────

        /// <summary>Adds a new backup job (unlimited in v2.0).</summary>
        public (bool Success, string Message) AddJob(string name, string source, string target, BackupType type)
        {
            if (string.IsNullOrWhiteSpace(name)) return (false, _language.Get("EmptyName"));
            if (string.IsNullOrWhiteSpace(source)) return (false, _language.Get("EmptySource"));
            if (string.IsNullOrWhiteSpace(target)) return (false, _language.Get("EmptyTarget"));

            // Invalid path check
            if (!Directory.Exists(source))
                return (false, _language.Get("InvalidSourcePath"));

            // Duplicate path check
            if (source.TrimEnd('\\', '/').Equals(
                target.TrimEnd('\\', '/'),
                StringComparison.OrdinalIgnoreCase))
                return (false, _language.Get("DuplicatePath"));

            if (_jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return (false, _language.Get("JobNameExists"));

            _jobs.Add(new BackupJob
            {
                Id = _jobs.Count + 1,
                Name = name,
                SourceDirectory = source,
                TargetDirectory = target,
                Type = type
            });

            _configService.SaveJobs(_jobs);
            return (true, _language.Get("JobAdded"));
        }

        /// <summary>Removes a job by 0-based index.</summary>
        public (bool Success, string Message) RemoveJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                return (false, _language.Get("InvalidJobIndex"));

            _jobs.RemoveAt(index);

            for (int i = 0; i < _jobs.Count; i++)
                _jobs[i].Id = i + 1;

            _configService.SaveJobs(_jobs);
            return (true, _language.Get("JobRemoved"));
        }

        /// <summary>Executes one job by 0-based index.</summary>
        public void ExecuteJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _backupService.Execute(_jobs[index]);
        }

        public Task StartJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _backupService.Start(_jobs[index]);
        }

        /// <summary>Executes all configured jobs in parallel for v3.0.</summary>
        public void ExecuteAllJobs()
        {
            _backupService.ExecuteAll(_jobs);
        }

        public Task StartAllJobs() => _backupService.StartAll(_jobs);

        public void PauseJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _backupService.PauseJob(_jobs[index].Id);
        }

        public void ResumeJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _backupService.ResumeJob(_jobs[index].Id);
        }

        public void StopJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _backupService.StopJob(_jobs[index].Id);
        }

        public void PauseAll() => _backupService.PauseAll();

        public void ResumeAll() => _backupService.ResumeAll();

        public void StopAll() => _backupService.StopAll();

        public BackupRuntimeStatus GetJobStatus(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _backupService.GetStatus(_jobs[index].Id);
        }
    }
}
