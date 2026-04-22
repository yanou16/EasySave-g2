namespace EasySave.Models
{
    public class BackupJob
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public string TargetDirectory { get; set; } = string.Empty;
        public BackupType Type { get; set; }
    }
}