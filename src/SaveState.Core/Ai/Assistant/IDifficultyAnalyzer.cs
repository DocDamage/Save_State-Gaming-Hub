using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SuggestedDifficulty = SaveState.Core.Assistant.Services.SuggestedDifficulty;

namespace SaveState.Core.AI.Assistant;

/// <summary>
/// Analyzes gameplay metrics using ML models to provide difficulty suggestions.
/// </summary>
public interface IDifficultyAnalyzer
{
    /// <summary>
    /// Analyzes gameplay metrics and predicts optimal difficulty adjustment.
    /// </summary>
    Task<Result<DifficultyAnalysisResult>> AnalyzeAsync(
        PlayerBehaviorMetrics metrics,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the confidence level of the ML model.
    /// </summary>
    Task<Result<ModelMetrics>> GetModelMetricsAsync(CancellationToken ct = default);

    /// <summary>
    /// Records feedback on a difficulty suggestion for model improvement.
    /// </summary>
    Task<Result> RecordFeedbackAsync(
        Guid sessionId,
        bool suggestionWasHelpful,
        CancellationToken ct = default);
}

/// <summary>
/// Result of difficulty analysis.
/// </summary>
public sealed record DifficultyAnalysisResult(
    Core.Assistant.Services.SuggestedDifficulty SuggestedDifficulty,
    float Confidence,
    string Reasoning,
    IReadOnlyList<string> ContributingFactors,
    float FrustrationProbability,
    float MasteryProbability);

/// <summary>
/// Player behavior metrics for ML analysis.
/// </summary>
public sealed record PlayerBehaviorMetrics
{
    public required Guid SessionId { get; init; }
    public required Guid GameId { get; init; }
    public required DateTime SessionStartTimeUtc { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required int DeathCount { get; init; }
    public required int RetryCount { get; init; }
    public required TimeSpan TimeInCurrentSection { get; init; }
    public required TimeSpan TotalSessionDuration { get; init; }
    public required float ActionsPerMinute { get; init; }
    public required float InputErrorRate { get; init; }
    public required bool HasRapidInputBursts { get; init; }
    public required bool HasIdleSpikes { get; init; }
    public required int PauseCount { get; init; }
    public required TimeSpan TotalPausedTime { get; init; }
    public int? CurrentDifficultyLevel { get; init; }
}

/// <summary>
/// ML model performance metrics.
/// </summary>
public sealed record ModelMetrics(
    float Accuracy,
    float Precision,
    float Recall,
    float F1Score,
    int TrainingSampleCount,
    DateTime LastTrainedAtUtc,
    string ModelVersion);

/// <summary>
/// Suggested difficulty adjustment.
/// </summary>
public enum SuggestedDifficulty
{
    DecreaseSignificantly,
    DecreaseSlightly,
    Maintain,
    IncreaseSlightly,
    IncreaseSignificantly
}
