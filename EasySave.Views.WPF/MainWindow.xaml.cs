using System.Windows;
using EasySave.ViewModels;
using EasySave.ViewModels.Services;

namespace EasySave.Views.WPF
{
    public partial class MainWindow : Window
    {
        private readonly BackupViewModel _viewModel;
        private readonly LanguageService _language;

        public MainWindow(BackupViewModel viewModel, LanguageService language)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _language  = language;
            RefreshJobsList();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void RefreshJobsList()
        {
            JobsGrid.ItemsSource = null;
            JobsGrid.ItemsSource = _viewModel.Jobs;
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusText.Text       = message;
            StatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.Gray;
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
                SetStatus("Please select a job to remove.", isError: true);
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
                SetStatus("Please select a job to execute.", isError: true);
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
            dialog.ShowDialog();
            SetStatus(_language.Get("SettingsSaved"));
        }
    }
}
