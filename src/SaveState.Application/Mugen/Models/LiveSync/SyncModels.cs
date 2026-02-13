namespace SaveState.Application.Mugen.Models.LiveSync;

/// <summary>
/// Represents the result of a synchronization operation.
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemsSynced { get; set; }
    public int ConflictsFound { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Represents the current state of synchronization for an account.
/// </summary>
public class SyncState
{
    public string AccountId { get; set; } = default!;
    public PlatformType Platform { get; set; }
    public SyncStatus Status { get; set; }
    public DateTime LastSyncAt { get; set; }
    public string? CurrentOperation { get; set; }
    public double ProgressPercentage { get; set; }
}

/// <summary>
/// Represents a synchronization operation request.
/// </summary>
public class SyncOperation
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString();
    public string AccountId { get; set; } = default!;
    public PlatformType SourcePlatform { get; set; }
    public IReadOnlyList<PlatformType> TargetPlatforms { get; set; } = default!;
    public SyncMode Mode { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public SyncPriority Priority { get; set; } = SyncPriority.Normal;
}

/// <summary>
/// Priority levels for sync operations.
/// </summary>
public enum SyncPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Represents a sync session with progress tracking.
/// </summary>
public class SyncSession
{
    public string SessionId { get; set; } = default!;
    public string AccountId { get; set; } = default!;
    public PlatformType InitiatingPlatform { get; set; }
    public IReadOnlyList<PlatformType> TargetPlatforms { get; set; } = default!;
    public SyncMode Mode { get; set; }
    public SyncStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public SyncProgress Progress { get; set; } = default!;
    public IReadOnlyList<SyncConflict>? Conflicts { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents the progress of a sync operation.
/// </summary>
public class SyncProgress
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public string CurrentPhase { get; set; } = default!;
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public double Percentage => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
}

/// <summary>
/// Request to start a new sync session.
/// </summary>
public class SyncSessionRequest
{
    public string AccountId { get; set; } = default!;
    public PlatformType InitiatingPlatform { get; set; }
    public IReadOnlyList<PlatformType> TargetPlatforms { get; set; } = default!;
    public SyncMode Mode { get; set; }
}
