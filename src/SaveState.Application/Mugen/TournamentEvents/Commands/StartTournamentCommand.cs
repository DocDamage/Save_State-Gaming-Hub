using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;
using TournamentModel = SaveState.Core.Mugen.TournamentEvents.TournamentEvent;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to start a tournament.
/// </summary>
public sealed record StartTournamentCommand(Guid TournamentId) : IRequest<Result<TournamentModel>>;

/// <summary>
/// Handler for StartTournamentCommand.
/// </summary>
public sealed class StartTournamentCommandHandler : IRequestHandler<StartTournamentCommand, Result<TournamentModel>>
{
    private readonly ITournamentEventService _tournamentService;

    public StartTournamentCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentModel>> Handle(StartTournamentCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.StartTournamentAsync(request.TournamentId, cancellationToken);
    }
}







