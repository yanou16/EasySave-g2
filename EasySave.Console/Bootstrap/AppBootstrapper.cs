using EasyLog;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Models;
using EasySave.ViewModels;
using EasySave.ViewModels.Services;

namespace EasySave.Console.Bootstrap
{
    public class AppBootstrapper
    {
        public ConsoleAppContext Create()
        {
            string appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProSoft",
                "EasySave");

            string logsDirectory = Path.Combine(appDataDirectory, "Logs");
            string stateFilePath = Path.Combine(appDataDirectory, "state.json");
            string resourcesDirectory = Path.Combine(AppContext.BaseDirectory, "Resources");

            var allStates = new List<BackupStateEntry>();

            // Load settings first to get the log format
            var settingsService = new SettingsService();
            var settings = settingsService.Load();
            var logFormat = settings.LogFormat == "XML" ? LogFormat.Xml : LogFormat.Json;

            var logger = new Logger(logsDirectory, logFormat);
            var configService = new ConfigService(appDataDirectory);
            var languageService = new LanguageService();
            var backupService = new BackupService(logger, stateFilePath, allStates);
            var backupViewModel = new BackupViewModel(configService, backupService, languageService, settingsService);

            foreach (var job in backupViewModel.Jobs)
                allStates.Add(new BackupStateEntry { BackupName = job.Name, State = "Inactive" });

            return new ConsoleAppContext(backupViewModel, languageService, resourcesDirectory, appDataDirectory);
        }
    }
}