using Microsoft.EntityFrameworkCore;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Core.OpenMK.ValueObjects;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.OpenMK;

/// <summary>
/// Repository implementation for OpenMK character data access.
/// </summary>
public class OpenMKCharacterRepository : IOpenMKCharacterRepository
{
    private readonly SaveStateDbContext _context;

    public OpenMKCharacterRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OpenMKCharacter>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Set<OpenMKCharacter>()
            .ToListAsync(ct);
    }

    public async Task<OpenMKCharacter?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Set<OpenMKCharacter>()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<OpenMKCharacter>> GetByRealmAsync(OpenMKRealm realm, CancellationToken ct = default)
    {
        return await _context.Set<OpenMKCharacter>()
            .Where(c => c.Realm == realm)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OpenMKCharacter>> GetByFightingStyleAsync(OpenMKFightingStyle style, CancellationToken ct = default)
    {
        return await _context.Set<OpenMKCharacter>()
            .Where(c => c.FightingStyle == style)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OpenMKCharacter>> GetByAlignmentAsync(OpenMKAlignment alignment, CancellationToken ct = default)
    {
        return await _context.Set<OpenMKCharacter>()
            .Where(c => c.Alignment == alignment)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OpenMKCharacter>> GetDefaultUnlockedAsync(CancellationToken ct = default)
    {
        return await _context.Set<OpenMKCharacter>()
            .Where(c => c.IsDefaultUnlocked)
            .ToListAsync(ct);
    }

    public async Task AddAsync(OpenMKCharacter character, CancellationToken ct = default)
    {
        _context.Set<OpenMKCharacter>().Add(character);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OpenMKCharacter character, CancellationToken ct = default)
    {
        _context.Set<OpenMKCharacter>().Update(character);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var character = await GetByIdAsync(id, ct);
        if (character != null)
        {
            _context.Set<OpenMKCharacter>().Remove(character);
            await _context.SaveChangesAsync(ct);
        }
    }
}