using SaveState.Core.Common;

namespace SaveState.Core.SaveStateCloudSync.Services;

/// <summary>
/// Service for syncing save states to cloud storage.
/// </summary>
public interface ICloudSyncService
{
    /// <summary>
    /// Uploads a save state to cloud storage.
    /// </summary>
    Task<Result<CloudSaveState>> UploadAsync(
        string localFilePath, 
        string name, 
        CloudUploadOptions options,
        CancellationToken ct = default);
    
    /// <summary>
    /// Downloads a save state from cloud storage.
    /// </summary>
    Task<Result<string>> DownloadAsync(
        string cloudId, 
        string localDirectory,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a save state from cloud storage.
    /// </summary>
    Task<Result> DeleteAsync(string cloudId, CancellationToken ct = default);
    
    /// <summary>
    /// Lists all cloud save states for the current user.
    /// </summary>
    Task<Result<List<CloudSaveState>>> ListAsync(
        string? provider = null,
        int? gameId = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Performs a full sync (upload/download based on timestamps).
    /// </summary>
    Task<Result<SyncResult>> SyncAsync(SyncOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Gets sync statistics.
    /// </summary>
    Task<Result<CloudSyncStats>> GetStatsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Resolves a sync conflict.
    /// </summary>
    Task<Result> ResolveConflictAsync(
        string cloudId, 
        ConflictResolution resolution,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all pending conflicts.
    /// </summary>
    Task<Result<List<SyncConflict>>> GetConflictsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Shares a save state with another user.
    /// </summary>
    Task<Result<ShareToken>> ShareAsync(
        string cloudId, 
        ShareOptions options,
        CancellationToken ct = default);
    
    /// <summary>
    /// Imports a shared save state.
    /// </summary>
    Task<Result<CloudSaveState>> ImportSharedAsync(
        string shareToken, 
        string? newName = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets available cloud providers.
    /// </summary>
    Task<Result<List<CloudProviderInfo>>> GetProvidersAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Connects to a cloud provider.
    /// </summary>
    Task<Result> ConnectProviderAsync(
        string providerId, 
        string authorizationCode,
        CancellationToken ct = default);
    
    /// <summary>
    /// Disconnects from a cloud provider.
    /// </summary>
    Task<Result> DisconnectProviderAsync(string providerId, CancellationToken ct = default);
    
    /// <summary>
    /// Sets up automatic sync.
    /// </summary>
    Task<Result> ConfigureAutoSyncAsync(AutoSyncOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Event raised when sync progress updates.
    /// </summary>
    event EventHandler<SyncProgressEventArgs>? SyncProgress;
    
    /// <summary>
    /// Event raised when a conflict is detected.
    /// </summary>
    event EventHandler<SyncConflictEventArgs>? ConflictDetected;
}

/// <summary>
/// Options for uploading a save state.
/// </summary>
public class CloudUploadOptions
{
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public bool Compress { get; set; } = true;
    public bool Encrypt { get; set; } = true;
    public List<string>? Tags { get; set; }
    public string? PreviewImagePath { get; set; }
    public int? GameId { get; set; }
    public string? Platform { get; set; }
    public string? Emulator { get; set; }
    public TimeSpan? GamePlaytime { get; set; }
    public string? GameLocation { get; set; }
}

/// <summary>
/// Options for syncing.
/// </summary>
public class SyncOptions
{
    public string? Provider { get; set; }
    public bool DownloadOnly { get; set; } = false;
    public bool UploadOnly { get; set; } = false;
    public bool ResolveConflictsAutomatically { get; set; } = false;
    public ConflictResolutionStrategy DefaultConflictStrategy { get; set; } = ConflictResolutionStrategy.NewestWins;
    public DateTime? Since { get; set; }
    public int? GameId { get; set; }
    public IProgress<SyncProgress>? Progress { get; set; }
}

/// <summary>
/// Strategies for automatic conflict resolution.
/// </summary>
public enum ConflictResolutionStrategy
{
    NewestWins,
    LocalWins,
    CloudWins,
    KeepBoth
}

/// <summary>
/// Manual conflict resolution choice.
/// </summary>
public enum ConflictResolution
{
    KeepLocal,
    KeepCloud,
    KeepBoth,
    Merge
}

/// <summary>
/// Options for sharing a save state.
/// </summary>
public class ShareOptions
{
    public bool AllowDownload { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public int? MaxDownloads { get; set; }
    public string? Password { get; set; }
    public bool IncludeMetadata { get; set; } = true;
}

/// <summary>
/// Share token for importing shared save states.
/// </summary>
public class ShareToken
{
    public string Token { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxDownloads { get; set; }
}

/// <summary>
/// Options for automatic sync.
/// </summary>
public class AutoSyncOptions
{
    public bool Enabled { get; set; } = true;
    public AutoSyncFrequency Frequency { get; set; } = AutoSyncFrequency.OnSave;
    public TimeSpan? ScheduledTime { get; set; }
    public bool OnlyOnWifi { get; set; } = false;
    public bool OnlyWhenCharging { get; set; } = false;
}

/// <summary>
/// Automatic sync frequencies.
/// </summary>
public enum AutoSyncFrequency
{
    OnSave,
    Hourly,
    Daily,
    Weekly,
    Manual
}

/// <summary>
/// Sync progress information.
/// </summary>
public class SyncProgress
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int UploadedFiles { get; set; }
    public int DownloadedFiles { get; set; }
    public long BytesTransferred { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public decimal PercentComplete => TotalFiles > 0 ? (decimal)ProcessedFiles / TotalFiles * 100 : 0;
}

/// <summary>
/// Event args for sync progress.
/// </summary>
public class SyncProgressEventArgs : EventArgs
{
    public SyncProgress Progress { get; set; } = null!;
}

/// <summary>
/// Event args for conflict detection.
/// </summary>
public class SyncConflictEventArgs : EventArgs
{
    public SyncConflict Conflict { get; set; } = null!;
}
