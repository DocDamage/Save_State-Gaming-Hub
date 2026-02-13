namespace SaveState.Application.Mugen.Models.LiveSync;

/// <summary>
/// Represents a snapshot of game state data.
/// </summary>
public class GameStateSnapshot
{
    public string SnapshotId { get; set; } = default!;
    public string AccountId { get; set; } = default!;
    public PlatformType Platform { get; set; }
    public DateTime CapturedAt { get; set; }
    public IReadOnlyDictionary<string, object> GameProgress { get; set; } = default!;
    public IReadOnlyList<string> Achievements { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Statistics { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Preferences { get; set; } = default!;
    public string? Checksum { get; set; }
    public long DataSize { get; set; }
}

/// <summary>
/// Represents a difference between two state snapshots.
/// </summary>
public class StateDiff
{
    public string DiffId { get; set; } = default!;
    public string SourceSnapshotId { get; set; } = default!;
    public string TargetSnapshotId { get; set; } = default!;
    public DateTime ComputedAt { get; set; }
    public IReadOnlyList<FieldChange> Changes { get; set; } = default!;
    public IReadOnlyList<string> AddedAchievements { get; set; } = default!;
    public IReadOnlyList<string> RemovedAchievements { get; set; } = default!;
    public bool HasConflicts { get; set; }
}

/// <summary>
/// Represents a single field change.
/// </summary>
public class FieldChange
{
    public string FieldPath { get; set; } = default!;
    public ChangeType Type { get; set; }
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}

/// <summary>
/// Types of field changes.
/// </summary>
public enum ChangeType
{
    Added,
    Modified,
    Removed
}

/// <summary>
/// Platform-specific data storage.
/// </summary>
public class PlatformData
{
    public string AccountId { get; set; } = default!;
    public PlatformType Platform { get; set; }
    public IReadOnlyDictionary<string, object> GameProgress { get; set; } = default!;
    public IReadOnlyList<string> Achievements { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Statistics { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Preferences { get; set; } = default!;
    public DateTime LastUpdated { get; set; }
    public string? Version { get; set; }
}

/// <summary>
/// Request for migrating data between platforms.
/// </summary>
public class PlatformMigrationRequest
{
    public PlatformType SourcePlatform { get; set; }
    public PlatformType TargetPlatform { get; set; }
    public bool MigrateProgress { get; set; }
    public bool MigrateAchievements { get; set; }
    public bool MigratePreferences { get; set; }
    public bool DeleteSourceData { get; set; }
}

/// <summary>
/// Result of platform data migration.
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemsMigrated { get; set; }
    public TimeSpan Duration { get; set; }
    public IReadOnlyList<string>? Warnings { get; set; }
}
