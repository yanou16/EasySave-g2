using EasyLog.Models;
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

            string logsDirectory = Path.Combine(appDataDirectory, "Logs");
            string stateFilePath = Path.Combine(appDataDirectory, "state.json");
            string resourcesDirectory = Path.Combine(AppContext.BaseDirectory, "Resources");

            // Shared list of all job states — written as one file on every update (spec: "fichier unique").
            var allStates = new List<BackupStateEntry>();

            var logger = new Logger(logsDirectory);
            var configService = new ConfigService(appDataDirectory);
            var languageService = new LanguageService();
            var backupService = new BackupService(logger, stateFilePath, allStates);
            var backupViewModel = new BackupViewModel(configService, backupService, languageService);

            // Pre-populate state list with all already-configured jobs (shown as Inactive on startup).
            foreach (var job in backupViewModel.Jobs)
                allStates.Add(new BackupStateEntry { BackupName = job.Name, State = "Inactive" });

            return new ConsoleAppContext(backupViewModel, languageService, resourcesDirectory, appDataDirectory);
        }
    }
}
