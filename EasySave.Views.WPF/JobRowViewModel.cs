using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using EasySave.Models;

namespace EasySave.Views.WPF
{
    /// <summary>
    /// Per-row view-model for the DataGrid. Wraps a BackupJob and adds live
    /// Progress / Status / colour properties that update via INotifyPropertyChanged.
    /// </summary>
    public class JobRowViewModel : INotifyPropertyChanged
    {
        // ── Immutable job properties ───────────────────────────────────────────

        public int    Id              { get; }
        public string Name            { get; }
        public string SourceDirectory { get; }
        public string TargetDirectory { get; }
        public string Type            { get; }

        // ── Live properties ───────────────────────────────────────────────────

        private double _progress;
        public double Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
        }

        public string ProgressText => $"{_progress:0.0}%";

        private BackupRuntimeStatus _status = BackupRuntimeStatus.Idle;
        public BackupRuntimeStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanResume));
                OnPropertyChanged(nameof(CanStop));
            }
        }

        public string StatusText => _status switch
        {
            BackupRuntimeStatus.Idle     => "Idle",
            BackupRuntimeStatus.Running  => "Running",
            BackupRuntimeStatus.Paused   => "Paused",
            BackupRuntimeStatus.Stopped  => "Stopped",
            BackupRuntimeStatus.Finished => "Finished",
            BackupRuntimeStatus.Error    => "Error",
            _                            => _status.ToString()
        };

        public Brush StatusColor => _status switch
        {
            BackupRuntimeStatus.Running  => new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),  // blue
            BackupRuntimeStatus.Paused   => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),  // orange
            BackupRuntimeStatus.Stopped  => new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),  // red
            BackupRuntimeStatus.Finished => new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47)),  // green
            BackupRuntimeStatus.Error    => new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)),  // red
            _                            => new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF))   // grey
        };

        // Button visibility helpers
        public bool CanPause  => _status == BackupRuntimeStatus.Running;
        public bool CanResume => _status == BackupRuntimeStatus.Paused;
        public bool CanStop   => _status == BackupRuntimeStatus.Running || _status == BackupRuntimeStatus.Paused;

        // ── Constructor ───────────────────────────────────────────────────────

        public JobRowViewModel(BackupJob job)
        {
            Id              = job.Id;
            Name            = job.Name;
            SourceDirectory = job.SourceDirectory;
            TargetDirectory = job.TargetDirectory;
            Type            = job.Type.ToString();
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
