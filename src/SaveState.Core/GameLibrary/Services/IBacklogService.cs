using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.Services;

public interface IBacklogService
{
    Task<Result<BacklogEntry>> AddToBacklogAsync(Guid gameId, int priority = 50, CancellationToken ct = default);
    Task<Result> RemoveFromBacklogAsync(Guid gameId, CancellationToken ct = default);
    Task<Result> UpdateBacklogStatusAsync(Guid gameId, BacklogStatus status, CancellationToken ct = default);
    Task<Result> UpdatePriorityAsync(Guid gameId, int priority, CancellationToken ct = default);
    Task<Result> UpdateNotesAsync(Guid gameId, string? notes, CancellationToken ct = default);
    Task<Result> SetEstimatedPlaytimeAsync(Guid gameId, TimeSpan? playtime, CancellationToken ct = default);
    Task<Result> SetTargetCompletionDateAsync(Guid gameId, DateTime? date, CancellationToken ct = default);
    Task<Result<BacklogEntry?>> GetBacklogEntryAsync(Guid gameId, CancellationToken ct = default);
    Task<Result<PagedResult<BacklogEntry>>> GetBacklogAsync(
        int pageNumber = 1,
        int pageSize = 50,
        BacklogStatus? status = null,
        CancellationToken ct = default);
    Task<Result<BacklogStatistics>> GetStatisticsAsync(CancellationToken ct = default);
}