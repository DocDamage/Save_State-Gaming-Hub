using SaveState.Core.Common;

namespace SaveState.Core.Recommendations.Services;

/// <summary>
/// Interface for A/B testing framework for recommendation algorithms.
/// </summary>
public interface IRecommendationExperimentService
{
    /// <summary>
    /// Gets the active experiment configuration for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Experiment configuration with algorithm weights.</returns>
    Task<Result<ExperimentConfig>> GetUserExperimentAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Records a recommendation interaction for experiment tracking.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="experimentId">The experiment ID.</param>
    /// <param name="gameId">The recommended game ID.</param>
    /// <param name="wasClicked">Whether the recommendation was clicked.</param>
    /// <param name="wasPlayed">Whether the game was played.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> RecordInteractionAsync(
        Guid userId,
        string experimentId,
        Guid gameId,
        bool wasClicked,
        bool wasPlayed,
        CancellationToken ct = default);

    /// <summary>
    /// Gets experiment results and metrics.
    /// </summary>
    /// <param name="experimentId">The experiment ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Experiment metrics including conversion rates.</returns>
    Task<Result<ExperimentResults>> GetExperimentResultsAsync(string experimentId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new A/B test experiment.
    /// </summary>
    /// <param name="name">Experiment name.</param>
    /// <param name="variants">List of algorithm weight variants to test.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<string>> CreateExperimentAsync(
        string name,
        List<AlgorithmWeights> variants,
        CancellationToken ct = default);
}

/// <summary>
/// Represents an experiment configuration.
/// </summary>
public record ExperimentConfig(
    string ExperimentId,
    string VariantId,
    AlgorithmWeights Weights);

/// <summary>
/// Represents algorithm weight configuration.
/// </summary>
public record AlgorithmWeights(
    float ContentWeight,
    float CollaborativeWeight,
    float PopularityWeight,
    float DeepLearningWeight,
    float DiversityBoost);

/// <summary>
/// Represents experiment results and metrics.
/// </summary>
public record ExperimentResults(
    string ExperimentId,
    Dictionary<string, VariantMetrics> VariantMetrics,
    DateTime StartedAt,
    int TotalUsers);

/// <summary>
/// Metrics for a specific experiment variant.
/// </summary>
public record VariantMetrics(
    string VariantId,
    int ImpressionCount,
    int ClickCount,
    int PlayCount,
    float ClickThroughRate,
    float PlayThroughRate,
    float ConfidenceInterval);
