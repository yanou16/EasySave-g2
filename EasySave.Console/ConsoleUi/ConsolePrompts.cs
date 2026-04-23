using EasySave.Models;
using EasySave.ViewModels.Services;

namespace EasySave.Console.ConsoleUi
{
    public static class ConsolePrompts
    {
        public static string Prompt(LanguageService language, string key)
        {
            // Keep prompt formatting centralized so menu actions stay focused on flow.
            System.Console.Write($"{language.Get(key)} ");
            return System.Console.ReadLine()?.Trim() ?? string.Empty;
        }

        public static int PromptMenuChoice(LanguageService language)
        {
            while (true)
            {
                string input = Prompt(language, "PromptMenuChoice");
                if (int.TryParse(input, out int choice))
                {
                    return choice;
                }

                System.Console.WriteLine(language.Get("InvalidNumber"));
            }
        }

        public static int PromptJobNumber(LanguageService language, int jobCount)
        {
            while (true)
            {
                string input = Prompt(language, "PromptJobNumber");
                if (int.TryParse(input, out int selected) && selected >= 1 && selected <= jobCount)
                {
                    return selected - 1;
                }

                System.Console.WriteLine(language.Get("InvalidJobSelection"));
            }
        }

        public static BackupType PromptBackupType(LanguageService language)
        {
            while (true)
            {
                // Accept both numeric and text input to keep the console UX forgiving.
                string input = Prompt(language, "PromptBackupType").ToLowerInvariant();
                if (input is "1" or "full")
                {
                    return BackupType.Full;
                }

                if (input is "2" or "differential")
                {
                    return BackupType.Differential;
                }

                System.Console.WriteLine(language.Get("InvalidBackupType"));
            }
        }

        public static string PromptLanguage()
        {
            System.Console.Write("Language / Langue (en/fr): ");
            string selected = System.Console.ReadLine()?.Trim().ToLowerInvariant() ?? "en";
            return selected == "fr" ? "fr" : "en";
        }

        public static void Pause(LanguageService language)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(language.Get("PressEnterToContinue"));
            System.Console.ReadLine();
        }
    }
}
