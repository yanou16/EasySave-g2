using System.Windows;
using EasySave.Models;
using EasySave.ViewModels.Services;
using WinForms = System.Windows.Forms;

namespace EasySave.Views.WPF
{
    public partial class AddJobWindow : Window
    {
        public string     JobName    { get; private set; } = string.Empty;
        public string     Source     { get; private set; } = string.Empty;
        public string     Target     { get; private set; } = string.Empty;
        public BackupType BackupType { get; private set; } = BackupType.Full;

        private readonly LanguageService _language;

        public AddJobWindow(LanguageService language)
        {
            InitializeComponent();
            _language = language;
        }

        private void BtnBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            string? path = BrowseFolder();
            if (path is not null) TxtSource.Text = path;
        }

        private void BtnBrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            string? path = BrowseFolder();
            if (path is not null) TxtTarget.Text = path;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))   { TxtError.Text = _language.Get("EmptyName");   return; }
            if (string.IsNullOrWhiteSpace(TxtSource.Text)) { TxtError.Text = _language.Get("EmptySource"); return; }
            if (string.IsNullOrWhiteSpace(TxtTarget.Text)) { TxtError.Text = _language.Get("EmptyTarget"); return; }

            JobName    = TxtName.Text.Trim();
            Source     = TxtSource.Text.Trim();
            Target     = TxtTarget.Text.Trim();
            BackupType = CmbType.SelectedIndex == 0 ? BackupType.Full : BackupType.Differential;

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string? BrowseFolder()
        {
            using var dialog = new WinForms.FolderBrowserDialog();
            return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.SelectedPath : null;
        }
    }
}
