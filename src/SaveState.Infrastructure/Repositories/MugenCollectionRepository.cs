namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Monitoring;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for managing MUGEN character collection entities.
/// </summary>
public class MugenCollectionRepository : IMugenCollectionRepository
{
    private readonly SaveStateDbContext _context;
    private readonly IApplicationMetrics _metrics;
    private readonly ITimeProvider _timeProvider;

    public MugenCollectionRepository(SaveStateDbContext context, IApplicationMetrics metrics, ITimeProvider timeProvider)
    {
        _context = context;
        _metrics = metrics;
        _timeProvider = timeProvider;
    }

    public async Task<MugenCharacterCollection?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            var collection = await _context.MugenCharacterCollections
                .Include(c => c.Characters)
                .ThenInclude(cc => cc.Character)
                .FirstOrDefaultAsync(c => c.Id == id, ct)
                .ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetByIdAsync", duration);

            return collection;
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetByIdAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.GetByIdAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenCharacterCollection>> GetAllAsync(CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            var collections = await _context.MugenCharacterCollections
                .AsNoTracking()
                .Include(c => c.Characters)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetAllAsync", duration);

            return collections.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetAllAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.GetAllAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<PagedResult<MugenCharacterCollection>> GetCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        Guid? userId = null,
        bool? isPublic = null,
        CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            var query = _context.MugenCharacterCollections.AsQueryable();

            // Apply filters
            if (userId.HasValue)
            {
                query = query.Where(c => c.UserId == userId.Value);
            }

            if (isPublic.HasValue)
            {
                query = query.Where(c => c.IsPublic == isPublic.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

            // Apply pagination and ordering
            var collections = await query
                .OrderByDescending(c => c.LastModified)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(c => c.Characters)
                .ThenInclude(cc => cc.Character)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetCollectionsAsync", duration);

            return new PagedResult<MugenCharacterCollection>(
                collections.AsReadOnly(),
                totalCount,
                pageNumber,
                pageSize);
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetCollectionsAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.GetCollectionsAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenCharacterCollection>> GetByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            var collections = await _context.MugenCharacterCollections
                .Where(c => c.UserId == userId)
                .Include(c => c.Characters)
                .ThenInclude(cc => cc.Character)
                .OrderByDescending(c => c.LastModified)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetByUserAsync", duration);

            return collections.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetByUserAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.GetByUserAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenCharacterCollection>> GetPublicCollectionsAsync(CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            var collections = await _context.MugenCharacterCollections
                .Where(c => c.IsPublic)
                .Include(c => c.Characters)
                .ThenInclude(cc => cc.Character)
                .OrderByDescending(c => c.LastModified)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetPublicCollectionsAsync", duration);

            return collections.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetPublicCollectionsAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.GetPublicCollectionsAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<int> CountAsync(Guid? userId = null, bool? isPublic = null, CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            var query = _context.MugenCharacterCollections.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(c => c.UserId == userId.Value);
            }

            if (isPublic.HasValue)
            {
                query = query.Where(c => c.IsPublic == isPublic.Value);
            }

            var count = await query.CountAsync(ct).ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.CountAsync", duration);

            return count;
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.CountAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.CountAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<bool> IsCharacterInCollectionAsync(Guid collectionId, Guid characterId, CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            var exists = await _context.MugenCollectionCharacters
                .AnyAsync(cc => cc.CollectionId == collectionId && cc.CharacterId == characterId, ct)
                .ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.IsCharacterInCollectionAsync", duration);

            return exists;
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.IsCharacterInCollectionAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.IsCharacterInCollectionAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<IReadOnlyList<MugenCharacterCollection>> GetCollectionsByCharacterAsync(Guid characterId, CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            var collections = await _context.MugenCharacterCollections
                .Where(c => c.Characters.Any(cc => cc.CharacterId == characterId))
                .Include(c => c.Characters)
                .ThenInclude(cc => cc.Character)
                .OrderByDescending(c => c.LastModified)
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetCollectionsByCharacterAsync", duration);

            return collections.AsReadOnly();
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.GetCollectionsByCharacterAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.GetCollectionsByCharacterAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task AddAsync(MugenCharacterCollection collection, CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            await _context.MugenCharacterCollections.AddAsync(collection, ct).ConfigureAwait(false);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.AddAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.AddAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.AddAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task UpdateAsync(MugenCharacterCollection collection, CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            _context.MugenCharacterCollections.Update(collection);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.UpdateAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.UpdateAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.UpdateAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task DeleteAsync(MugenCharacterCollection collection, CancellationToken ct = default)
    {
        var startTime = _timeProvider.UtcNow;
        try
        {
            _context.MugenCharacterCollections.Remove(collection);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);

            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.DeleteAsync", duration);
        }
        catch (Exception ex)
        {
            var duration = _timeProvider.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("MugenCollectionRepository.DeleteAsync", duration);
            _metrics.RecordDatabaseError("MugenCollectionRepository.DeleteAsync", ex.GetType().Name);
            throw;
        }
    }
}
