using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EasySave.ViewModels.Services
{
    public class BusinessSoftwareWatcher
    {
        private readonly List<string> _businessSoftwareNames;

        public BusinessSoftwareWatcher(List<string> businessSoftwareNames)
        {
            _businessSoftwareNames = businessSoftwareNames;
        }

        public string? GetRunningBusinessSoftware()
        {
            var running = Process.GetProcesses()
                                 .Select(p => p.ProcessName.ToLower())
                                 .ToList();

            return _businessSoftwareNames
                .FirstOrDefault(name => running.Contains(name.ToLower()));
        }
    }
}
