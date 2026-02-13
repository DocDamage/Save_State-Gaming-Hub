namespace SaveState.Core.GameLibrary.Models.AiCoach;

/// <summary>
/// A milestone achieved in skill development.
/// </summary>
public sealed record SkillMilestone(
    string Description,
    bool Achieved,
    DateTime? AchievedAt);

/// <summary>
/// Assessment of player skills.
/// </summary>
public sealed record SkillAssessment(
    SkillLevel CurrentLevel,
    SkillLevel PotentialLevel,
    IReadOnlyDictionary<SkillArea, SkillRating> SkillBreakdown,
    IReadOnlyList<SkillMilestone> Milestones,
    string AssessmentSummary);

/// <summary>
/// Performance metrics for a coaching session.
/// </summary>
public sealed record SessionMetrics(
    Guid SessionId,
    DateTime StartTime,
    TimeSpan Duration,
    int TipsGiven,
    int FeedbackProvided,
    int AnalysesCompleted,
    double PlayerEngagementScore);

/// <summary>
/// Comprehensive report for a completed coaching session.
/// </summary>
public sealed record CoachingReport(
    Guid SessionId,
    DateTime SessionStart,
    DateTime SessionEnd,
    TimeSpan Duration,
    IReadOnlyList<CoachingFeedback> FeedbackGiven,
    IReadOnlyList<StrategyAnalysis> StrategyAnalyses,
    IReadOnlyList<SkillAssessment> SkillAssessments,
    IReadOnlyList<ImprovementGoal> GoalsAchieved,
    string OverallAssessment,
    IReadOnlyList<string> Recommendations);
