using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to get shared collections with filtering.
/// </summary>
public record GetSharedCollectionsQuery(
    int PageNumber = 1,
    int PageSize = 50,
    bool? IsPublic = null,
    string? SearchTerm = null) : IRequest<Result<PagedResult<SharedCollection>>>;

/// <summary>
/// Handler for getting shared collections.
/// </summary>
public class GetSharedCollectionsQueryHandler : IRequestHandler<GetSharedCollectionsQuery, Result<PagedResult<SharedCollection>>>
{
    private readonly Core.Social.Services.ISharedCollectionService _collectionService;

    public GetSharedCollectionsQueryHandler(Core.Social.Services.ISharedCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result<PagedResult<SharedCollection>>> Handle(GetSharedCollectionsQuery request, CancellationToken ct)
    {
        return await _collectionService.GetCollectionsAsync(
            request.PageNumber,
            request.PageSize,
            request.IsPublic,
            request.SearchTerm,
            ct);
    }
}