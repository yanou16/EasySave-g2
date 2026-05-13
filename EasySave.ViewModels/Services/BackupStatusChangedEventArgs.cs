using EasySave.Models;

namespace EasySave.ViewModels.Services
{
    public class BackupStatusChangedEventArgs : EventArgs
    {
        public BackupStatusChangedEventArgs(int jobId, string jobName, BackupRuntimeStatus status)
        {
            JobId = jobId;
            JobName = jobName;
            Status = status;
        }

        public int JobId { get; }
        public string JobName { get; }
        public BackupRuntimeStatus Status { get; }
    }
}
