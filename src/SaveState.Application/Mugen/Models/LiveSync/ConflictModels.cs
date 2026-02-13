namespace SaveState.Application.Mugen.Models.LiveSync;

/// <summary>
/// Represents a synchronization conflict between local and remote data.
/// </summary>
public class SyncConflict
{
    public string ConflictId { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public ConflictType Type { get; set; }
    public IReadOnlyDictionary<string, object> LocalVersion { get; set; } = default!;
    public IReadOnlyDictionary<string, object> RemoteVersion { get; set; } = default!;
    public DateTime DetectedAt { get; set; }
    public PlatformType SourcePlatform { get; set; }
    public PlatformType TargetPlatform { get; set; }
}

/// <summary>
/// Represents a resolution for a sync conflict.
/// </summary>
public class ConflictResolution
{
    public string ConflictId { get; set; } = default!;
    public ResolutionStrategy Strategy { get; set; }
    public IReadOnlyDictionary<string, object>? ResolvedData { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of conflict resolution.
/// </summary>
public class ConflictResolutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResolutionId { get; set; }
    public IReadOnlyDictionary<string, object>? FinalData { get; set; }
}

/// <summary>
/// Options for conflict resolution.
/// </summary>
public class ConflictResolutionOptions
{
    public ResolutionStrategy DefaultStrategy { get; set; } = ResolutionStrategy.Merge;
    public bool AutoResolveSimpleConflicts { get; set; } = true;
    public TimeSpan? MaxConflictAge { get; set; }
    public IReadOnlyList<ConflictType>? AutoResolvableTypes { get; set; }
}

/// <summary>
/// Summary of conflicts for a sync session.
/// </summary>
public class ConflictSummary
{
    public string SessionId { get; set; } = default!;
    public int TotalConflicts { get; set; }
    public int ResolvedConflicts { get; set; }
    public int PendingConflicts { get; set; }
    public Dictionary<ConflictType, int> ConflictsByType { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
