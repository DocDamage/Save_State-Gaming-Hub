namespace SaveState.Application.Mugen.Commands;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;

/// <summary>
/// Command to create a new MUGEN tournament.
/// </summary>
public record CreateMugenTournamentCommand(
    string Name,
    string Format,
    IReadOnlyList<Guid> ParticipantIds
) : IRequest<Result<MugenTournament>>;