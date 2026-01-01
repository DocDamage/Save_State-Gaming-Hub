using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.Services;

public interface IVirtualCollectionService
{
    Task<Result<VirtualCollection>> CreateManualCollectionAsync(string name, string? icon = null, CancellationToken ct = default);
    Task<Result<VirtualCollection>> CreateSmartCollectionAsync(string name, CollectionFilter filter, string? icon = null, CancellationToken ct = default);
    Task<Result> DeleteCollectionAsync(Guid collectionId, CancellationToken ct = default);
    Task<Result> AddGameToCollectionAsync(Guid collectionId, Guid gameId, CancellationToken ct = default);
    Task<Result> RemoveGameFromCollectionAsync(Guid collectionId, Guid gameId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<Game>>> GetGamesInCollectionAsync(Guid collectionId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<VirtualCollection>>> GetAllCollectionsAsync(bool includeSystem = true, CancellationToken ct = default);
    Task<Result<IReadOnlyList<Game>>> ExecuteSmartFilterAsync(CollectionFilter filter, CancellationToken ct = default);
    Task<Result> CreateSystemCollectionsAsync(CancellationToken ct = default);
}