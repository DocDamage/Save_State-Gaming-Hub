using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.DeathBattle;
using SaveState.Core.Mugen.DeathBattle.Services;

namespace SaveState.Application.Mugen.DeathBattle.Commands;

/// <summary>
/// Command to create a new Death Battle.
/// </summary>
public sealed record CreateDeathBattleCommand(
    Guid Combatant1Id,
    Guid Combatant2Id,
    string? CustomBattleCode = null,
    bool IsPublic = true,
    List<string>? Tags = null) : IRequest<Result<DeathBattleMatch>>;

/// <summary>
/// Handler for CreateDeathBattleCommand.
/// </summary>
public sealed class CreateDeathBattleCommandHandler : IRequestHandler<CreateDeathBattleCommand, Result<DeathBattleMatch>>
{
    private readonly IDeathBattleService _deathBattleService;

    public CreateDeathBattleCommandHandler(IDeathBattleService deathBattleService)
    {
        _deathBattleService = deathBattleService;
    }

    public async Task<Result<DeathBattleMatch>> Handle(CreateDeathBattleCommand request, CancellationToken cancellationToken)
    {
        var createRequest = new CreateDeathBattleRequest
        {
            Combatant1Id = request.Combatant1Id,
            Combatant2Id = request.Combatant2Id,
            CustomBattleCode = request.CustomBattleCode,
            IsPublic = request.IsPublic,
            Tags = request.Tags
        };

        return await _deathBattleService.CreateBattleAsync(createRequest, cancellationToken);
    }
}
