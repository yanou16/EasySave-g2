using System.Text.Json;
using EasyLog.Models;

namespace EasyLog.Services
{
    /// <summary>
    /// Persists a single backup job's real-time state to a JSON file.
    /// The file is overwritten on every call so monitoring tools always see the latest snapshot.
    /// Compatible with all EasySave versions (v1.0 and above).
    /// </summary>
    public class StateManager
    {
        private readonly string _stateFilePath;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Initialises the state manager and ensures the parent directory exists.
        /// </summary>
        /// <param name="stateFilePath">
        /// Full path of the state file (e.g. "%LocalAppData%\ProSoft\EasySave\state.json").
        /// Paths such as "C:\temp\state.json" must not be used in production deployments.
        /// </param>
        public StateManager(string stateFilePath)
        {
            _stateFilePath = stateFilePath;

            string? directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Serialises <paramref name="state"/> and overwrites the state file.
        /// Errors are silently swallowed so a write failure never interrupts a backup.
        /// </summary>
        /// <param name="state">Current state snapshot of one backup job.</param>
        public void WriteState(BackupStateEntry state)
        {
            try
            {
                File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state, JsonOptions));
            }
            catch
            {
                // State persistence is best-effort — a failed write must not abort the backup.
            }
        }
    }
}
