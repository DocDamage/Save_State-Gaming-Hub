using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to check in a participant for a tournament.
/// </summary>
public record CheckInParticipantCommand(
    Guid TournamentId,
    Guid ParticipantId,
    string CheckInCode
) : IRequest<Result>;

/// <summary>
/// Handler for checking in a participant.
/// </summary>
public sealed class CheckInParticipantCommandHandler : IRequestHandler<CheckInParticipantCommand, Result>
{
    private readonly ITournamentService _tournamentService;

    public CheckInParticipantCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result> Handle(CheckInParticipantCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.CheckInParticipantAsync(
            request.TournamentId,
            request.ParticipantId,
            request.CheckInCode,
            cancellationToken).ConfigureAwait(false);
    }
}
