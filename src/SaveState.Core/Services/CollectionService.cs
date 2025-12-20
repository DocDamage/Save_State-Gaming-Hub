using Microsoft.EntityFrameworkCore;
using SaveState.Core.Data;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

public class CollectionService : ICollectionService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger _logger = Log.ForContext<CollectionService>();

    public CollectionService(SaveStateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Collection>> GetAllAsync()
    {
        return await _dbContext.Collections
            .Include(c => c.Games)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Collection?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Collections
            .Include(c => c.Games)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Collection?> GetByNameAsync(string name)
    {
        return await _dbContext.Collections
            .Include(c => c.Games)
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<Collection> CreateAsync(string name, string? description = null)
    {
        var maxOrder = await _dbContext.Collections.MaxAsync(c => (int?)c.SortOrder) ?? 0;
        
        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            SortOrder = maxOrder + 1
        };

        _dbContext.Collections.Add(collection);
        await _dbContext.SaveChangesAsync();
        
        _logger.Information("Created collection: {Name}", name);
        return collection;
    }

    public async Task UpdateAsync(Collection collection)
    {
        _dbContext.Collections.Update(collection);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var collection = await _dbContext.Collections.FindAsync(id);
        if (collection != null)
        {
            _dbContext.Collections.Remove(collection);
            await _dbContext.SaveChangesAsync();
            _logger.Information("Deleted collection: {Name}", collection.Name);
        }
    }

    public async Task AddGameToCollectionAsync(Guid collectionId, Guid gameId)
    {
        var collection = await GetByIdAsync(collectionId);
        var game = await _dbContext.Games.FindAsync(gameId);
        
        if (collection != null && game != null)
        {
            if (!collection.Games.Any(g => g.Id == gameId))
            {
                collection.Games.Add(game);
                await _dbContext.SaveChangesAsync();
                _logger.Information("Added {Game} to {Collection}", game.Title, collection.Name);
            }
        }
    }

    public async Task RemoveGameFromCollectionAsync(Guid collectionId, Guid gameId)
    {
        var collection = await GetByIdAsync(collectionId);
        
        if (collection != null)
        {
            var game = collection.Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
            {
                collection.Games.Remove(game);
                await _dbContext.SaveChangesAsync();
                _logger.Information("Removed {Game} from {Collection}", game.Title, collection.Name);
            }
        }
    }

    public async Task<IEnumerable<Game>> GetGamesInCollectionAsync(Guid collectionId)
    {
        var collection = await GetByIdAsync(collectionId);
        return collection?.Games ?? Enumerable.Empty<Game>();
    }
}
