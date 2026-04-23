using EasyLog.Models;
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
        private readonly ConfigService _configService;
        private readonly BackupService _backupService;
        private readonly LanguageService _language;
        private List<BackupJob> _jobs;

        /// <summary>Jobs exposed to the View (read-only).</summary>
        public IReadOnlyList<BackupJob> Jobs => _jobs.AsReadOnly();

        public BackupViewModel(ConfigService configService, BackupService backupService, LanguageService language)
        {
            _configService = configService;
            _backupService = backupService;
            _language = language;
            _jobs = configService.LoadJobs();
        }

        /// <summary>Adds a new backup job. Returns success flag + localized message.</summary>
        public (bool Success, string Message) AddJob(string name, string source, string target, BackupType type)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, _language.Get("EmptyName"));

            if (string.IsNullOrWhiteSpace(source))
                return (false, _language.Get("EmptySource"));

            if (string.IsNullOrWhiteSpace(target))
                return (false, _language.Get("EmptyTarget"));

            if (_jobs.Count >= 5)
                return (false, _language.Get("MaxJobsReached"));

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

        /// <summary>Removes a job by 0-based index. Returns success flag + localized message.</summary>
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

        /// <summary>Executes all configured jobs sequentially.</summary>
        public void ExecuteAllJobs()
        {
            foreach (BackupJob job in _jobs)
                _backupService.Execute(job);
        }
    }
}
