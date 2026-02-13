using Microsoft.EntityFrameworkCore;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.OpenMK;

/// <summary>
/// Repository implementation for OpenMK match state persistence.
/// </summary>
public class OpenMKMatchStateRepository : IOpenMKMatchStateRepository
{
    private readonly SaveStateDbContext _context;

    public OpenMKMatchStateRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<OpenMKMatchState?> GetByMatchIdAsync(Guid matchId, CancellationToken ct = default)
    {
        return await _context.Set<OpenMKMatchState>()
            .FirstOrDefaultAsync(ms => ms.MatchId == matchId, ct);
    }

    public async Task AddAsync(OpenMKMatchState matchState, CancellationToken ct = default)
    {
        _context.Set<OpenMKMatchState>().Add(matchState);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OpenMKMatchState matchState, CancellationToken ct = default)
    {
        _context.Set<OpenMKMatchState>().Update(matchState);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid matchId, CancellationToken ct = default)
    {
        var matchState = await GetByMatchIdAsync(matchId, ct);
        if (matchState != null)
        {
            _context.Set<OpenMKMatchState>().Remove(matchState);
            await _context.SaveChangesAsync(ct);
        }
    }
}
