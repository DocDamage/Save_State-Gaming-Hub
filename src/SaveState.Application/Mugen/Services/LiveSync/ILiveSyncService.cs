using SaveState.Application.Mugen.Models.LiveSync;
using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.LiveSync;

/// <summary>
/// Interface for cross-platform synchronization service.
/// Provides unified accounts, seamless data sync, and consistent experiences across all devices and platforms.
/// </summary>
public interface ILiveSyncService
{
    #region Account Management

    /// <summary>
    /// Creates a new unified account.
    /// </summary>
    Task<Result<UnifiedAccount>> CreateUnifiedAccountAsync(
        UnifiedAccountRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a unified account by ID.
    /// </summary>
    Task<Result<UnifiedAccount>> GetUnifiedAccountAsync(
        string accountId,
        CancellationToken ct = default);

    /// <summary>
    /// Links a platform account to a unified account.
    /// </summary>
    Task<Result> LinkPlatformAccountAsync(
        string accountId,
        PlatformAccountLinkRequest request,
        CancellationToken ct = default);

    #endregion

    #region Sync Operations

    /// <summary>
    /// Starts a new sync session.
    /// </summary>
    Task<Result<SyncSession>> StartSyncSessionAsync(
        SyncSessionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current sync status for a session.
    /// </summary>
    Task<Result<SyncStatus>> GetSyncStatusAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the progress of a sync session.
    /// </summary>
    Task<Result<SyncProgress>> GetSyncProgressAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets conflicts for a sync session.
    /// </summary>
    Task<Result<IReadOnlyList<SyncConflict>>> GetSyncConflictsAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a sync conflict.
    /// </summary>
    Task<Result> ResolveSyncConflictAsync(
        string sessionId,
        ConflictResolution resolution,
        CancellationToken ct = default);

    #endregion

    #region Data Operations

    /// <summary>
    /// Gets platform-specific data for an account.
    /// </summary>
    Task<Result<PlatformData>> GetPlatformDataAsync(
        string accountId,
        PlatformType platform,
        CancellationToken ct = default);

    /// <summary>
    /// Gets cross-platform statistics for an account.
    /// </summary>
    Task<Result<CrossPlatformStats>> GetCrossPlatformStatsAsync(
        string accountId,
        CancellationToken ct = default);

    /// <summary>
    /// Migrates data between platforms.
    /// </summary>
    Task<Result> MigratePlatformDataAsync(
        string accountId,
        PlatformMigrationRequest request,
        CancellationToken ct = default);

    #endregion

    #region Backup Operations

    /// <summary>
    /// Creates a backup of account data.
    /// </summary>
    Task<Result<AccountBackup>> CreateAccountBackupAsync(
        string accountId,
        CancellationToken ct = default);

    /// <summary>
    /// Restores account data from a backup.
    /// </summary>
    Task<Result> RestoreAccountBackupAsync(
        string accountId,
        AccountBackup backup,
        CancellationToken ct = default);

    #endregion
}
