using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using EasyLog;
using EasyLog.Models;
using EasySave.Models;
using EasySave.Services;
using EasySave.ViewModels;

namespace EasySave
{
    class Program
    {
        private static readonly LanguageService Lang = new();
        private static string _resourcesDir = string.Empty;

        static void Main(string[] args)
        {
            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
            _resourcesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            string langCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? "fr" : "en";
            Lang.Load(_resourcesDir, langCode);

            var states = new List<BackupStateEntry>();
            var logger = new Logger(Path.Combine(appDataDir, "Logs"));
            var stateManager = new StateManager(appDataDir);
            var configService = new ConfigService(appDataDir);
            var backupService = new BackupService(logger, stateManager, states);
            var viewModel = new BackupViewModel(configService, backupService, Lang, states);

            if (args.Length > 0)
            {
                RunFromCommandLine(args[0], viewModel);
                return;
            }

            RunInteractiveMenu(viewModel);
        }

        static void RunFromCommandLine(string arg, BackupViewModel viewModel)
        {
            List<int> indices = ParseJobIndices(arg, viewModel.Jobs.Count);

            if (indices.Count == 0)
            {
                Console.WriteLine(Lang.Get("InvalidArgument"));
                return;
            }

            foreach (int index in indices)
            {
                Console.Write($"{Lang.Get("ExecutingJob")} \"{viewModel.Jobs[index].Name}\"... ");
                try
                {
                    viewModel.ExecuteJob(index);
                    Console.WriteLine(Lang.Get("Done"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{Lang.Get("JobError")}: {ex.Message}");
                }
            }
        }

        static List<int> ParseJobIndices(string arg, int jobCount)
        {
            var indices = new List<int>();

            if (arg.Contains('-'))
            {
                string[] parts = arg.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out int start) &&
                    int.TryParse(parts[1].Trim(), out int end))
                {
                    for (int i = Math.Max(1, start); i <= Math.Min(end, jobCount); i++)
                        indices.Add(i - 1);
                }
            }
            else if (arg.Contains(';'))
            {
                foreach (string part in arg.Split(';'))
                {
                    if (int.TryParse(part.Trim(), out int num) && num >= 1 && num <= jobCount)
                        indices.Add(num - 1);
                }
            }
            else if (int.TryParse(arg.Trim(), out int single) && single >= 1 && single <= jobCount)
            {
                indices.Add(single - 1);
            }

            return indices;
        }

        static void RunInteractiveMenu(BackupViewModel viewModel)
        {
            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine("╔══════════════════════════════╗");
                Console.WriteLine("║        EasySave  v1.0        ║");
                Console.WriteLine("╚══════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine($"  1. {Lang.Get("MenuListJobs")}");
                Console.WriteLine($"  2. {Lang.Get("MenuAddJob")}");
                Console.WriteLine($"  3. {Lang.Get("MenuRemoveJob")}");
                Console.WriteLine($"  4. {Lang.Get("MenuExecuteJob")}");
                Console.WriteLine($"  5. {Lang.Get("MenuExecuteAll")}");
                Console.WriteLine($"  6. {Lang.Get("MenuChangeLanguage")}");
                Console.WriteLine($"  0. {Lang.Get("MenuExit")}");
                Console.WriteLine();
                Console.Write($"{Lang.Get("MenuChoice")}: ");

                switch (Console.ReadLine()?.Trim())
                {
                    case "1": ShowJobs(viewModel); break;
                    case "2": AddJob(viewModel); break;
                    case "3": RemoveJob(viewModel); break;
                    case "4": ExecuteJob(viewModel); break;
                    case "5": ExecuteAllJobs(viewModel); break;
                    case "6": ChangeLanguage(); break;
                    case "0": running = false; break;
                    default:
                        Console.WriteLine(Lang.Get("InvalidChoice"));
                        WaitForKey();
                        break;
                }
            }

            Console.WriteLine(Lang.Get("Goodbye"));
        }

        static void ShowJobs(BackupViewModel viewModel)
        {
            Console.Clear();
            Console.WriteLine($"=== {Lang.Get("MenuListJobs")} ===\n");

            if (viewModel.Jobs.Count == 0)
            {
                Console.WriteLine(Lang.Get("NoJobsDefined"));
            }
            else
            {
                foreach (var job in viewModel.Jobs)
                {
                    Console.WriteLine($"  [{job.Id}] {job.Name}");
                    Console.WriteLine($"      {Lang.Get("Source")}: {job.SourceDirectory}");
                    Console.WriteLine($"      {Lang.Get("Target")}: {job.TargetDirectory}");
                    Console.WriteLine($"      {Lang.Get("Type")}: {job.Type}");
                    Console.WriteLine();
                }
            }

            WaitForKey();
        }

