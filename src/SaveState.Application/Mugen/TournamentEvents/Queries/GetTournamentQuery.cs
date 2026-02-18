using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;
using TournamentModel = SaveState.Core.Mugen.TournamentEvents.TournamentEvent;

namespace SaveState.Application.Mugen.TournamentEvents.Queries;

/// <summary>
/// Query to get a tournament by ID.
/// </summary>
public sealed record GetTournamentQuery(Guid TournamentId) : IRequest<Result<TournamentModel>>;

/// <summary>
/// Handler for GetTournamentQuery.
/// </summary>
public sealed class GetTournamentQueryHandler : IRequestHandler<GetTournamentQuery, Result<TournamentModel>>
{
    private readonly ITournamentEventService _tournamentService;

    public GetTournamentQueryHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentModel>> Handle(GetTournamentQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetTournamentAsync(request.TournamentId, cancellationToken);
    }
}







