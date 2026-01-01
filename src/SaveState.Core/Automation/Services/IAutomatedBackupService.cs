using SaveState.Core.Common;
using SaveState.Core.Automation.Services.DTOs;

namespace SaveState.Core.Automation.Services;

/// <summary>
/// Service for performing automated backups of games, save states, and configurations.
/// </summary>
public interface IAutomatedBackupService
{
    /// <summary>
    /// Performs a backup of a game and its save states.
    /// </summary>
    Task<Result<BackupResult>> BackupGameAsync(
        Guid gameId,
        BackupOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Performs a backup of all save states for a game.
    /// </summary>
    Task<Result<BackupResult>> BackupSaveStatesAsync(
        Guid gameId,
        BackupOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Performs a backup of application configuration and settings.
    /// </summary>
    Task<Result<BackupResult>> BackupConfigurationAsync(
        BackupOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Restores a game from a backup.
    /// </summary>
    Task<Result<RestoreResult>> RestoreGameAsync(
        Guid backupId,
        RestoreOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Restores save states from a backup.
    /// </summary>
    Task<Result<RestoreResult>> RestoreSaveStatesAsync(
        Guid backupId,
        RestoreOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Restores configuration from a backup.
    /// </summary>
    Task<Result<RestoreResult>> RestoreConfigurationAsync(
        Guid backupId,
        RestoreOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets information about a backup.
    /// </summary>
    Task<Result<BackupInfo>> GetBackupInfoAsync(
        Guid backupId,
        CancellationToken ct = default);

    /// <summary>
    /// Lists all available backups.
    /// </summary>
    Task<Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(
        BackupFilter filter,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a backup.
    /// </summary>
    Task<Result> DeleteBackupAsync(
        Guid backupId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates that a backup is intact and restorable.
    /// </summary>
    Task<Result<BackupValidationResult>> ValidateBackupAsync(
        Guid backupId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets backup storage statistics.
    /// </summary>
    Task<Result<BackupStorageStats>> GetStorageStatsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Cleans up old backups based on retention policies.
    /// </summary>
    Task<Result<CleanupResult>> CleanupOldBackupsAsync(
        CleanupPolicy policy,
        CancellationToken ct = default);
}