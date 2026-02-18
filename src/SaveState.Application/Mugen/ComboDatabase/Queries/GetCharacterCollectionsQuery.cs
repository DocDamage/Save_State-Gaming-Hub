using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get collections for a character.
/// </summary>
public sealed record GetCharacterCollectionsQuery(
    string CharacterName,
    bool IncludePrivate = false) : IRequest<Result<List<ComboCollection>>>;

/// <summary>
/// Handler for GetCharacterCollectionsQuery.
/// </summary>
public sealed class GetCharacterCollectionsQueryHandler : IRequestHandler<GetCharacterCollectionsQuery, Result<List<ComboCollection>>>
{
    private readonly IComboDatabaseService _comboService;

    public GetCharacterCollectionsQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<List<ComboCollection>>> Handle(GetCharacterCollectionsQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetCharacterCollectionsAsync(
            request.CharacterName,
            request.IncludePrivate,
            cancellationToken);
    }
}
