using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Queries;

/// <summary>
/// Query to get tournament statistics.
/// </summary>
public sealed record GetTournamentStatisticsQuery(Guid TournamentId) : IRequest<Result<TournamentStatistics>>;

/// <summary>
/// Handler for GetTournamentStatisticsQuery.
/// </summary>
public sealed class GetTournamentStatisticsQueryHandler : IRequestHandler<GetTournamentStatisticsQuery, Result<TournamentStatistics>>
{
    private readonly ITournamentEventService _tournamentService;

    public GetTournamentStatisticsQueryHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentStatistics>> Handle(GetTournamentStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetStatisticsAsync(request.TournamentId, cancellationToken);
    }
}







