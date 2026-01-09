using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Entities;
using SaveState.Infrastructure.Persistence;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.Repositories;

public class SaveStateRepository : ISaveStateRepository
{
    private readonly SaveStateDbContext _context;

    public SaveStateRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<SaveStateEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.SaveStates
            .FirstOrDefaultAsync(ss => ss.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SaveStateEntity>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.SaveStates
            .Where(ss => ss.GameId == gameId)
            .OrderByDescending(ss => ss.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<SaveStateEntity>> GetPagedByGameIdAsync(
        Guid gameId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _context.SaveStates
            .Where(ss => ss.GameId == gameId)
            .OrderByDescending(ss => ss.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<SaveStateEntity>(items, totalCount, pageNumber, pageSize);
    }

    public async Task AddAsync(SaveStateEntity saveState, CancellationToken ct = default)
    {
        await _context.SaveStates.AddAsync(saveState, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(SaveStateEntity saveState, CancellationToken ct = default)
    {
        _context.SaveStates.Update(saveState);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var saveState = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (saveState != null)
        {
            _context.SaveStates.Remove(saveState);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<int> CountByGameIdAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.SaveStates
            .CountAsync(ss => ss.GameId == gameId, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SaveStateEntity>> GetTimelineAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.SaveStates
            .Where(ss => ss.GameId == gameId)
            .OrderBy(ss => ss.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddBranchAsync(SaveStateBranch branch, CancellationToken ct = default)
    {
        await _context.SaveStateBranches.AddAsync(branch, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
