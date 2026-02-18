using SaveState.Core.Common.Base;

namespace SaveState.Core.AutoSave;

/// <summary>
/// Represents an auto-save configuration for a game.
/// </summary>
public class AutoSaveConfiguration : EntityBase
{
    /// <summary>
    /// Game ID this configuration belongs to.
    /// </summary>
    public Guid GameId { get; set; }
    
    /// <summary>
    /// Whether auto-save is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Auto-save interval in minutes.
    /// </summary>
    public int IntervalMinutes { get; set; } = 10;
    
    /// <summary>
    /// Maximum number of auto-saves to keep.
    /// </summary>
    public int MaxAutoSaves { get; set; } = 5;
    
    /// <summary>
    /// Whether to auto-save on level completion.
    /// </summary>
    public bool SaveOnLevelComplete { get; set; } = true;
    
    /// <summary>
    /// Whether to auto-save before boss fights.
    /// </summary>
    public bool SaveBeforeBoss { get; set; } = true;
    
    /// <summary>
    /// Whether to auto-save on checkpoint.
    /// </summary>
    public bool SaveOnCheckpoint { get; set; } = true;
    
    /// <summary>
    /// Minimum play time before first auto-save (in minutes).
    /// </summary>
    public int MinPlayTimeMinutes { get; set; } = 2;
    
    /// <summary>
    /// Naming pattern for auto-saves.
    /// </summary>
    public string NamingPattern { get; set; } = "{GameName} - {Level} - {Time}";
    
    /// <summary>
    /// Whether to include date in filename.
    /// </summary>
    public bool IncludeDate { get; set; } = true;
    
    /// <summary>
    /// Whether to include time in filename.
    /// </summary>
    public bool IncludeTime { get; set; } = true;
    
    /// <summary>
    /// Whether to compress auto-saves.
    /// </summary>
    public bool CompressSaves { get; set; } = false;
    
    /// <summary>
    /// Custom tags to add to auto-saves.
    /// </summary>
    public List<string> Tags { get; set; } = new() { "auto-save" };
    
    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an auto-save entry.
/// </summary>
public class AutoSaveEntry : EntityBase
{
    /// <summary>
    /// Game ID this auto-save belongs to.
    /// </summary>
    public Guid GameId { get; set; }
    
    /// <summary>
    /// Display name for the auto-save.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// File path to the save state.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of auto-save trigger.
    /// </summary>
    public AutoSaveTriggerType TriggerType { get; set; }
    
    /// <summary>
    /// When the auto-save was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Play time at the moment of save (in seconds).
    /// </summary>
    public int PlayTimeSeconds { get; set; }
    
    /// <summary>
    /// Current level/area when saved.
    /// </summary>
    public string? Level { get; set; }
    
    /// <summary>
    /// Current checkpoint identifier.
    /// </summary>
    public string? Checkpoint { get; set; }
    
    /// <summary>
    /// Screenshot thumbnail path.
    /// </summary>
    public string? ThumbnailPath { get; set; }
    
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Whether this save is locked (won't be auto-deleted).
    /// </summary>
    public bool IsLocked { get; set; }
    
    /// <summary>
    /// Tags associated with this save.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Whether this entry has been pruned.
    /// </summary>
    public bool IsPruned { get; set; }
    
    /// <summary>
    /// Sequence number (for ordering).
    /// </summary>
    public int SequenceNumber { get; set; }
}

/// <summary>
/// Types of auto-save triggers.
/// </summary>
public enum AutoSaveTriggerType
{
    /// <summary>
    /// Time-based interval.
    /// </summary>
    Interval,
    
    /// <summary>
    /// Level completed.
    /// </summary>
    LevelComplete,
    
    /// <summary>
    /// Boss fight approaching.
    /// </summary>
    BossApproach,
    
    /// <summary>
    /// Checkpoint reached.
    /// </summary>
    Checkpoint,
    
    /// <summary>
    /// Manual trigger.
    /// </summary>
    Manual,
    
    /// <summary>
    /// Game-specific trigger.
    /// </summary>
    GameSpecific
}

/// <summary>
/// Represents the auto-save session for a running game.
/// </summary>
public class AutoSaveSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Game ID.
    /// </summary>
    public Guid GameId { get; set; }
    
    /// <summary>
    /// When the session started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the last auto-save occurred.
    /// </summary>
    public DateTime? LastAutoSaveAt { get; set; }
    
    /// <summary>
    /// Current play time in seconds.
    /// </summary>
    public int CurrentPlayTimeSeconds { get; set; }
    
    /// <summary>
    /// Current level/area.
    /// </summary>
    public string? CurrentLevel { get; set; }
    
    /// <summary>
    /// Whether auto-save is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Count of auto-saves this session.
    /// </summary>
    public int AutoSaveCount { get; set; }
    
