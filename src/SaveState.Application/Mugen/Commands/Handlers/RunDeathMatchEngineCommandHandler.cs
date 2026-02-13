namespace SaveState.Application.Mugen.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Handler for running actual MUGEN engine death matches.
/// </summary>
public class RunDeathMatchEngineCommandHandler : IRequestHandler<RunDeathMatchEngineCommand, Result<DeathMatchResult>>
{
    private readonly IMugenDeathMatchService _deathMatchService;

    public RunDeathMatchEngineCommandHandler(IMugenDeathMatchService deathMatchService)
    {
        _deathMatchService = deathMatchService;
    }

    public Task<Result<DeathMatchResult>> Handle(RunDeathMatchEngineCommand request, CancellationToken ct)
    {
        return _deathMatchService.RunDeathMatchAsync(
            request.Character1Id,
            request.Character2Id,
            request.MatchCount,
            ct);
    }
}
