namespace EasySave.ViewModels.Services
{
    public class BusinessSoftwareChangedEventArgs : EventArgs
    {
        public BusinessSoftwareChangedEventArgs(string softwareName, bool isRunning)
        {
            SoftwareName = softwareName;
            IsRunning = isRunning;
        }

        public string SoftwareName { get; }
        public bool IsRunning { get; }
    }
}
