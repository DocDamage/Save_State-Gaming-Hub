using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Social;
using SaveState.Core.Social.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shared collections.
/// </summary>
public class SharedCollectionRepository : ISharedCollectionRepository
{
    private readonly SaveStateDbContext _context;

    public SharedCollectionRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<SharedCollection?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.SharedCollections
            .Include(c => c.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<SharedCollection?> GetByShareCodeAsync(string shareCode, CancellationToken ct = default)
    {
        return await _context.SharedCollections
            .Include(c => c.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(c => c.ShareCode == shareCode, ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<SharedCollection>> GetCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        bool? isPublic = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = _context.SharedCollections
            .Include(c => c.Items.OrderBy(i => i.SortOrder))
            .AsQueryable();

        if (isPublic.HasValue)
        {
            query = query.Where(c => c.IsPublic == isPublic.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c =>
                c.Title.Contains(searchTerm) ||
                (c.Description != null && c.Description.Contains(searchTerm)));
        }

        // Order by creation date (newest first)
        query = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<SharedCollection>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<SharedCollection>> GetUserCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        // For now, return all collections (in a real app, you'd filter by user)
        // This would need user context to be properly implemented
        var query = _context.SharedCollections
            .Include(c => c.Items.OrderBy(i => i.SortOrder))
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<SharedCollection>(items, totalCount, pageNumber, pageSize);
    }

    public async Task AddAsync(SharedCollection collection, CancellationToken ct = default)
    {
        await _context.SharedCollections.AddAsync(collection, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(SharedCollection collection, CancellationToken ct = default)
    {
        _context.SharedCollections.Update(collection);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var collection = await _context.SharedCollections.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        if (collection is not null)
        {
            _context.SharedCollections.Remove(collection);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task AddItemAsync(SharedCollectionItem item, CancellationToken ct = default)
    {
        await _context.SharedCollectionItems.AddAsync(item, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveItemAsync(Guid collectionId, string gameTitle, CancellationToken ct = default)
    {
        var item = await _context.SharedCollectionItems
            .FirstOrDefaultAsync(i => i.CollectionId == collectionId && i.GameTitle == gameTitle, ct)
            .ConfigureAwait(false);

        if (item is not null)
        {
            _context.SharedCollectionItems.Remove(item);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task UpdateItemsAsync(Guid collectionId, IReadOnlyList<SharedCollectionItem> items, CancellationToken ct = default)
    {
        // Remove existing items
        var existingItems = await _context.SharedCollectionItems
            .Where(i => i.CollectionId == collectionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _context.SharedCollectionItems.RemoveRange(existingItems);

        // Add new items
        await _context.SharedCollectionItems.AddRangeAsync(items, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> IsShareCodeUniqueAsync(string shareCode, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.SharedCollections.Where(c => c.ShareCode == shareCode);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return !await query.AnyAsync(ct).ConfigureAwait(false);
    }

    public async Task<SharedCollectionStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var collections = await _context.SharedCollections.ToListAsync(ct).ConfigureAwait(false);

        if (!collections.Any())
        {
            return new SharedCollectionStatistics(0, 0, 0, 0, null);
        }

        var totalCollections = collections.Count;
        var publicCollections = collections.Count(c => c.IsPublic);
        var totalDownloads = collections.Sum(c => c.DownloadCount);
        var averageItemsPerCollection = (int)collections.Average(c => c.Items.Count);
        var lastCreatedAt = collections.Max(c => c.CreatedAt);

        return new SharedCollectionStatistics(
            totalCollections,
            publicCollections,
            totalDownloads,
            averageItemsPerCollection,
            lastCreatedAt);
    }
}