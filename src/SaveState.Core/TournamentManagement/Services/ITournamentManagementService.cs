using SaveState.Core.Common;
using SaveState.Core.TournamentManagement.Models;

namespace SaveState.Core.TournamentManagement.Services;

/// <summary>
/// Service for managing gaming tournaments with multiple bracket formats, scheduling, and prize pools.
/// </summary>
public interface ITournamentManagementService
{
    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    /// <param name="request">Tournament creation request.</param>
    /// <param name="organizerId">Organizer user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the created tournament.</returns>
    Task<Result<Tournament>> CreateTournamentAsync(CreateTournamentRequest request, string organizerId, CancellationToken ct = default);

    /// <summary>
    /// Gets a tournament by ID.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the tournament.</returns>
    Task<Result<Tournament>> GetTournamentAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Updates a tournament.
    /// </summary>
    /// <param name="tournament">Updated tournament data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated tournament.</returns>
    Task<Result<Tournament>> UpdateTournamentAsync(Tournament tournament, CancellationToken ct = default);

    /// <summary>
    /// Deletes a tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteTournamentAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Lists tournaments with optional filters.
    /// </summary>
    /// <param name="status">Filter by status.</param>
    /// <param name="gameId">Filter by game.</param>
    /// <param name="organizerId">Filter by organizer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing list of tournaments.</returns>
    Task<Result<IReadOnlyList<Tournament>>> ListTournamentsAsync(TournamentStatus? status = null, string? gameId = null, string? organizerId = null, CancellationToken ct = default);

    /// <summary>
    /// Registers a participant for a tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="displayName">Display name for the tournament.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the participant.</returns>
    Task<Result<TournamentParticipant>> RegisterParticipantAsync(string tournamentId, string userId, string displayName, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a participant from a tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="participantId">Participant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UnregisterParticipantAsync(string tournamentId, string participantId, CancellationToken ct = default);

    /// <summary>
    /// Checks in a participant for the tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="participantId">Participant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CheckInParticipantAsync(string tournamentId, string participantId, CancellationToken ct = default);

    /// <summary>
    /// Generates the tournament bracket.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated bracket.</returns>
    Task<Result<TournamentBracket>> GenerateBracketAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Gets the tournament bracket.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the bracket.</returns>
    Task<Result<TournamentBracket>> GetBracketAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Starts the tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StartTournamentAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Reports match results.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="participant1Score">Participant 1 score.</param>
    /// <param name="participant2Score">Participant 2 score.</param>
    /// <param name="reporterId">User ID reporting the results.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated match.</returns>
    Task<Result<TournamentMatch>> ReportMatchResultAsync(string tournamentId, string matchId, int participant1Score, int participant2Score, string reporterId, CancellationToken ct = default);

    /// <summary>
    /// Confirms match results.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="confirmerId">User ID confirming the results.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the confirmed match.</returns>
    Task<Result<TournamentMatch>> ConfirmMatchResultAsync(string tournamentId, string matchId, string confirmerId, CancellationToken ct = default);

    /// <summary>
    /// Advances the winner to the next round.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AdvanceWinnerAsync(string tournamentId, string matchId, CancellationToken ct = default);

    /// <summary>
    /// Generates automated tournament schedule.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="startTime">Preferred start time.</param>
    /// <param name="timeBetweenMatches">Time between matches.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the schedule.</returns>
    Task<Result<TournamentSchedule>> GenerateScheduleAsync(string tournamentId, DateTime startTime, TimeSpan timeBetweenMatches, CancellationToken ct = default);

    /// <summary>
    /// Gets the tournament schedule.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the schedule.</returns>
    Task<Result<TournamentSchedule>> GetScheduleAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Updates match schedule.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="scheduledTime">New scheduled time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated match.</returns>
    Task<Result<TournamentMatch>> UpdateMatchScheduleAsync(string tournamentId, string matchId, DateTime scheduledTime, CancellationToken ct = default);

    /// <summary>
    /// Adds funds to the prize pool.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="contributorId">Contributor user ID.</param>
    /// <param name="amount">Amount to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing updated prize pool.</returns>
    Task<Result<PrizePool>> ContributeToPrizePoolAsync(string tournamentId, string contributorId, decimal amount, CancellationToken ct = default);

    /// <summary>
    /// Distributes prizes to winners.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DistributePrizesAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Gets current tournament standings.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing standings.</returns>
    Task<Result<TournamentStandings>> GetStandingsAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Gets matches for a participant.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="participantId">Participant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing participant's matches.</returns>
    Task<Result<IReadOnlyList<TournamentMatch>>> GetParticipantMatchesAsync(string tournamentId, string participantId, CancellationToken ct = default);

    /// <summary>
    /// Disqualifies a participant.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="participantId">Participant identifier.</param>
    /// <param name="reason">Disqualification reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DisqualifyParticipantAsync(string tournamentId, string participantId, string reason, CancellationToken ct = default);

    /// <summary>
    /// Completes the tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CompleteTournamentAsync(string tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Gets tournament statistics.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing tournament statistics.</returns>
    Task<Result<Dictionary<string, object>>> GetTournamentStatsAsync(string tournamentId, CancellationToken ct = default);
}
