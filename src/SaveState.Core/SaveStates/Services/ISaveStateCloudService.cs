using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services.DTOs;

namespace SaveState.Core.SaveStates.Services;

/// <summary>
/// Service for synchronizing save states with cloud storage providers.
/// </summary>
public interface ISaveStateCloudService
{
    /// <summary>
    /// Synchronizes a save state for the given game.
    /// </summary>
    Task<Result<SaveStateCloudSyncStatus>> SyncSaveStateAsync(
        Guid gameId,
        SaveStateCloudMetadata metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Detects conflicts between local and cloud save state versions.
    /// </summary>
    Task<Result<SaveStateConflictResolution>> DetectConflictsAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates and stores a local version snapshot for the latest save state.
    /// </summary>
    Task<Result<SaveStateCloudVersion>> CreateVersionAsync(
        Guid gameId,
        string versionName,
        CancellationToken ct = default);

    /// <summary>
    /// Applies a conflict resolution strategy for a game's cloud save state.
    /// </summary>
    Task<Result<SaveStateCloudSyncStatus>> ResolveConflictAsync(
        Guid gameId,
        SaveStateConflictResolutionStrategy strategy,
        SaveStateCloudMetadata? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets version history for a game.
    /// </summary>
    Task<Result<IReadOnlyList<SaveStateCloudVersion>>> GetVersionHistoryAsync(
        Guid gameId,
        CancellationToken ct = default);
}
