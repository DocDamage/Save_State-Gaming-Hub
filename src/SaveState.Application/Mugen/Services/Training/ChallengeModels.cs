using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Application.Mugen.Services.Training;

/// <summary>
/// Training statistics data.
/// </summary>
public class TrainingStatistics
{
    public string UserId { get; set; } = default!;
    public TimeSpan Period { get; set; }
    public int TotalSessions { get; set; }
    public TimeSpan TotalTrainingTime { get; set; }
    public TimeSpan AverageSessionLength { get; set; }
    public int ReflexTrainingSessions { get; set; }
    public int PatternTrainingSessions { get; set; }
    public int ComboLabSessions { get; set; }
    public double OverallAccuracy { get; set; }
    public TimeSpan BestReactionTime { get; set; }
    public TimeSpan AverageReactionTime { get; set; }
    public IReadOnlyList<string> SkillsImproved { get; set; } = Array.Empty<string>();
    public double TrainingConsistency { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Training recommendations data.
/// </summary>
public class TrainingRecommendations
{
    public string UserId { get; set; } = default!;
    public IReadOnlyList<RecommendedTraining> RecommendedTrainings { get; set; } = Array.Empty<RecommendedTraining>();
    public IReadOnlyList<string> SkillGaps { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> NextMilestones { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PersonalizedTips { get; set; } = Array.Empty<string>();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Recommended training data.
/// </summary>
public class RecommendedTraining
{
    public TrainingType TrainingType { get; set; }
    public string Reason { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; }
    public double EstimatedBenefit { get; set; }
    public Priority Priority { get; set; }
}

/// <summary>
/// Challenge definition.
/// </summary>
public class Challenge
{
    public string ChallengeId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ChallengeDifficulty Difficulty { get; set; }
    public TrainingType TrainingType { get; set; }
    public ChallengeObjective Objective { get; set; } = default!;
    public ChallengeRewards Rewards { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public TimeSpan? TimeLimit { get; set; }
    public int? MaxAttempts { get; set; }
}

/// <summary>
/// Challenge objective.
/// </summary>
public class ChallengeObjective
{
    public string Type { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int TargetValue { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Challenge rewards.
/// </summary>
public class ChallengeRewards
{
    public int ExperiencePoints { get; set; }
    public IReadOnlyList<string> Unlockables { get; set; } = Array.Empty<string>();
    public string? AchievementId { get; set; }
}

/// <summary>
/// Challenge attempt tracking.
/// </summary>
public class ChallengeAttempt
{
    public string AttemptId { get; set; } = default!;
    public string ChallengeId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ChallengeAttemptStatus Status { get; set; } = ChallengeAttemptStatus.InProgress;
    public int CurrentScore { get; set; }
    public int ProgressPercentage { get; set; }
    public IReadOnlyList<AttemptEvent> Events { get; set; } = Array.Empty<AttemptEvent>();
    public TimeSpan ElapsedTime { get; set; }
}

/// <summary>
/// Challenge attempt status.
/// </summary>
public enum ChallengeAttemptStatus
{
    InProgress,
    Completed,
    Failed,
    Abandoned,
    Timeout
}

/// <summary>
/// Individual attempt event.
/// </summary>
public class AttemptEvent
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int? ScoreDelta { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
