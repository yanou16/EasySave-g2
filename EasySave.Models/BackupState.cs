namespace EasySave.Models
{
    public class BackupState
    {
        public string BackupName { get; set; } = string.Empty;
        public string CurrentSourcePath { get; set; } = string.Empty;
        public string CurrentTargetPath { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public long CurrentFileSize { get; set; }
        public DateTime LastBackupUpdateTime { get; set; }
        public long TransferredBytes { get; set; }
        public long TotalEligibleFileCount { get; set; }
        public long RemainingFileCount { get; set; }
        public long TotalEligibleBytes { get; set; }
        public long RemainingBytes { get; set; }
        public double Progress => TotalEligibleBytes > 0 ? (double)TransferredBytes / TotalEligibleBytes * 100 : 0;
    }
}
