using System.Text.Json;
using EasyLog.Models;

namespace EasyLog.Services
{
    /// <summary>
    /// Verifies the integrity of a daily log file produced by <see cref="Logger"/>.
    /// Each entry's hash is recalculated and compared against the stored value,
    /// and the chain linkage (PreviousHash) is validated from "GENESIS" onward.
    /// Any mismatch indicates the file has been tampered with.
    /// </summary>
    internal class LogIntegrityVerifier
    {
        /// <summary>
        /// Reads and validates every entry in the specified log file.
        /// </summary>
        /// <param name="logFilePath">Absolute path to the daily JSON log file.</param>
        /// <returns>
        /// <c>true</c> if the file exists and the entire chain is intact;
        /// <c>false</c> if the file is missing, unreadable, or any entry fails verification.
        /// </returns>
        public bool VerifyLogChain(string logFilePath)
        {
            if (!File.Exists(logFilePath))
                return false;

            try
            {
                string json = File.ReadAllText(logFilePath);
                var logs = JsonSerializer.Deserialize<List<LogEntry>>(json) ?? new List<LogEntry>();

                string expectedPreviousHash = "GENESIS";

                foreach (var log in logs)
                {
                    if (log.PreviousHash != expectedPreviousHash)
                        return false;

                    string recalculated = SecurityHelper.BuildLogSignature(
                        log.Timestamp,
                        log.BackupName,
                        log.SourcePath,
                        log.TargetPath,
                        log.FileSize,
                        log.TransferTimeMs,
                        log.PreviousHash);

                    if (log.Hash != recalculated)
                        return false;

                    expectedPreviousHash = log.Hash;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
