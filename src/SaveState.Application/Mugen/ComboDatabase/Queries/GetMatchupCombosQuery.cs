using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get matchup-specific combo recommendations.
/// </summary>
public sealed record GetMatchupCombosQuery(
    string CharacterName,
    string OpponentName) : IRequest<Result<ComboMatchupInfo>>;

/// <summary>
/// Handler for GetMatchupCombosQuery.
/// </summary>
public sealed class GetMatchupCombosQueryHandler : IRequestHandler<GetMatchupCombosQuery, Result<ComboMatchupInfo>>
{
    private readonly IComboDatabaseService _comboService;

    public GetMatchupCombosQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<ComboMatchupInfo>> Handle(GetMatchupCombosQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetMatchupCombosAsync(
            request.CharacterName,
            request.OpponentName,
            cancellationToken);
    }
}
