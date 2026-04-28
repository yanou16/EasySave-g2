using EasySave.Views.Console.Bootstrap;
using EasySave.Views.Console.Cli;
using EasySave.Models;

namespace EasySave.Views.Console.ConsoleUi
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

        // ── Command-line mode ────────────────────────────────────────────────

        private int RunCommandLineMode(IReadOnlyList<int> indexes)
        {
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

        // ── Interactive mode ─────────────────────────────────────────────────

        private void RunInteractiveMode()
        {
            bool running = true;

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
                    case 1: ConsoleMenu.PrintJobs(_context.LanguageService, _context.BackupViewModel.Jobs); break;
                    case 2: AddJob();          break;
                    case 3: RemoveJob();       break;
                    case 4: ExecuteJob();      break;
                    case 5: ExecuteAllJobs();  break;
                    case 6: ChangeLogFormat(); break;
                    case 7: running = false;   continue;
                    default:
                        System.Console.WriteLine(_context.LanguageService.Get("InvalidMenuChoice"));
                        break;
                }

                ConsolePrompts.Pause(_context.LanguageService);
            }
        }

        // ── Menu actions ─────────────────────────────────────────────────────

        private void AddJob()
        {
            string name   = ConsolePrompts.Prompt(_context.LanguageService, "PromptName");
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

            int index  = ConsolePrompts.PromptJobNumber(_context.LanguageService, _context.BackupViewModel.Jobs.Count);
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

        private void ChangeLogFormat()
        {
            System.Console.WriteLine($"{_context.LanguageService.Get("CurrentLogFormat")}: JSON / XML");
            string format = ConsolePrompts.Prompt(_context.LanguageService, "LogFormatPrompt").ToUpperInvariant();
            var result = _context.BackupViewModel.ChangeLogFormat(format);
            System.Console.WriteLine(result.Message);
        }

        // ── Language loading ─────────────────────────────────────────────────

        private void LoadLanguage(bool interactive)
        {
            string language = interactive ? ConsolePrompts.PromptLanguage() : GetDefaultLanguage();

            string filePath = Path.Combine(_context.ResourcesDirectory, $"{language}.json");
            if (File.Exists(filePath))
            {
                _context.LanguageService.Load(_context.ResourcesDirectory, language);
                return;
            }

            // Fall back to embedded resources (single-file publish scenario).
            var assembly     = typeof(ConsoleApp).Assembly;
            string resName   = $"EasySave.Views.Console.Resources.{language}.json";
            using var stream = assembly.GetManifestResourceStream(resName)
                            ?? assembly.GetManifestResourceStream("EasySave.Views.Console.Resources.en.json");

            if (stream is null) return;

            using var reader = new StreamReader(stream);
            _context.LanguageService.LoadFromJson(reader.ReadToEnd());
        }

        private static string GetDefaultLanguage()
        {
            string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return culture == "fr" ? "fr" : "en";
        }
    }
}
