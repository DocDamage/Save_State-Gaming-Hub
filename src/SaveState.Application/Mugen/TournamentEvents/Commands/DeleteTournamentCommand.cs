using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to delete a tournament.
/// </summary>
public sealed record DeleteTournamentCommand(Guid TournamentId) : IRequest<Result>;

/// <summary>
/// Handler for DeleteTournamentCommand.
/// </summary>
public sealed class DeleteTournamentCommandHandler : IRequestHandler<DeleteTournamentCommand, Result>
{
    private readonly ITournamentEventService _tournamentService;

    public DeleteTournamentCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result> Handle(DeleteTournamentCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.DeleteTournamentAsync(request.TournamentId, cancellationToken);
    }
}







