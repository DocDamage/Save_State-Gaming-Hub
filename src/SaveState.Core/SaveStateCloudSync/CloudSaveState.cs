using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;

namespace SaveState.Core.SaveStateCloudSync;

/// <summary>
/// Represents a save state stored in cloud storage.
/// </summary>
public class CloudSaveState : EntityBase, ISoftDelete
{
    /// <summary>
    /// User who owns this save state.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Game this save state belongs to.
    /// </summary>
    public int? GameId { get; set; }
    
    /// <summary>
    /// Name/title of the save state.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description or notes about this save state.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Cloud storage provider (e.g., "GoogleDrive", "Dropbox", "OneDrive").
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique identifier in the cloud storage.
    /// </summary>
    public string CloudId { get; set; } = string.Empty;
    
    /// <summary>
    /// URL to download the save state.
    /// </summary>
    public string? DownloadUrl { get; set; }
    
    /// <summary>
    /// Size of the save state in bytes.
    /// </summary>
    public long SizeBytes { get; set; }
    
    /// <summary>
    /// Hash of the save state file for integrity verification.
    /// </summary>
    public string? FileHash { get; set; }
    
    /// <summary>
    /// Version of the save state (for conflict resolution).
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Current sync status.
    /// </summary>
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    
    /// <summary>
    /// When this save state was created locally.
    /// </summary>
    public DateTime LocalCreatedAt { get; set; }
    
    /// <summary>
    /// When this save state was last modified locally.
    /// </summary>
    public DateTime LocalModifiedAt { get; set; }
    
    /// <summary>
    /// When this was last synced to cloud.
    /// </summary>
    public DateTime? CloudSyncedAt { get; set; }
    
    /// <summary>
    /// Whether this is a favorite/bookmarked save state.
    /// </summary>
    public bool IsFavorite { get; set; }
    
    /// <summary>
    /// Tags for organization.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Screenshot or preview image URL.
    /// </summary>
    public string? PreviewImageUrl { get; set; }
    
    /// <summary>
    /// In-game timestamp when save was created.
    /// </summary>
    public TimeSpan? GamePlaytime { get; set; }
    
    /// <summary>
    /// Current game level/area when save was created.
    /// </summary>
    public string? GameLocation { get; set; }
    
    /// <summary>
    /// Platform the save state is for.
    /// </summary>
    public string? Platform { get; set; }
    
    /// <summary>
    /// Emulator used to create this save state.
    /// </summary>
    public string? Emulator { get; set; }

    // ISoftDelete implementation
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Sync status for cloud save states.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Pending initial sync.
    /// </summary>
    Pending,
    
    /// <summary>
    /// Currently syncing.
    /// </summary>
    Syncing,
    
    /// <summary>
    /// Successfully synced.
    /// </summary>
    Synced,
    
    /// <summary>
    /// Conflict detected (both local and cloud modified).
    /// </summary>
    Conflict,
    
    /// <summary>
    /// Failed to sync.
    /// </summary>
    Failed,
    
    /// <summary>
    /// Marked for deletion.
    /// </summary>
    MarkedForDeletion
}

/// <summary>
/// Conflict information for cloud sync.
/// </summary>
public class SyncConflict
{
    public Guid CloudSaveStateId { get; set; }
    public required CloudSaveState LocalVersion { get; set; }
    public required CloudSaveState CloudVersion { get; set; }
    public ConflictType Type { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Types of sync conflicts.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Both versions modified since last sync.
    /// </summary>
    BothModified,
    
    /// <summary>
    /// Cloud version deleted but local modified.
    /// </summary>
    CloudDeleted,
    
    /// <summary>
    /// Local deleted but cloud modified.
    /// </summary>
    LocalDeleted,
    
    /// <summary>
    /// Checksum/hash mismatch.
    /// </summary>
    ChecksumMismatch
}

/// <summary>
/// Sync statistics for a user.
/// </summary>
public class CloudSyncStats
{
    public int TotalSaveStates { get; set; }
    public int SyncedCount { get; set; }
    public int PendingCount { get; set; }
    public int ConflictCount { get; set; }
    public int FailedCount { get; set; }
    public long TotalStorageBytes { get; set; }
    public long AvailableStorageBytes { get; set; }
    public DateTime? LastSyncAttempt { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
}

/// <summary>
/// Sync result information.
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int UploadedCount { get; set; }
    public int DownloadedCount { get; set; }
    public int ConflictCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Cloud provider information.
/// </summary>
public class CloudProviderInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string? AccountEmail { get; set; }
    public long? TotalStorageBytes { get; set; }
    public long? UsedStorageBytes { get; set; }
    public DateTime? LastConnectedAt { get; set; }
}
