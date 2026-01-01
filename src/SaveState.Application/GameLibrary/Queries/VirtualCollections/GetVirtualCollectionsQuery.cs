using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Queries.VirtualCollections;

public sealed record GetVirtualCollectionsQuery(bool IncludeSystemCollections = true) : IRequest<Result<IReadOnlyList<VirtualCollection>>>;

public sealed class GetVirtualCollectionsQueryHandler : IRequestHandler<GetVirtualCollectionsQuery, Result<IReadOnlyList<VirtualCollection>>>
{
    private readonly IVirtualCollectionService _collectionService;

    public GetVirtualCollectionsQueryHandler(IVirtualCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result<IReadOnlyList<VirtualCollection>>> Handle(GetVirtualCollectionsQuery request, CancellationToken ct)
    {
        return await _collectionService.GetAllCollectionsAsync(request.IncludeSystemCollections, ct);
    }
}