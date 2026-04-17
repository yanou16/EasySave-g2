namespace EasyLog.Models
{
    public class BackupStateEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string State { get; set; } = "Inactive";
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int Progress { get; set; }
        public int RemainingFiles { get; set; }
        public long RemainingSize { get; set; }
        public string CurrentSourceFile { get; set; } = string.Empty;
        public string CurrentDestFile { get; set; } = string.Empty;
    }
}
