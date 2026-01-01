using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

public class BacklogRepository : IBacklogRepository
{
    private readonly SaveStateDbContext _context;

    public BacklogRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<BacklogEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.BacklogEntries
            .Include(be => be.Game)
            .FirstOrDefaultAsync(be => be.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<BacklogEntry?> GetByGameIdAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.BacklogEntries
            .Include(be => be.Game)
            .FirstOrDefaultAsync(be => be.GameId == gameId, ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<BacklogEntry>> GetBacklogAsync(
        int pageNumber = 1,
        int pageSize = 50,
        BacklogStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _context.BacklogEntries
            .Include(be => be.Game)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(be => be.Status == status.Value);
        }

        // Order by priority (descending), then by added date (ascending)
        query = query.OrderByDescending(be => be.Priority)
                    .ThenBy(be => be.AddedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<BacklogEntry>(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task AddAsync(BacklogEntry entry, CancellationToken ct = default)
    {
        await _context.BacklogEntries.AddAsync(entry, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(BacklogEntry entry, CancellationToken ct = default)
    {
        _context.BacklogEntries.Update(entry);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (entry != null)
        {
            _context.BacklogEntries.Remove(entry);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<int> CountAsync(BacklogStatus? status = null, CancellationToken ct = default)
    {
        var query = _context.BacklogEntries.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(be => be.Status == status.Value);
        }

        return await query.CountAsync(ct).ConfigureAwait(false);
    }

    public async Task<BacklogStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var stats = await _context.BacklogEntries
            .GroupBy(be => 1)
            .Select(g => new
            {
                TotalGames = g.Count(),
                NotStarted = g.Count(be => be.Status == BacklogStatus.NotStarted),
                InProgress = g.Count(be => be.Status == BacklogStatus.InProgress),
                OnHold = g.Count(be => be.Status == BacklogStatus.OnHold),
                Completed = g.Count(be => be.Status == BacklogStatus.Completed),
                Abandoned = g.Count(be => be.Status == BacklogStatus.Abandoned),
                TotalEstimatedPlaytime = TimeSpan.FromTicks(g.Sum(be => (be.EstimatedPlaytime ?? TimeSpan.Zero).Ticks))
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (stats == null)
        {
            return new BacklogStatistics(0, 0, 0, 0, 0, 0, TimeSpan.Zero);
        }

        return new BacklogStatistics(
            TotalGames: stats.TotalGames,
            NotStarted: stats.NotStarted,
            InProgress: stats.InProgress,
            OnHold: stats.OnHold,
            Completed: stats.Completed,
            Abandoned: stats.Abandoned,
            TotalEstimatedPlaytime: stats.TotalEstimatedPlaytime);
    }
}