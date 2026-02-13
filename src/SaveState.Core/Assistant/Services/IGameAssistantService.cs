using SaveState.Core.Common;

namespace SaveState.Core.Assistant.Services;

public interface IGameAssistantService
{
    Task<Result<AssistantRecommendation>> AnalyzeSessionAsync(
        SessionContext context,
        CancellationToken ct = default);

    Task<Result> EnableSmartPauseAsync(
        SmartPauseOptions options,
        CancellationToken ct = default);

    Task<Result<DifficultySuggestion>> AnalyzeDifficultyAsync(
        Guid gameId,
        GameplayMetrics metrics,
        CancellationToken ct = default);

    Task<Result<AssistantResponse>> AskAsync(
        Guid gameId,
        string question,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<string>>> GetQuickTipsAsync(
        Guid gameId,
        CancellationToken ct = default);

    Task<Result<string>> GetWalkthroughHintAsync(
        Guid gameId,
        string currentLocation,
        bool avoidSpoilers = true,
        CancellationToken ct = default);

    Task<Result> ClearContextAsync(Guid gameId, CancellationToken ct = default);
}

public sealed record SessionContext(
    Guid GameId,
    DateTime SessionStartTimeUtc,
    int RecentDeaths,
    int RecentRetries,
    int BreaksTaken,
    InputPattern InputPattern,
    int? LookAwayDurationSeconds = null);

public sealed record SmartPauseOptions(
    bool Enabled,
    int LookAwayThresholdSeconds,
    bool ResumeOnGazeReturn,
    bool RequireEyeTracking);

public sealed record GameplayMetrics(
    int DeathCount,
    TimeSpan TimeInCurrentSection,
    int RetryCount,
    InputPattern InputPattern,
    DateTime SessionStartTimeUtc);

public sealed record InputPattern(
    int ActionsPerMinute,
    float ErrorRate,
    bool HasRapidInputBursts,
    bool HasIdleSpikes);

public sealed record DifficultySuggestion(
    SuggestedDifficulty Difficulty,
    float Confidence,
    string Reasoning,
    IReadOnlyList<string> SupportingMetrics);

public enum SuggestedDifficulty
{
    Decrease,
    Maintain,
    Increase
}

public sealed record AssistantRecommendation(
    AssistantRecommendationType Type,
    string Message,
    float Confidence,
    IReadOnlyList<string> SuggestedActions,
    DateTime GeneratedAtUtc,
    bool ShouldInterruptGameplay);

public enum AssistantRecommendationType
{
    None,
    SmartPause,
    BreakReminder,
    DifficultyAdjustment,
    CoachingTip
}

public sealed record AssistantResponse(
    string Answer,
    IReadOnlyList<string> Sources,
    bool ContainsSpoilers,
    IReadOnlyList<string> RelatedQuestions,
    float Confidence);
