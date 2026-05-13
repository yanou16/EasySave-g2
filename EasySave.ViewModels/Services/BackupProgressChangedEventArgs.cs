using EasySave.Models;

namespace EasySave.ViewModels.Services
{
    public class BackupProgressChangedEventArgs : EventArgs
    {
        public BackupProgressChangedEventArgs(int jobId, string jobName, double progress, BackupRuntimeStatus status)
        {
            JobId = jobId;
            JobName = jobName;
            Progress = progress;
            Status = status;
        }

        public int JobId { get; }
        public string JobName { get; }
        public double Progress { get; }
        public BackupRuntimeStatus Status { get; }
    }
}
