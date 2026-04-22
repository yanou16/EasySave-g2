using System.Text.Json;
using EasySave.Models;

namespace EasySave.ViewModels.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public ConfigService(string appDataDirectory)
        {
            Directory.CreateDirectory(appDataDirectory);
            _configPath = Path.Combine(appDataDirectory, "jobs.json");
        }

        public List<BackupJob> LoadJobs()
        {
            if (!File.Exists(_configPath))
                return new List<BackupJob>();

            try
            {
                return JsonSerializer.Deserialize<List<BackupJob>>(File.ReadAllText(_configPath))
                       ?? new List<BackupJob>();
            }
            catch
            {
                return new List<BackupJob>();
            }
        }

        public void SaveJobs(List<BackupJob> jobs)
        {
            File.WriteAllText(_configPath, JsonSerializer.Serialize(jobs, JsonOptions));
        }
    }
}
