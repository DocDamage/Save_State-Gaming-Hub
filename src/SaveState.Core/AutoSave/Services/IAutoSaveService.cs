using SaveState.Core.Common;

namespace SaveState.Core.AutoSave.Services;

/// <summary>
/// Service for managing auto-save functionality.
/// </summary>
public interface IAutoSaveService
{
    /// <summary>
    /// Configures auto-save settings for a game.
    /// </summary>
    Task<Result<AutoSaveConfiguration>> ConfigureAutoSaveAsync(
        ConfigureAutoSaveRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets auto-save configuration for a game.
    /// </summary>
    Task<Result<AutoSaveConfiguration>> GetConfigurationAsync(
        Guid gameId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Enables auto-save for a game.
    /// </summary>
    Task<Result> EnableAutoSaveAsync(
        Guid gameId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Disables auto-save for a game.
    /// </summary>
    Task<Result> DisableAutoSaveAsync(
        Guid gameId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Triggers a manual auto-save.
    /// </summary>
    Task<Result<AutoSaveEntry>> TriggerAutoSaveAsync(
        TriggerAutoSaveRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Starts auto-save session for a running game.
    /// </summary>
    Task<Result<AutoSaveSession>> StartSessionAsync(
        Guid gameId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Stops auto-save session.
    /// </summary>
    Task<Result> StopSessionAsync(
        Guid sessionId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets active session for a game.
    /// </summary>
    Task<Result<AutoSaveSession>> GetActiveSessionAsync(
        Guid gameId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Updates session with current game state.
    /// </summary>
    Task<Result> UpdateSessionAsync(
        Guid sessionId,
        string? currentLevel,
        int playTimeSeconds,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all auto-saves for a game.
    /// </summary>
    Task<Result<List<AutoSaveEntry>>> GetAutoSavesAsync(
        Guid gameId,
        AutoSaveFilter? filter = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a specific auto-save entry.
    /// </summary>
    Task<Result<AutoSaveEntry>> GetAutoSaveAsync(
        Guid autoSaveId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Locks an auto-save (prevents deletion).
    /// </summary>
    Task<Result> LockAutoSaveAsync(
        Guid autoSaveId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Unlocks an auto-save.
    /// </summary>
    Task<Result> UnlockAutoSaveAsync(
        Guid autoSaveId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes an auto-save.
    /// </summary>
    Task<Result> DeleteAutoSaveAsync(
        Guid autoSaveId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes all auto-saves for a game.
    /// </summary>
    Task<Result<int>> DeleteAllAutoSavesAsync(
        Guid gameId,
        bool includeLocked = false,
        CancellationToken ct = default);
    
    /// <summary>
    /// Cleans up old auto-saves based on retention policy.
    /// </summary>
    Task<Result<int>> CleanupOldSavesAsync(
        Guid gameId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets auto-save statistics for a game.
    /// </summary>
    Task<Result<AutoSaveStatistics>> GetStatisticsAsync(
        Guid gameId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Restores from an auto-save.
    /// </summary>
    Task<Result<string>> RestoreAutoSaveAsync(
        Guid autoSaveId,
        string? targetPath = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Handles level completion event.
    /// </summary>
    Task<Result<AutoSaveEntry>> HandleLevelCompleteAsync(
        Guid gameId,
        string levelName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Handles checkpoint reached event.
    /// </summary>
    Task<Result<AutoSaveEntry>> HandleCheckpointAsync(
        Guid gameId,
        string checkpointName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Detects boss fight approaching.
    /// </summary>
    Task<Result<bool>> DetectBossFightAsync(
        Guid gameId,
        Dictionary<string, object> gameState,
        CancellationToken ct = default);
    
    /// <summary>
    /// Handles boss approach event.
    /// </summary>
    Task<Result<AutoSaveEntry>> HandleBossApproachAsync(
        Guid gameId,
        string? bossName = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exports an auto-save to a file.
    /// </summary>
    Task<Result<string>> ExportAutoSaveAsync(
        Guid autoSaveId,
        string outputPath,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets storage usage for auto-saves.
    /// </summary>
    Task<Result<long>> GetStorageUsageAsync(
        Guid gameId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Prunes old auto-saves to free space.
    /// </summary>
    Task<Result<int>> PruneAutoSavesAsync(
        Guid gameId,
        long targetFreeSpace,
        CancellationToken ct = default);
}
