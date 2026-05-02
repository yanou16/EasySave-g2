using System.IO;
using System.Windows;
using System.Windows.Threading;
using EasySave.ViewModels;
using Application = System.Windows.Application;

namespace EasySave.Views.WPF
{
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            // Catch any unhandled exception anywhere in the app and write it to a file.
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            try
            {
                string appDataDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ProSoft",
                    "EasySave");

                var (backupViewModel, languageService) = ViewModelFactory.Create(appDataDirectory);

                var mainWindow = new MainWindow(backupViewModel, languageService);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                WriteCrashLog(ex);
                MessageBox.Show(
                    $"EasySave failed to start:\n\n{ex.GetType().Name}: {ex.Message}\n\nDetails saved to:\n{CrashLogPath()}",
                    "EasySave – Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            WriteCrashLog(e.Exception);
            MessageBox.Show(
                $"Unhandled error:\n\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\nDetails saved to:\n{CrashLogPath()}",
                "EasySave – Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                WriteCrashLog(ex);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static string CrashLogPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProSoft", "EasySave", "crash.log");

        private static void WriteCrashLog(Exception ex)
        {
            try
            {
                string path = CrashLogPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n" +
                    $"{ex}\n" +
                    new string('-', 80) + "\n");
            }
            catch { /* never crash inside the crash handler */ }
        }
    }
}
