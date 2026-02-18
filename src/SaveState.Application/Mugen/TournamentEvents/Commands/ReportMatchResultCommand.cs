using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to report match results.
/// </summary>
public sealed record ReportMatchResultCommand(
    Guid MatchId,
    int Score1,
    int Score2,
    Guid WinnerId,
    List<RoundResult>? RoundResults = null,
    MatchEndCondition EndCondition = MatchEndCondition.Normal,
    string? ReplayPath = null,
    string? Notes = null) : IRequest<Result<TournamentMatch>>;

/// <summary>
/// Handler for ReportMatchResultCommand.
/// </summary>
public sealed class ReportMatchResultCommandHandler : IRequestHandler<ReportMatchResultCommand, Result<TournamentMatch>>
{
    private readonly ITournamentEventService _tournamentService;

    public ReportMatchResultCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentMatch>> Handle(ReportMatchResultCommand request, CancellationToken cancellationToken)
    {
        var reportRequest = new ReportMatchResultRequest
        {
            Score1 = request.Score1,
            Score2 = request.Score2,
            WinnerId = request.WinnerId,
            RoundResults = request.RoundResults,
            EndCondition = request.EndCondition,
            ReplayPath = request.ReplayPath,
            Notes = request.Notes
        };

        return await _tournamentService.ReportMatchResultAsync(
            request.MatchId,
            reportRequest,
            cancellationToken);
    }
}







