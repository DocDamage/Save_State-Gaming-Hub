namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;

/// <summary>
/// Represents a match within a MUGEN tournament (Entity for persistence).
/// </summary>
public class TournamentMatchEntity : EntityBase
{
    /// <summary>
    /// The ID of the tournament this match belongs to.
    /// </summary>
    public Guid TournamentId { get; private set; }

    /// <summary>
    /// The tournament this match belongs to.
    /// </summary>
    public MugenTournament Tournament { get; private set; } = null!;

    /// <summary>
    /// The round number of this match.
    /// </summary>
    public int Round { get; private set; }

    /// <summary>
    /// The match number within the round.
    /// </summary>
    public int MatchNumber { get; private set; }

    /// <summary>
    /// The ID of player 1's character.
    /// </summary>
    public Guid? Player1CharacterId { get; private set; }

    /// <summary>
    /// The ID of player 2's character.
    /// </summary>
    public Guid? Player2CharacterId { get; private set; }

    /// <summary>
    /// The ID of the winning character (if match is completed).
    /// </summary>
    public Guid? WinnerId { get; private set; }

    /// <summary>
    /// The current status of the match.
    /// </summary>
    public MatchStatus Status { get; private set; }

    /// <summary>
    /// When the match was completed (if finished).
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Optional notes about the match.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Creates a new tournament match.
    /// </summary>
    /// <param name="tournamentId">The tournament ID.</param>
    /// <param name="round">The round number.</param>
    /// <param name="matchNumber">The match number within the round.</param>
    /// <param name="player1CharacterId">Player 1 character ID.</param>
    /// <param name="player2CharacterId">Player 2 character ID.</param>
    /// <returns>A new TournamentMatchEntity instance.</returns>
    public static TournamentMatchEntity Create(
        Guid tournamentId,
        int round,
        int matchNumber,
        Guid? player1CharacterId,
        Guid? player2CharacterId)
    {
        return new TournamentMatchEntity
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            Round = round,
            MatchNumber = matchNumber,
            Player1CharacterId = player1CharacterId,
            Player2CharacterId = player2CharacterId,
            Status = MatchStatus.Scheduled
        };
    }

    /// <summary>
    /// Marks the match as completed with a winner.
    /// </summary>
    /// <param name="winnerId">The winning character ID.</param>
    /// <param name="notes">Optional notes about the match.</param>
    public void Complete(Guid winnerId, string? notes = null)
    {
        if (Status != MatchStatus.InProgress && Status != MatchStatus.Scheduled)
            throw new InvalidOperationException("Match can only be completed from scheduled or in-progress state.");

        WinnerId = winnerId;
        CompletedAt = DateTime.UtcNow;
        Status = MatchStatus.Completed;
        Notes = notes;
    }

    /// <summary>
    /// Starts the match.
    /// </summary>
    public void Start()
    {
        if (Status != MatchStatus.Scheduled)
            throw new InvalidOperationException("Match can only be started from scheduled state.");

        Status = MatchStatus.InProgress;
    }

    /// <summary>
    /// Cancels the match.
    /// </summary>
    public void Cancel()
    {
        if (Status == MatchStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed match.");

        Status = MatchStatus.Cancelled;
    }

    // EF Core constructor
    private TournamentMatchEntity() { }
}

/// <summary>
/// Represents the status of a tournament match.
/// </summary>
public enum MatchStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}