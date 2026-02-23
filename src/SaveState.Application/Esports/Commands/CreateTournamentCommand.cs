using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to create a new tournament.
/// </summary>
public record CreateTournamentCommand(
    string Name,
    string Description,
    GameInfo Game,
    TournamentFormat Format,
    DateTime StartDate,
    DateTime RegistrationDeadline,
    int MaxParticipants,
    TournamentRules? Rules = null,
    PrizePool? PrizePool = null
) : IRequest<Result<Tournament>>;

/// <summary>
/// Handler for creating a tournament.
/// </summary>
public sealed class CreateTournamentCommandHandler : IRequestHandler<CreateTournamentCommand, Result<Tournament>>
{
    private readonly ITournamentService _tournamentService;

    public CreateTournamentCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<Tournament>> Handle(CreateTournamentCommand request, CancellationToken cancellationToken)
    {
        var createRequest = new CreateTournamentRequest(
            request.Name,
            request.Description,
            request.Game,
            request.Format,
            request.StartDate,
            request.RegistrationDeadline,
            request.MaxParticipants,
            request.Rules,
            request.PrizePool
        );

        return await _tournamentService.CreateTournamentAsync(createRequest, cancellationToken).ConfigureAwait(false);
    }
}
