using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.DeathBattle.Services;

namespace SaveState.Application.Mugen.DeathBattle.Queries;

/// <summary>
/// Query to get a random Death Battle matchup suggestion.
/// </summary>
public sealed record GetRandomDeathBattleMatchupQuery : IRequest<Result<(Guid Character1Id, Guid Character2Id)>>;

/// <summary>
/// Handler for GetRandomDeathBattleMatchupQuery.
/// </summary>
public sealed class GetRandomDeathBattleMatchupQueryHandler : IRequestHandler<GetRandomDeathBattleMatchupQuery, Result<(Guid Character1Id, Guid Character2Id)>>
{
    private readonly IDeathBattleService _deathBattleService;

    public GetRandomDeathBattleMatchupQueryHandler(IDeathBattleService deathBattleService)
    {
        _deathBattleService = deathBattleService;
    }

    public async Task<Result<(Guid Character1Id, Guid Character2Id)>> Handle(GetRandomDeathBattleMatchupQuery request, CancellationToken cancellationToken)
    {
        return await _deathBattleService.GetRandomMatchupAsync(cancellationToken);
    }
}
