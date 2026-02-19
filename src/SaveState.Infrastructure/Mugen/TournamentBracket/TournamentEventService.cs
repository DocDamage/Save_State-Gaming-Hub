using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.TournamentEvents.Services;

/// <summary>
/// Thin facade for tournament event operations.
/// </summary>
public class TournamentEventService : ITournamentEventService
{
    private readonly TournamentEventServiceOperations _operations;

    public TournamentEventService(
        SaveStateDbContext dbContext,
        ILogger<TournamentEventService> logger)
    {
        _operations = new TournamentEventServiceOperations(dbContext, logger);
    }

    public Task<Result<TournamentEvent>> CreateTournamentAsync(CreateTournamentRequest request, CancellationToken ct = default)
        => _operations.CreateTournamentAsync(request, ct);

    public Task<Result<TournamentEvent>> GetTournamentAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.GetTournamentAsync(tournamentId, ct);

    public Task<Result<TournamentEvent>> UpdateTournamentAsync(Guid tournamentId, CreateTournamentRequest request, CancellationToken ct = default)
        => _operations.UpdateTournamentAsync(tournamentId, request, ct);

    public Task<Result> DeleteTournamentAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.DeleteTournamentAsync(tournamentId, ct);

    public Task<Result<List<TournamentEvent>>> SearchTournamentsAsync(TournamentFilter filter, int page = 1, int pageSize = 20, CancellationToken ct = default)
        => _operations.SearchTournamentsAsync(filter, page, pageSize, ct);

    public Task<Result<TournamentParticipant>> RegisterParticipantAsync(Guid tournamentId, RegisterParticipantRequest request, CancellationToken ct = default)
        => _operations.RegisterParticipantAsync(tournamentId, request, ct);

    public Task<Result> UnregisterParticipantAsync(Guid tournamentId, Guid participantId, CancellationToken ct = default)
        => _operations.UnregisterParticipantAsync(tournamentId, participantId, ct);

    public Task<Result> CheckInParticipantAsync(Guid tournamentId, Guid participantId, CancellationToken ct = default)
        => _operations.CheckInParticipantAsync(tournamentId, participantId, ct);

    public Task<Result<TournamentEvent>> StartTournamentAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.StartTournamentAsync(tournamentId, ct);

    public Task<Result<TournamentEvent>> GenerateBracketAsync(Guid tournamentId, SeedingMethod seedingMethod, CancellationToken ct = default)
        => _operations.GenerateBracketAsync(tournamentId, seedingMethod, ct);

    public Task<Result<TournamentMatch>> GetMatchAsync(Guid matchId, CancellationToken ct = default)
        => _operations.GetMatchAsync(matchId, ct);

    public Task<Result<TournamentMatch>> ReportMatchResultAsync(Guid matchId, ReportMatchResultRequest request, CancellationToken ct = default)
        => _operations.ReportMatchResultAsync(matchId, request, ct);

    public Task<Result> AdvanceWinnerAsync(Guid matchId, CancellationToken ct = default)
        => _operations.AdvanceWinnerAsync(matchId, ct);

    public Task<Result<List<TournamentMatch>>> GetTournamentMatchesAsync(Guid tournamentId, int? round = null, BracketPosition? bracket = null, CancellationToken ct = default)
        => _operations.GetTournamentMatchesAsync(tournamentId, round, bracket, ct);

    public Task<Result<List<TournamentMatch>>> GetParticipantMatchesAsync(Guid tournamentId, Guid participantId, CancellationToken ct = default)
        => _operations.GetParticipantMatchesAsync(tournamentId, participantId, ct);

    public Task<Result<List<TournamentMatch>>> GetUpcomingMatchesAsync(Guid tournamentId, int count = 5, CancellationToken ct = default)
        => _operations.GetUpcomingMatchesAsync(tournamentId, count, ct);

    public Task<Result<TournamentMatch>> ScheduleMatchAsync(Guid matchId, DateTime scheduledTime, string? station = null, CancellationToken ct = default)
        => _operations.ScheduleMatchAsync(matchId, scheduledTime, station, ct);

    public Task<Result<TournamentMatch>> AssignStationAsync(Guid matchId, string station, bool isStreamed = false, CancellationToken ct = default)
        => _operations.AssignStationAsync(matchId, station, isStreamed, ct);

    public Task<Result<List<TournamentParticipant>>> GetStandingsAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.GetStandingsAsync(tournamentId, ct);

    public Task<Result> SeedParticipantsAsync(Guid tournamentId, List<Guid> participantIdsInOrder, CancellationToken ct = default)
        => _operations.SeedParticipantsAsync(tournamentId, participantIdsInOrder, ct);

    public Task<Result> RandomizeSeedsAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.RandomizeSeedsAsync(tournamentId, ct);

    public Task<Result<List<TournamentParticipant>>> GetTop8Async(Guid tournamentId, CancellationToken ct = default)
        => _operations.GetTop8Async(tournamentId, ct);

    public Task<Result<string>> ExportToChallongeAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.ExportToChallongeAsync(tournamentId, ct);

    public Task<Result<string>> GenerateStreamOverlayAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.GenerateStreamOverlayAsync(tournamentId, ct);

    public Task<Result<StreamOverlayData>> GetStreamOverlayDataAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.GetStreamOverlayDataAsync(tournamentId, ct);

    public Task<Result> SendDiscordNotificationAsync(Guid tournamentId, string message, CancellationToken ct = default)
        => _operations.SendDiscordNotificationAsync(tournamentId, message, ct);

    public Task<Result> PauseTournamentAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.PauseTournamentAsync(tournamentId, ct);

    public Task<Result> ResumeTournamentAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.ResumeTournamentAsync(tournamentId, ct);

    public Task<Result<TournamentEvent>> EndTournamentAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.EndTournamentAsync(tournamentId, ct);

    public Task<Result<TournamentStatistics>> GetStatisticsAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.GetStatisticsAsync(tournamentId, ct);

    public Task<Result<List<string>>> ValidateBracketAsync(Guid tournamentId, CancellationToken ct = default)
        => _operations.ValidateBracketAsync(tournamentId, ct);

    public Task<Result<TournamentMatch>> ResetMatchAsync(Guid matchId, CancellationToken ct = default)
        => _operations.ResetMatchAsync(matchId, ct);

    public Task<Result> DisqualifyParticipantAsync(Guid tournamentId, Guid participantId, string reason, CancellationToken ct = default)
        => _operations.DisqualifyParticipantAsync(tournamentId, participantId, reason, ct);

    public Task<Result<TournamentParticipant>> ReplaceParticipantAsync(Guid tournamentId, Guid participantIdToReplace, RegisterParticipantRequest newParticipant, CancellationToken ct = default)
        => _operations.ReplaceParticipantAsync(tournamentId, participantIdToReplace, newParticipant, ct);
}
