using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.Services
{
    public class LanguageService
    {
        private Dictionary<string, string> _strings = new();

        public void Load(string resourcesDirectory, string language)
        {
            string filePath = Path.Combine(resourcesDirectory, $"{language}.json");
            if (!File.Exists(filePath))
                filePath = Path.Combine(resourcesDirectory, "en.json");

            _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filePath))
                       ?? new Dictionary<string, string>();
        }

        public string Get(string key) => _strings.TryGetValue(key, out string? value) ? value : key;
    }
}
