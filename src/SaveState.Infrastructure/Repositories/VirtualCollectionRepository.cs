using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

public class VirtualCollectionRepository : IVirtualCollectionRepository
{
    private readonly SaveStateDbContext _context;

    public VirtualCollectionRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<VirtualCollection?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.VirtualCollections
            .Include(vc => vc.Games)
                .ThenInclude(vcg => vcg.Game)
            .FirstOrDefaultAsync(vc => vc.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<VirtualCollection>> GetAllAsync(bool includeSystemCollections = true, CancellationToken ct = default)
    {
        var query = _context.VirtualCollections.AsQueryable();

        if (!includeSystemCollections)
        {
            query = query.Where(vc => !vc.IsSystemCollection);
        }

        return await query
            .OrderBy(vc => vc.SortOrder)
            .ThenBy(vc => vc.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<VirtualCollection>> GetSmartCollectionsAsync(CancellationToken ct = default)
    {
        return await _context.VirtualCollections
            .Where(vc => vc.Type == CollectionType.Smart)
            .OrderBy(vc => vc.SortOrder)
            .ThenBy(vc => vc.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<VirtualCollection>> GetManualCollectionsAsync(CancellationToken ct = default)
    {
        return await _context.VirtualCollections
            .Where(vc => vc.Type == CollectionType.Manual)
            .OrderBy(vc => vc.SortOrder)
            .ThenBy(vc => vc.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(VirtualCollection collection, CancellationToken ct = default)
    {
        await _context.VirtualCollections.AddAsync(collection, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(VirtualCollection collection, CancellationToken ct = default)
    {
        _context.VirtualCollections.Update(collection);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var collection = await GetByIdAsync(id, ct).ConfigureAwait(false);
        if (collection != null)
        {
            _context.VirtualCollections.Remove(collection);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task AddGameToCollectionAsync(Guid collectionId, Guid gameId, int sortOrder = 0, CancellationToken ct = default)
    {
        var gameExists = await _context.Games.AnyAsync(g => g.Id == gameId, ct);
        if (!gameExists)
            throw new ArgumentException("Game not found", nameof(gameId));

        var collectionExists = await _context.VirtualCollections.AnyAsync(vc => vc.Id == collectionId, ct);
        if (!collectionExists)
            throw new ArgumentException("Collection not found", nameof(collectionId));

        var existingEntry = await _context.Set<VirtualCollectionGame>()
            .FirstOrDefaultAsync(vcg => vcg.CollectionId == collectionId && vcg.GameId == gameId, ct);

        if (existingEntry != null)
            return; // Already in collection

        var entry = VirtualCollectionGame.Create(collectionId, gameId, sortOrder);
        await _context.Set<VirtualCollectionGame>().AddAsync(entry, ct);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveGameFromCollectionAsync(Guid collectionId, Guid gameId, CancellationToken ct = default)
    {
        var entry = await _context.Set<VirtualCollectionGame>()
            .FirstOrDefaultAsync(vcg => vcg.CollectionId == collectionId && vcg.GameId == gameId, ct);

        if (entry != null)
        {
            _context.Set<VirtualCollectionGame>().Remove(entry);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<Game>> GetGamesInCollectionAsync(Guid collectionId, CancellationToken ct = default)
    {
        var collection = await _context.VirtualCollections
            .Include(vc => vc.Games)
                .ThenInclude(vcg => vcg.Game)
            .FirstOrDefaultAsync(vc => vc.Id == collectionId, ct);

        if (collection == null)
            return Array.Empty<Game>();

        return collection.Games
            .OrderBy(vcg => vcg.SortOrder)
            .ThenBy(vcg => vcg.AddedAt)
            .Select(vcg => vcg.Game)
            .ToList();
    }

    public async Task<IReadOnlyList<VirtualCollection>> GetCollectionsForGameAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _context.Set<VirtualCollectionGame>()
            .Where(vcg => vcg.GameId == gameId)
            .Include(vcg => vcg.Collection)
            .Select(vcg => vcg.Collection)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<int> GetCollectionCountAsync(Guid collectionId, CancellationToken ct = default)
    {
        return await _context.Set<VirtualCollectionGame>()
            .CountAsync(vcg => vcg.CollectionId == collectionId, ct)
            .ConfigureAwait(false);
    }
}