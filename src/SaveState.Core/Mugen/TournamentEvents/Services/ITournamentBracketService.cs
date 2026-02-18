using SaveState.Core.Common;

namespace SaveState.Core.Mugen.TournamentEvents.Services;

/// <summary>
/// Service for managing tournament brackets.
/// </summary>
public interface ITournamentEventService
{
    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    Task<Result<TournamentEvent>> CreateTournamentAsync(
        CreateTournamentRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a tournament by ID.
    /// </summary>
    Task<Result<TournamentEvent>> GetTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Updates a tournament.
    /// </summary>
    Task<Result<TournamentEvent>> UpdateTournamentAsync(
        Guid tournamentId,
        CreateTournamentRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a tournament.
    /// </summary>
    Task<Result> DeleteTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Searches tournaments.
    /// </summary>
    Task<Result<List<TournamentEvent>>> SearchTournamentsAsync(
        TournamentFilter filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    
    /// <summary>
    /// Registers a participant.
    /// </summary>
    Task<Result<TournamentParticipant>> RegisterParticipantAsync(
        Guid tournamentId,
        RegisterParticipantRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Unregisters a participant.
    /// </summary>
    Task<Result> UnregisterParticipantAsync(
        Guid tournamentId,
        Guid participantId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Checks in a participant.
    /// </summary>
    Task<Result> CheckInParticipantAsync(
        Guid tournamentId,
        Guid participantId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Starts the tournament.
    /// </summary>
    Task<Result<TournamentEvent>> StartTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generates the bracket.
    /// </summary>
    Task<Result<TournamentEvent>> GenerateBracketAsync(
        Guid tournamentId,
        SeedingMethod seedingMethod,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a match by ID.
    /// </summary>
    Task<Result<TournamentMatch>> GetMatchAsync(
        Guid matchId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Reports match results.
    /// </summary>
    Task<Result<TournamentMatch>> ReportMatchResultAsync(
        Guid matchId,
        ReportMatchResultRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Advances a match to next round.
    /// </summary>
    Task<Result> AdvanceWinnerAsync(
        Guid matchId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all matches for a tournament.
    /// </summary>
    Task<Result<List<TournamentMatch>>> GetTournamentMatchesAsync(
        Guid tournamentId,
        int? round = null,
        BracketPosition? bracket = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets matches for a participant.
    /// </summary>
    Task<Result<List<TournamentMatch>>> GetParticipantMatchesAsync(
        Guid tournamentId,
        Guid participantId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets upcoming matches.
    /// </summary>
    Task<Result<List<TournamentMatch>>> GetUpcomingMatchesAsync(
        Guid tournamentId,
        int count = 5,
        CancellationToken ct = default);
    
    /// <summary>
    /// Sets a match schedule.
    /// </summary>
    Task<Result<TournamentMatch>> ScheduleMatchAsync(
        Guid matchId,
        DateTime scheduledTime,
        string? station = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Assigns a station to a match.
    /// </summary>
    Task<Result<TournamentMatch>> AssignStationAsync(
        Guid matchId,
        string station,
        bool isStreamed = false,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets standings for Swiss/Round Robin.
    /// </summary>
    Task<Result<List<TournamentParticipant>>> GetStandingsAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Seeds participants manually.
    /// </summary>
    Task<Result> SeedParticipantsAsync(
        Guid tournamentId,
        List<Guid> participantIdsInOrder,
        CancellationToken ct = default);
    
    /// <summary>
    /// Randomizes seeds.
    /// </summary>
    Task<Result> RandomizeSeedsAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets top 8/placements.
    /// </summary>
    Task<Result<List<TournamentParticipant>>> GetTop8Async(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exports bracket to Challonge format.
    /// </summary>
    Task<Result<string>> ExportToChallongeAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generates stream overlay HTML.
    /// </summary>
    Task<Result<string>> GenerateStreamOverlayAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets stream overlay data.
    /// </summary>
    Task<Result<StreamOverlayData>> GetStreamOverlayDataAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Sends Discord notification.
    /// </summary>
    Task<Result> SendDiscordNotificationAsync(
        Guid tournamentId,
        string message,
        CancellationToken ct = default);
    
    /// <summary>
    /// Pauses the tournament.
    /// </summary>
    Task<Result> PauseTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Resumes the tournament.
    /// </summary>
    Task<Result> ResumeTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Ends the tournament.
    /// </summary>
    Task<Result<TournamentEvent>> EndTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets tournament statistics.
    /// </summary>
    Task<Result<TournamentStatistics>> GetStatisticsAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Validates a bracket.
    /// </summary>
    Task<Result<List<string>>> ValidateBracketAsync(
        Guid tournamentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Resets a match.
    /// </summary>
    Task<Result<TournamentMatch>> ResetMatchAsync(
        Guid matchId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Disqualifies a participant.
    /// </summary>
    Task<Result> DisqualifyParticipantAsync(
        Guid tournamentId,
        Guid participantId,
        string reason,
        CancellationToken ct = default);
    
    /// <summary>
    /// Replaces a participant.
    /// </summary>
    Task<Result<TournamentParticipant>> ReplaceParticipantAsync(
        Guid tournamentId,
        Guid participantIdToReplace,
        RegisterParticipantRequest newParticipant,
        CancellationToken ct = default);
}

