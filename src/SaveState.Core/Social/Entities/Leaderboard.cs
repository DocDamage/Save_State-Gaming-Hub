using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;

namespace SaveState.Core.Social.Entities;

/// <summary>
/// Represents a leaderboard for tracking player rankings.
/// </summary>
public class Leaderboard : EntityBase, ISoftDelete
{
    public string Name { get; private set; } = string.Empty;
    public LeaderboardCategory Category { get; private set; }
    public LeaderboardScope Scope { get; private set; }
    public ICollection<LeaderboardRanking> Entries { get; private set; } = new List<LeaderboardRanking>();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    private Leaderboard() { } // EF Core

    public static Leaderboard Create(string name, LeaderboardCategory category, LeaderboardScope scope)
    {
        return new Leaderboard
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            Scope = scope
        };
    }

    public void AddEntry(LeaderboardRanking entry)
    {
        Entries.Add(entry);
    }
}

public enum LeaderboardCategory
{
    Global,
    GameSpecific,
    ChallengeSpecific,
    PlatformSpecific
}

public enum LeaderboardScope
{
    Global,
    Region,
    Friends,
    Local
}

public class LeaderboardRanking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeaderboardId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public double Score { get; set; }
    public int Rank { get; set; }
    public string? Metadata { get; set; }
    public DateTime LastUpdated { get; set; }
}
