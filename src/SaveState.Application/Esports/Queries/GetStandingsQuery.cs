using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Queries;

/// <summary>
/// Query to get tournament standings.
/// </summary>
public record GetStandingsQuery(Guid TournamentId) : IRequest<Result<IReadOnlyList<Participant>>>;

/// <summary>
/// Handler for getting standings.
/// </summary>
public sealed class GetStandingsQueryHandler : IRequestHandler<GetStandingsQuery, Result<IReadOnlyList<Participant>>>
{
    private readonly ITournamentService _tournamentService;

    public GetStandingsQueryHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<IReadOnlyList<Participant>>> Handle(GetStandingsQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetStandingsAsync(request.TournamentId, cancellationToken).ConfigureAwait(false);
    }
}
