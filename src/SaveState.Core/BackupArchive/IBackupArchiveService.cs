using SaveState.Core.Common;

namespace SaveState.Core.BackupArchive;

/// <summary>
/// Next-generation backup and archive service with block-level incremental backups,
/// cold storage tiering, and Git-like branching for save states.
/// </summary>
public interface IBackupArchiveService
{
    /// <summary>
    /// Creates a new backup job.
    /// </summary>
    Task<Result<BackupJob>> CreateBackupJobAsync(CreateBackupJobRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a backup job.
    /// </summary>
    Task<Result<BackupJob>> GetBackupJobAsync(string jobId, CancellationToken ct = default);

    /// <summary>
    /// Updates a backup job.
    /// </summary>
    Task<Result<BackupJob>> UpdateBackupJobAsync(string jobId, UpdateBackupJobRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a backup job.
    /// </summary>
    Task<Result> DeleteBackupJobAsync(string jobId, CancellationToken ct = default);

    /// <summary>
    /// Lists all backup jobs.
    /// </summary>
    Task<Result<IReadOnlyList<BackupJob>>> ListBackupJobsAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes a backup job immediately.
    /// </summary>
    Task<Result<BackupResult>> ExecuteBackupAsync(string jobId, CancellationToken ct = default);

    /// <summary>
    /// Executes a backup with specific options.
    /// </summary>
    Task<Result<BackupResult>> ExecuteBackupWithOptionsAsync(string jobId, BackupOptions options, CancellationToken ct = default);

    /// <summary>
    /// Gets backup history for a job.
    /// </summary>
    Task<Result<IReadOnlyList<BackupExecution>>> GetBackupHistoryAsync(string jobId, int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Restores from a backup.
    /// </summary>
    Task<Result<RestoreResult>> RestoreAsync(string executionId, RestoreOptions options, CancellationToken ct = default);

    /// <summary>
    /// Gets restore points available.
    /// </summary>
    Task<Result<IReadOnlyList<RestorePoint>>> GetRestorePointsAsync(string jobId, CancellationToken ct = default);

    /// <summary>
    /// Verifies a backup integrity.
    /// </summary>
    Task<Result<VerificationResult>> VerifyBackupAsync(string executionId, CancellationToken ct = default);

    /// <summary>
    /// Deletes old backups based on retention policy.
    /// </summary>
    Task<Result<CleanupResult>> CleanupBackupsAsync(string jobId, CancellationToken ct = default);

    /// <summary>
    /// Gets backup storage statistics.
    /// </summary>
    Task<Result<BackupStatistics>> GetStatisticsAsync(CancellationToken ct = default);

    /// <summary>
    /// Exports a backup to a different location.
    /// </summary>
    Task<Result> ExportBackupAsync(string executionId, string destinationPath, CancellationToken ct = default);

    /// <summary>
    /// Imports a backup from a file.
    /// </summary>
    Task<Result<BackupExecution>> ImportBackupAsync(string sourcePath, string jobId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a backup starts.
    /// </summary>
    event EventHandler<BackupStartedEventArgs>? BackupStarted;

    /// <summary>
    /// Event raised when a backup completes.
    /// </summary>
    event EventHandler<BackupCompletedEventArgs>? BackupCompleted;

    /// <summary>
    /// Event raised when a restore completes.
    /// </summary>
    event EventHandler<RestoreCompletedEventArgs>? RestoreCompleted;
}

/// <summary>
/// Backup job definition.
/// </summary>
public sealed record BackupJob(
    string Id,
    string Name,
    string? Description,
    BackupType Type,
    BackupSource Source,
    BackupDestination Destination,
    BackupSchedule? Schedule,
    RetentionPolicy Retention,
    StorageTieringPolicy? Tiering,
    CompressionOptions Compression,
    EncryptionOptions Encryption,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? LastExecutedAt = null);

/// <summary>
/// Request to create a backup job.
/// </summary>
public sealed record CreateBackupJobRequest(
    string Name,
    string? Description,
    BackupType Type,
    BackupSource Source,
    BackupDestination Destination,
    BackupSchedule? Schedule = null,
    RetentionPolicy? Retention = null,
    StorageTieringPolicy? Tiering = null,
    CompressionOptions? Compression = null,
    EncryptionOptions? Encryption = null);

/// <summary>
/// Request to update a backup job.
/// </summary>
public sealed record UpdateBackupJobRequest(
    string? Name = null,
    string? Description = null,
    BackupSchedule? Schedule = null,
    RetentionPolicy? Retention = null,
    StorageTieringPolicy? Tiering = null,
    bool? IsEnabled = null);

/// <summary>
/// Backup source configuration.
/// </summary>
public sealed record BackupSource(
    string Path,
    BackupSourceType Type,
    IReadOnlyList<string>? IncludePatterns = null,
    IReadOnlyList<string>? ExcludePatterns = null,
    IReadOnlyList<string>? ExcludeFiles = null);

/// <summary>
/// Backup destination configuration.
/// </summary>
public sealed record BackupDestination(
    string Path,
    DestinationType Type,
    DestinationCredentials? Credentials = null);

/// <summary>
/// Destination credentials.
/// </summary>
public sealed record DestinationCredentials(
    string? Username,
    string? Password,
    string? AccessKey,
    string? SecretKey,
    string? Token);

/// <summary>
/// Backup schedule.
/// </summary>
public sealed record BackupSchedule(
    bool Enabled,
    ScheduleFrequency Frequency,
    DateTime? StartTime,
    IReadOnlyList<DayOfWeek>? DaysOfWeek,
    int? DayOfMonth,
    string? CronExpression);

/// <summary>
/// Retention policy.
/// </summary>
public sealed record RetentionPolicy(
    int KeepLastN,
    int? KeepDailyForDays,
    int? KeepWeeklyForWeeks,
    int? KeepMonthlyForMonths,
    int? KeepYearlyForYears,
    bool DeletePermanently = false);

/// <summary>
/// Storage tiering policy for cold storage.
/// </summary>
public sealed record StorageTieringPolicy(
    bool Enabled,
    TimeSpan MoveToColdAfter,
    TimeSpan MoveToArchiveAfter,
    StorageTier ColdStorageTier,
    StorageTier ArchiveStorageTier);

/// <summary>
/// Compression options.
/// </summary>
public sealed record CompressionOptions(
    bool Enabled,
    CompressionLevel Level,
    CompressionAlgorithm Algorithm);

/// <summary>
/// Encryption options.
/// </summary>
public sealed record EncryptionOptions(
    bool Enabled,
    EncryptionAlgorithm Algorithm,
    string KeyId);

/// <summary>
/// Backup options for execution.
/// </summary>
public sealed record BackupOptions(
    bool FullBackup,
    bool VerifyAfter,
    string? Label,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// Backup result.
/// </summary>
public sealed record BackupResult(
    string ExecutionId,
    string JobId,
    bool Success,
    BackupType Type,
    long FilesProcessed,
    long BytesProcessed,
    long BytesChanged,
    long BlocksProcessed,
    long BlocksChanged,
    TimeSpan Duration,
    DateTime ExecutedAt,
    string? ErrorMessage = null);

/// <summary>
/// Backup execution record.
/// </summary>
public sealed record BackupExecution(
    string Id,
    string JobId,
    BackupType Type,
    bool Success,
    long FilesProcessed,
    long BytesProcessed,
    DateTime ExecutedAt,
    TimeSpan Duration,
    string? Label = null,
    string? ErrorMessage = null);

/// <summary>
/// Restore options.
/// </summary>
public sealed record RestoreOptions(
    string TargetPath,
    bool OverwriteExisting,
    IReadOnlyList<string>? SpecificFiles = null,
    DateTime? PointInTime = null);

/// <summary>
/// Restore result.
/// </summary>
public sealed record RestoreResult(
    bool Success,
    long FilesRestored,
    long BytesRestored,
    TimeSpan Duration,
    string TargetPath,
    DateTime RestoredAt,
    string? ErrorMessage = null);

/// <summary>
/// Restore point.
/// </summary>
public sealed record RestorePoint(
    string ExecutionId,
    DateTime Timestamp,
    BackupType Type,
    long Size,
    string? Label = null);

/// <summary>
/// Verification result.
/// </summary>
public sealed record VerificationResult(
    string ExecutionId,
    bool IsValid,
    long FilesVerified,
    long FilesCorrupted,
    IReadOnlyList<string>? CorruptedFiles = null);

/// <summary>
/// Cleanup result.
/// </summary>
public sealed record CleanupResult(
    int BackupsDeleted,
    long SpaceFreed,
    IReadOnlyList<string> DeletedExecutionIds);

/// <summary>
/// Backup statistics.
/// </summary>
public sealed record BackupStatistics(
    int TotalJobs,
    int TotalExecutions,
    long TotalSize,
    long CompressedSize,
    double CompressionRatio,
    long ColdStorageSize,
    long ArchiveStorageSize,
    DateTime CalculatedAt);

/// <summary>
/// Backup types.
/// </summary>
public enum BackupType
{
    Full,
    Incremental,
    Differential,
    BlockLevelIncremental
}

/// <summary>
/// Backup source types.
/// </summary>
public enum BackupSourceType
{
    Directory,
    GameLibrary,
    SaveStates,
    Screenshots,
    Configuration,
    Custom
}

/// <summary>
/// Destination types.
/// </summary>
public enum DestinationType
{
    Local,
    Network,
    S3,
    AzureBlob,
    GoogleCloud,
    Ftp,
    Sftp
}

/// <summary>
/// Schedule frequencies.
/// </summary>
public enum ScheduleFrequency
{
    Manual,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Custom
}

/// <summary>
/// Compression levels.
/// </summary>
public enum CompressionLevel
{
    None,
    Fast,
    Balanced,
    Maximum
}

/// <summary>
/// Compression algorithms.
/// </summary>
public enum CompressionAlgorithm
{
    None,
    Deflate,
    LZ4,
    Zstd,
    Brotli
}

/// <summary>
/// Encryption algorithms.
/// </summary>
public enum EncryptionAlgorithm
{
    None,
    Aes256,
    ChaCha20
}

/// <summary>
/// Storage tiers.
/// </summary>
public enum StorageTier
{
    Hot,
    Cool,
    Cold,
    Archive
}

/// <summary>
/// Event args for backup started events.
/// </summary>
public sealed class BackupStartedEventArgs : EventArgs
{
    public string ExecutionId { get; }
    public string JobId { get; }
    public DateTime StartedAt { get; }

    public BackupStartedEventArgs(string executionId, string jobId)
    {
        ExecutionId = executionId;
        JobId = jobId;
        StartedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event args for backup completed events.
/// </summary>
public sealed class BackupCompletedEventArgs : EventArgs
{
    public string ExecutionId { get; }
    public string JobId { get; }
    public bool Success { get; }
    public long FilesProcessed { get; }
    public TimeSpan Duration { get; }
    public DateTime CompletedAt { get; }

    public BackupCompletedEventArgs(string executionId, string jobId, bool success, long filesProcessed, TimeSpan duration)
    {
        ExecutionId = executionId;
        JobId = jobId;
        Success = success;
        FilesProcessed = filesProcessed;
        Duration = duration;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event args for restore completed events.
/// </summary>
public sealed class RestoreCompletedEventArgs : EventArgs
{
    public string ExecutionId { get; }
    public bool Success { get; }
    public long FilesRestored { get; }
    public TimeSpan Duration { get; }
    public DateTime CompletedAt { get; }

    public RestoreCompletedEventArgs(string executionId, bool success, long filesRestored, TimeSpan duration)
    {
        ExecutionId = executionId;
        Success = success;
        FilesRestored = filesRestored;
        Duration = duration;
        CompletedAt = DateTime.UtcNow;
    }
}
