using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

public interface ISaveStateBranchRepository
{
    Task<SaveStateBranch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SaveStateBranch>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<SaveStateBranch>> GetByRootStateIdAsync(Guid rootStateId, CancellationToken ct = default);
    Task AddAsync(SaveStateBranch branch, CancellationToken ct = default);
    Task UpdateAsync(SaveStateBranch branch, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class SaveStateBranchRepository : ISaveStateBranchRepository
{
    private readonly SaveStateDbContext _context;

    public SaveStateBranchRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<SaveStateBranch?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.SaveStateBranches
            .FirstOrDefaultAsync(sb => sb.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SaveStateBranch>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.SaveStateBranches
            .Join(_context.SaveStates,
                branch => branch.RootStateId,
                state => state.Id,
                (branch, state) => new { Branch = branch, State = state })
            .Where(x => x.State.GameId == gameId)
            .Select(x => x.Branch)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SaveStateBranch>> GetByRootStateIdAsync(Guid rootStateId, CancellationToken ct = default)
    {
        return await _context.SaveStateBranches
            .Where(sb => sb.RootStateId == rootStateId)
            .OrderByDescending(sb => sb.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(SaveStateBranch branch, CancellationToken ct = default)
    {
        await _context.SaveStateBranches.AddAsync(branch, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(SaveStateBranch branch, CancellationToken ct = default)
    {
        _context.SaveStateBranches.Update(branch);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var branch = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (branch != null)
        {
            _context.SaveStateBranches.Remove(branch);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}