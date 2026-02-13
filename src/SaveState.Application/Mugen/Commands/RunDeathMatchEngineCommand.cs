namespace SaveState.Application.Mugen.Commands;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Command to run real MUGEN engine matches between two characters.
/// </summary>
public record RunDeathMatchEngineCommand(
    Guid Character1Id,
    Guid Character2Id,
    int MatchCount = 3
) : IRequest<Result<DeathMatchResult>>;
