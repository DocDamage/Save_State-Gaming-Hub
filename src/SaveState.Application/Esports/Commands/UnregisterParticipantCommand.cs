using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to unregister a participant from a tournament.
/// </summary>
public record UnregisterParticipantCommand(
    Guid TournamentId,
    Guid ParticipantId
) : IRequest<Result>;

/// <summary>
/// Handler for unregistering a participant.
/// </summary>
public sealed class UnregisterParticipantCommandHandler : IRequestHandler<UnregisterParticipantCommand, Result>
{
    private readonly ITournamentService _tournamentService;

    public UnregisterParticipantCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result> Handle(UnregisterParticipantCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.UnregisterParticipantAsync(
            request.TournamentId,
            request.ParticipantId,
            cancellationToken).ConfigureAwait(false);
    }
}
