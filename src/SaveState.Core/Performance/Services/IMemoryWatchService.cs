using SaveState.Core.Common;
using SaveState.Core.Performance.Entities;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Core.Performance.Services;

/// <summary>
/// Service for managing memory watches.
/// </summary>
public interface IMemoryWatchService
{
    /// <summary>
    /// Creates a new memory watch.
    /// </summary>
    Task<Result<MemoryWatch>> CreateWatchAsync(
        Guid gameId,
        string label,
        MemoryAddress address,
        MemoryDataType dataType,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active watches for a game.
    /// </summary>
    Task<Result<IReadOnlyList<MemoryWatch>>> GetWatchesAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the value of a watch by reading from memory.
    /// </summary>
    Task<Result> UpdateWatchValueAsync(
        Guid watchId,
        int processId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates all active watches for a game.
    /// </summary>
    Task<Result<int>> UpdateAllWatchesAsync(
        Guid gameId,
        int processId,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a memory watch.
    /// </summary>
    Task<Result> DeleteWatchAsync(
        Guid watchId,
        CancellationToken ct = default);

    /// <summary>
    /// Toggles the freeze state of a watch.
    /// </summary>
    Task<Result> ToggleFreezeAsync(
        Guid watchId,
        CancellationToken ct = default);

    /// <summary>
    /// Writes a value to the monitored memory address.
    /// </summary>
    Task<Result> WriteWatchValueAsync(
        Guid watchId,
        int processId,
        string newValue,
        CancellationToken ct = default);

    /// <summary>
    /// Exports watches to JSON.
    /// </summary>
    Task<Result<string>> ExportWatchesAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Imports watches from JSON.
    /// </summary>
    Task<Result<int>> ImportWatchesAsync(
        Guid gameId,
        string json,
        CancellationToken ct = default);
}
