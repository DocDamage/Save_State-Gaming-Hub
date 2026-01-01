using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary;

public interface IBacklogRepository
{
    Task<BacklogEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BacklogEntry?> GetByGameIdAsync(Guid gameId, CancellationToken ct = default);
    Task<PagedResult<BacklogEntry>> GetBacklogAsync(
        int pageNumber = 1,
        int pageSize = 50,
        BacklogStatus? status = null,
        CancellationToken ct = default);
    Task AddAsync(BacklogEntry entry, CancellationToken ct = default);
    Task UpdateAsync(BacklogEntry entry, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(BacklogStatus? status = null, CancellationToken ct = default);
    Task<BacklogStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

public sealed record BacklogStatistics(
    int TotalGames,
    int NotStarted,
    int InProgress,
    int OnHold,
    int Completed,
    int Abandoned,
    TimeSpan TotalEstimatedPlaytime);