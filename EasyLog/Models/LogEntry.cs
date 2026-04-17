namespace EasyLog.Models
{
    public class LogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string BackupName { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public long TransferTimeMs { get; set; }
    }
}
