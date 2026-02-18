using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to check in a participant.
/// </summary>
public sealed record CheckInParticipantCommand(
    Guid TournamentId,
    Guid ParticipantId) : IRequest<Result>;

/// <summary>
/// Handler for CheckInParticipantCommand.
/// </summary>
public sealed class CheckInParticipantCommandHandler : IRequestHandler<CheckInParticipantCommand, Result>
{
    private readonly ITournamentEventService _tournamentService;

    public CheckInParticipantCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result> Handle(CheckInParticipantCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.CheckInParticipantAsync(
            request.TournamentId,
            request.ParticipantId,
            cancellationToken);
    }
}







