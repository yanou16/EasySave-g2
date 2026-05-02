using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.ViewModels.Services
{
    public class BusinessSoftwareDetectedException : Exception
    {
        public string SoftwareName { get; }

        public BusinessSoftwareDetectedException(string softwareName)
            : base($"Business software detected: {softwareName}")
        {
            SoftwareName = softwareName;
        }
    }
}
