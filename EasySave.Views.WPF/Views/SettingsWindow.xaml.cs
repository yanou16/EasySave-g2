using System.Windows;
using EasySave.Models;
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
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            AppSettings settings = _viewModel.GetSettings();

            RbJson.IsChecked = settings.LogFormat != "XML";
            RbXml.IsChecked  = settings.LogFormat == "XML";

            TxtBusinessSoftware.Text  = settings.BusinessSoftware;
            TxtCryptoExtensions.Text  = settings.CryptoExtensions;

            RbEn.IsChecked = settings.Language != "fr";
            RbFr.IsChecked = settings.Language == "fr";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var settings = new AppSettings
            {
                LogFormat        = RbXml.IsChecked == true ? "XML" : "JSON",
                BusinessSoftware = TxtBusinessSoftware.Text.Trim(),
                CryptoExtensions = TxtCryptoExtensions.Text.Trim(),
                Language         = RbFr.IsChecked == true ? "fr" : "en"
            };

            _viewModel.SaveSettings(settings);

            // Reload language from embedded resources (works in single-file publish).
            LoadLanguageFromEmbeddedResource(settings.Language);

            DialogResult = true;
            Close();
        }

        private void LoadLanguageFromEmbeddedResource(string lang) =>
            MainWindow.LoadLanguageFromEmbeddedResource(_language, lang);
    }
}
