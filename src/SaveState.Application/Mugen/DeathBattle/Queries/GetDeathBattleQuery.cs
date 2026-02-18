using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.DeathBattle;
using SaveState.Core.Mugen.DeathBattle.Services;

namespace SaveState.Application.Mugen.DeathBattle.Queries;

/// <summary>
/// Query to get a Death Battle by code.
/// </summary>
public sealed record GetDeathBattleQuery(string BattleCode) : IRequest<Result<DeathBattleMatch>>;

/// <summary>
/// Handler for GetDeathBattleQuery.
/// </summary>
public sealed class GetDeathBattleQueryHandler : IRequestHandler<GetDeathBattleQuery, Result<DeathBattleMatch>>
{
    private readonly IDeathBattleService _deathBattleService;

    public GetDeathBattleQueryHandler(IDeathBattleService deathBattleService)
    {
        _deathBattleService = deathBattleService;
    }

    public async Task<Result<DeathBattleMatch>> Handle(GetDeathBattleQuery request, CancellationToken cancellationToken)
    {
        return await _deathBattleService.GetBattleAsync(request.BattleCode, cancellationToken);
    }
}
