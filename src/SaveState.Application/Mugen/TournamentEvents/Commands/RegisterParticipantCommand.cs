using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to register a participant.
/// </summary>
public sealed record RegisterParticipantCommand(
    Guid TournamentId,
    string Name,
    string? UserId = null,
    string? ContactInfo = null,
    string? Country = null,
    string? Team = null,
    string? Character = null,
    string? StreamUrl = null) : IRequest<Result<TournamentParticipant>>;

/// <summary>
/// Handler for RegisterParticipantCommand.
/// </summary>
public sealed class RegisterParticipantCommandHandler : IRequestHandler<RegisterParticipantCommand, Result<TournamentParticipant>>
{
    private readonly ITournamentEventService _tournamentService;

    public RegisterParticipantCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentParticipant>> Handle(RegisterParticipantCommand request, CancellationToken cancellationToken)
    {
        var registerRequest = new RegisterParticipantRequest
        {
            Name = request.Name,
            UserId = request.UserId,
            ContactInfo = request.ContactInfo,
            Country = request.Country,
            Team = request.Team,
            Character = request.Character,
            StreamUrl = request.StreamUrl
        };

        return await _tournamentService.RegisterParticipantAsync(
            request.TournamentId,
            registerRequest,
            cancellationToken);
    }
}







