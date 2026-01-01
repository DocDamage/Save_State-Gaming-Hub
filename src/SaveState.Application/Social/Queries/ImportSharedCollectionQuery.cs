using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Application.Social.Queries;

/// <summary>
/// Query to import a shared collection by share code.
/// </summary>
public record ImportSharedCollectionQuery(string ShareCode) : IRequest<Result<SharedCollection>>;

/// <summary>
/// Handler for importing shared collections.
/// </summary>
public class ImportSharedCollectionQueryHandler : IRequestHandler<ImportSharedCollectionQuery, Result<SharedCollection>>
{
    private readonly Core.Social.Services.ISharedCollectionService _collectionService;

    public ImportSharedCollectionQueryHandler(Core.Social.Services.ISharedCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<Result<SharedCollection>> Handle(ImportSharedCollectionQuery request, CancellationToken ct)
    {
        return await _collectionService.ImportCollectionAsync(request.ShareCode, ct);
    }
}