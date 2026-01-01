using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

public interface IAiCoachService
{
    Task<Result<CoachingSession>> StartCoachingSessionAsync(Guid gameId, CoachingPreferences preferences, CancellationToken ct = default);
    Task<Result> EndCoachingSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<Result<CoachingFeedback>> GetRealTimeFeedbackAsync(Guid sessionId, GameStateSnapshot gameState, CancellationToken ct = default);
    Task<Result<StrategyAnalysis>> AnalyzePlayerStrategyAsync(Guid sessionId, IReadOnlyList<GameAction> recentActions, CancellationToken ct = default);
    Task<Result<OpponentAnalysis>> AnalyzeOpponentPatternsAsync(Guid sessionId, IReadOnlyList<GameAction> opponentActions, CancellationToken ct = default);
    Task<Result<SkillAssessment>> AssessPlayerSkillAsync(Guid sessionId, PerformanceMetrics metrics, CancellationToken ct = default);
    Task<Result<ImprovementPlan>> GenerateImprovementPlanAsync(Guid sessionId, SkillAssessment assessment, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CoachingTip>>> GetContextualTipsAsync(Guid sessionId, string context, CancellationToken ct = default);
    Task<Result<CoachingReport>> GenerateSessionReportAsync(Guid sessionId, CancellationToken ct = default);
}

public sealed record CoachingPreferences(
    CoachingStyle Style,
    SkillLevel TargetSkillLevel,
    IReadOnlyList<CoachingFocus> FocusAreas,
    bool EnableRealTimeFeedback,
    bool EnableStrategyAnalysis,
    bool EnableOpponentAnalysis);

public sealed record CoachingSession(
    Guid Id,
    Guid GameId,
    CoachingPreferences Preferences,
    DateTime StartedAt,
    CoachingPhase CurrentPhase);

public sealed record GameStateSnapshot(
    DateTime Timestamp,
    string GameMode,
    int PlayerScore,
    int OpponentScore,
    TimeSpan GameTime,
    IReadOnlyDictionary<string, object> GameSpecificData);

public sealed record GameAction(
    DateTime Timestamp,
    string ActionType,
    IReadOnlyDictionary<string, object> ActionData,
    ActionOutcome Outcome);

public sealed record CoachingFeedback(
    FeedbackType Type,
    string Message,
    FeedbackPriority Priority,
    IReadOnlyList<string> Suggestions,
    IReadOnlyDictionary<string, object> ContextData);

public sealed record StrategyStrength(string Description, double Impact);
public sealed record StrategyWeakness(string Description, double Impact, string Improvement);
public sealed record StrategyRecommendation(string Action, string Rationale, int Priority);

public sealed record StrategyAnalysis(
    StrategyRating OverallRating,
    IReadOnlyList<StrategyStrength> Strengths,
    IReadOnlyList<StrategyWeakness> Weaknesses,
    IReadOnlyList<StrategyRecommendation> Recommendations,
    string AnalysisSummary);

public sealed record OpponentPattern(string Pattern, string Description, double Frequency);
public sealed record CounterStrategy(string Strategy, string Description, double Effectiveness);

public sealed record OpponentAnalysis(
    OpponentType OpponentType,
    OpponentSkillLevel SkillLevel,
    IReadOnlyList<OpponentPattern> Patterns,
    IReadOnlyList<CounterStrategy> CounterStrategies,
    string AnalysisSummary);

public sealed record SkillMilestone(string Description, bool Achieved, DateTime? AchievedAt);

public sealed record SkillAssessment(
    SkillLevel CurrentLevel,
    SkillLevel PotentialLevel,
    IReadOnlyDictionary<SkillArea, SkillRating> SkillBreakdown,
    IReadOnlyList<SkillMilestone> Milestones,
    string AssessmentSummary);

public sealed record ImprovementGoal(string Title, string Description, bool Achieved, double Progress);
public sealed record TrainingExercise(string Name, string Description, TimeSpan Duration, Difficulty Difficulty);
public sealed record Milestone(string Title, string Description, DateTime TargetDate, bool Achieved);

public sealed record ImprovementPlan(
    Guid SessionId,
    DateTime GeneratedAt,
    TimeSpan EstimatedDuration,
    IReadOnlyList<ImprovementGoal> Goals,
    IReadOnlyList<TrainingExercise> Exercises,
    IReadOnlyList<Milestone> Milestones);

public sealed record CoachingTip(
    string Title,
    string Description,
    TipCategory Category,
    TipDifficulty Difficulty,
    IReadOnlyList<string> Prerequisites);

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

public enum CoachingStyle { Supportive, Challenging, Analytical, Motivational }
public enum SkillLevel { Beginner, Intermediate, Advanced, Expert, Master }
public enum CoachingFocus { Strategy, Technique, Mindset, Physical, Adaptation }
public enum CoachingPhase { Assessment, ActiveCoaching, Analysis, WrapUp }

public enum FeedbackType { Positive, Constructive, Warning, Encouragement, Analysis }
public enum FeedbackPriority { Low, Medium, High, Critical }

public enum StrategyRating { Poor, BelowAverage, Average, Good, Excellent, Masterful }
public enum ActionOutcome { Success, Failure, Partial, Neutral }

public enum OpponentType { Aggressive, Defensive, Technical, Adaptive, Random }
public enum OpponentSkillLevel { Low, Medium, High, Expert }

public enum SkillArea { DecisionMaking, Execution, Adaptation, Strategy, Awareness }
public enum SkillRating { Developing, Competent, Proficient, Expert, Master }

public enum TipCategory { Strategy, Technique, Mindset, Health, Equipment }
public enum TipDifficulty { Easy, Medium, Hard, Expert }

public enum Difficulty { Easy, Medium, Hard, Expert }