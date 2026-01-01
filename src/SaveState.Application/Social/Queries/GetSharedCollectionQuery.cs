using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to get a shared collection by ID.
/// </summary>
public record GetSharedCollectionQuery(Guid CollectionId) : IRequest<Result<SharedCollection>>;

/// <summary>
/// Handler for getting a shared collection.
/// </summary>
public class GetSharedCollectionQueryHandler : IRequestHandler<GetSharedCollectionQuery, Result<SharedCollection>>
{
    private readonly Core.Social.Services.ISharedCollectionService _collectionService;

    public GetSharedCollectionQueryHandler(Core.Social.Services.ISharedCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result<SharedCollection>> Handle(GetSharedCollectionQuery request, CancellationToken ct)
    {
        return await _collectionService.GetCollectionAsync(request.CollectionId, ct);
    }
}