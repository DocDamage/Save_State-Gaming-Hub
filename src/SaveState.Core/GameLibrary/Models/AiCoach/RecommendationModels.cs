namespace SaveState.Core.GameLibrary.Models.AiCoach;

/// <summary>
/// A personalized recommendation for player improvement.
/// </summary>
public sealed record Recommendation(
    Guid Id,
    string Title,
    string Description,
    RecommendationPriority Priority,
    RecommendationCategory Category,
    IReadOnlyList<string> Prerequisites,
    TimeSpan EstimatedTimeToComplete,
    DateTime CreatedAt);

/// <summary>
/// A specific action the player can take to improve.
/// </summary>
public sealed record SuggestedAction(
    string Action,
    string Context,
    double ExpectedImpact,
    IReadOnlyList<string> Steps);

/// <summary>
/// Priority level for recommendations.
/// </summary>
public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Category for recommendations.
/// </summary>
public enum RecommendationCategory
{
    Strategy,
    Technique,
    Practice,
    Study,
    Mindset,
    Equipment
}

/// <summary>
/// A goal for player improvement.
/// </summary>
public sealed record ImprovementGoal(
    string Title,
    string Description,
    bool Achieved,
    double Progress);

/// <summary>
/// A training exercise for skill development.
/// </summary>
public sealed record TrainingExercise(
    string Name,
    string Description,
    TimeSpan Duration,
    Difficulty Difficulty);

/// <summary>
/// Complete improvement plan for a player.
/// </summary>
public sealed record ImprovementPlan(
    Guid SessionId,
    DateTime GeneratedAt,
    TimeSpan EstimatedDuration,
    IReadOnlyList<ImprovementGoal> Goals,
    IReadOnlyList<TrainingExercise> Exercises,
    IReadOnlyList<Milestone> Milestones);
