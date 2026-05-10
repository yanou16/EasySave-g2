using EasySave.Models;
using EasySave.ViewModels.Services;

namespace EasySave.Views.Console.ConsoleUi
{
    public static class ConsolePrompts
    {
        private static readonly ConsoleColor ColorPrompt = ConsoleColor.Cyan;
        private static readonly ConsoleColor ColorError = ConsoleColor.Red;
        private static readonly ConsoleColor ColorSuccess = ConsoleColor.Green;
        private static readonly ConsoleColor ColorSubtle = ConsoleColor.DarkGray;

        public static string Prompt(LanguageService language, string key)
        {
            System.Console.ForegroundColor = ColorPrompt;
            System.Console.Write($"  ❯ {language.Get(key)} ");
            System.Console.ResetColor();
            return System.Console.ReadLine()?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Arrow-key selector for a list of string options. Returns selected index (0-based).
        /// </summary>
        public static int PromptArrowSelect(string[] options)
        {
            int selected = 0;
            int top = System.Console.CursorTop;
            System.Console.CursorVisible = false;

            while (true)
            {
                System.Console.SetCursorPosition(0, top);
                for (int i = 0; i < options.Length; i++)
                {
                    if (i == selected)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Black;
                        System.Console.BackgroundColor = ConsoleColor.Cyan;
                        System.Console.WriteLine($"  › {options[i].PadRight(30)}");
                    }
                    else
                    {
                        System.Console.ForegroundColor = ConsoleColor.White;
                        System.Console.BackgroundColor = ConsoleColor.Black;
                        System.Console.WriteLine($"    {options[i].PadRight(30)}");
                    }
                    System.Console.ResetColor();
                }

                var key = System.Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selected = (selected - 1 + options.Length) % options.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        selected = (selected + 1) % options.Length;
                        break;
                    case ConsoleKey.Enter:
                        System.Console.CursorVisible = true;
                        System.Console.WriteLine();
                        return selected;
                }
            }
        }

        public static int PromptMenuChoice(LanguageService language)
        {
            // Kept for CLI compatibility — not used in interactive mode anymore
            while (true)
            {
                string input = Prompt(language, "PromptMenuChoice");
                if (int.TryParse(input, out int choice)) return choice;
                PrintError(language.Get("InvalidNumber"));
            }
        }

        public static int PromptJobNumber(LanguageService language, int jobCount)
        {
            string[] options = Enumerable.Range(1, jobCount)
                                         .Select(i => $"Job {i}")
                                         .ToArray();

            System.Console.ForegroundColor = ColorPrompt;
            System.Console.WriteLine($"  {language.Get("PromptJobNumber")}");
            System.Console.ResetColor();

            return PromptArrowSelect(options); // returns 0-based index
        }

        public static BackupType PromptBackupType(LanguageService language)
        {
            System.Console.ForegroundColor = ColorPrompt;
            System.Console.WriteLine($"  {language.Get("PromptBackupType")}");
            System.Console.ResetColor();

            string[] options = { "Full", "Differential" };
            int selected = PromptArrowSelect(options);
            return selected == 0 ? BackupType.Full : BackupType.Differential;
        }

        public static string PromptLanguage()
        {
            System.Console.Clear();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine(@"
  ___         ___
 | __|__ _ __|_ _|___  __ ___ _____
 | _|/ _` (_-< | |(_-</ _` \ V / -_)
 |___\__,_/__/ |_|/__/\__,_|\_/\___|");
            System.Console.ResetColor();
            System.Console.WriteLine();

            string[] options = { "English", "Français" };
            int selected = PromptArrowSelect(options);
            return selected == 0 ? "en" : "fr";
        }

        public static void Pause(LanguageService language)
        {
            System.Console.WriteLine();
            System.Console.ForegroundColor = ColorSubtle;
            System.Console.WriteLine($"  {language.Get("PressEnterToContinue")}");
            System.Console.ResetColor();
            System.Console.ReadLine();
        }

        public static void PrintError(string message)
        {
            System.Console.ForegroundColor = ColorError;
            System.Console.WriteLine($"  ✖ {message}");
            System.Console.ResetColor();
        }

        public static void PrintSuccess(string message)
        {
            System.Console.ForegroundColor = ColorSuccess;
            System.Console.WriteLine($"  ✔ {message}");
            System.Console.ResetColor();
        }
    }
}