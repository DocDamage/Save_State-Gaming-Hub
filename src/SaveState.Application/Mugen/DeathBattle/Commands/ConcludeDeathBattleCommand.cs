using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.DeathBattle;
using SaveState.Core.Mugen.DeathBattle.Services;

namespace SaveState.Application.Mugen.DeathBattle.Commands;

/// <summary>
/// Command to conclude a Death Battle with a winner.
/// </summary>
public sealed record ConcludeDeathBattleCommand(
    string BattleCode,
    Guid WinnerId,
    DeathBattleOutcome Outcome,
    string Reasoning) : IRequest<Result<DeathBattleMatch>>;

/// <summary>
/// Handler for ConcludeDeathBattleCommand.
/// </summary>
public sealed class ConcludeDeathBattleCommandHandler : IRequestHandler<ConcludeDeathBattleCommand, Result<DeathBattleMatch>>
{
    private readonly IDeathBattleService _deathBattleService;

    public ConcludeDeathBattleCommandHandler(IDeathBattleService deathBattleService)
    {
        _deathBattleService = deathBattleService;
    }

    public async Task<Result<DeathBattleMatch>> Handle(ConcludeDeathBattleCommand request, CancellationToken cancellationToken)
    {
        return await _deathBattleService.ConcludeBattleAsync(
            request.BattleCode,
            request.WinnerId,
            request.Outcome,
            request.Reasoning,
            cancellationToken);
    }
}
