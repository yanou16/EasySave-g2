using EasySave.Console.Bootstrap;
using EasySave.Console.Cli;
using EasySave.Models;

namespace EasySave.Console.ConsoleUi
{
    public class ConsoleApp
    {
        private readonly ConsoleAppContext _context;

        public ConsoleApp(ConsoleAppContext context)
        {
            _context = context;
        }

        public int Run(CommandLineParseResult parseResult)
        {
            if (!parseResult.IsValid)
            {
                LoadLanguage(interactive: false);
                System.Console.WriteLine(parseResult.ErrorMessage);
                return 1;
            }

            if (!parseResult.IsInteractive)
            {
                LoadLanguage(interactive: false);
                return RunCommandLineMode(parseResult.JobIndexes);
            }

            LoadLanguage(interactive: true);
            RunInteractiveMode();
            return 0;
        }

        private int RunCommandLineMode(IReadOnlyList<int> indexes)
        {
            // CLI mode should be scriptable: validate everything up front, then execute sequentially.
            if (indexes.Count == 0)
            {
                System.Console.WriteLine(_context.LanguageService.Get("NoCliSelection"));
                return 1;
            }

            foreach (int index in indexes)
            {
                if (index < 0 || index >= _context.BackupViewModel.Jobs.Count)
                {
                    System.Console.WriteLine(_context.LanguageService.Get("InvalidJobSelection"));
                    return 1;
                }
            }

            foreach (int index in indexes)
            {
                try
                {
                    _context.BackupViewModel.ExecuteJob(index);
                    System.Console.WriteLine(
                        $"{_context.LanguageService.Get("CliExecutedPrefix")} {_context.BackupViewModel.Jobs[index].Name}");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"{_context.LanguageService.Get("ExecutionFailed")}: {ex.Message}");
                    return 1;
                }
            }

            return 0;
        }

        private void RunInteractiveMode()
        {
            bool running = true;

            // Interactive mode is a simple console loop over the shared view-model actions.
            while (running)
            {
                System.Console.Clear();
                ConsoleMenu.PrintHeader(_context.LanguageService, _context.AppDataDirectory);
                ConsoleMenu.PrintJobs(_context.LanguageService, _context.BackupViewModel.Jobs);
                ConsoleMenu.PrintMainMenu(_context.LanguageService);

                int choice = ConsolePrompts.PromptMenuChoice(_context.LanguageService);
                System.Console.WriteLine();

                switch (choice)
                {
                    case 1:
                        ConsoleMenu.PrintJobs(_context.LanguageService, _context.BackupViewModel.Jobs);
                        break;
                    case 2:
                        AddJob();
                        break;
                    case 3:
                        RemoveJob();
                        break;
                    case 4:
                        ExecuteJob();
                        break;
                    case 5:
                        ExecuteAllJobs();
                        break;
                    case 6:
                        ChangeLogFormat();
                        break;
                    case 7:
                        running = false;
                        break;
                    default:
                        System.Console.WriteLine(_context.LanguageService.Get("InvalidMenuChoice"));
                        break;
                }

                ConsolePrompts.Pause(_context.LanguageService);
            }
        }

        private void AddJob()
        {
            // Collect raw console input here and keep validation/business rules inside the shared layer.
            string name = ConsolePrompts.Prompt(_context.LanguageService, "PromptName");
            string source = ConsolePrompts.Prompt(_context.LanguageService, "PromptSource");
            string target = ConsolePrompts.Prompt(_context.LanguageService, "PromptTarget");
            BackupType type = ConsolePrompts.PromptBackupType(_context.LanguageService);

            var result = _context.BackupViewModel.AddJob(name, source, target, type);
            System.Console.WriteLine(result.Message);
        }

        private void RemoveJob()
        {
            if (_context.BackupViewModel.Jobs.Count == 0)
            {
                System.Console.WriteLine(_context.LanguageService.Get("NoJobs"));
                return;
            }

            int index = ConsolePrompts.PromptJobNumber(_context.LanguageService, _context.BackupViewModel.Jobs.Count);
            var result = _context.BackupViewModel.RemoveJob(index);
            System.Console.WriteLine(result.Message);
        }

        private void ExecuteJob()
        {
            if (_context.BackupViewModel.Jobs.Count == 0)
            {
                System.Console.WriteLine(_context.LanguageService.Get("NoJobs"));
                return;
            }

            int index = ConsolePrompts.PromptJobNumber(_context.LanguageService, _context.BackupViewModel.Jobs.Count);

            try
            {
                _context.BackupViewModel.ExecuteJob(index);
                System.Console.WriteLine(_context.LanguageService.Get("ExecutionCompleted"));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"{_context.LanguageService.Get("ExecutionFailed")}: {ex.Message}");
            }
        }

        private void ExecuteAllJobs()
        {
            if (_context.BackupViewModel.Jobs.Count == 0)
            {
                System.Console.WriteLine(_context.LanguageService.Get("NoJobs"));
                return;
            }

            try
            {
                _context.BackupViewModel.ExecuteAllJobs();
                System.Console.WriteLine(_context.LanguageService.Get("ExecutionCompleted"));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"{_context.LanguageService.Get("ExecutionFailed")}: {ex.Message}");
            }
        }

        private void LoadLanguage(bool interactive)
        {
            string language = interactive
                ? ConsolePrompts.PromptLanguage()
                : GetDefaultLanguage();

            string filePath = Path.Combine(_context.ResourcesDirectory, $"{language}.json");
            if (File.Exists(filePath))
            {
                _context.LanguageService.Load(_context.ResourcesDirectory, language);
                return;
            }

            // Fall back to embedded resources (single-file publish scenario).
            var assembly = typeof(ConsoleApp).Assembly;
            string resourceName = $"EasySave.Console.Resources.{language}.json";
            using var stream = assembly.GetManifestResourceStream(resourceName)
                              ?? assembly.GetManifestResourceStream("EasySave.Console.Resources.en.json");
            if (stream is null)
                return;
            using var reader = new System.IO.StreamReader(stream);
            _context.LanguageService.LoadFromJson(reader.ReadToEnd());
        }

        private static string GetDefaultLanguage()
        {
            string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return culture == "fr" ? "fr" : "en";
        }

        private void ChangeLogFormat()
        {
            System.Console.WriteLine(_context.LanguageService.Get("CurrentLogFormat") +
                ": " + _context.LanguageService.Get("LogFormatPrompt"));
            System.Console.WriteLine("1. JSON");
            System.Console.WriteLine("2. XML");

            string input = ConsolePrompts.Prompt(_context.LanguageService, "PromptChoice");
            string format = input.Trim() == "2" ? "XML" : "JSON";

            var result = _context.BackupViewModel.ChangeLogFormat(format);
            System.Console.WriteLine(result.Message);
        }
    }
}
