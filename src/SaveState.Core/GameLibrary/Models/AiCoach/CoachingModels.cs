namespace SaveState.Core.GameLibrary.Models.AiCoach;

/// <summary>
/// Configuration preferences for an AI coaching session.
/// </summary>
public sealed record CoachingPreferences(
    CoachingStyle Style,
    SkillLevel TargetSkillLevel,
    IReadOnlyList<CoachingFocus> FocusAreas,
    bool EnableRealTimeFeedback,
    bool EnableStrategyAnalysis,
    bool EnableOpponentAnalysis);

/// <summary>
/// Represents an active AI coaching session.
/// </summary>
public sealed record CoachingSession(
    Guid Id,
    Guid GameId,
    CoachingPreferences Preferences,
    DateTime StartedAt,
    CoachingPhase CurrentPhase);

/// <summary>
/// Snapshot of the current game state for analysis.
/// </summary>
public sealed record GameStateSnapshot(
    DateTime Timestamp,
    string GameMode,
    int PlayerScore,
    int OpponentScore,
    TimeSpan GameTime,
    IReadOnlyDictionary<string, object> GameSpecificData);

/// <summary>
/// Represents a single action performed by a player.
/// </summary>
public sealed record GameAction(
    DateTime Timestamp,
    string ActionType,
    IReadOnlyDictionary<string, object> ActionData,
    ActionOutcome Outcome);

/// <summary>
/// Feedback provided by the AI coach in real-time.
/// </summary>
public sealed record CoachingFeedback(
    FeedbackType Type,
    string Message,
    FeedbackPriority Priority,
    IReadOnlyList<string> Suggestions,
    IReadOnlyDictionary<string, object> ContextData);

/// <summary>
/// A milestone within a coaching plan.
/// </summary>
public sealed record Milestone(
    string Title,
    string Description,
    DateTime TargetDate,
    bool Achieved);
