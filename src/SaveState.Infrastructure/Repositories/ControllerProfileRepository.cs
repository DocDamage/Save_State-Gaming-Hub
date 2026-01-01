using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Input;
using SaveState.Core.Input.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

public class ControllerProfileRepository : IControllerProfileRepository
{
    private readonly SaveStateDbContext _context;

    public ControllerProfileRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<ControllerProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ControllerProfiles
            .FirstOrDefaultAsync(cp => cp.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ControllerProfile>> GetByGameIdAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.ControllerProfiles
            .Where(cp => cp.GameId == gameId)
            .OrderByDescending(cp => cp.LastUsedAt)
            .ThenBy(cp => cp.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ControllerProfile>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ControllerProfiles
            .OrderBy(cp => cp.Type)
            .ThenBy(cp => cp.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ControllerProfile>> GetByTypeAsync(ControllerType type, CancellationToken ct = default)
    {
        return await _context.ControllerProfiles
            .Where(cp => cp.Type == type)
            .OrderByDescending(cp => cp.LastUsedAt)
            .ThenBy(cp => cp.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<ControllerProfile?> GetDefaultForGameAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.ControllerProfiles
            .Where(cp => cp.GameId == gameId && cp.IsDefault)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ControllerProfile profile, CancellationToken ct = default)
    {
        await _context.ControllerProfiles.AddAsync(profile, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ControllerProfile profile, CancellationToken ct = default)
    {
        _context.ControllerProfiles.Update(profile);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var profile = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (profile != null)
        {
            _context.ControllerProfiles.Remove(profile);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<int> CountAsync(ControllerType? type = null, CancellationToken ct = default)
    {
        var query = _context.ControllerProfiles.AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(cp => cp.Type == type.Value);
        }

        return await query.CountAsync(ct).ConfigureAwait(false);
    }
}