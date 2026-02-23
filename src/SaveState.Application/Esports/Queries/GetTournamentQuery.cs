using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Queries;

/// <summary>
/// Query to get a tournament by ID.
/// </summary>
public record GetTournamentQuery(Guid TournamentId) : IRequest<Result<Tournament>>;

/// <summary>
/// Handler for getting a tournament.
/// </summary>
public sealed class GetTournamentQueryHandler : IRequestHandler<GetTournamentQuery, Result<Tournament>>
{
    private readonly ITournamentService _tournamentService;

    public GetTournamentQueryHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<Tournament>> Handle(GetTournamentQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetTournamentAsync(request.TournamentId, cancellationToken).ConfigureAwait(false);
    }
}
