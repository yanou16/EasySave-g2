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
            ApplyLanguage();
            LoadCurrentSettings();
        }

        private void ApplyLanguage()
        {
            Title                      = _language.Get("MenuSettings");
            TxtSettingsTitle.Text      = _language.Get("MenuSettings");

            TxtSectionLogFormat.Text   = _language.Get("SectionLogFormat");
            TxtSectionLanguage.Text    = _language.Get("SectionLanguage");

            TxtSectionBusiness.Text    = _language.Get("SectionBusiness");
            TxtHintBusiness.Text       = _language.Get("HintBusiness");

            TxtSectionCrypto.Text      = _language.Get("SectionCrypto");
            TxtHintCrypto.Text         = _language.Get("HintCrypto");

            TxtSectionPriority.Text    = _language.Get("PriorityExtensions").ToUpper();
            TxtHintPriority.Text       = _language.Get("HintPriority");

            TxtSectionMaxSize.Text     = _language.Get("MaxFileSizeKb").ToUpper();
            TxtHintMaxSize.Text        = _language.Get("HintMaxSize");

            TxtSectionLogDest.Text     = _language.Get("SectionLogDest");

            RbLogLocal.Content         = _language.Get("LogDestLocal");
            RbLogDocker.Content        = _language.Get("LogDestDocker");
            RbLogBoth.Content          = _language.Get("LogDestBoth");

            TxtSectionDockerUrl.Text   = _language.Get("SectionDockerUrl");
            TxtHintDockerUrl.Text      = _language.Get("HintDockerUrl");

            BtnSave.Content            = _language.Get("ButtonSave");
        }

        private void LoadCurrentSettings()
        {
            AppSettings settings = _viewModel.GetSettings();

            // Log format
            RbJson.IsChecked = settings.LogFormat != "XML";
            RbXml.IsChecked  = settings.LogFormat == "XML";

            // Language
            RbEn.IsChecked = settings.Language != "fr";
            RbFr.IsChecked = settings.Language == "fr";

            // Text fields
            TxtBusinessSoftware.Text  = settings.BusinessSoftware;
            TxtCryptoExtensions.Text  = settings.CryptoExtensions;
            TxtPriorityExtensions.Text = settings.PriorityExtensions;
            TxtMaxFileSizeKb.Text     = settings.MaxParallelFileSizeKb.ToString();
            TxtDockerLogUrl.Text      = settings.DockerLogUrl;

            // Log destination
            RbLogLocal.IsChecked  = settings.LogDestination != "Docker" && settings.LogDestination != "Both";
            RbLogDocker.IsChecked = settings.LogDestination == "Docker";
            RbLogBoth.IsChecked   = settings.LogDestination == "Both";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate max file size field.
            if (!long.TryParse(TxtMaxFileSizeKb.Text.Trim(), out long maxKb) || maxKb < 0)
                maxKb = 0;

            string logDest = RbLogDocker.IsChecked == true ? "Docker"
                           : RbLogBoth.IsChecked   == true ? "Both"
                           : "Local";

            var settings = new AppSettings
            {
                LogFormat            = RbXml.IsChecked == true ? "XML" : "JSON",
                Language             = RbFr.IsChecked  == true ? "fr"  : "en",
                BusinessSoftware     = TxtBusinessSoftware.Text.Trim(),
                CryptoExtensions     = TxtCryptoExtensions.Text.Trim(),
                PriorityExtensions   = TxtPriorityExtensions.Text.Trim(),
                MaxParallelFileSizeKb = maxKb,
                LogDestination       = logDest,
                DockerLogUrl         = TxtDockerLogUrl.Text.Trim()
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
