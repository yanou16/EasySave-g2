namespace EasyLog.Models
{
    /// <summary>
    /// Represents the real-time execution state of a single backup job.
    /// Written to state.json after every file operation so external tools
    /// can monitor progress without reading the log.
    /// </summary>
    public class BackupStateEntry
    {
        /// <summary>Name of the backup job this state entry belongs to.</summary>
        public string BackupName { get; set; } = string.Empty;

        /// <summary>Timestamp of the last state update, formatted as "yyyy-MM-dd HH:mm:ss".</summary>
        public string LastActionTimestamp { get; set; } = string.Empty;

        /// <summary>
        /// Current execution state of the job.
        /// Expected values: "Active" while running, "Inactive" when idle or finished.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>Total number of files eligible for backup in the source directory.</summary>
        public int TotalFiles { get; set; }

        /// <summary>Total size in bytes of all eligible files.</summary>
        public long TotalSize { get; set; }

        /// <summary>Completion percentage, between 0 and 100.</summary>
        public double Progress { get; set; }

        /// <summary>Number of files not yet transferred.</summary>
        public int RemainingFiles { get; set; }

        /// <summary>Total size in bytes of files not yet transferred.</summary>
        public long RemainingSize { get; set; }

        /// <summary>Absolute path of the source file currently being transferred. Empty when idle.</summary>
        public string CurrentSourceFile { get; set; } = string.Empty;

        /// <summary>Absolute path of the destination file currently being written. Empty when idle.</summary>
        public string CurrentTargetFile { get; set; } = string.Empty;

        /// <summary>Last error message encountered during transfer. Empty when no error occurred.</summary>
        public string Error { get; set; } = string.Empty;
    }
}
