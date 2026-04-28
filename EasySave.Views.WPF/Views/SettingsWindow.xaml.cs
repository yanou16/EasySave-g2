using System.Windows;
using EasySave.ViewModels;
using EasySave.ViewModels.Services;

namespace EasySave.Views.WPF
{
    public partial class SettingsWindow : Window
    {
        private readonly BackupViewModel _viewModel;
        private readonly LanguageService _language;

        public SettingsWindow(BackupViewModel viewModel, LanguageService language)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _language  = language;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string format = RbXml.IsChecked == true ? "XML" : "JSON";
            _viewModel.ChangeLogFormat(format);
            DialogResult = true;
            Close();
        }
    }
}
