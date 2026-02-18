using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Queries;

/// <summary>
/// Query to get upcoming matches.
/// </summary>
public sealed record GetUpcomingMatchesQuery(
    Guid TournamentId,
    int Count = 5) : IRequest<Result<List<TournamentMatch>>>;

/// <summary>
/// Handler for GetUpcomingMatchesQuery.
/// </summary>
public sealed class GetUpcomingMatchesQueryHandler : IRequestHandler<GetUpcomingMatchesQuery, Result<List<TournamentMatch>>>
{
    private readonly ITournamentEventService _tournamentService;

    public GetUpcomingMatchesQueryHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<List<TournamentMatch>>> Handle(GetUpcomingMatchesQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetUpcomingMatchesAsync(
            request.TournamentId,
            request.Count,
            cancellationToken);
    }
}







