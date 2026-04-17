using System;
using System.Collections.Generic;
using System.Linq;
using EasyLog.Models;
using EasySave.Models;
using EasySave.Services;

namespace EasySave.ViewModels
{
    public class BackupViewModel
    {
        private readonly ConfigService _configService;
        private readonly BackupService _backupService;
        private readonly LanguageService _language;
        private List<BackupJob> _jobs;

        public IReadOnlyList<BackupJob> Jobs => _jobs.AsReadOnly();

        public BackupViewModel(ConfigService configService, BackupService backupService, LanguageService language, List<BackupStateEntry> states)
        {
            _configService = configService;
            _backupService = backupService;
            _language = language;
            _jobs = configService.LoadJobs();
        }

        public (bool success, string message) AddJob(string name, string source, string target, BackupType type)
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

        public (bool success, string message) RemoveJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                return (false, _language.Get("InvalidJobIndex"));

            _jobs.RemoveAt(index);
            for (int i = 0; i < _jobs.Count; i++)
                _jobs[i].Id = i + 1;

            _configService.SaveJobs(_jobs);
            return (true, _language.Get("JobRemoved"));
        }

        public void ExecuteJob(int index)
        {
            if (index < 0 || index >= _jobs.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _backupService.Execute(_jobs[index]);
        }

        public void ExecuteAllJobs()
        {
            for (int i = 0; i < _jobs.Count; i++)
                _backupService.Execute(_jobs[i]);
        }
    }
}
