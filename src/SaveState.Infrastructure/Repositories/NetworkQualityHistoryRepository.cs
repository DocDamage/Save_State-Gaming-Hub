using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Sync;
using SaveState.Core.Sync.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

/// <summary>
/// Repository for managing network quality history records in the database.
/// </summary>
public class NetworkQualityHistoryRepository : INetworkQualityHistoryRepository
{
    private readonly SaveStateDbContext _context;

    /// <summary>
    /// Initializes a new instance of NetworkQualityHistoryRepository.
    /// </summary>
    /// <param name="context">The database context for accessing network quality history data.</param>
    public NetworkQualityHistoryRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a new network quality history record.
    /// </summary>
    /// <param name="history">The history record to add.</param>
    /// <param name="ct">Cancellation token for operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> AddAsync(NetworkQualityHistory history, CancellationToken ct = default)
    {
        try
        {
            await _context.NetworkQualityHistories.AddAsync(history, ct).ConfigureAwait(false);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to add network quality history: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets network quality history for the specified time range.
    /// </summary>
    /// <param name="startTime">The start of the time range.</param>
    /// <param name="endTime">The end of the time range.</param>
    /// <param name="ct">Cancellation token for operation.</param>
    /// <returns>A result containing historical network quality data.</returns>
    public async Task<Result<IReadOnlyList<NetworkQualityHistory>>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct = default)
    {
        try
        {
            var history = await _context.NetworkQualityHistories
                .AsNoTracking()
                .Where(h => h.MeasuredAt >= startTime && h.MeasuredAt <= endTime)
                .OrderBy(h => h.MeasuredAt)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return Result.Success<IReadOnlyList<NetworkQualityHistory>>(history.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<NetworkQualityHistory>>(
                $"Failed to get network quality history: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets network quality history for a specific session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="ct">Cancellation token for operation.</param>
    /// <returns>A result containing historical network quality data for the session.</returns>
    public async Task<Result<IReadOnlyList<NetworkQualityHistory>>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            var history = await _context.NetworkQualityHistories
                .AsNoTracking()
                .Where(h => h.SessionId == sessionId)
                .OrderBy(h => h.MeasuredAt)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return Result.Success<IReadOnlyList<NetworkQualityHistory>>(history.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<NetworkQualityHistory>>(
                $"Failed to get network quality history for session: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Deletes network quality history records older than the specified date.
    /// </summary>
    /// <param name="beforeDate">The cutoff date. Records older than this will be deleted.</param>
    /// <param name="ct">Cancellation token for operation.</param>
    /// <returns>A result containing the number of records deleted.</returns>
    public async Task<Result<int>> DeleteOlderThanAsync(DateTime beforeDate, CancellationToken ct = default)
    {
        try
        {
            var oldRecords = await _context.NetworkQualityHistories
                .Where(h => h.MeasuredAt < beforeDate)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _context.NetworkQualityHistories.RemoveRange(oldRecords);
            var deletedCount = await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            return Result.Success<int>(deletedCount);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Failed to delete old network quality history: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets the count of network quality history records.
    /// </summary>
    /// <param name="ct">Cancellation token for operation.</param>
    /// <returns>The total number of records.</returns>
    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        return await _context.NetworkQualityHistories.CountAsync(ct).ConfigureAwait(false);
    }
}
