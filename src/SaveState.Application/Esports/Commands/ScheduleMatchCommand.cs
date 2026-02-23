using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Application.Esports.Commands;

/// <summary>
/// Command to schedule a match.
/// </summary>
public record ScheduleMatchCommand(
    Guid TournamentId,
    Guid MatchId,
    DateTime ScheduledTime,
    string? StreamUrl = null
) : IRequest<Result<Match>>;

/// <summary>
/// Handler for scheduling a match.
/// </summary>
public sealed class ScheduleMatchCommandHandler : IRequestHandler<ScheduleMatchCommand, Result<Match>>
{
    private readonly ITournamentService _tournamentService;

    public ScheduleMatchCommandHandler(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<Match>> Handle(ScheduleMatchCommand request, CancellationToken cancellationToken)
    {
        var scheduleRequest = new ScheduleMatchRequest(
            request.ScheduledTime,
            request.StreamUrl
        );

        return await _tournamentService.ScheduleMatchAsync(
            request.TournamentId,
            request.MatchId,
            scheduleRequest,
            cancellationToken).ConfigureAwait(false);
    }
}