        static void AddJob(BackupViewModel viewModel)
        {
            Console.Clear();
            Console.WriteLine($"=== {Lang.Get("MenuAddJob")} ===\n");

            Console.Write($"{Lang.Get("EnterJobName")}: ");
            string name = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write($"{Lang.Get("EnterSource")}: ");
            string source = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write($"{Lang.Get("EnterTarget")}: ");
            string target = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.WriteLine($"{Lang.Get("SelectType")}:");
            Console.WriteLine($"  1. {Lang.Get("TypeFull")}");
            Console.WriteLine($"  2. {Lang.Get("TypeDifferential")}");
            Console.Write($"{Lang.Get("MenuChoice")}: ");
            BackupType type = Console.ReadLine()?.Trim() == "2" ? BackupType.Differential : BackupType.Full;

            var (_, message) = viewModel.AddJob(name, source, target, type);
            Console.WriteLine($"\n{message}");
            WaitForKey();
        }

        static void RemoveJob(BackupViewModel viewModel)
        {
            Console.Clear();
            Console.WriteLine($"=== {Lang.Get("MenuRemoveJob")} ===\n");
            PrintJobList(viewModel);

            if (viewModel.Jobs.Count == 0) { WaitForKey(); return; }

            Console.Write($"{Lang.Get("EnterJobNumber")}: ");
            if (int.TryParse(Console.ReadLine(), out int num))
            {
                var (_, message) = viewModel.RemoveJob(num - 1);
                Console.WriteLine($"\n{message}");
            }
            else
            {
                Console.WriteLine(Lang.Get("InvalidInput"));
            }

            WaitForKey();
        }

        static void ExecuteJob(BackupViewModel viewModel)
        {
            Console.Clear();
            Console.WriteLine($"=== {Lang.Get("MenuExecuteJob")} ===\n");
            PrintJobList(viewModel);

            if (viewModel.Jobs.Count == 0) { WaitForKey(); return; }

            Console.Write($"{Lang.Get("EnterJobNumber")}: ");
            if (int.TryParse(Console.ReadLine(), out int num) && num >= 1 && num <= viewModel.Jobs.Count)
            {
                Console.Write($"\n{Lang.Get("ExecutingJob")} \"{viewModel.Jobs[num - 1].Name}\"... ");
                try
                {
                    viewModel.ExecuteJob(num - 1);
                    Console.WriteLine(Lang.Get("JobCompleted"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{Lang.Get("JobError")}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(Lang.Get("InvalidInput"));
            }

            WaitForKey();
        }

        static void ExecuteAllJobs(BackupViewModel viewModel)
        {
            Console.Clear();
            Console.WriteLine($"=== {Lang.Get("MenuExecuteAll")} ===\n");

            if (viewModel.Jobs.Count == 0)
            {
                Console.WriteLine(Lang.Get("NoJobsDefined"));
                WaitForKey();
                return;
            }

            for (int i = 0; i < viewModel.Jobs.Count; i++)
            {
                Console.Write($"{Lang.Get("ExecutingJob")} \"{viewModel.Jobs[i].Name}\"... ");
                try
                {
                    viewModel.ExecuteJob(i);
                    Console.WriteLine(Lang.Get("Done"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{Lang.Get("JobError")}: {ex.Message}");
                }
            }

            WaitForKey();
        }

        static void ChangeLanguage()
        {
            Console.Clear();
            Console.WriteLine($"=== {Lang.Get("MenuChangeLanguage")} ===\n");
            Console.WriteLine("  1. English");
            Console.WriteLine("  2. Français");
            Console.Write($"\n{Lang.Get("MenuChoice")}: ");

            string langCode = Console.ReadLine()?.Trim() == "2" ? "fr" : "en";
            Lang.Load(_resourcesDir, langCode);
        }

        static void PrintJobList(BackupViewModel viewModel)
        {
            if (viewModel.Jobs.Count == 0)
            {
                Console.WriteLine(Lang.Get("NoJobsDefined"));
                return;
            }

            foreach (var job in viewModel.Jobs)
                Console.WriteLine($"  [{job.Id}] {job.Name}");

            Console.WriteLine();
        }

        static void WaitForKey()
        {
            Console.WriteLine();
            Console.Write(Lang.Get("PressAnyKey"));
            Console.ReadKey();
        }
    }
}
