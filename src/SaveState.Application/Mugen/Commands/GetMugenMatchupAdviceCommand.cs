namespace SaveState.Application.Mugen.Commands;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Command to get matchup advice between two MUGEN characters.
/// </summary>
public record GetMugenMatchupAdviceCommand(
    Guid CharacterId,
    Guid OpponentId
) : IRequest<Result<MatchupAdvice>>;