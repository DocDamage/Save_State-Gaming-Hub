using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using ComboEntryModel = SaveState.Core.Mugen.ComboDatabase.ComboEntry;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get optimal combos for a character.
/// </summary>
public sealed record GetOptimalCombosQuery(
    string CharacterName,
    string? StartingPosition = null) : IRequest<Result<List<ComboEntryModel>>>;

/// <summary>
/// Handler for GetOptimalCombosQuery.
/// </summary>
public sealed class GetOptimalCombosQueryHandler : IRequestHandler<GetOptimalCombosQuery, Result<List<ComboEntryModel>>>
{
    private readonly IComboDatabaseService _comboService;

    public GetOptimalCombosQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<List<ComboEntryModel>>> Handle(GetOptimalCombosQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetOptimalCombosAsync(
            request.CharacterName,
            request.StartingPosition,
            cancellationToken);
    }
}
