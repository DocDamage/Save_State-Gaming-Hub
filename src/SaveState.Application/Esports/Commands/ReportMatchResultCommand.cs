using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to report a match result.
/// </summary>
public record ReportMatchResultCommand(
    Guid TournamentId,
    Guid MatchId,
    int Player1Score,
    int Player2Score,
    string? Notes = null,
    List<MatchGame>? Games = null
) : IRequest<Result<Match>>;

/// <summary>
/// Handler for reporting a match result.
/// </summary>
public sealed class ReportMatchResultCommandHandler : IRequestHandler<ReportMatchResultCommand, Result<Match>>
{
    private readonly ITournamentService _tournamentService;

    public ReportMatchResultCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<Match>> Handle(ReportMatchResultCommand request, CancellationToken cancellationToken)
    {
        var resultRequest = new ReportMatchResultRequest(
            request.Player1Score,
            request.Player2Score,
            request.Notes,
            request.Games
        );

        return await _tournamentService.ReportMatchResultAsync(
            request.TournamentId,
            request.MatchId,
            resultRequest,
            cancellationToken).ConfigureAwait(false);
    }
}
