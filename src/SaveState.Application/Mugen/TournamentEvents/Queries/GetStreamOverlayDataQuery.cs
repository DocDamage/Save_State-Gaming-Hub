using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Queries;

/// <summary>
/// Query to get stream overlay data.
/// </summary>
public sealed record GetStreamOverlayDataQuery(Guid TournamentId) : IRequest<Result<StreamOverlayData>>;

/// <summary>
/// Handler for GetStreamOverlayDataQuery.
/// </summary>
public sealed class GetStreamOverlayDataQueryHandler : IRequestHandler<GetStreamOverlayDataQuery, Result<StreamOverlayData>>
{
    private readonly ITournamentEventService _tournamentService;

    public GetStreamOverlayDataQueryHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<StreamOverlayData>> Handle(GetStreamOverlayDataQuery request, CancellationToken cancellationToken)
    {
        return await _tournamentService.GetStreamOverlayDataAsync(request.TournamentId, cancellationToken);
    }
}







