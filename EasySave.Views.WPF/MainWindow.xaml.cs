using System.Collections.ObjectModel;
using System.ComponentModel;
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

        /// <summary>Observable collection that drives the DataGrid — one row per job.</summary>
        private readonly ObservableCollection<JobRowViewModel> _rows = new();

        /// <summary>Wraps _rows with a live filter (search) and sort proxy.</summary>
        private readonly CollectionViewSource _rowsView = new();

        public MainWindow(BackupViewModel viewModel, LanguageService language)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _language  = language;

            // Load saved language from embedded resources before building the UI.
            string savedLang = _viewModel.GetSettings().Language;
            LoadLanguageFromEmbeddedResource(savedLang);

            // Wire up ViewModel events
            _viewModel.ProgressChanged += OnProgressChanged;
            _viewModel.StatusChanged   += OnStatusChanged;

            // Set up filterable / sortable view over _rows
            _rowsView.Source = _rows;
            _rowsView.Filter += OnRowsFilter;
            JobsGrid.ItemsSource = _rowsView.View;

            // Keyboard shortcuts (window-level)
            RegisterShortcuts();

            InitLanguageCombo();
            RefreshLanguage();
            RebuildRows();
            UpdateSelectedCount();
        }

        // ── Keyboard shortcuts ────────────────────────────────────────────────

        private void RegisterShortcuts()
        {
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => BtnAdd_Click(this, new RoutedEventArgs())),
                              new KeyGesture(Key.N, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => BtnEdit_Click(this, new RoutedEventArgs())),
                              new KeyGesture(Key.E, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => TxtSearch.Focus()),
                              new KeyGesture(Key.F, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => BtnExecute_Click(this, new RoutedEventArgs())),
                              new KeyGesture(Key.F5)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => BtnExecuteAll_Click(this, new RoutedEventArgs())),
                              new KeyGesture(Key.F6)));
            InputBindings.Add(new KeyBinding(new RelayCommand(_ => BtnSettings_Click(this, new RoutedEventArgs())),
                              new KeyGesture(Key.OemComma, ModifierKeys.Control)));
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
            });
        }

        private void OnStatusChanged(object? sender, BackupStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var row = _rows.FirstOrDefault(r => r.Id == e.JobId);
                if (row is not null) row.Status = e.Status;
            });
        }

        // ── Search filter ─────────────────────────────────────────────────────

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

            int visibleCount = _rowsView.View?.Cast<object>().Count() ?? 0;
            EmptyState.Visibility = visibleCount == 0 && _rows.Count > 0
                ? Visibility.Visible
                : (_rows.Count == 0 ? Visibility.Visible : Visibility.Hidden);

            if (_rows.Count > 0 && visibleCount == 0)
                TxtEmptyState.Text = $"No results for \"{TxtSearch.Text}\"";
            else if (_rows.Count == 0)
                TxtEmptyState.Text = _language.Get("NoJobs");
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
            BtnAdd.Content        = _language.Get("MenuAddJob");
            BtnEdit.Content       = _language.Get("MenuEditJob");
            BtnRemove.Content     = _language.Get("MenuRemoveJob");
            BtnExecute.Content    = _language.Get("MenuExecuteJob");
            BtnExecuteAll.Content = _language.Get("MenuExecuteAllJobs");
            BtnSettings.Content   = _language.Get("MenuSettings");
            BtnPauseAll.Content   = _language.Get("MenuPauseAll");
            BtnResumeAll.Content  = _language.Get("MenuResumeAll");
            BtnStopAll.Content    = _language.Get("MenuStopAll");

            ColId.Header       = "Id";
            ColName.Header     = _language.Get("LabelName");
            ColSource.Header   = _language.Get("LabelSource");
            ColTarget.Header   = _language.Get("LabelTarget");
            ColType.Header     = _language.Get("LabelType");
            ColStatus.Header   = _language.Get("LabelStatus");
            ColProgress.Header = _language.Get("LabelProgress");
            ColActions.Header  = _language.Get("LabelActions");

            TxtEmptyState.Text = _language.Get("NoJobs");

            // Search placeholder via Tag-based watermark trick (just sets a Tag we read in code-behind if empty).
            TxtSearch.ToolTip = _language.Get("SearchPlaceholder");

            // Context menu labels
            MnuCtxExecute.Header   = _language.Get("MenuExecuteJob");
            MnuCtxEdit.Header      = _language.Get("MenuEditJob");
            MnuCtxDuplicate.Header = _language.Get("MenuDuplicate");
            MnuCtxRemove.Header    = _language.Get("MenuRemoveJob");

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
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusText.Text       = message;
            StatusText.Foreground = isError ? Brushes.Red : new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));
            StatusDot.Fill        = isError ? Brushes.Red : Brushes.Green;
        }

        private void UpdateSelectedCount()
        {
            int n = JobsGrid.SelectedItems.Count;
            TxtSelectedCount.Text = n == 0
                ? string.Empty
                : string.Format(_language.Get("SelectedCount"), n);
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

        // ── Toolbar: CRUD + Execute ───────────────────────────────────────────

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
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
            var dialog = new AddJobWindow(_language, existing) { Owner = this };
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

            // Confirmation popup
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
                                    ex.Message,
                                    ToastHost.ToastKind.Error);
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
                                    ex.Message,
                                    ToastHost.ToastKind.Error);
                    });
                }
            });
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
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

        // ── Per-row action buttons ────────────────────────────────────────────

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

        // ── Context menu handlers ─────────────────────────────────────────────

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

    // Minimal ICommand used only to wire up KeyBinding/InputBinding shortcuts.
    internal sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
