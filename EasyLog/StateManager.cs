using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasyLog.Models;

namespace EasyLog
{
    public class StateManager
    {
        private readonly string _stateFilePath;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public StateManager(string stateDirectory)
        {
            Directory.CreateDirectory(stateDirectory);
            _stateFilePath = Path.Combine(stateDirectory, "state.json");
        }

        public void UpdateState(List<BackupStateEntry> states)
        {
            File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(states, JsonOptions));
        }
    }
}
