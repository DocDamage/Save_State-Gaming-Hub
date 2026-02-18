using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;
using TournamentModel = SaveState.Core.Mugen.TournamentEvents.TournamentEvent;

namespace SaveState.Application.Mugen.TournamentEvents.Queries;

/// <summary>
/// Query to search tournaments.
/// </summary>
public sealed record SearchTournamentsQuery(
    TournamentFormat? Format = null,
    TournamentStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Organizer = null,
    List<string>? Tags = null,
    bool? IsPublic = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<List<TournamentModel>>>;

/// <summary>
/// Handler for SearchTournamentsQuery.
/// </summary>
public sealed class SearchTournamentsQueryHandler : IRequestHandler<SearchTournamentsQuery, Result<List<TournamentModel>>>
{
    private readonly ITournamentEventService _tournamentService;

    public SearchTournamentsQueryHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<List<TournamentModel>>> Handle(SearchTournamentsQuery request, CancellationToken cancellationToken)
    {
        var filter = new TournamentFilter
        {
            Format = request.Format,
            Status = request.Status,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Organizer = request.Organizer,
            Tags = request.Tags,
            IsPublic = request.IsPublic,
            SearchTerm = request.SearchTerm
        };

        return await _tournamentService.SearchTournamentsAsync(
            filter,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}







