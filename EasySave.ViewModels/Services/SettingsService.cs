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

        public SettingsService()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "config.json");
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