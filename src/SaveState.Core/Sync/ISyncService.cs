namespace SaveState.Core.Sync;

/// <summary>
/// Service for orchestrating sync operations between local and cloud storage.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Gets the current sync status.
    /// </summary>
    SyncStatus Status { get; }

    /// <summary>
    /// Gets the active cloud provider name.
    /// </summary>
    string? ActiveProviderName { get; }

    /// <summary>
    /// Configures the sync service with a specific cloud provider.
    /// </summary>
    Task ConfigureProviderAsync(ICloudStorageProvider provider, CancellationToken ct = default);

    /// <summary>
    /// Performs a full synchronization.
    /// </summary>
    Task<SyncResult> SyncAsync(CancellationToken ct = default);

    /// <summary>
    /// Uploads pending local changes to the cloud.
    /// </summary>
    Task<SyncResult> PushAsync(CancellationToken ct = default);

    /// <summary>
    /// Downloads cloud changes to local storage.
    /// </summary>
    Task<SyncResult> PullAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets sync status for a specific file or directory.
    /// </summary>
    Task<FileSyncStatus> GetFileSyncStatusAsync(string localPath, CancellationToken ct = default);

    /// <summary>
    /// Event raised when sync progress updates.
    /// </summary>
    event EventHandler<SyncProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// Event raised when a sync conflict is detected.
    /// </summary>
    event EventHandler<SyncConflictEventArgs>? ConflictDetected;
}

/// <summary>
/// Current status of the sync service.
/// </summary>
public enum SyncStatus
{
    NotConfigured,
    Idle,
    Syncing,
    Error
}

/// <summary>
/// Sync status for an individual file.
/// </summary>
public enum FileSyncStatus
{
    Synced,
    LocalNewer,
    RemoteNewer,
    Conflict,
    NotTracked
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public sealed record SyncResult(
    bool Success,
    int FilesUploaded,
    int FilesDownloaded,
    int Conflicts,
    IReadOnlyList<string> Errors);

/// <summary>
/// Progress event for sync operations.
/// </summary>
public sealed class SyncProgressEventArgs : EventArgs
{
    public int TotalFiles { get; init; }
    public int ProcessedFiles { get; init; }
    public string CurrentFile { get; init; } = string.Empty;
    public double PercentComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}

/// <summary>
/// Event args for sync conflict detection.
/// </summary>
public sealed class SyncConflictEventArgs : EventArgs
{
    public string LocalPath { get; init; } = string.Empty;
    public string RemotePath { get; init; } = string.Empty;
    public DateTime LocalModified { get; init; }
    public DateTime RemoteModified { get; init; }
}
