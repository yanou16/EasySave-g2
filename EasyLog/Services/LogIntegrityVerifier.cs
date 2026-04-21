using EasyLog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyLog.Services
{
    internal class LogIntegrityVerifier
    {
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

                    string recalculatedHash = SecurityHelper.BuildLogSignature(
                        log.Timestamp,
                        log.BackupName,
                        log.SourcePath,
                        log.TargetPath,
                        log.FileSize,
                        log.TransferTimeMs,
                        log.PreviousHash
                    );

                    if (log.Hash != recalculatedHash)
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
