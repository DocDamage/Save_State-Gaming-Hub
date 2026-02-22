using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.Services;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.OpenMK;

/// <summary>
/// Repository implementation for OpenMK user progress and unlocks.
/// </summary>
public class OpenMKProgressRepository : IOpenMKProgressRepository
{
    private readonly SaveStateDbContext _context;
    private readonly ITimeProvider _timeProvider;

    public OpenMKProgressRepository(SaveStateDbContext context, ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<OpenMKCharacter>> GetUnlockedCharactersAsync(Guid userId, CancellationToken ct = default)
    {
        var defaultUnlocked = await _context.Set<OpenMKCharacter>()
            .Where(c => c.IsDefaultUnlocked)
            .ToListAsync(ct);

        var unlockedCharacterIds = await _context.Set<OpenMKCharacterUnlock>()
            .Where(u => u.UserId == userId)
            .Select(u => u.CharacterId)
            .ToListAsync(ct);

        var userUnlocked = unlockedCharacterIds.Count == 0
            ? new List<OpenMKCharacter>()
            : await _context.Set<OpenMKCharacter>()
                .Where(c => unlockedCharacterIds.Contains(c.Id))
                .ToListAsync(ct);

        return defaultUnlocked
            .Concat(userUnlocked)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();
    }

    public async Task<bool> IsCharacterUnlockedAsync(Guid userId, Guid characterId, CancellationToken ct = default)
    {
        var character = await _context.Set<OpenMKCharacter>()
            .FirstOrDefaultAsync(c => c.Id == characterId, ct);

        if (character == null)
            return false;

        // If character is default unlocked, it's available
        if (character.IsDefaultUnlocked)
            return true;

        return await _context.Set<OpenMKCharacterUnlock>()
            .AnyAsync(u => u.UserId == userId && u.CharacterId == characterId, ct);
    }

    public async Task UnlockCharacterAsync(Guid userId, Guid characterId, CancellationToken ct = default)
    {
        var alreadyUnlocked = await _context.Set<OpenMKCharacterUnlock>()
            .AnyAsync(u => u.UserId == userId && u.CharacterId == characterId, ct);

        if (alreadyUnlocked)
        {
            return;
        }

        var unlock = new OpenMKCharacterUnlock(userId, characterId, _timeProvider);
        _context.Set<OpenMKCharacterUnlock>().Add(unlock);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> GetKoinCountAsync(Guid userId, CancellationToken ct = default)
    {
        var progress = await GetOrCreateUserProgressAsync(userId, ct);
        return progress.Koins;
    }

    public async Task AddKoinsAsync(Guid userId, int amount, CancellationToken ct = default)
    {
        var progress = await GetOrCreateUserProgressAsync(userId, ct);
        progress.AddKoins(amount, _timeProvider);
        _context.Set<OpenMKUserProgress>().Update(progress);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> SpendKoinsAsync(Guid userId, int amount, CancellationToken ct = default)
    {
        var progress = await GetOrCreateUserProgressAsync(userId, ct);
        var spent = progress.TrySpendKoins(amount, _timeProvider);
        if (!spent)
        {
            return false;
        }

        _context.Set<OpenMKUserProgress>().Update(progress);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private async Task<OpenMKUserProgress> GetOrCreateUserProgressAsync(Guid userId, CancellationToken ct)
    {
        var progress = await _context.Set<OpenMKUserProgress>()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (progress != null)
        {
            return progress;
        }

        progress = new OpenMKUserProgress(userId, _timeProvider);
        _context.Set<OpenMKUserProgress>().Add(progress);
        await _context.SaveChangesAsync(ct);
        return progress;
    }
}
