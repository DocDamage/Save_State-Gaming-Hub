namespace SaveState.Application.Mugen.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Handler for running death match simulations.
/// </summary>
public class RunDeathMatchSimulationCommandHandler : IRequestHandler<RunDeathMatchSimulationCommand, Result<SimulationResult>>
{
    private readonly IDeathMatchSimulator _deathMatchSimulator;

    public RunDeathMatchSimulationCommandHandler(IDeathMatchSimulator deathMatchSimulator)
    {
        _deathMatchSimulator = deathMatchSimulator;
    }

    public async Task<Result<SimulationResult>> Handle(RunDeathMatchSimulationCommand request, CancellationToken ct)
    {
        return await _deathMatchSimulator.SimulateMatchesAsync(
            request.Character1Id,
            request.Character2Id,
            request.MatchCount,
            ct);
    }
}