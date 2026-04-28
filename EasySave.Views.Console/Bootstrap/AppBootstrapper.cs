using EasySave.ViewModels;

namespace EasySave.Views.Console.Bootstrap
{
    /// <summary>
    /// Composition root for the console application.
    /// Delegates all infrastructure wiring to ViewModelFactory — no EasyLog dependency here.
    /// </summary>
    public class AppBootstrapper
    {
        public ConsoleAppContext Create()
        {
            string appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProSoft",
                "EasySave");

            string resourcesDirectory = Path.Combine(AppContext.BaseDirectory, "Resources");

            var (backupViewModel, languageService) = ViewModelFactory.Create(appDataDirectory);

            return new ConsoleAppContext(backupViewModel, languageService, resourcesDirectory, appDataDirectory);
        }
    }
}
