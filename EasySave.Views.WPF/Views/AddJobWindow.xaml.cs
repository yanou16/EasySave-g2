using System.IO;
using System.Windows;
using Microsoft.Win32;
using EasySave.Models;
using EasySave.ViewModels.Services;

namespace EasySave.Views.WPF
{
    /// <summary>
    /// Dual-mode dialog: Add a new job (default ctor) or Edit an existing one
    /// (overloaded ctor that pre-fills the fields and changes the header / button labels).
    /// </summary>
    public partial class AddJobWindow : Window
    {
        public string     JobName    { get; private set; } = string.Empty;
        public string     Source     { get; private set; } = string.Empty;
        public string     Target     { get; private set; } = string.Empty;
        public BackupType BackupType { get; private set; } = BackupType.Full;

        private readonly LanguageService _language;
        private readonly bool            _isEditMode;

        // ── Constructors ──────────────────────────────────────────────────────

        /// <summary>Add-mode: empty form.</summary>
        public AddJobWindow(LanguageService language) : this(language, null) { }

        /// <summary>Edit-mode: pre-fills fields from an existing job.</summary>
        public AddJobWindow(LanguageService language, BackupJob? existing)
        {
            InitializeComponent();
            _language   = language;
            _isEditMode = existing is not null;

            ApplyLanguage();

            if (existing is not null)
            {
                TxtName.Text   = existing.Name;
                TxtSource.Text = existing.SourceDirectory;
                TxtTarget.Text = existing.TargetDirectory;
                CmbType.SelectedIndex = existing.Type == BackupType.Full ? 0 : 1;
            }

            // Focus first field for keyboard users.
            Loaded += (_, _) => TxtName.Focus();
        }

        // ── Language ──────────────────────────────────────────────────────────

        private void ApplyLanguage()
        {
            Title              = _isEditMode ? _language.Get("EditJobTitle") : _language.Get("AddJobTitle");
            TxtHeader.Text     = Title;
            LblName.Text       = _language.Get("FieldJobName");
            LblSource.Text     = _language.Get("FieldSourceFolder");
            LblTarget.Text     = _language.Get("FieldTargetFolder");
            LblType.Text       = _language.Get("FieldBackupType");
            CmbItemFull.Content         = _language.Get("TypeFull");
            CmbItemDifferential.Content = _language.Get("TypeDifferential");
            BtnCancel.Content  = _language.Get("ButtonCancel");
            BtnAdd.Content     = _isEditMode ? _language.Get("ButtonSave") : _language.Get("ButtonAdd");
        }

        // ── Browse buttons ────────────────────────────────────────────────────

        private void BtnBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            string? path = BrowseFolder(TxtSource.Text);
            if (path is not null) TxtSource.Text = path;
        }

        private void BtnBrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            string? path = BrowseFolder(TxtTarget.Text);
            if (path is not null) TxtTarget.Text = path;
        }

        // ── Primary action ────────────────────────────────────────────────────

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Prevent double-submit on rapid clicks (test case #9)
            BtnAdd.IsEnabled = false;
            TxtError.Text = string.Empty;

            // 1. Presence
            if (string.IsNullOrWhiteSpace(TxtName.Text))   { ShowError(_language.Get("EmptyName"));   TxtName.Focus();   return; }
            if (string.IsNullOrWhiteSpace(TxtSource.Text)) { ShowError(_language.Get("EmptySource")); TxtSource.Focus(); return; }
            if (string.IsNullOrWhiteSpace(TxtTarget.Text)) { ShowError(_language.Get("EmptyTarget")); TxtTarget.Focus(); return; }

            string src = TxtSource.Text.Trim();
            string tgt = TxtTarget.Text.Trim();

            // 2. Source must exist
            if (!Directory.Exists(src))
            {
                ShowError($"{_language.Get("InvalidSourcePath")}\n\n\"{src}\"");
                TxtSource.Focus(); TxtSource.SelectAll();
                return;
            }

            // 3. Target root must be accessible
            string? tgtRoot = Path.GetPathRoot(tgt);
            if (string.IsNullOrEmpty(tgtRoot) || !Directory.Exists(tgtRoot))
            {
                ShowError($"{_language.Get("InvalidTargetPath")}\n\n\"{tgt}\"");
                TxtTarget.Focus(); TxtTarget.SelectAll();
                return;
            }

            // 4. Source ≠ Target
            try
            {
                if (string.Equals(Path.GetFullPath(src), Path.GetFullPath(tgt),
                                  StringComparison.OrdinalIgnoreCase))
                {
                    ShowError(_language.Get("DuplicatePath"));
                    TxtTarget.Focus(); TxtTarget.SelectAll();
                    return;
                }
            }
            catch
            {
                ShowError($"{_language.Get("InvalidTargetPath")}\n\n\"{tgt}\"");
                return;
            }

            // Re-enable the button in case of validation error so user can fix and retry
            BtnAdd.IsEnabled = true;

            // 5. Empty source folder → non-blocking confirmation
            bool hasFiles = Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories).Any();
            if (!hasFiles)
            {
                var answer = MessageBox.Show(
                    $"⚠  {_language.Get("EmptySourceFiles")}\n\n{_language.Get("EmptySourceContinue")}",
                    _language.Get("WarningTitle"),
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (answer == MessageBoxResult.No) return;
            }

            JobName    = TxtName.Text.Trim();
            Source     = src;
            Target     = tgt;
            BackupType = CmbType.SelectedIndex == 0 ? BackupType.Full : BackupType.Differential;

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            TxtError.Text = $"❌  {message.Split('\n')[0]}";
            MessageBox.Show(message, _language.Get("ErrorTitle"),
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string? BrowseFolder(string currentPath = "")
        {
            var dialog = new OpenFolderDialog { Title = "Select a folder" };

            // Open the dialog at the path already typed in the field (if it exists)
            if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath))
                dialog.InitialDirectory = currentPath;

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }
    }
}
