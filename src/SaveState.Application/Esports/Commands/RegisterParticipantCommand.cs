using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to register a participant for a tournament.
/// </summary>
public record RegisterParticipantCommand(
    Guid TournamentId,
    string UserId,
    string DisplayName,
    int? Seed = null
) : IRequest<Result<Participant>>;

/// <summary>
/// Handler for registering a participant.
/// </summary>
public sealed class RegisterParticipantCommandHandler : IRequestHandler<RegisterParticipantCommand, Result<Participant>>
{
    private readonly ITournamentService _tournamentService;

    public RegisterParticipantCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<Participant>> Handle(RegisterParticipantCommand request, CancellationToken cancellationToken)
    {
        var registerRequest = new RegisterParticipantRequest(
            request.UserId,
            request.DisplayName,
            request.Seed
        );

        return await _tournamentService.RegisterParticipantAsync(request.TournamentId, registerRequest, cancellationToken).ConfigureAwait(false);
    }
}
