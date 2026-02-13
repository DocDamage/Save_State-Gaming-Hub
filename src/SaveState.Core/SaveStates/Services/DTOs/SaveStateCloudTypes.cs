namespace SaveState.Core.SaveStates.Services.DTOs;

/// <summary>
/// Metadata used when synchronizing a save state with cloud storage.
/// </summary>
public sealed record SaveStateCloudMetadata
{
    /// <summary>
    /// Optional explicit save state identifier to sync.
    /// If not set, the latest local save state for the game is used.
    /// </summary>
    public Guid? SaveStateId { get; init; }

    /// <summary>
    /// Optional custom version label.
    /// </summary>
    public string? VersionName { get; init; }

    /// <summary>
    /// Optional device name used in version metadata.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Optional user-provided encryption key for client-side encryption.
    /// </summary>
    public string? EncryptionKey { get; init; }

    /// <summary>
    /// When true, upload proceeds even when a conflict is detected.
    /// </summary>
    public bool ForceUpload { get; init; }
}

/// <summary>
/// Result data returned after a cloud synchronization attempt.
/// </summary>
public sealed record SaveStateCloudSyncStatus
{
    public required Guid GameId { get; init; }
    public required string Provider { get; init; }
    public required bool Uploaded { get; init; }
    public required bool Downloaded { get; init; }
    public required bool HasConflict { get; init; }
    public required SaveStateConflictType ConflictType { get; init; }
    public required DateTime SyncedAtUtc { get; init; }
    public required bool IsEncrypted { get; init; }
    public string? Message { get; init; }
    public SaveStateCloudVersion? LocalVersion { get; init; }
    public SaveStateCloudVersion? CloudVersion { get; init; }
}

/// <summary>
/// Represents a cloud-tracked version of a save state.
/// </summary>
public sealed record SaveStateCloudVersion
{
    public required Guid Id { get; init; }
    public required Guid GameId { get; init; }
    public required Guid SaveStateId { get; init; }
    public required string VersionName { get; init; }
    public required string StoragePath { get; init; }
    public required string ContentHash { get; init; }
    public required long FileSizeBytes { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required bool IsEncrypted { get; init; }
    public DateTime? SourceSaveStateCreatedAtUtc { get; init; }
    public string? DeviceName { get; init; }
    public string? EncryptionKeyFingerprint { get; init; }
}

/// <summary>
/// Describes a detected conflict between local and cloud save state versions.
/// </summary>
public sealed record SaveStateConflictResolution
{
    public required Guid GameId { get; init; }
    public required SaveStateConflictType Type { get; init; }
    public required DateTime DetectedAtUtc { get; init; }
    public SaveStateCloudVersion? LocalVersion { get; init; }
    public SaveStateCloudVersion? CloudVersion { get; init; }
    public SaveStateConflictResolutionStrategy? ResolvedStrategy { get; set; }
    public string? Details { get; init; }
}

/// <summary>
/// Supported conflict categories.
/// </summary>
public enum SaveStateConflictType
{
    None = 0,
    LocalNewer,
    CloudNewer,
    BothModified,
    DeletedOnOneSide
}

/// <summary>
/// Conflict resolution strategies.
/// </summary>
public enum SaveStateConflictResolutionStrategy
{
    KeepLocal,
    KeepCloud,
    Merge,
    KeepBoth,
    PromptUser
}
