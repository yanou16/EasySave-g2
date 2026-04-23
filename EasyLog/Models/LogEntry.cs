namespace EasyLog.Models
{
    /// <summary>
    /// Represents a single file transfer event recorded in the daily log file.
    /// Each entry is cryptographically chained to the previous one via SHA-256 hashing,
    /// ensuring tamper detection across the log chain.
    /// </summary>
    public class LogEntry
    {
        /// <summary>UTC timestamp of the transfer, formatted as "yyyy-MM-dd HH:mm:ss".</summary>
        public string Timestamp { get; set; } = string.Empty;

        /// <summary>Name of the backup job that triggered this transfer.</summary>
        public string BackupName { get; set; } = string.Empty;

        /// <summary>Absolute UNC path of the source file.</summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>Absolute UNC path of the destination file.</summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>Size of the transferred file in bytes.</summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Transfer duration in milliseconds.
        /// Negative value indicates a transfer error (absolute value = elapsed time before failure).
        /// </summary>
        public long TransferTimeMs { get; set; }

        /// <summary>
        /// SHA-256 hash of the previous log entry.
        /// Set to "GENESIS" for the very first entry in a log file.
        /// </summary>
        public string PreviousHash { get; set; } = string.Empty;

        /// <summary>
        /// SHA-256 hash of this entry's content concatenated with PreviousHash.
        /// Allows integrity verification of the entire log chain.
        /// </summary>
        public string Hash { get; set; } = string.Empty;
    }
}
