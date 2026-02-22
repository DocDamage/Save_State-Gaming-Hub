namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

/// <summary>
/// Represents matchup statistics between two MUGEN characters.
/// Tracks win/loss ratios and performance metrics for specific character combinations.
/// </summary>
public class MugenMatchupStats : EntityBase
{
    /// <summary>
    /// The ID of the first character in this matchup.
    /// </summary>
    public Guid Character1Id { get; private set; }

    /// <summary>
    /// The first character in this matchup.
    /// </summary>
    public MugenCharacter Character1 { get; private set; } = null;

    /// <summary>
    /// The ID of the second character in this matchup.
    /// </summary>
    public Guid Character2Id { get; private set; }

    /// <summary>
    /// The second character in this matchup.
    /// </summary>
    public MugenCharacter Character2 { get; private set; } = null;

    /// <summary>
    /// Total number of matches played between these characters.
    /// </summary>
    public int TotalMatches { get; private set; }

    /// <summary>
    /// Number of wins for character 1.
    /// </summary>
    public int Character1Wins { get; private set; }

    /// <summary>
    /// Number of wins for character 2.
    /// </summary>
    public int Character2Wins { get; private set; }

    /// <summary>
    /// Number of draws between these characters.
    /// </summary>
    public int Draws { get; private set; }

    /// <summary>
    /// Average match duration for this matchup.
    /// </summary>
    public TimeSpan AverageMatchDuration { get; private set; }

    /// <summary>
    /// The win rate for character 1 (0.0 to 1.0).
    /// </summary>
    public double Character1WinRate => TotalMatches > 0 ? (double)Character1Wins / TotalMatches : 0;

    /// <summary>
    /// The win rate for character 2 (0.0 to 1.0).
    /// </summary>
    public double Character2WinRate => TotalMatches > 0 ? (double)Character2Wins / TotalMatches : 0;

    /// <summary>
    /// When these statistics were last updated.
    /// </summary>
    public DateTime LastUpdated { get; private set; }

    /// <summary>
    /// Creates matchup statistics for two characters.
    /// </summary>
    /// <param name="character1Id">First character ID.</param>
    /// <param name="character2Id">Second character ID.</param>
    /// <param name="timeProvider">The time provider for timestamp generation.</param>
    /// <returns>A new MugenMatchupStats instance.</returns>
    public static MugenMatchupStats Create(Guid character1Id, Guid character2Id, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new MugenMatchupStats
        {
            Id = Guid.NewGuid(),
            Character1Id = character1Id,
            Character2Id = character2Id,
            TotalMatches = 0,
            Character1Wins = 0,
            Character2Wins = 0,
            Draws = 0,
            AverageMatchDuration = TimeSpan.Zero,
            LastUpdated = timeProvider.UtcNow
        };
    }

    /// <summary>
    /// Creates matchup statistics for two characters with explicit timestamp.
    /// </summary>
    /// <param name="character1Id">First character ID.</param>
    /// <param name="character2Id">Second character ID.</param>
    /// <param name="createdAt">Creation timestamp.</param>
    /// <returns>A new MugenMatchupStats instance.</returns>
    public static MugenMatchupStats Create(Guid character1Id, Guid character2Id, DateTime createdAt)
    {
        return new MugenMatchupStats
        {
            Id = Guid.NewGuid(),
            Character1Id = character1Id,
            Character2Id = character2Id,
            TotalMatches = 0,
            Character1Wins = 0,
            Character2Wins = 0,
            Draws = 0,
            AverageMatchDuration = TimeSpan.Zero,
            LastUpdated = createdAt
        };
    }

    [Obsolete("Use Create(Guid, Guid, ITimeProvider) or Create(Guid, Guid, DateTime) instead")]
    public static MugenMatchupStats Create(Guid character1Id, Guid character2Id)
    {
        return Create(character1Id, character2Id, SystemTimeProvider.Instance);
    }

    /// <summary>
    /// Records a match result and updates the statistics.
    /// </summary>
    /// <param name="character1Won">True if character 1 won, false if character 2 won.</param>
    /// <param name="wasDraw">True if the match was a draw.</param>
    /// <param name="matchDuration">The duration of the match.</param>
    /// <param name="timeProvider">The time provider for timestamp generation.</param>
    public void RecordMatch(bool character1Won, bool wasDraw, TimeSpan matchDuration, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        TotalMatches++;

        if (wasDraw)
        {
            Draws++;
        }
        else if (character1Won)
        {
            Character1Wins++;
        }
        else
        {
            Character2Wins++;
        }

        // Update average duration using running average formula
        if (TotalMatches == 1)
        {
            AverageMatchDuration = matchDuration;
        }
        else
        {
            var totalDuration = AverageMatchDuration * (TotalMatches - 1) + matchDuration;
            AverageMatchDuration = totalDuration / TotalMatches;
        }

        LastUpdated = timeProvider.UtcNow;
    }

    /// <summary>
    /// Records a match result and updates the statistics with explicit timestamp.
    /// </summary>
    /// <param name="character1Won">True if character 1 won, false if character 2 won.</param>
    /// <param name="wasDraw">True if the match was a draw.</param>
    /// <param name="matchDuration">The duration of the match.</param>
    /// <param name="updatedAt">Update timestamp.</param>
    public void RecordMatch(bool character1Won, bool wasDraw, TimeSpan matchDuration, DateTime updatedAt)
    {
        TotalMatches++;

        if (wasDraw)
        {
            Draws++;
        }
        else if (character1Won)
        {
            Character1Wins++;
        }
        else
        {
            Character2Wins++;
        }

        // Update average duration using running average formula
        if (TotalMatches == 1)
        {
            AverageMatchDuration = matchDuration;
        }
        else
        {
            var totalDuration = AverageMatchDuration * (TotalMatches - 1) + matchDuration;
            AverageMatchDuration = totalDuration / TotalMatches;
        }

        LastUpdated = updatedAt;
    }

    [Obsolete("Use RecordMatch(bool, bool, TimeSpan, ITimeProvider) or RecordMatch(bool, bool, TimeSpan, DateTime) instead")]
    public void RecordMatch(bool character1Won, bool wasDraw, TimeSpan matchDuration)
    {
        RecordMatch(character1Won, wasDraw, matchDuration, SystemTimeProvider.Instance);
    }

    /// <summary>
    /// Gets the win rate for a specific character in this matchup.
    /// </summary>
    /// <param name="characterId">The character ID to get win rate for.</param>
    /// <returns>The win rate (0.0 to 1.0).</returns>
    public double GetWinRateForCharacter(Guid characterId)
    {
        if (characterId == Character1Id)
            return Character1WinRate;
        if (characterId == Character2Id)
            return Character2WinRate;

        throw new ArgumentException("Character is not part of this matchup", nameof(characterId));
    }

    // EF Core constructor
    private MugenMatchupStats() { }
}