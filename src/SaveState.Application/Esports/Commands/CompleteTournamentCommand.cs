using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to complete a tournament.
/// </summary>
public record CompleteTournamentCommand(Guid TournamentId) : IRequest<Result>;

/// <summary>
/// Handler for completing a tournament.
/// </summary>
public sealed class CompleteTournamentCommandHandler : IRequestHandler<CompleteTournamentCommand, Result>
{
    private readonly ITournamentService _tournamentService;

    public CompleteTournamentCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result> Handle(CompleteTournamentCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.CompleteTournamentAsync(request.TournamentId, cancellationToken).ConfigureAwait(false);
    }
}
