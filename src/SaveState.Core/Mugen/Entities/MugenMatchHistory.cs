namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;

/// <summary>
/// Represents the history of a MUGEN match result.
/// Contains information about the match outcome, rounds won, duration, and participants.
/// </summary>
public class MugenMatchHistory : EntityBase
{
    /// <summary>
    /// The ID of player 1's character.
    /// </summary>
    public Guid Player1CharacterId { get; private set; }

    /// <summary>
    /// The ID of player 2's character.
    /// </summary>
    public Guid Player2CharacterId { get; private set; }

    /// <summary>
    /// The result of the match.
    /// </summary>
    public MatchResult Result { get; private set; }

    /// <summary>
    /// Number of rounds won by player 1.
    /// </summary>
    public int RoundsWonP1 { get; private set; }

    /// <summary>
    /// Number of rounds won by player 2.
    /// </summary>
    public int RoundsWonP2 { get; private set; }

    /// <summary>
    /// The total duration of the match.
    /// </summary>
    public TimeSpan MatchDuration { get; private set; }

    /// <summary>
    /// When the match was played.
    /// </summary>
    public DateTime PlayedAt { get; private set; }

    /// <summary>
    /// The game mode of the match.
    /// </summary>
    public GameMode Mode { get; private set; }

    /// <summary>
    /// Optional path to replay file.
    /// </summary>
    public string? ReplayPath { get; private set; }

    /// <summary>
    /// Creates a new match history record.
    /// </summary>
    /// <param name="p1Id">Player 1 character ID.</param>
    /// <param name="p2Id">Player 2 character ID.</param>
    /// <param name="result">Match result.</param>
    /// <param name="roundsP1">Rounds won by player 1.</param>
    /// <param name="roundsP2">Rounds won by player 2.</param>
    /// <param name="duration">Match duration.</param>
    /// <param name="mode">Game mode.</param>
    /// <returns>A new MugenMatchHistory instance.</returns>
    public static MugenMatchHistory Create(
        Guid p1Id,
        Guid p2Id,
        MatchResult result,
        int roundsP1,
        int roundsP2,
        TimeSpan duration,
        GameMode mode)
    {
        return new MugenMatchHistory
        {
            Id = Guid.NewGuid(),
            Player1CharacterId = p1Id,
            Player2CharacterId = p2Id,
            Result = result,
            RoundsWonP1 = roundsP1,
            RoundsWonP2 = roundsP2,
            MatchDuration = duration,
            PlayedAt = DateTime.UtcNow,
            Mode = mode
        };
    }

    /// <summary>
    /// Sets the replay path for this match.
    /// </summary>
    /// <param name="path">Path to the replay file.</param>
    public void SetReplayPath(string? path)
    {
        ReplayPath = path;
    }

    // EF Core constructor
    private MugenMatchHistory() { }
}

/// <summary>
/// Represents the result of a MUGEN match.
/// </summary>
public enum MatchResult
{
    Player1Win,
    Player2Win,
    Draw,
    Timeout
}

/// <summary>
/// Represents the game mode of a MUGEN match.
/// </summary>
public enum GameMode
{
    Versus,
    Training,
    SinglePlayer,
    Tournament,
    Watch
}