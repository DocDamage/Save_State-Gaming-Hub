namespace SaveState.Core.GameLibrary.Models.AiCoach;

/// <summary>
/// Defines the style of coaching to be used in a session.
/// </summary>
public enum CoachingStyle
{
    Supportive,
    Challenging,
    Analytical,
    Motivational
}

/// <summary>
/// Represents the skill level of a player.
/// </summary>
public enum SkillLevel
{
    Beginner,
    Intermediate,
    Advanced,
    Expert,
    Master
}

/// <summary>
/// Defines areas of focus for coaching sessions.
/// </summary>
public enum CoachingFocus
{
    Strategy,
    Technique,
    Mindset,
    Physical,
    Adaptation
}

/// <summary>
/// Represents the current phase of a coaching session.
/// </summary>
public enum CoachingPhase
{
    Assessment,
    ActiveCoaching,
    Analysis,
    WrapUp
}

/// <summary>
/// Defines the type of feedback provided to the player.
/// </summary>
public enum FeedbackType
{
    Positive,
    Constructive,
    Warning,
    Encouragement,
    Analysis
}

/// <summary>
/// Represents the priority level of coaching feedback.
/// </summary>
public enum FeedbackPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Defines the rating scale for strategy analysis.
/// </summary>
public enum StrategyRating
{
    Poor,
    BelowAverage,
    Average,
    Good,
    Excellent,
    Masterful
}

/// <summary>
/// Represents the outcome of a player action.
/// </summary>
public enum ActionOutcome
{
    Success,
    Failure,
    Partial,
    Neutral
}

/// <summary>
/// Defines the type of opponent behavior.
/// </summary>
public enum OpponentType
{
    Aggressive,
    Defensive,
    Technical,
    Adaptive,
    Random
}

/// <summary>
/// Represents the skill level of an opponent.
/// </summary>
public enum OpponentSkillLevel
{
    Low,
    Medium,
    High,
    Expert
}

/// <summary>
/// Defines different skill areas for assessment.
/// </summary>
public enum SkillArea
{
    DecisionMaking,
    Execution,
    Adaptation,
    Strategy,
    Awareness
}

/// <summary>
/// Represents the rating for a specific skill area.
/// </summary>
public enum SkillRating
{
    Developing,
    Competent,
    Proficient,
    Expert,
    Master
}

/// <summary>
/// Defines categories for coaching tips.
/// </summary>
public enum TipCategory
{
    Strategy,
    Technique,
    Mindset,
    Health,
    Equipment
}

/// <summary>
/// Represents the difficulty level of a coaching tip.
/// </summary>
public enum TipDifficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}

/// <summary>
/// Defines the general difficulty level for exercises and challenges.
/// </summary>
public enum Difficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}

/// <summary>
/// Defines the type of analysis to perform on gameplay.
/// </summary>
public enum AnalysisType
{
    Strategy,
    Performance,
    OpponentBehavior,
    SkillAssessment,
    PatternDetection
}
