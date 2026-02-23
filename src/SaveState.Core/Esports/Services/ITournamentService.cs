using SaveState.Core.Common;
using SaveState.Core.Esports.Models;

namespace SaveState.Core.Esports.Services;

/// <summary>
/// Service for managing esports tournaments with support for multiple bracket formats,
/// participant management, match scheduling, and comprehensive statistics.
/// </summary>
public interface ITournamentService
{
    #region Tournament Management

    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    /// <param name="request">Tournament creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the created tournament.</returns>
    Task<Result<Tournament>> CreateTournamentAsync(CreateTournamentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="request">Update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated tournament.</returns>
    Task<Result<Tournament>> UpdateTournamentAsync(Guid tournamentId, UpdateTournamentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteTournamentAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Gets a tournament by ID.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the tournament.</returns>
    Task<Result<Tournament>> GetTournamentAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Gets a list of tournaments with optional filtering.
    /// </summary>
    /// <param name="filter">Filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the list of tournaments.</returns>
    Task<Result<IReadOnlyList<Tournament>>> GetTournamentsAsync(TournamentFilter filter, CancellationToken ct = default);

    #endregion

    #region Registration

    /// <summary>
    /// Registers a participant for a tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="request">Registration request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the registered participant.</returns>
    Task<Result<Participant>> RegisterParticipantAsync(Guid tournamentId, RegisterParticipantRequest request, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a participant from a tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="participantId">Participant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UnregisterParticipantAsync(Guid tournamentId, Guid participantId, CancellationToken ct = default);

    /// <summary>
    /// Checks in a participant for the tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="participantId">Participant identifier.</param>
    /// <param name="checkInCode">Check-in code for verification.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CheckInParticipantAsync(Guid tournamentId, Guid participantId, string checkInCode, CancellationToken ct = default);

    #endregion

    #region Bracket Management

    /// <summary>
    /// Generates the tournament bracket based on format and participants.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="options">Bracket generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated bracket.</returns>
    Task<Result<Bracket>> GenerateBracketAsync(Guid tournamentId, BracketOptions options, CancellationToken ct = default);

    /// <summary>
    /// Gets the tournament bracket.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the bracket.</returns>
    Task<Result<Bracket>> GetBracketAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Resets the tournament bracket.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ResetBracketAsync(Guid tournamentId, CancellationToken ct = default);

    #endregion

    #region Match Management

    /// <summary>
    /// Schedules a match for a specific time.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="request">Schedule request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the scheduled match.</returns>
    Task<Result<Match>> ScheduleMatchAsync(Guid tournamentId, Guid matchId, ScheduleMatchRequest request, CancellationToken ct = default);

    /// <summary>
    /// Reports the result of a match.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="request">Match result request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated match.</returns>
    Task<Result<Match>> ReportMatchResultAsync(Guid tournamentId, Guid matchId, ReportMatchResultRequest request, CancellationToken ct = default);

    /// <summary>
    /// Starts a match.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StartMatchAsync(Guid tournamentId, Guid matchId, CancellationToken ct = default);

    /// <summary>
    /// Disputes a match result.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="matchId">Match identifier.</param>
    /// <param name="reason">Dispute reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DisputeMatchAsync(Guid tournamentId, Guid matchId, string reason, CancellationToken ct = default);

    #endregion

    #region Tournament Operations

    /// <summary>
    /// Starts the tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StartTournamentAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Pauses the tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> PauseTournamentAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Resumes a paused tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ResumeTournamentAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Completes the tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CompleteTournamentAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Cancels the tournament.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="reason">Cancellation reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CancelTournamentAsync(Guid tournamentId, string reason, CancellationToken ct = default);

    #endregion

    #region Standings & Stats

    /// <summary>
    /// Gets the current tournament standings.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the standings.</returns>
    Task<Result<IReadOnlyList<Participant>>> GetStandingsAsync(Guid tournamentId, CancellationToken ct = default);

    /// <summary>
    /// Gets tournament statistics.
    /// </summary>
    /// <param name="tournamentId">Tournament identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing tournament statistics.</returns>
    Task<Result<TournamentStatistics>> GetStatisticsAsync(Guid tournamentId, CancellationToken ct = default);

    #endregion
}
