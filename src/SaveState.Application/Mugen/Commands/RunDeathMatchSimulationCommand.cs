namespace SaveState.Application.Mugen.Commands;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Command to run a death match simulation between two characters.
/// </summary>
public record RunDeathMatchSimulationCommand(
    Guid Character1Id,
    Guid Character2Id,
    int MatchCount = 1000
) : IRequest<Result<SimulationResult>>;