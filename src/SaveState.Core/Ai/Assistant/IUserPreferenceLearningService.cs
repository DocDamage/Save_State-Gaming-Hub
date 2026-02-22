using SaveState.Core.Common;

namespace SaveState.Core.Ai.Assistant;

/// <summary>
/// Learns user preferences over time to personalize assistant behavior.
/// </summary>
public interface IUserPreferenceLearningService
{
    /// <summary>
    /// Records user feedback on a suggestion.
    /// </summary>
    Task<Result> RecordSuggestionFeedbackAsync(
        SuggestionFeedback feedback,
        CancellationToken ct = default);

    /// <summary>
    /// Records a user action following a suggestion.
    /// </summary>
    Task<Result> RecordUserActionAsync(
        Guid sessionId,
        UserActionType action,
        CancellationToken ct = default);

    /// <summary>
    /// Gets learned preferences for a user.
    /// </summary>
    Task<Result<UserPreferences>> GetUserPreferencesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Updates preference weights based on historical data.
    /// </summary>
    Task<Result> UpdatePreferencesAsync(CancellationToken ct = default);
}

/// <summary>
/// Feedback on a suggestion.
/// </summary>
public sealed record SuggestionFeedback(
    Guid SessionId,
    SuggestionType SuggestionType,
    bool WasHelpful,
    string? UserComment,
    DateTime TimestampUtc);

/// <summary>
/// Types of suggestions.
/// </summary>
public enum SuggestionType
{
    DifficultyAdjustment,
    BreakReminder,
    SmartPause,
    CoachingTip
}

/// <summary>
/// User action types.
/// </summary>
public enum UserActionType
{
    AcceptedSuggestion,
    IgnoredSuggestion,
    DismissedSuggestion,
    ModifiedSuggestion
}

/// <summary>
/// Learned user preferences.
/// </summary>
public sealed record UserPreferences(
    float BreakReminderFrequency, // 0.0 = rarely, 1.0 = frequently
    float DifficultySuggestionThreshold, // Minimum confidence before suggesting
    float CoachingTipFrequency,
    bool PrefersSpoilerFreeHints,
    bool AutoAcceptHighConfidenceSuggestions,
    TimeSpan PreferredSessionDuration,
    IReadOnlyList<string> PreferredGameGenres,
    DateTime LastUpdatedAtUtc);
