using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to start a tournament.
/// </summary>
public record StartTournamentCommand(Guid TournamentId) : IRequest<Result>;

/// <summary>
/// Handler for starting a tournament.
/// </summary>
public sealed class StartTournamentCommandHandler : IRequestHandler<StartTournamentCommand, Result>
{
    private readonly ITournamentService _tournamentService;

    public StartTournamentCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result> Handle(StartTournamentCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.StartTournamentAsync(request.TournamentId, cancellationToken).ConfigureAwait(false);
    }
}
