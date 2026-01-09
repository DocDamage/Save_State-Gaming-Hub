using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.Social.Entities;

/// <summary>
/// Represents a community challenge for players to participate in.
/// </summary>
public class Challenge : EntityBase, ISoftDelete
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ChallengeType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ICollection<ChallengeRequirement> Requirements { get; private set; } = new List<ChallengeRequirement>();
    public ICollection<ChallengeParticipant> Participants { get; private set; } = new List<ChallengeParticipant>();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    private Challenge() { } // EF Core

    public static Challenge Create(string name, string description, ChallengeType type, DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate) throw new ArgumentException("End date must be after start date.");

        return new Challenge
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Type = type,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    public void AddRequirement(ChallengeRequirement requirement)
    {
        Requirements.Add(requirement);
    }
}

public enum ChallengeType
{
    Daily,
    Weekly,
    Monthly,
    Custom,
    Event,
    Playtime,
    Achievement,
    HighScore,
    Speedrun
}

public class ChallengeRequirement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty; // e.g. "KillCount", "TimePlayed", "Score"
    public double TargetValue { get; set; }
    public string TargetMetric { get; set; } = string.Empty; // For stat-based challenges
}

public class ChallengeParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public double Progress { get; set; }
    public double CurrentProgress { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
