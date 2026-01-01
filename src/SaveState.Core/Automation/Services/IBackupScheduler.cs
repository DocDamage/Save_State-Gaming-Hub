using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Core.Automation.Services;

/// <summary>
/// Service for scheduling automated backups of games and save states.
/// </summary>
public interface IBackupScheduler
{
    /// <summary>
    /// Creates a new backup schedule.
    /// </summary>
    Task<Result<BackupSchedule>> CreateScheduleAsync(
        BackupScheduleConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing backup schedule.
    /// </summary>
    Task<Result> UpdateScheduleAsync(
        Guid scheduleId,
        BackupScheduleConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a backup schedule.
    /// </summary>
    Task<Result> DeleteScheduleAsync(
        Guid scheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Enables a backup schedule.
    /// </summary>
    Task<Result> EnableScheduleAsync(
        Guid scheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Disables a backup schedule.
    /// </summary>
    Task<Result> DisableScheduleAsync(
        Guid scheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a backup schedule by ID.
    /// </summary>
    Task<Result<BackupSchedule>> GetScheduleAsync(
        Guid scheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all backup schedules.
    /// </summary>
    Task<Result<IReadOnlyList<BackupSchedule>>> GetAllSchedulesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets backup schedules for a specific game.
    /// </summary>
    Task<Result<IReadOnlyList<BackupSchedule>>> GetSchedulesForGameAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Manually triggers a backup for a schedule.
    /// </summary>
    Task<Result<BackupResult>> TriggerBackupAsync(
        Guid scheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the next scheduled backup time for a schedule.
    /// </summary>
    Task<Result<DateTime?>> GetNextBackupTimeAsync(
        Guid scheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets backup history for a schedule.
    /// </summary>
    Task<Result<IReadOnlyList<BackupResult>>> GetBackupHistoryAsync(
        Guid scheduleId,
        DateTime? since = null,
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when a backup is scheduled.
    /// </summary>
    event EventHandler<BackupScheduledEventArgs>? BackupScheduled;

    /// <summary>
    /// Event raised when a backup starts.
    /// </summary>
    event EventHandler<BackupStartedEventArgs>? BackupStarted;

    /// <summary>
    /// Event raised when a backup completes.
    /// </summary>
    event EventHandler<BackupCompletedEventArgs>? BackupCompleted;

    /// <summary>
    /// Validates a backup schedule configuration.
    /// </summary>
    Task<Result> ValidateScheduleAsync(
        BackupScheduleConfig config,
        CancellationToken ct = default);
}
