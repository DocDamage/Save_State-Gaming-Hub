using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get all combos for a character.
/// </summary>
public sealed record GetCharacterCombosQuery(string CharacterName) : IRequest<Result<CharacterComboDatabase>>;

/// <summary>
/// Handler for GetCharacterCombosQuery.
/// </summary>
public sealed class GetCharacterCombosQueryHandler : IRequestHandler<GetCharacterCombosQuery, Result<CharacterComboDatabase>>
{
    private readonly IComboDatabaseService _comboService;

    public GetCharacterCombosQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<CharacterComboDatabase>> Handle(GetCharacterCombosQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetCharacterCombosAsync(request.CharacterName, cancellationToken);
    }
}
