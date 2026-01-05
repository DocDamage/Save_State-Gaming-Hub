namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

public class GameModRepository : IGameModRepository
{
    private readonly SaveStateDbContext _context;

    public GameModRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GameMod>> GetByGameIdAsync(GameId gameId, CancellationToken ct = default)
    {
        return await _context.GameMods
            .Where(m => m.GameId == gameId)
            .OrderBy(m => m.LoadOrder)
            .ThenBy(m => m.Name)
            .ToListAsync(ct);
    }

    public async Task<GameMod?> GetByIdAsync(Guid modId, CancellationToken ct = default)
    {
        return await _context.GameMods
            .FirstOrDefaultAsync(m => m.Id == modId, ct);
    }

    public async Task<GameMod> AddAsync(GameMod mod, CancellationToken ct = default)
    {
        await _context.GameMods.AddAsync(mod, ct);
        await _context.SaveChangesAsync(ct);
        return mod;
    }

    public async Task UpdateAsync(GameMod mod, CancellationToken ct = default)
    {
        _context.GameMods.Update(mod);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid modId, CancellationToken ct = default)
    {
        var mod = await GetByIdAsync(modId, ct);
        if (mod != null)
        {
            _context.GameMods.Remove(mod);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<GameMod>> GetByCategoryAsync(GameId gameId, string category, CancellationToken ct = default)
    {
        return await _context.GameMods
            .Where(m => m.GameId == gameId && m.Category == category)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GameMod>> GetEnabledModsAsync(GameId gameId, CancellationToken ct = default)
    {
        return await _context.GameMods
            .Where(m => m.GameId == gameId && m.IsEnabled)
            .OrderBy(m => m.LoadOrder)
            .ToListAsync(ct);
    }
}
