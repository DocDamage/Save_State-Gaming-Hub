using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Queries;

/// <summary>
/// Query to get tournament statistics.
/// </summary>
public record GetTournamentStatisticsQuery(Guid TournamentId) : IRequest<Result<TournamentStatistics>>;

/// <summary>
/// Handler for getting tournament statistics.
/// </summary>
public sealed class GetTournamentStatisticsQueryHandler : IRequestHandler<GetTournamentStatisticsQuery, Result<TournamentStatistics>>
{
    private readonly ITournamentService _tournamentService;

    public GetTournamentStatisticsQueryHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentStatistics>> Handle(GetTournamentStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetStatisticsAsync(request.TournamentId, cancellationToken).ConfigureAwait(false);
    }
}
