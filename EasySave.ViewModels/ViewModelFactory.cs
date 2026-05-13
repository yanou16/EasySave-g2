using EasyLog;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.ViewModels.Services;

namespace EasySave.ViewModels
{
    /// <summary>
    /// Composition root for the ViewModel layer.
    /// Views call this factory so they never depend on EasyLog or infrastructure directly.
    /// </summary>
    public static class ViewModelFactory
    {
        public static (BackupViewModel BackupViewModel, LanguageService LanguageService) Create(
            string appDataDirectory)
        {
            string logsDirectory = Path.Combine(appDataDirectory, "Logs");
            string stateFilePath = Path.Combine(appDataDirectory, "state.json");

            var settingsService = new SettingsService();
            var settings = settingsService.Load();
            var logFormat = settings.LogFormat == "XML" ? LogFormat.Xml : LogFormat.Json;

            var logger = new Logger(logsDirectory, logFormat);
            var configService = new ConfigService(appDataDirectory);
            var languageService = new LanguageService();
            var allStates = new List<BackupStateEntry>();
            var cryptoSoft = new CryptoSoftService(appDataDirectory);
            var coordinator = new ParallelCoordinator();

            var businessList = string.IsNullOrWhiteSpace(settings.BusinessSoftware)
                ? new List<string>()
                : settings.BusinessSoftware
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

            var businessWatcher = new BusinessSoftwareWatcher(businessList);

            var backupService = new BackupService(
                logger,
                stateFilePath,
                allStates,
                settingsService,
                cryptoSoft,
                businessWatcher,
                coordinator);

            var backupViewModel = new BackupViewModel(
                configService,
                backupService,
                languageService,
                settingsService);

            return (backupViewModel, languageService);
        }
    }
}