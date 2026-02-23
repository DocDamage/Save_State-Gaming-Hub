using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to delete a tournament.
/// </summary>
public record DeleteTournamentCommand(Guid TournamentId) : IRequest<Result>;

/// <summary>
/// Handler for deleting a tournament.
/// </summary>
public sealed class DeleteTournamentCommandHandler : IRequestHandler<DeleteTournamentCommand, Result>
{
    private readonly ITournamentService _tournamentService;

    public DeleteTournamentCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result> Handle(DeleteTournamentCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.DeleteTournamentAsync(request.TournamentId, cancellationToken).ConfigureAwait(false);
    }
}
