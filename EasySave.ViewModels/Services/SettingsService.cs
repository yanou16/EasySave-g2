using System.Text.Json;
using EasySave.Models;

namespace EasySave.ViewModels.Services
{
    /// <summary>
    /// Loads and saves application settings to config.json.
    /// </summary>
    public class SettingsService
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>Production constructor — stores settings in %AppData%\EasySave.</summary>
        public SettingsService() : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave")) { }

        /// <summary>Testable constructor — stores settings in the given directory.</summary>
        public SettingsService(string directory)
        {
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "config.json");
        }

        public AppSettings Load()
        {
            if (!File.Exists(_path)) return new AppSettings();
            try
            {
                string json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }

        public void Save(AppSettings settings)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions));
        }
    }
}