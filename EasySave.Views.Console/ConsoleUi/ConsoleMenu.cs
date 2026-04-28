using EasySave.Models;
using EasySave.ViewModels.Services;

namespace EasySave.Views.Console.ConsoleUi
{
    public static class ConsoleMenu
    {
        public static void PrintHeader(LanguageService language, string appDataDirectory)
        {
            System.Console.WriteLine(language.Get("AppTitle"));
            System.Console.WriteLine(new string('=', 40));
            System.Console.WriteLine($"{language.Get("StorageLocation")}: {appDataDirectory}");
            System.Console.WriteLine();
        }

        public static void PrintMainMenu(LanguageService language)
        {
            System.Console.WriteLine(language.Get("MainMenuTitle"));
            System.Console.WriteLine($"1. {language.Get("MenuListJobs")}");
            System.Console.WriteLine($"2. {language.Get("MenuAddJob")}");
            System.Console.WriteLine($"3. {language.Get("MenuRemoveJob")}");
            System.Console.WriteLine($"4. {language.Get("MenuExecuteJob")}");
            System.Console.WriteLine($"5. {language.Get("MenuExecuteAllJobs")}");
            System.Console.WriteLine($"6. {language.Get("MenuSettings")}");
            System.Console.WriteLine($"7. {language.Get("MenuQuit")}");
            System.Console.WriteLine();
        }

        public static void PrintJobs(LanguageService language, IReadOnlyList<BackupJob> jobs)
        {
            if (jobs.Count == 0)
            {
                System.Console.WriteLine(language.Get("NoJobs"));
                System.Console.WriteLine();
                return;
            }

            System.Console.WriteLine(language.Get("JobsListTitle"));
            foreach (var job in jobs)
            {
                System.Console.WriteLine(
                    $"[{job.Id}] {job.Name} | " +
                    $"{language.Get("LabelSource")}: {job.SourceDirectory} | " +
                    $"{language.Get("LabelTarget")}: {job.TargetDirectory} | " +
                    $"{language.Get("LabelType")}: {job.Type}");
            }

            System.Console.WriteLine();
        }
    }
}
