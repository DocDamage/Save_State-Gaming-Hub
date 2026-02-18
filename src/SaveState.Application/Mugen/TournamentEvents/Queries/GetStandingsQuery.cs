using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Queries;

/// <summary>
/// Query to get tournament standings.
/// </summary>
public sealed record GetStandingsQuery(Guid TournamentId) : IRequest<Result<List<TournamentParticipant>>>;

/// <summary>
/// Handler for GetStandingsQuery.
/// </summary>
public sealed class GetStandingsQueryHandler : IRequestHandler<GetStandingsQuery, Result<List<TournamentParticipant>>>
{
    private readonly ITournamentEventService _tournamentService;

    public GetStandingsQueryHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<List<TournamentParticipant>>> Handle(GetStandingsQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetStandingsAsync(request.TournamentId, cancellationToken);
    }
}







