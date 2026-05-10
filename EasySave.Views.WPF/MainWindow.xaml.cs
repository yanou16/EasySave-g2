using System.Windows;
using System.Windows.Controls;
using EasySave.ViewModels;
using EasySave.ViewModels.Services;

namespace EasySave.Views.WPF
{
    public partial class MainWindow : Window
    {
        private readonly BackupViewModel _viewModel;
        private readonly LanguageService _language;
        private bool _languageComboReady = false;

        public MainWindow(BackupViewModel viewModel, LanguageService language)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _language  = language;

            // Load saved language from embedded resources before building the UI.
            string savedLang = _viewModel.GetSettings().Language;
            LoadLanguageFromEmbeddedResource(savedLang);

            InitLanguageCombo();
            RefreshLanguage();
            RefreshJobsList();
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

            // Persist language choice
            var settings  = _viewModel.GetSettings();
            settings.Language = lang;
            _viewModel.SaveSettings(settings);

            RefreshLanguage();
        }

        /// <summary>Loads a language JSON from the embedded resources of this assembly.</summary>
        internal static void LoadLanguageFromEmbeddedResource(LanguageService languageService, string lang)
        {
            var assembly   = typeof(MainWindow).Assembly;
            string resName = $"EasySave.Views.WPF.Resources.{lang}.json";
            using var stream = assembly.GetManifestResourceStream(resName)
                            ?? assembly.GetManifestResourceStream("EasySave.Views.WPF.Resources.en.json");

            if (stream is not null)
            {
                using var reader = new System.IO.StreamReader(stream);
                languageService.LoadFromJson(reader.ReadToEnd());
            }
        }

        private void LoadLanguageFromEmbeddedResource(string lang) =>
            LoadLanguageFromEmbeddedResource(_language, lang);

        /// <summary>Updates all UI text from the LanguageService.</summary>
        private void RefreshLanguage()
        {
            BtnAdd.Content        = _language.Get("MenuAddJob");
            BtnRemove.Content     = _language.Get("MenuRemoveJob");
            BtnExecute.Content    = _language.Get("MenuExecuteJob");
            BtnExecuteAll.Content = _language.Get("MenuExecuteAllJobs");
            BtnSettings.Content   = _language.Get("MenuSettings");

            ColId.Header     = "Id";
            ColName.Header   = _language.Get("LabelName");
            ColSource.Header = _language.Get("LabelSource");
            ColTarget.Header = _language.Get("LabelTarget");
            ColType.Header   = _language.Get("LabelType");

            SetStatus(_language.Get("StatusReady"));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshJobsList()
        {
            JobsGrid.ItemsSource = null;
            JobsGrid.ItemsSource = _viewModel.Jobs;

            // Update job count badge in header
            int count = _viewModel.Jobs.Count;
            TxtJobCount.Text = count.ToString();

            // Show/hide empty state overlay
            EmptyState.Visibility = count == 0
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Hidden;
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusText.Text       = message;
            StatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Gray;

            // Status dot: red for error, green for success, grey default
            StatusDot.Fill = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Green;
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddJobWindow(_language);
            if (dialog.ShowDialog() != true) return;

            var result = _viewModel.AddJob(dialog.JobName, dialog.Source, dialog.Target, dialog.BackupType);
            SetStatus(result.Message, !result.Success);
            RefreshJobsList();
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (JobsGrid.SelectedIndex < 0)
            {
                SetStatus(_language.Get("NoJobSelected"), isError: true);
                return;
            }

            var result = _viewModel.RemoveJob(JobsGrid.SelectedIndex);
            SetStatus(result.Message, !result.Success);
            RefreshJobsList();
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (JobsGrid.SelectedIndex < 0)
            {
                SetStatus(_language.Get("NoJobSelected"), isError: true);
                return;
            }

            try
            {
                _viewModel.ExecuteJob(JobsGrid.SelectedIndex);
                SetStatus(_language.Get("ExecutionCompleted"));
            }
            catch (Exception ex)
            {
                SetStatus($"{_language.Get("ExecutionFailed")}: {ex.Message}", isError: true);
            }
        }

        private void BtnExecuteAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.ExecuteAllJobs();
                SetStatus(_language.Get("ExecutionCompleted"));
            }
            catch (Exception ex)
            {
                SetStatus($"{_language.Get("ExecutionFailed")}: {ex.Message}", isError: true);
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsWindow(_viewModel, _language);
            if (dialog.ShowDialog() != true) return;

            // Re-sync language combo if changed from settings window
            string lang = _viewModel.GetSettings().Language;
            _languageComboReady = false;
            CmbLanguage.SelectedIndex = lang == "fr" ? 1 : 0;
            _languageComboReady = true;

            RefreshLanguage();
            SetStatus(_language.Get("SettingsSaved"));
        }
    }
}
