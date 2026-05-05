namespace EasySave.ViewModels.Services
{
    /// <summary>
    /// Shared coordination primitives for all parallel backup jobs.
    /// Single instance shared across all BackupService instances.
    /// </summary>
    public class ParallelCoordinator
    {
        // Only one large file can transfer at a time across ALL jobs
        private readonly SemaphoreSlim _largeFileSemaphore = new(1, 1);

        // CryptoSoft is mono-instance — only one encryption at a time
        private readonly SemaphoreSlim _cryptoSemaphore = new(1, 1);

        // Tracks how many priority files are currently pending across ALL jobs
        private int _pendingPriorityFiles = 0;
        private readonly object _priorityLock = new();

        public void IncrementPendingPriority()
        {
            lock (_priorityLock) _pendingPriorityFiles++;
        }

        public void DecrementPendingPriority()
        {
            lock (_priorityLock) _pendingPriorityFiles--;
        }

        public bool HasPendingPriorityFiles()
        {
            lock (_priorityLock) return _pendingPriorityFiles > 0;
        }

        public void WaitForLargeFileSlot(CancellationToken ct) =>
            _largeFileSemaphore.Wait(ct);

        public void ReleaseLargeFileSlot() =>
            _largeFileSemaphore.Release();

        public void WaitForCryptoSlot(CancellationToken ct) =>
            _cryptoSemaphore.Wait(ct);

        public void ReleaseCryptoSlot() =>
            _cryptoSemaphore.Release();
    }
}