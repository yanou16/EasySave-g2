using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyLog.Models
{
    public class BackupStateEntry
    {
        public string BackupName { get; set; } = string.Empty;
        public string LastActionTimestamp { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;

        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }

        public double Progress { get; set; }

        public int RemainingFiles { get; set; }
        public long RemainingSize { get; set; }

        public string CurrentSourceFile { get; set; } = string.Empty;
        public string CurrentTargetFile { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;
    }
}
