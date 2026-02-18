using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.DeathBattle;
using SaveState.Core.Mugen.DeathBattle.Services;

namespace SaveState.Application.Mugen.DeathBattle.Queries;

/// <summary>
/// Query to get the Death Battle leaderboard.
/// </summary>
public sealed record GetDeathBattleLeaderboardQuery(int Top = 100) : IRequest<Result<List<DeathBattleLeaderboardEntry>>>;

/// <summary>
/// Handler for GetDeathBattleLeaderboardQuery.
/// </summary>
public sealed class GetDeathBattleLeaderboardQueryHandler : IRequestHandler<GetDeathBattleLeaderboardQuery, Result<List<DeathBattleLeaderboardEntry>>>
{
    private readonly IDeathBattleService _deathBattleService;

    public GetDeathBattleLeaderboardQueryHandler(IDeathBattleService deathBattleService)
    {
        _deathBattleService = deathBattleService;
    }

    public async Task<Result<List<DeathBattleLeaderboardEntry>>> Handle(GetDeathBattleLeaderboardQuery request, CancellationToken cancellationToken)
    {
        return await _deathBattleService.GetLeaderboardAsync(request.Top, cancellationToken);
    }
}
