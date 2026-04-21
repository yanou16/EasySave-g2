using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using EasyLog.Models;

namespace EasyLog.Services
{
    public class StateManager
    {
        private readonly string _stateFilePath;

        public StateManager(string stateFilePath)
        {
            _stateFilePath = stateFilePath;

            string? directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void WriteState(BackupStateEntry state)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(state, options);
                File.WriteAllText(_stateFilePath, json);
            }
            catch
            {
            }
        }
    }
}
