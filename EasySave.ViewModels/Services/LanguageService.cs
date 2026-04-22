using System.Text.Json;

namespace EasySave.ViewModels.Services
{
    public class LanguageService
    {
        private Dictionary<string, string> _strings = new();

        public void Load(string resourcesDirectory, string language)
        {
            string path = Path.Combine(resourcesDirectory, $"{language}.json");

            if (!File.Exists(path))
                path = Path.Combine(resourcesDirectory, "en.json");

            _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                       ?? new Dictionary<string, string>();
        }

        public string Get(string key) =>
            _strings.TryGetValue(key, out string? value) ? value : key;
    }
}
