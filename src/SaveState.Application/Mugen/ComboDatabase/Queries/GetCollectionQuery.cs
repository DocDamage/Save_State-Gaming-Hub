using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get a combo collection.
/// </summary>
public sealed record GetCollectionQuery(Guid CollectionId) : IRequest<Result<ComboCollection>>;

/// <summary>
/// Handler for GetCollectionQuery.
/// </summary>
public sealed class GetCollectionQueryHandler : IRequestHandler<GetCollectionQuery, Result<ComboCollection>>
{
    private readonly IComboDatabaseService _comboService;

    public GetCollectionQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<ComboCollection>> Handle(GetCollectionQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetCollectionAsync(request.CollectionId, cancellationToken);
    }
}
