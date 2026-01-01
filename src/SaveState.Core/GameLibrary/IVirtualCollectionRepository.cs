using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary;

public interface IVirtualCollectionRepository
{
    Task<VirtualCollection?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<VirtualCollection>> GetAllAsync(bool includeSystemCollections = true, CancellationToken ct = default);
    Task<IReadOnlyList<VirtualCollection>> GetSmartCollectionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<VirtualCollection>> GetManualCollectionsAsync(CancellationToken ct = default);
    Task AddAsync(VirtualCollection collection, CancellationToken ct = default);
    Task UpdateAsync(VirtualCollection collection, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task AddGameToCollectionAsync(Guid collectionId, Guid gameId, int sortOrder = 0, CancellationToken ct = default);
    Task RemoveGameFromCollectionAsync(Guid collectionId, Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetGamesInCollectionAsync(Guid collectionId, CancellationToken ct = default);
    Task<IReadOnlyList<VirtualCollection>> GetCollectionsForGameAsync(Guid gameId, CancellationToken ct = default);
    Task<int> GetCollectionCountAsync(Guid collectionId, CancellationToken ct = default);
}