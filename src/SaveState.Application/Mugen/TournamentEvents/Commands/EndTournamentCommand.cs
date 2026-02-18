using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;
using TournamentModel = SaveState.Core.Mugen.TournamentEvents.TournamentEvent;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to end a tournament.
/// </summary>
public sealed record EndTournamentCommand(Guid TournamentId) : IRequest<Result<TournamentModel>>;

/// <summary>
/// Handler for EndTournamentCommand.
/// </summary>
public sealed class EndTournamentCommandHandler : IRequestHandler<EndTournamentCommand, Result<TournamentModel>>
{
    private readonly ITournamentEventService _tournamentService;

    public EndTournamentCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentModel>> Handle(EndTournamentCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.EndTournamentAsync(request.TournamentId, cancellationToken);
    }
}







