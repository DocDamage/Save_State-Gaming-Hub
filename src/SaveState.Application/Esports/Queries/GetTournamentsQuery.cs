using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Queries;

/// <summary>
/// Query to get a list of tournaments with optional filtering.
/// </summary>
public record GetTournamentsQuery(
    TournamentStatus? Status = null,
    TournamentFormat? Format = null,
    Guid? GameId = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    string? CreatedBy = null,
    bool IncludeCompleted = false
) : IRequest<Result<IReadOnlyList<Tournament>>>;

/// <summary>
/// Handler for getting tournaments.
/// </summary>
public sealed class GetTournamentsQueryHandler : IRequestHandler<GetTournamentsQuery, Result<IReadOnlyList<Tournament>>>
{
    private readonly ITournamentService _tournamentService;

    public GetTournamentsQueryHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<IReadOnlyList<Tournament>>> Handle(GetTournamentsQuery request, CancellationToken cancellationToken)
    {
        var filter = new TournamentFilter(
            request.Status,
            request.Format,
            request.GameId,
            request.StartDateFrom,
            request.StartDateTo,
            request.CreatedBy,
            request.IncludeCompleted
        );

        return await _tournamentService.GetTournamentsAsync(filter, cancellationToken).ConfigureAwait(false);
    }
}
