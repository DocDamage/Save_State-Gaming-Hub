using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Queries;

/// <summary>
/// Query to get matches for a tournament.
/// </summary>
public sealed record GetTournamentMatchesQuery(
    Guid TournamentId,
    int? Round = null,
    BracketPosition? Bracket = null) : IRequest<Result<List<TournamentMatch>>>;

/// <summary>
/// Handler for GetTournamentMatchesQuery.
/// </summary>
public sealed class GetTournamentMatchesQueryHandler : IRequestHandler<GetTournamentMatchesQuery, Result<List<TournamentMatch>>>
{
    private readonly ITournamentEventService _tournamentService;

    public GetTournamentMatchesQueryHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<List<TournamentMatch>>> Handle(GetTournamentMatchesQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetTournamentMatchesAsync(
            request.TournamentId,
            request.Round,
            request.Bracket,
            cancellationToken);
    }
}







