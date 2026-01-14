using SaveState.Core.Common;
using SaveState.Core.Sync.Entities;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Core.Sync;

/// <summary>
/// Repository interface for managing network quality history records.
/// </summary>
public interface INetworkQualityHistoryRepository
{
    /// <summary>
    /// Adds a new network quality history record.
    /// </summary>
    /// <param name="history">The history record to add.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AddAsync(NetworkQualityHistory history, CancellationToken ct = default);

    /// <summary>
    /// Gets network quality history for the specified time range.
    /// </summary>
    /// <param name="startTime">The start of the time range.</param>
    /// <param name="endTime">The end of the time range.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the historical network quality data.</returns>
    Task<Result<IReadOnlyList<NetworkQualityHistory>>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct = default);

    /// <summary>
    /// Gets network quality history for a specific session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the historical network quality data for the session.</returns>
    Task<Result<IReadOnlyList<NetworkQualityHistory>>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes network quality history records older than the specified date.
    /// </summary>
    /// <param name="beforeDate">The cutoff date. Records older than this will be deleted.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the number of records deleted.</returns>
    Task<Result<int>> DeleteOlderThanAsync(DateTime beforeDate, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of network quality history records.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The total number of records.</returns>
    Task<int> CountAsync(CancellationToken ct = default);
}
