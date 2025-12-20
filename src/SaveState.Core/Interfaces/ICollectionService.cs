using SaveState.Core.Entities;

namespace SaveState.Core.Interfaces;

public interface ICollectionService
{
    Task<IEnumerable<Collection>> GetAllAsync();
    Task<Collection?> GetByIdAsync(Guid id);
    Task<Collection?> GetByNameAsync(string name);
    Task<Collection> CreateAsync(string name, string? description = null);
    Task UpdateAsync(Collection collection);
    Task DeleteAsync(Guid id);
    Task AddGameToCollectionAsync(Guid collectionId, Guid gameId);
    Task RemoveGameFromCollectionAsync(Guid collectionId, Guid gameId);
    Task<IEnumerable<Game>> GetGamesInCollectionAsync(Guid collectionId);
}
