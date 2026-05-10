using EasySave.Models;
using EasySave.ViewModels.Services;

namespace EasySave.Views.Console.ConsoleUi
{
    public static class ConsoleMenu
    {
        // Color palette 
        private static readonly ConsoleColor ColorTitle = ConsoleColor.Cyan;
        private static readonly ConsoleColor ColorSubtle = ConsoleColor.DarkGray;
        private static readonly ConsoleColor ColorSelected = ConsoleColor.Black;
        private static readonly ConsoleColor ColorSelectedBg = ConsoleColor.Cyan;
        private static readonly ConsoleColor ColorNormal = ConsoleColor.White;
        private static readonly ConsoleColor ColorAccent = ConsoleColor.DarkCyan;
        private static readonly ConsoleColor ColorJobId = ConsoleColor.Yellow;

        public static void PrintHeader(LanguageService language, string appDataDirectory)
        {
            System.Console.ForegroundColor = ColorTitle;
            System.Console.WriteLine(@"
  ___         ___
 | __|__ _ __|_ _|___  __ ___ _____
 | _|/ _` (_-< | |(_-</ _` \ V / -_)
 |___\__,_/__/ |_|/__/\__,_|\_/\___|");
            System.Console.ResetColor();
            System.Console.WriteLine(); 
        }

        public static void PrintJobs(LanguageService language, IReadOnlyList<BackupJob> jobs)
        {
            if (jobs.Count == 0)
            {
                System.Console.ForegroundColor = ColorSubtle;
                System.Console.WriteLine($"  {language.Get("NoJobs")}");
                System.Console.ResetColor();
                System.Console.WriteLine();
                return;
            }

            System.Console.ForegroundColor = ColorAccent;
            System.Console.WriteLine($"  {language.Get("JobsListTitle")}");
            System.Console.ResetColor();

            foreach (var job in jobs)
            {
                System.Console.ForegroundColor = ColorJobId;
                System.Console.Write($"  [{job.Id}] ");
                System.Console.ForegroundColor = ColorNormal;
                System.Console.Write($"{job.Name}  ");
                System.Console.ForegroundColor = ColorSubtle;
                System.Console.Write($"{language.Get("LabelSource")}: {job.SourceDirectory}  ");
                System.Console.Write($"{language.Get("LabelTarget")}: {job.TargetDirectory}  ");
                System.Console.Write($"{language.Get("LabelType")}: {job.Type}");
                System.Console.ResetColor();
                System.Console.WriteLine();
            }

            System.Console.WriteLine();
        }

        /// <summary>
        /// Displays an interactive arrow-key menu and returns the selected index (0-based).
        /// </summary>
        public static int PrintMainMenu(LanguageService language)
        {
            string[] items =
            {
                language.Get("MenuListJobs"),
                language.Get("MenuAddJob"),
                language.Get("MenuRemoveJob"),
                language.Get("MenuExecuteJob"),
                language.Get("MenuExecuteAllJobs"),
                language.Get("MenuSettings"),
                language.Get("MenuQuit")
            };

            System.Console.ForegroundColor = ColorAccent;
            System.Console.WriteLine($"  {language.Get("MainMenuTitle")}");
            System.Console.ResetColor();

            int selected = 0;
            int menuTop = System.Console.CursorTop;

            System.Console.CursorVisible = false;

            while (true)
            {
                // Redraw menu
                System.Console.SetCursorPosition(0, menuTop);
                for (int i = 0; i < items.Length; i++)
                {
                    if (i == selected)
                    {
                        System.Console.ForegroundColor = ColorSelected;
                        System.Console.BackgroundColor = ColorSelectedBg;
                        System.Console.WriteLine($"  › {items[i].PadRight(40)}");
                    }
                    else
                    {
                        System.Console.ForegroundColor = ColorNormal;
                        System.Console.BackgroundColor = ConsoleColor.Black;
                        System.Console.WriteLine($"    {items[i].PadRight(40)}");
                    }
                    System.Console.ResetColor();
                }

                System.Console.ForegroundColor = ColorSubtle;
                System.Console.ResetColor();

                var key = System.Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selected = (selected - 1 + items.Length) % items.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        selected = (selected + 1) % items.Length;
                        break;
                    case ConsoleKey.Enter:
                        System.Console.CursorVisible = true;
                        System.Console.WriteLine();
                        return selected + 1; // 1-based to match existing switch cases
                }
            }
        }

       
    }
}