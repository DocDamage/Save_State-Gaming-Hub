using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Core.Analytics.Services;

/// <summary>
/// Service for AI-powered game completion predictions.
/// </summary>
public interface ICompletionPredictionService
{
    /// <summary>
    /// Generates AI-powered completion time predictions for active games.
    /// </summary>
    /// <param name="count">Number of predictions to generate (default: 5)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of completion predictions with confidence scores</returns>
    Task<Result<IReadOnlyList<GameCompletionPrediction>>> GetPredictionsAsync(
        int count = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a completion prediction for a specific game.
    /// </summary>
    /// <param name="gameId">Game identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion prediction for the specified game</returns>
    Task<Result<GameCompletionPrediction>> GetPredictionForGameAsync(
        GameId gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates prediction accuracy based on actual completion data.
    /// </summary>
    /// <param name="gameId">Game that was completed</param>
    /// <param name="actualTimeToComplete">Actual time it took to complete</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result> RecordCompletionAsync(
        GameId gameId,
        TimeSpan actualTimeToComplete,
        CancellationToken ct = default);
}

/// <summary>
/// Represents an AI-powered game completion prediction.
/// </summary>
/// <param name="GameId">Game identifier</param>
/// <param name="GameName">Game title</param>
/// <param name="EstimatedTimeRemaining">Predicted time to completion</param>
/// <param name="ConfidenceScore">Confidence percentage (0-100)</param>
/// <param name="BasedOn">Data sources used for prediction</param>
/// <param name="ReasoningFactors">Key factors influencing the prediction</param>
public record GameCompletionPrediction(
    GameId GameId,
    string GameName,
    TimeSpan EstimatedTimeRemaining,
    double ConfidenceScore,
    string BasedOn,
    List<string> ReasoningFactors);
