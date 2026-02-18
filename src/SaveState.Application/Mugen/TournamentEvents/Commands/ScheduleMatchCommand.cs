using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;

namespace SaveState.Application.Mugen.TournamentEvents.Commands;

/// <summary>
/// Command to schedule a match.
/// </summary>
public sealed record ScheduleMatchCommand(
    Guid MatchId,
    DateTime ScheduledTime,
    string? Station = null) : IRequest<Result<TournamentMatch>>;

/// <summary>
/// Handler for ScheduleMatchCommand.
/// </summary>
public sealed class ScheduleMatchCommandHandler : IRequestHandler<ScheduleMatchCommand, Result<TournamentMatch>>
{
    private readonly ITournamentEventService _tournamentService;

    public ScheduleMatchCommandHandler(ITournamentEventService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    public async Task<Result<TournamentMatch>> Handle(ScheduleMatchCommand request, CancellationToken cancellationToken)
    {
        return await _tournamentService.ScheduleMatchAsync(
            request.MatchId,
            request.ScheduledTime,
            request.Station,
            cancellationToken);
    }
}







