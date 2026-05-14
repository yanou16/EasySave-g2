using EasySave.Views.Console.Bootstrap;
using EasySave.Views.Console.Cli;
using EasySave.Models;
using EasySave.ViewModels.Services;

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

            try
            {
                if (indexes.Count == 1)
                    _context.BackupViewModel.ExecuteJob(indexes[0]);
                else
                    RunSelectedJobs(indexes).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"{_context.LanguageService.Get("ExecutionFailed")}: {ex.Message}");
                return 1;
            }

            foreach (int index in indexes)
            {
                System.Console.WriteLine(
                    $"{_context.LanguageService.Get("CliExecutedPrefix")} {_context.BackupViewModel.Jobs[index].Name}");
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
                int choice = ConsoleMenu.PrintMainMenu(_context.LanguageService);

                System.Console.WriteLine();

                switch (choice)
                {
                    case 1: ConsoleMenu.PrintJobs(_context.LanguageService, _context.BackupViewModel.Jobs); break;
                    case 2: AddJob();          break;
                    case 3: RemoveJob();       break;
                    case 4: ExecuteJob();      break;
                    case 5: ExecuteAllJobs();  break;
                    case 6: ChangeSettings();  break;
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
            // v1.1 keeps the 5-job limit (v2.0 GUI is unlimited).
            if (_context.BackupViewModel.Jobs.Count >= 5)
            {
                System.Console.WriteLine(_context.LanguageService.Get("MaxJobsReached"));
                return;
            }

            string name     = ConsolePrompts.Prompt(_context.LanguageService, "PromptName");
            string source   = ConsolePrompts.Prompt(_context.LanguageService, "PromptSource");
            string target   = ConsolePrompts.Prompt(_context.LanguageService, "PromptTarget");
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

            int index = ConsolePrompts.PromptJobNumber(_context.LanguageService, _context.BackupViewModel.Jobs); var result = _context.BackupViewModel.RemoveJob(index);
            System.Console.WriteLine(result.Message);
        }

        private void ExecuteJob()
        {
            if (_context.BackupViewModel.Jobs.Count == 0)
            {
                System.Console.WriteLine(_context.LanguageService.Get("NoJobs"));
                return;
            }

            var indexes = ConsolePrompts.PromptMultiJobSelect(
                _context.LanguageService,
                _context.BackupViewModel.Jobs);

            try
            {
                foreach (int index in indexes)
                    _context.BackupViewModel.ExecuteJob(index);

                System.Console.WriteLine(_context.LanguageService.Get("ExecutionCompleted"));
            }
            catch (BusinessSoftwareDetectedException ex)
            {
                System.Console.WriteLine($"Backup stopped: business software detected ({ex.SoftwareName})");
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
                RunExecutionDashboard(Enumerable.Range(0, _context.BackupViewModel.Jobs.Count).ToArray());
                System.Console.WriteLine(_context.LanguageService.Get("ExecutionCompleted"));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"{_context.LanguageService.Get("ExecutionFailed")}: {ex.Message}");
            }
        }

        private void ChangeSettings()
        {
            // v1.1 exposes only log format (JSON/XML). CryptoSoft and business software are v2.0+ features.
            var settings = _context.BackupViewModel.GetSettings();
            System.Console.WriteLine($"{_context.LanguageService.Get("CurrentLogFormat")}: {settings.LogFormat}");
            string format = ConsolePrompts.Prompt(_context.LanguageService, "LogFormatPrompt").ToUpperInvariant();
            var formatResult = _context.BackupViewModel.ChangeLogFormat(format);
            System.Console.WriteLine(formatResult.Message);
        }

        private void RunExecutionDashboard(IReadOnlyList<int> indexes)
        {
            RunExecutionDashboardAsync(indexes).GetAwaiter().GetResult();
        }

        private async Task RunExecutionDashboardAsync(IReadOnlyList<int> indexes)
        {
            int selected = 0;
            var snapshots = indexes.ToDictionary(
                index => _context.BackupViewModel.Jobs[index].Id,
                index => new JobSnapshot(
                    _context.BackupViewModel.Jobs[index].Name,
                    0,
                    _context.BackupViewModel.GetJobStatus(index)));

            void OnProgress(object? sender, BackupProgressChangedEventArgs e)
            {
                if (snapshots.TryGetValue(e.JobId, out var snapshot))
                    snapshots[e.JobId] = snapshot with { Progress = e.Progress, Status = e.Status };
            }

            void OnStatus(object? sender, BackupStatusChangedEventArgs e)
            {
                if (snapshots.TryGetValue(e.JobId, out var snapshot))
                    snapshots[e.JobId] = snapshot with { Status = e.Status };
            }

            _context.BackupViewModel.ProgressChanged += OnProgress;
            _context.BackupViewModel.StatusChanged += OnStatus;

            try
            {
                Task runTask = RunSelectedJobs(indexes);

                while (!runTask.IsCompleted)
                {
                    DrawExecutionDashboard(indexes, snapshots, selected);
                    HandleDashboardInput(indexes, ref selected);
                    await Task.Delay(200);
                }

                await runTask;
                DrawExecutionDashboard(indexes, snapshots, selected);
            }
            finally
            {
                _context.BackupViewModel.ProgressChanged -= OnProgress;
                _context.BackupViewModel.StatusChanged -= OnStatus;
                System.Console.CursorVisible = true;
            }
        }

        private Task RunSelectedJobs(IReadOnlyList<int> indexes)
        {
            var tasks = indexes.Select(_context.BackupViewModel.StartJob).ToArray();
            return Task.WhenAll(tasks);
        }

        private void DrawExecutionDashboard(
            IReadOnlyList<int> indexes,
            Dictionary<int, JobSnapshot> snapshots,
            int selected)
        {
            System.Console.Clear();
            ConsoleMenu.PrintHeader(_context.LanguageService, _context.AppDataDirectory);

            System.Console.ForegroundColor = ConsoleColor.DarkCyan;
            System.Console.WriteLine($"  {_context.LanguageService.Get("ExecutionDashboardTitle")}");
            System.Console.ResetColor();
            System.Console.WriteLine();

            for (int i = 0; i < indexes.Count; i++)
            {
                BackupJob job = _context.BackupViewModel.Jobs[indexes[i]];
                JobSnapshot snapshot = snapshots[job.Id];
                string marker = i == selected ? ">" : " ";
                string progressBar = BuildProgressBar(snapshot.Progress, 24);

                System.Console.ForegroundColor = i == selected ? ConsoleColor.Cyan : ConsoleColor.White;
                System.Console.Write($"  {marker} [{job.Id}] {snapshot.Name,-20}");
                System.Console.ResetColor();
                System.Console.Write($" {snapshot.Status,-8} {progressBar} {snapshot.Progress,6:0.0}%");
                System.Console.WriteLine();
            }

            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($"  {_context.LanguageService.Get("ExecutionDashboardHelp")}");
            System.Console.ResetColor();
            System.Console.CursorVisible = false;
        }

        private void HandleDashboardInput(IReadOnlyList<int> indexes, ref int selected)
        {
            while (System.Console.KeyAvailable)
            {
                var key = System.Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selected = (selected - 1 + indexes.Count) % indexes.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selected = (selected + 1) % indexes.Count;
                        break;
                    case ConsoleKey.P:
                        _context.BackupViewModel.PauseJob(indexes[selected]);
                        break;
                    case ConsoleKey.R:
                        _context.BackupViewModel.ResumeJob(indexes[selected]);
                        break;
                    case ConsoleKey.S:
                        _context.BackupViewModel.StopJob(indexes[selected]);
                        break;
                    case ConsoleKey.A:
                        _context.BackupViewModel.PauseAll();
                        break;
                    case ConsoleKey.T:
                        _context.BackupViewModel.ResumeAll();
                        break;
                    case ConsoleKey.X:
                        _context.BackupViewModel.StopAll();
                        break;
                }
            }
        }

        private static string BuildProgressBar(double progress, int width)
        {
            int complete = Math.Clamp((int)Math.Round(progress / 100 * width), 0, width);
            return $"[{new string('#', complete)}{new string('-', width - complete)}]";
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

        
        private sealed record JobSnapshot(string Name, double Progress, BackupRuntimeStatus Status);
    }
}
