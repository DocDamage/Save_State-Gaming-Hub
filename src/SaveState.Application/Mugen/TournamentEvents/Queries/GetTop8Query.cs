using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Queries;

/// <summary>
/// Query to get top 8 placements.
/// </summary>
public sealed record GetTop8Query(Guid TournamentId) : IRequest<Result<List<TournamentParticipant>>>;

/// <summary>
/// Handler for GetTop8Query.
/// </summary>
public sealed class GetTop8QueryHandler : IRequestHandler<GetTop8Query, Result<List<TournamentParticipant>>>
{
    private readonly ITournamentEventService _tournamentService;

    public GetTop8QueryHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<List<TournamentParticipant>>> Handle(GetTop8Query request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetTop8Async(request.TournamentId, cancellationToken);
    }
}







