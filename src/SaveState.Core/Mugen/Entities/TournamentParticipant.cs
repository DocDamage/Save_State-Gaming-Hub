namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

/// <summary>
/// Represents a participant in a MUGEN tournament.
/// </summary>
public class TournamentParticipant : EntityBase
{
    /// <summary>
    /// The ID of the tournament this participant belongs to.
    /// </summary>
    public Guid TournamentId { get; private set; }

    /// <summary>
    /// The tournament this participant belongs to.
    /// </summary>
    public MugenTournament Tournament { get; private set; } = null;

    /// <summary>
    /// The matches this participant has played in.
    /// </summary>
    public ICollection<TournamentMatchEntity> Matches { get; private set; } = new List<TournamentMatchEntity>();

    /// <summary>
    /// The ID of the character this participant is using.
    /// </summary>
    public Guid CharacterId { get; private set; }

    /// <summary>
    /// The character this participant is using.
    /// </summary>
    public MugenCharacter Character { get; private set; } = null;

    /// <summary>
    /// The participant's seed/position in the tournament bracket.
    /// </summary>
    public int Seed { get; private set; }

    /// <summary>
    /// The current status of this participant in the tournament.
    /// </summary>
    public ParticipantStatus Status { get; private set; }

    /// <summary>
    /// The participant's current score/wins in the tournament.
    /// </summary>
    public int Score { get; private set; }

    /// <summary>
    /// When the participant was eliminated (if applicable).
    /// </summary>
    public DateTime? EliminatedAt { get; private set; }

    /// <summary>
    /// Creates a new tournament participant.
    /// </summary>
    /// <param name="tournamentId">The tournament ID.</param>
    /// <param name="characterId">The character ID.</param>
    /// <param name="seed">The participant's seed position.</param>
    /// <returns>A new TournamentParticipant instance.</returns>
    public static TournamentParticipant Create(Guid tournamentId, Guid characterId, int seed)
    {
        return new TournamentParticipant
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            CharacterId = characterId,
            Seed = seed,
            Status = ParticipantStatus.Active,
            Score = 0
        };
    }

    // Note: TournamentParticipant.Create does not use DateTime, so no changes needed

    /// <summary>
    /// Records a win for this participant.
    /// </summary>
    public void RecordWin()
    {
        if (Status != ParticipantStatus.Active)
            throw new InvalidOperationException("Cannot record wins for inactive participants.");

        Score++;
    }

    /// <summary>
    /// Eliminates this participant from the tournament.
    /// </summary>
    /// <param name="timeProvider">The time provider for timestamp generation.</param>
    public void Eliminate(ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        if (Status != ParticipantStatus.Active)
            throw new InvalidOperationException("Participant is already eliminated.");

        Status = ParticipantStatus.Eliminated;
        EliminatedAt = timeProvider.UtcNow;
    }

    /// <summary>
    /// Eliminates this participant from the tournament with explicit timestamp.
    /// </summary>
    /// <param name="eliminatedAt">Elimination timestamp.</param>
    public void Eliminate(DateTime eliminatedAt)
    {
        if (Status != ParticipantStatus.Active)
            throw new InvalidOperationException("Participant is already eliminated.");

        Status = ParticipantStatus.Eliminated;
        EliminatedAt = eliminatedAt;
    }

    [Obsolete("Use Eliminate(ITimeProvider) or Eliminate(DateTime) instead")]
    public void Eliminate()
    {
        if (Status != ParticipantStatus.Active)
            throw new InvalidOperationException("Participant is already eliminated.");

        Status = ParticipantStatus.Eliminated;
        EliminatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    /// <summary>
    /// Marks this participant as the winner.
    /// </summary>
    public void MarkAsWinner()
    {
        Status = ParticipantStatus.Winner;
    }

    // EF Core constructor
    private TournamentParticipant() { }
}

/// <summary>
/// Represents the status of a tournament participant.
/// </summary>
public enum ParticipantStatus
{
    Active,
    Eliminated,
    Winner
}