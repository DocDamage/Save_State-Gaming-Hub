using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to update an existing tournament.
/// </summary>
public record UpdateTournamentCommand(
    Guid TournamentId,
    string? Name = null,
    string? Description = null,
    DateTime? StartDate = null,
    DateTime? RegistrationDeadline = null,
    int? MaxParticipants = null,
    TournamentRules? Rules = null,
    string? StreamUrl = null
) : IRequest<Result<Tournament>>;

/// <summary>
/// Handler for updating a tournament.
/// </summary>
public sealed class UpdateTournamentCommandHandler : IRequestHandler<UpdateTournamentCommand, Result<Tournament>>
{
    private readonly ITournamentService _tournamentService;

    public UpdateTournamentCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<Tournament>> Handle(UpdateTournamentCommand request, CancellationToken cancellationToken)
    {
        var updateRequest = new UpdateTournamentRequest(
            request.Name,
            request.Description,
            request.StartDate,
            request.RegistrationDeadline,
            request.MaxParticipants,
            request.Rules,
            request.StreamUrl
        );

        return await _tournamentService.UpdateTournamentAsync(request.TournamentId, updateRequest, cancellationToken).ConfigureAwait(false);
    }
}
