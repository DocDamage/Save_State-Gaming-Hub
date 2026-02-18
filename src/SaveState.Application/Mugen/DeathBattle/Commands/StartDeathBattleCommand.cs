using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.DeathBattle.Services;

namespace SaveState.Application.Mugen.DeathBattle.Commands;

/// <summary>
/// Command to start a Death Battle.
/// </summary>
public sealed record StartDeathBattleCommand(string BattleCode) : IRequest<Result>;

/// <summary>
/// Handler for StartDeathBattleCommand.
/// </summary>
public sealed class StartDeathBattleCommandHandler : IRequestHandler<StartDeathBattleCommand, Result>
{
    private readonly IDeathBattleService _deathBattleService;

    public StartDeathBattleCommandHandler(IDeathBattleService deathBattleService)
    {
        _deathBattleService = deathBattleService;
    }

    public async Task<Result> Handle(StartDeathBattleCommand request, CancellationToken cancellationToken)
    {
        return await _deathBattleService.StartBattleAsync(request.BattleCode, cancellationToken);
    }
}
