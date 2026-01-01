using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Queries.VirtualCollections;

public sealed record GetGamesInCollectionQuery(Guid CollectionId) : IRequest<Result<IReadOnlyList<Game>>>;

public sealed class GetGamesInCollectionQueryHandler : IRequestHandler<GetGamesInCollectionQuery, Result<IReadOnlyList<Game>>>
{
    private readonly IVirtualCollectionService _collectionService;

    public GetGamesInCollectionQueryHandler(IVirtualCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result<IReadOnlyList<Game>>> Handle(GetGamesInCollectionQuery request, CancellationToken ct)
    {
        return await _collectionService.GetGamesInCollectionAsync(request.CollectionId, ct);
    }
}