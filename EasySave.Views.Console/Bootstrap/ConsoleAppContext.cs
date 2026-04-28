using EasySave.ViewModels;
using EasySave.ViewModels.Services;

namespace EasySave.Views.Console.Bootstrap
{
    public sealed class ConsoleAppContext
    {
        public ConsoleAppContext(
            BackupViewModel backupViewModel,
            LanguageService languageService,
            string resourcesDirectory,
            string appDataDirectory)
        {
            BackupViewModel    = backupViewModel;
            LanguageService    = languageService;
            ResourcesDirectory = resourcesDirectory;
            AppDataDirectory   = appDataDirectory;
        }

        public BackupViewModel BackupViewModel    { get; }
        public LanguageService LanguageService    { get; }
        public string          ResourcesDirectory { get; }
        public string          AppDataDirectory   { get; }
    }
}
