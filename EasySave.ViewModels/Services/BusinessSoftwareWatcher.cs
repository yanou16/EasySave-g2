using System.Diagnostics;

namespace EasySave.ViewModels.Services
{
    public class BusinessSoftwareWatcher : IDisposable
    {
        private readonly object _sync = new();
        private readonly TimeSpan _pollInterval;
        private List<string> _businessSoftwareNames;
        private CancellationTokenSource? _pollCts;
        private Task? _pollTask;
        private string? _lastDetected;

        public event EventHandler<BusinessSoftwareChangedEventArgs>? BusinessSoftwareChanged;

        public BusinessSoftwareWatcher(List<string> businessSoftwareNames)
            : this(businessSoftwareNames, TimeSpan.FromSeconds(2))
        {
        }

        public BusinessSoftwareWatcher(List<string> businessSoftwareNames, TimeSpan pollInterval)
        {
            _businessSoftwareNames = NormalizeNames(businessSoftwareNames);
            _pollInterval = pollInterval;
        }

        public string? GetRunningBusinessSoftware()
        {
            var running = Process.GetProcesses()
                                 .Select(p => p.ProcessName.ToLower())
                                 .ToList();

            List<string> names;
            lock (_sync)
                names = _businessSoftwareNames.ToList();

            return names.FirstOrDefault(name => running.Contains(name));
        }

        public void UpdateSoftwareNames(IEnumerable<string> businessSoftwareNames)
        {
            lock (_sync)
            {
                _businessSoftwareNames = NormalizeNames(businessSoftwareNames);
                _lastDetected = null;
            }
        }

        public void Start()
        {
            lock (_sync)
            {
                if (_pollTask is { IsCompleted: false })
                    return;

                _pollCts = new CancellationTokenSource();
                _pollTask = Task.Run(() => PollLoop(_pollCts.Token));
            }
        }

        public void Stop()
        {
            CancellationTokenSource? cts;
            Task? task;

            lock (_sync)
            {
                cts = _pollCts;
                task = _pollTask;
                _pollCts = null;
                _pollTask = null;
                _lastDetected = null;
            }

            if (cts is null)
                return;

            cts.Cancel();
            try { task?.Wait(TimeSpan.FromSeconds(1)); } catch { }
            cts.Dispose();
        }

        public void Dispose() => Stop();

        private async Task PollLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                string? detected = GetRunningBusinessSoftware();
                string? previous;

                lock (_sync)
                {
                    previous = _lastDetected;
                    _lastDetected = detected;
                }

                if (detected != previous)
                {
                    if (detected is not null)
                        BusinessSoftwareChanged?.Invoke(this, new BusinessSoftwareChangedEventArgs(detected, true));
                    else if (previous is not null)
                        BusinessSoftwareChanged?.Invoke(this, new BusinessSoftwareChangedEventArgs(previous, false));
                }

                try
                {
                    await Task.Delay(_pollInterval, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private static List<string> NormalizeNames(IEnumerable<string> names)
        {
            return names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim().ToLowerInvariant())
                .ToList();
        }
    }
}
