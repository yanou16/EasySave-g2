using EasyLog.Services;
using EasySave.ViewModels;
using EasySave.ViewModels.Services;

namespace EasySave.Console.Bootstrap
{
    public class AppBootstrapper
    {
        public ConsoleAppContext Create()
        {
            // Store config, state, and logs under a stable per-user app data root.
            string appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProSoft",
                "EasySave");

            string logsDirectory = Path.Combine(appDataDirectory, "logs");
            string stateFilePath = Path.Combine(appDataDirectory, "state.json");
            string resourcesDirectory = Path.Combine(AppContext.BaseDirectory, "Resources");

            // Centralize object graph construction here so the console layer stays presentation-only.
            var logger = new Logger(logsDirectory);
            var stateManager = new StateManager(stateFilePath);
            var configService = new ConfigService(appDataDirectory);
            var languageService = new LanguageService();
            var backupService = new BackupService(logger, stateManager);
            var backupViewModel = new BackupViewModel(configService, backupService, languageService);

            return new ConsoleAppContext(backupViewModel, languageService, resourcesDirectory, appDataDirectory);
        }
    }
}
