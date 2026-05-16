using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using EasySave.Models;
using EasySave.ViewModels;
using EasySave.ViewModels.Services;
using EasySave.Views.WPF.Controls;

namespace EasySave.Views.WPF
{
    public partial class MainWindow : Window
    {
        private readonly BackupViewModel _viewModel;
        private readonly LanguageService _language;
        private bool _languageComboReady = false;
        private string _currentPage = "Dashboard";

        private readonly ObservableCollection<JobRowViewModel> _rows = new();
        private readonly CollectionViewSource _rowsView = new();

        // ── Sidebar nav colours ───────────────────────────────────────────────
        private static readonly Brush NavActiveBg   = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
        private static readonly Brush NavInactiveBg = Brushes.Transparent;
        private static readonly Brush NavActiveFg   = Brushes.White;
        private static readonly Brush NavInactiveFg = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));

        public MainWindow(BackupViewModel viewModel, LanguageService language)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _language  = language;

            string savedLang = _viewModel.GetSettings().Language;
            LoadLanguageFromEmbeddedResource(savedLang);

            _viewModel.ProgressChanged += OnProgressChanged;
            _viewModel.StatusChanged   += OnStatusChanged;

            _rowsView.Source = _rows;
            _rowsView.Filter += OnRowsFilter;
            JobsGrid.ItemsSource    = _rowsView.View;
            DashJobsGrid.ItemsSource = _rows;

            RegisterShortcuts();
            InitLanguageCombo();
            RefreshLanguage();
            RebuildRows();
            NavigateTo("Dashboard");
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private void NavigateTo(string page)
        {
            _currentPage = page;

            PageDashboard.Visibility = page == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
            PageJobs.Visibility      = page == "Jobs"      ? Visibility.Visible : Visibility.Collapsed;

            // Page header
            TxtPageTitle.Text = page switch
            {
                "Dashboard" => _language.Get("DashboardTitle"),
                "Jobs"      => _language.Get("MenuBackupJobs"),
                _           => page
            };
            TxtPageSubtitle.Text = page switch
            {
                "Dashboard" => _language.Get("SubtitleDashboard"),
                "Jobs"      => _language.Get("SubtitleJobs"),
                _           => string.Empty
            };

            UpdateNavState();

            if (page == "Dashboard") RefreshDashboard();
        }

        private void UpdateNavState()
        {
            SetNavActive(NavDashboard, _currentPage == "Dashboard");
            SetNavActive(NavJobs,      _currentPage == "Jobs");
            SetNavActive(NavLogs,      false);
            SetNavActive(NavSettings,  false);
        }

        private static void SetNavActive(Button btn, bool active)
        {
            btn.Background = active ? NavActiveBg : NavInactiveBg;
            btn.Foreground = active ? NavActiveFg : NavInactiveFg;
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e) => NavigateTo("Dashboard");
        private void NavJobs_Click(object sender, RoutedEventArgs e)      => NavigateTo("Jobs");

        private void NavLogs_Click(object sender, RoutedEventArgs e)
        {
            // Open the Logs folder in Windows Explorer
            string logsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ProSoft", "EasySave", "Logs");

            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            Process.Start("explorer.exe", logsPath);
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e) => OpenSettingsWindow();

        // ── Dashboard ─────────────────────────────────────────────────────────

        private void RefreshDashboard()
        {
            int total    = _rows.Count;
            int running  = _rows.Count(r => r.Status == BackupRuntimeStatus.Running);
            int finished = _rows.Count(r => r.Status == BackupRuntimeStatus.Finished);
            int errors   = _rows.Count(r => r.Status == BackupRuntimeStatus.Error || r.Status == BackupRuntimeStatus.Stopped);

            StatTotal.Text    = total.ToString();
            StatRunning.Text  = running.ToString();
            StatFinished.Text = finished.ToString();
            StatErrors.Text   = errors.ToString();

            DashEmptyState.Visibility = total == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── ViewModel event handlers ──────────────────────────────────────────

        private void OnProgressChanged(object? sender, BackupProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var row = _rows.FirstOrDefault(r => r.Id == e.JobId);
                if (row is not null)
                {
                    row.Progress = e.Progress;
                    row.Status   = e.Status;
                }
                UpdateToolbarState();
                if (_currentPage == "Dashboard") RefreshDashboard();
            });
        }

        private void OnStatusChanged(object? sender, BackupStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var row = _rows.FirstOrDefault(r => r.Id == e.JobId);
                if (row is not null) row.Status = e.Status;
                UpdateToolbarState();
                if (_currentPage == "Dashboard") RefreshDashboard();
            });
        }

        // ── Search ────────────────────────────────────────────────────────────

        private void OnRowsFilter(object sender, FilterEventArgs e)
        {
            string q = TxtSearch?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(q)) { e.Accepted = true; return; }
            if (e.Item is JobRowViewModel row)
                e.Accepted = row.Name.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _rowsView.View?.Refresh();

            int visible = _rowsView.View?.Cast<object>().Count() ?? 0;
            EmptyState.Visibility = visible == 0 && _rows.Count > 0
                ? Visibility.Visible
                : (_rows.Count == 0 ? Visibility.Visible : Visibility.Hidden);

            TxtEmptyState.Text = visible == 0 && _rows.Count > 0
                ? $"No results for \"{TxtSearch.Text}\""
                : _language.Get("NoJobs");
        }

        // ── Language ──────────────────────────────────────────────────────────

        private void InitLanguageCombo()
        {
            string saved = _viewModel.GetSettings().Language;
            CmbLanguage.SelectedIndex = saved == "fr" ? 1 : 0;
            _languageComboReady = true;
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_languageComboReady) return;
            string lang = ((ComboBoxItem)CmbLanguage.SelectedItem).Tag.ToString() ?? "en";
            LoadLanguageFromEmbeddedResource(lang);

            var settings = _viewModel.GetSettings();
            settings.Language = lang;
            _viewModel.SaveSettings(settings);
            RefreshLanguage();
        }

        internal static void LoadLanguageFromEmbeddedResource(LanguageService languageService, string lang)
        {
            var assembly   = typeof(MainWindow).Assembly;
            string resName = $"EasySave.Views.WPF.Resources.{lang}.json";
            using var stream = assembly.GetManifestResourceStream(resName)
                            ?? assembly.GetManifestResourceStream("EasySave.Views.WPF.Resources.en.json");

            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                languageService.LoadFromJson(reader.ReadToEnd());
            }
        }

        private void LoadLanguageFromEmbeddedResource(string lang) =>
            LoadLanguageFromEmbeddedResource(_language, lang);

        private void RefreshLanguage()
        {
            // ── Sidebar nav ───────────────────────────────────────────────────
            TxtNavMenuLabel.Text = _language.Get("NavMenuLabel");
            TxtNavDashboard.Text = _language.Get("NavDashboard");
            TxtNavJobs.Text      = _language.Get("MenuBackupJobs");
            TxtNavLogs.Text      = _language.Get("NavLogs");
            TxtNavSettings.Text  = _language.Get("MenuSettings");
            TxtLangLabel.Text    = _language.Get("LangLabel");

            // ── Dashboard ─────────────────────────────────────────────────────
            TxtStatTotalLabel.Text     = _language.Get("DashStatTotal");
            TxtStatRunningLabel.Text   = _language.Get("DashStatRunning");
            TxtStatCompletedLabel.Text = _language.Get("DashStatCompleted");
            TxtStatErrorsLabel.Text    = _language.Get("DashStatErrors");
            TxtDashOverviewTitle.Text  = _language.Get("DashJobsOverview");
            TxtBtnManageJobs.Text      = _language.Get("DashManageJobs");
            BtnDashCreate.Content      = _language.Get("DashCreateFirstJob");

            // ── Toolbar button labels ─────────────────────────────────────────
            TxtBtnAdd.Text        = _language.Get("MenuAddJob");
            TxtBtnEdit.Text       = _language.Get("MenuEditJob");
            TxtBtnRemove.Text     = _language.Get("MenuRemoveJob");
            TxtBtnExecute.Text    = _language.Get("MenuExecuteJob");
            TxtBtnExecuteAll.Text = _language.Get("MenuExecuteAllJobs");

            TxtGlobalControls.Text = _language.Get("GlobalControls");
            BtnPauseAll.Content    = _language.Get("MenuPauseAll");
            BtnResumeAll.Content   = _language.Get("MenuResumeAll");
            BtnStopAll.Content     = _language.Get("MenuStopAll");

            // ── DataGrid headers ──────────────────────────────────────────────
            ColId.Header       = "Id";
            ColName.Header     = _language.Get("LabelName");
            ColSource.Header   = _language.Get("LabelSource");
            ColTarget.Header   = _language.Get("LabelTarget");
            ColType.Header     = _language.Get("LabelType");
            ColStatus.Header   = _language.Get("LabelStatus");
            ColProgress.Header = _language.Get("LabelProgress");
            ColActions.Header  = _language.Get("LabelActions");

            TxtEmptyState.Text    = _language.Get("NoJobs");
            TxtBtnAddFirstJob.Text = _language.Get("BtnAddFirstJob");
            TxtDashEmpty.Text     = _language.Get("NoJobs");
            TxtSearch.ToolTip     = _language.Get("SearchPlaceholder");

            // ── Context menu ──────────────────────────────────────────────────
            MnuCtxExecute.Header   = _language.Get("MenuExecuteJob");
            MnuCtxEdit.Header      = _language.Get("MenuEditJob");
            MnuCtxDuplicate.Header = _language.Get("MenuDuplicate");
            MnuCtxRemove.Header    = _language.Get("MenuRemoveJob");

            // ── Page header (title + subtitle) ────────────────────────────────
            TxtPageTitle.Text = _currentPage switch
            {
                "Dashboard" => _language.Get("DashboardTitle"),
                "Jobs"      => _language.Get("MenuBackupJobs"),
                _           => _currentPage
            };
            TxtPageSubtitle.Text = _currentPage switch
            {
                "Dashboard" => _language.Get("SubtitleDashboard"),
                "Jobs"      => _language.Get("SubtitleJobs"),
                _           => string.Empty
            };

            SetStatus(_language.Get("StatusReady"));
            UpdateSelectedCount();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RebuildRows()
        {
            _rows.Clear();
            foreach (var job in _viewModel.Jobs)
                _rows.Add(new JobRowViewModel(job));

            int count = _rows.Count;
            TxtJobCount.Text = count.ToString();
            EmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Hidden;
            RefreshDashboard();
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusText.Text       = message;
            StatusText.Foreground = isError
                ? Brushes.Red
                : new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            StatusDot.Fill = isError ? Brushes.Red : new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        }

        private void UpdateSelectedCount()
        {
            int n = JobsGrid.SelectedItems.Count;
            TxtSelectedCount.Text = n == 0
                ? string.Empty
                : string.Format(_language.Get("SelectedCount"), n);
        }

        /// <summary>
        /// Disables Edit and Delete when at least one job is actively running or paused
        /// (prevents data corruption — test cases #11 and #12).
        /// </summary>
        private void UpdateToolbarState()
        {
            bool anyRunning     = _rows.Any(r => r.IsRunning);
            BtnEdit.IsEnabled   = !anyRunning;
            BtnRemove.IsEnabled = !anyRunning;
        }

        private void JobsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateSelectedCount();

        private List<int> GetSelectedIndexes()
        {
            var result = new List<int>();
            foreach (var item in JobsGrid.SelectedItems)
                if (item is JobRowViewModel row)
                {
                    int idx = FindIndexByJobId(row.Id);
                    if (idx >= 0) result.Add(idx);
                }
            return result;
        }

        private int FindIndexByJobId(int jobId)
        {
            for (int i = 0; i < _viewModel.Jobs.Count; i++)
                if (_viewModel.Jobs[i].Id == jobId) return i;
            return -1;
        }

        // ── Keyboard shortcuts ────────────────────────────────────────────────

        private void RegisterShortcuts()
        {
            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => BtnAdd_Click(this, new RoutedEventArgs())),
                new KeyGesture(Key.N, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => BtnEdit_Click(this, new RoutedEventArgs())),
                new KeyGesture(Key.E, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => TxtSearch.Focus()),
                new KeyGesture(Key.F, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => BtnExecute_Click(this, new RoutedEventArgs())),
                new KeyGesture(Key.F5)));
            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => BtnExecuteAll_Click(this, new RoutedEventArgs())),
                new KeyGesture(Key.F6)));
            InputBindings.Add(new KeyBinding(
                new RelayCommand(_ => OpenSettingsWindow()),
                new KeyGesture(Key.OemComma, ModifierKeys.Control)));
        }

        // ── DataGrid interaction ──────────────────────────────────────────────

        private void JobsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (JobsGrid.SelectedItem is JobRowViewModel)
                BtnEdit_Click(sender, new RoutedEventArgs());
        }

        private void JobsGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                BtnRemove_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // ── CRUD + Execute ────────────────────────────────────────────────────

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("Jobs"); // ensure we're on Jobs page
            var dialog = new AddJobWindow(_language) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var result = _viewModel.AddJob(dialog.JobName, dialog.Source, dialog.Target, dialog.BackupType);
            SetStatus(result.Message, !result.Success);

            if (result.Success) RebuildRows();
            else MessageBox.Show(result.Message, _language.Get("ErrorTitle"),
                                 MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (JobsGrid.SelectedItem is not JobRowViewModel row)
            {
                SetStatus(_language.Get("NoJobSelected"), isError: true);
                return;
            }

            int idx = FindIndexByJobId(row.Id);
            if (idx < 0) return;

            var existing = _viewModel.Jobs[idx];
            var dialog   = new AddJobWindow(_language, existing) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var result = _viewModel.UpdateJob(idx, dialog.JobName, dialog.Source, dialog.Target, dialog.BackupType);
            SetStatus(result.Message, !result.Success);

            if (result.Success) RebuildRows();
            else MessageBox.Show(result.Message, _language.Get("ErrorTitle"),
                                 MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var indexes = GetSelectedIndexes();
            if (indexes.Count == 0)
            {
                SetStatus(_language.Get("NoJobSelected"), isError: true);
                return;
            }

            var answer = MessageBox.Show(
                _language.Get("ConfirmRemoveMessage"),
                _language.Get("ConfirmRemoveTitle"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            int removed = indexes.Count;
            foreach (int idx in indexes.OrderByDescending(i => i))
                _viewModel.RemoveJob(idx);

            SetStatus(_language.Get("JobRemoved"));
            RebuildRows();
            Toasts.Push(_language.Get("ToastJobsRemoved"),
                        $"{removed} job(s)",
                        ToastHost.ToastKind.Info);
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            var indexes = GetSelectedIndexes();
            if (indexes.Count == 0)
            {
                SetStatus(_language.Get("NoJobSelected"), isError: true);
                return;
            }

            foreach (int idx in indexes)
            {
                var job = _viewModel.Jobs[idx];
                if (!Directory.Exists(job.SourceDirectory))
                {
                    string msg = $"{_language.Get("InvalidSourcePath")}\n\n{job.Name} → \"{job.SourceDirectory}\"";
                    SetStatus($"{_language.Get("InvalidSourcePath")}: {job.SourceDirectory}", isError: true);
                    MessageBox.Show(msg, _language.Get("ErrorTitle"),
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            SetStatus(_language.Get("StatusRunning"));

            _ = Task.Run(async () =>
            {
                try
                {
                    if (indexes.Count == 1) await _viewModel.StartJob(indexes[0]);
                    else                    await Task.WhenAll(indexes.Select(_viewModel.StartJob));

                    Dispatcher.Invoke(() =>
                    {
                        SetStatus(_language.Get("ExecutionCompleted"));
                        Toasts.Push(_language.Get("ToastBackupComplete"),
                                    _language.Get("ExecutionCompleted"),
                                    ToastHost.ToastKind.Success);
                    });
                }
                catch (BusinessSoftwareDetectedException ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus($"{_language.Get("BusinessSoftwareDetected")}: {ex.SoftwareName}", isError: true);
                        Toasts.Push(_language.Get("ToastBackupPaused"),
                                    $"{_language.Get("BusinessSoftwareDetected")}: {ex.SoftwareName}",
                                    ToastHost.ToastKind.Warning);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus($"{_language.Get("ExecutionFailed")}: {ex.Message}", isError: true);
                        Toasts.Push(_language.Get("ToastBackupFailed"),
                                    ex.Message, ToastHost.ToastKind.Error);
                    });
                }
            });
        }

        private void BtnExecuteAll_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Jobs.Count == 0)
            {
                SetStatus(_language.Get("NoJobs"), isError: true);
                return;
            }

            for (int i = 0; i < _viewModel.Jobs.Count; i++)
            {
                var job = _viewModel.Jobs[i];
                if (!Directory.Exists(job.SourceDirectory))
                {
                    string msg = $"{_language.Get("InvalidSourcePath")}\n\n{job.Name} → \"{job.SourceDirectory}\"";
                    SetStatus($"{_language.Get("InvalidSourcePath")}: {job.SourceDirectory}", isError: true);
                    MessageBox.Show(msg, _language.Get("ErrorTitle"),
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            SetStatus(_language.Get("StatusRunning"));

            _ = Task.Run(async () =>
            {
                try
                {
                    await _viewModel.StartAllJobs();
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus(_language.Get("ExecutionCompleted"));
                        Toasts.Push(_language.Get("ToastBackupComplete"),
                                    _language.Get("ExecutionCompleted"),
                                    ToastHost.ToastKind.Success);
                    });
                }
                catch (BusinessSoftwareDetectedException ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus($"{_language.Get("BusinessSoftwareDetected")}: {ex.SoftwareName}", isError: true);
                        Toasts.Push(_language.Get("ToastBackupPaused"),
                                    $"{_language.Get("BusinessSoftwareDetected")}: {ex.SoftwareName}",
                                    ToastHost.ToastKind.Warning);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus($"{_language.Get("ExecutionFailed")}: {ex.Message}", isError: true);
                        Toasts.Push(_language.Get("ToastBackupFailed"),
                                    ex.Message, ToastHost.ToastKind.Error);
                    });
                }
            });
        }

        // ── Settings ──────────────────────────────────────────────────────────

        private void OpenSettingsWindow()
        {
            var dialog = new SettingsWindow(_viewModel, _language) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            string lang = _viewModel.GetSettings().Language;
            _languageComboReady = false;
            CmbLanguage.SelectedIndex = lang == "fr" ? 1 : 0;
            _languageComboReady = true;

            RefreshLanguage();
            SetStatus(_language.Get("SettingsSaved"));
        }

        // ── Global controls ───────────────────────────────────────────────────

        private void BtnPauseAll_Click(object sender, RoutedEventArgs e)
        { _viewModel.PauseAll();  SetStatus(_language.Get("StatusPaused")); }

        private void BtnResumeAll_Click(object sender, RoutedEventArgs e)
        { _viewModel.ResumeAll(); SetStatus(_language.Get("StatusRunning")); }

        private void BtnStopAll_Click(object sender, RoutedEventArgs e)
        { _viewModel.StopAll();   SetStatus(_language.Get("StatusStopped")); }

        // ── Per-row controls ──────────────────────────────────────────────────

        private void BtnRowPause_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int jobId)
            {
                int idx = FindIndexByJobId(jobId);
                if (idx >= 0) _viewModel.PauseJob(idx);
            }
        }

        private void BtnRowResume_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int jobId)
            {
                int idx = FindIndexByJobId(jobId);
                if (idx >= 0) _viewModel.ResumeJob(idx);
            }
        }

        private void BtnRowStop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int jobId)
            {
                int idx = FindIndexByJobId(jobId);
                if (idx >= 0) _viewModel.StopJob(idx);
            }
        }

        // ── Context menu ──────────────────────────────────────────────────────

        private void MnuCtxExecute_Click(object sender, RoutedEventArgs e)
            => BtnExecute_Click(sender, e);

        private void MnuCtxEdit_Click(object sender, RoutedEventArgs e)
            => BtnEdit_Click(sender, e);

        private void MnuCtxRemove_Click(object sender, RoutedEventArgs e)
            => BtnRemove_Click(sender, e);

        private void MnuCtxDuplicate_Click(object sender, RoutedEventArgs e)
        {
            var indexes = GetSelectedIndexes();
            if (indexes.Count == 0)
            {
                SetStatus(_language.Get("NoJobSelected"), isError: true);
                return;
            }

            int duplicated = 0;
            foreach (int idx in indexes.OrderBy(i => i))
            {
                var result = _viewModel.DuplicateJob(idx);
                if (result.Success) duplicated++;
            }

            RebuildRows();
            SetStatus(_language.Get("JobDuplicated"));
            Toasts.Push(_language.Get("JobDuplicated"),
                        $"{duplicated} job(s)",
                        ToastHost.ToastKind.Success);
        }
    }

    // Minimal ICommand for keyboard shortcuts
    internal sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