    /// <summary>
    /// Next scheduled auto-save time.
    /// </summary>
    public DateTime? NextScheduledSave { get; set; }
}

/// <summary>
/// Statistics for auto-saves.
/// </summary>
public class AutoSaveStatistics
{
    public int TotalAutoSaves { get; set; }
    public int IntervalSaves { get; set; }
    public int LevelCompleteSaves { get; set; }
    public int BossSaves { get; set; }
    public int CheckpointSaves { get; set; }
    public long TotalStorageUsed { get; set; }
    public TimeSpan TotalPlayTimeTracked { get; set; }
    public DateTime FirstSaveDate { get; set; }
    public DateTime LastSaveDate { get; set; }
    public int AverageSaveSize { get; set; }
}

/// <summary>
/// Request to configure auto-save for a game.
/// </summary>
public class ConfigureAutoSaveRequest
{
    public Guid GameId { get; set; }
    public bool? IsEnabled { get; set; }
    public int? IntervalMinutes { get; set; }
    public int? MaxAutoSaves { get; set; }
    public bool? SaveOnLevelComplete { get; set; }
    public bool? SaveBeforeBoss { get; set; }
    public bool? SaveOnCheckpoint { get; set; }
    public string? NamingPattern { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request to trigger a manual auto-save.
/// </summary>
public class TriggerAutoSaveRequest
{
    public Guid GameId { get; set; }
    public AutoSaveTriggerType TriggerType { get; set; } = AutoSaveTriggerType.Manual;
    public string? Level { get; set; }
    public string? Checkpoint { get; set; }
    public int? PlayTimeSeconds { get; set; }
    public string? CustomName { get; set; }
}

/// <summary>
/// Filter for auto-save entries.
/// </summary>
public class AutoSaveFilter
{
    public Guid? GameId { get; set; }
    public AutoSaveTriggerType? TriggerType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IncludePruned { get; set; }
    public bool? OnlyLocked { get; set; }
}

/// <summary>
/// Boss detection heuristic configuration.
/// </summary>
public class BossDetectionConfig
{
    /// <summary>
    /// Keywords that indicate boss approach.
    /// </summary>
    public List<string> BossKeywords { get; set; } = new()
    {
        "boss", "final", "last", "end", "stage", "world", "level"
    };
    
    /// <summary>
    /// Health threshold percentage (if health suddenly increases).
    /// </summary>
    public int HealthThresholdPercent { get; set; } = 150;
    
    /// <summary>
    /// Time spent in same area threshold (seconds).
    /// </summary>
    public int AreaTimeThresholdSeconds { get; set; } = 300;
    
    /// <summary>
    /// Whether to use music change detection.
    /// </summary>
    public bool DetectMusicChange { get; set; } = true;
    
    /// <summary>
    /// Whether to use screen effect detection.
    /// </summary>
    public bool DetectScreenEffects { get; set; } = true;
}

/// <summary>
/// Represents a detected boss fight event.
/// </summary>
public class BossFightEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public DateTime DetectedAt { get; set; }
    public string? BossName { get; set; }
    public string DetectionMethod { get; set; } = string.Empty;
    public bool AutoSaveCreated { get; set; }
    public Guid? AutoSaveId { get; set; }
}

/// <summary>
/// Smart naming helper for auto-saves.
/// </summary>
public class AutoSaveNamingHelper
{
    /// <summary>
    /// Generates a smart name for an auto-save.
    /// </summary>
    public static string GenerateName(string pattern, string gameName, string? level, DateTime timestamp)
    {
        var name = pattern
            .Replace("{GameName}", gameName)
            .Replace("{Level}", level ?? "Unknown")
            .Replace("{Time}", timestamp.ToString("HH:mm"))
            .Replace("{Date}", timestamp.ToString("yyyy-MM-dd"))
            .Replace("{Timestamp}", timestamp.ToString("yyyy-MM-dd HH:mm"));
        
        return name;
    }
    
    /// <summary>
    /// Generates a default name when pattern is not specified.
    /// </summary>
    public static string GenerateDefaultName(string gameName, string? level, AutoSaveTriggerType trigger, DateTime timestamp)
    {
        var triggerLabel = trigger switch
        {
            AutoSaveTriggerType.Interval => "Auto",
            AutoSaveTriggerType.LevelComplete => "Complete",
            AutoSaveTriggerType.BossApproach => "Boss",
            AutoSaveTriggerType.Checkpoint => "Checkpoint",
            AutoSaveTriggerType.Manual => "Manual",
            _ => "Save"
        };
        
        return $"{gameName} - {level ?? "Unknown"} - {triggerLabel} - {timestamp:HH:mm}";
    }
}
