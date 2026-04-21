using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyLog.Models
{
    public class LogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string BackupName { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public long TransferTimeMs { get; set; }

        public string PreviousHash { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
    }
}
