namespace SaveState.Core.Automation.Services.DTOs;

/// <summary>
/// Configuration for a backup schedule.
/// </summary>
public sealed record BackupScheduleConfig(
    Guid GameId,
    string Name,
    string Description,
    BackupFrequency Frequency,
    string DestinationPath,
    TimeSpan? TimeOfDay = null,
    IReadOnlyList<DayOfWeek>? DaysOfWeek = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    BackupOptions Options = default!,
    bool IsEnabled = true,
    bool Compress = true,
    RetentionPolicy? Retention = null);

/// <summary>
/// Frequency for backup scheduling.
/// </summary>
public enum BackupFrequency
{
    Daily,
    Weekly,
    Monthly,
    Manual,
    AfterGameExit
}

/// <summary>
/// Backup options.
/// </summary>
public sealed record BackupOptions(
    bool IncludeSaveStates = true,
    bool IncludeGameFiles = false,
    bool IncludeScreenshots = false,
    bool Compress = true,
    string? Password = null,
    long? MaxSizeBytes = null);

/// <summary>
/// A backup schedule.
/// </summary>
public sealed record BackupSchedule(
    Guid Id,
    string Name,
    string Description,
    BackupScheduleConfig Config,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime LastModified,
    DateTime? LastExecutedAt = null,
    DateTime? NextExecutionTime = null);

/// <summary>
/// Result of a backup operation.
/// </summary>
public sealed record BackupResult(
    Guid Id,
    Guid? ScheduleId,
    Guid? GameId,
    DateTime StartedAt,
    DateTime CompletedAt,
    BackupStatus Status,
    long TotalSizeBytes,
    int FilesBackedUp,
    string BackupPath,
    IReadOnlyList<string> Errors)
{
    // Convenience property for implementation
    public bool Success => Status == BackupStatus.Success || Status == BackupStatus.PartialSuccess;
}

/// <summary>
/// Status of a backup operation.
/// </summary>
public enum BackupStatus
{
    Success,
    PartialSuccess,
    Failed,
    Cancelled
}

/// <summary>
/// Information about a backup.
/// </summary>
public sealed record BackupInfo(
    Guid Id,
    Guid GameId,
    string Name,
    DateTime CreatedAt,
    long SizeBytes,
    int FileCount,
    string Path,
    BackupStatus Status,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Filter for listing backups.
/// </summary>
public sealed record BackupFilter(
    Guid? GameId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    BackupStatus? Status = null,
    string? NamePattern = null);

/// <summary>
/// Restore options.
/// </summary>
public sealed record RestoreOptions(
    string RestorePath,
    bool OverwriteExisting = false,
    bool ValidateOnly = false,
    IReadOnlyList<string>? FilesToRestore = null);

/// <summary>
/// Result of a restore operation.
/// </summary>
public sealed record RestoreResult(
    Guid BackupId,
    DateTime StartedAt,
    DateTime CompletedAt,
    RestoreStatus Status,
    int FilesRestored,
    long BytesRestored,
    IReadOnlyList<string> Errors);

/// <summary>
/// Status of a restore operation.
/// </summary>
public enum RestoreStatus
{
    Success,
    PartialSuccess,
    Failed,
    Cancelled
}

/// <summary>
/// Result of backup validation.
/// </summary>
public sealed record BackupValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    BackupInfo Info);

/// <summary>
/// Storage statistics for backups.
/// </summary>
public sealed record BackupStorageStats(
    long TotalSizeBytes,
    int TotalBackups,
    long AverageBackupSize,
    DateTime OldestBackup,
    DateTime NewestBackup,
    IReadOnlyDictionary<string, long> SizeByGame);

/// <summary>
/// Cleanup policy for old backups.
/// </summary>
public sealed record CleanupPolicy(
    TimeSpan? MaxAge = null,
    int? MaxBackupsPerGame = null,
    long? MaxTotalSizeBytes = null,
    bool KeepWeeklyBackups = true,
    bool KeepMonthlyBackups = true);

/// <summary>
/// Result of cleanup operation.
/// </summary>
public sealed record CleanupResult(
    int BackupsDeleted,
    long BytesFreed,
    IReadOnlyList<string> Errors);

/// <summary>
/// Event arguments for backup scheduled.
/// </summary>
public sealed class BackupScheduledEventArgs : EventArgs
{
    public BackupSchedule Schedule { get; init; } = null!;
    public DateTime NextRunTime { get; init; }
}

/// <summary>
/// Event arguments for backup started.
/// </summary>
public sealed class BackupStartedEventArgs : EventArgs
{
    public Guid ScheduleId { get; init; }
    public Guid BackupId { get; init; }
    public DateTime StartedAt { get; init; }
}

/// <summary>
/// Event arguments for backup completed.
/// </summary>
public sealed class BackupCompletedEventArgs : EventArgs
{
    public BackupResult Result { get; init; } = null!;
}

/// <summary>
/// Retention policy for backups.
/// </summary>
public sealed record RetentionPolicy(
    int MaxBackups = 10,
    TimeSpan? MaxAge = null,
    bool KeepWeekly = true,
    bool KeepMonthly = true);

/// <summary>
/// Target types for backup.
/// </summary>
public enum BackupTarget
{
    GameLibrary,
    SaveStates,
    Settings,
    All
}
