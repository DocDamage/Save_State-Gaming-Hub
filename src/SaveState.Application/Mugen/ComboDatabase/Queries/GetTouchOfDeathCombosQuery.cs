using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using ComboEntryModel = SaveState.Core.Mugen.ComboDatabase.ComboEntry;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get Touch of Death combos.
/// </summary>
public sealed record GetTouchOfDeathCombosQuery(
    string? CharacterName = null) : IRequest<Result<List<ComboEntryModel>>>;

/// <summary>
/// Handler for GetTouchOfDeathCombosQuery.
/// </summary>
public sealed class GetTouchOfDeathCombosQueryHandler : IRequestHandler<GetTouchOfDeathCombosQuery, Result<List<ComboEntryModel>>>
{
    private readonly IComboDatabaseService _comboService;

    public GetTouchOfDeathCombosQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<List<ComboEntryModel>>> Handle(GetTouchOfDeathCombosQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetTouchOfDeathCombosAsync(
            request.CharacterName,
            cancellationToken);
    }
}
