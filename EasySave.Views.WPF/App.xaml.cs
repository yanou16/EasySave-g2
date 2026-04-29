using System.IO;
using System.Windows;
using EasySave.ViewModels;
using Application = System.Windows.Application;

namespace EasySave.Views.WPF
{
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            string appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProSoft",
                "EasySave");

            var (backupViewModel, languageService) = ViewModelFactory.Create(appDataDirectory);

            var mainWindow = new MainWindow(backupViewModel, languageService);
            mainWindow.Show();
        }
    }
}
