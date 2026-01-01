namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service interface for AI-powered match prediction.
/// Uses character attributes, historical data, and AI analysis to predict match outcomes.
/// </summary>
public interface IMatchPredictionEngine
{
    /// <summary>
    /// Predicts match outcome based on character attributes, historical data, and AI analysis.
    /// </summary>
    /// <param name="character1">First character entity.</param>
    /// <param name="character2">Second character entity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The match prediction.</returns>
    Task<Result<MatchPrediction>> PredictMatchAsync(
        MugenCharacter character1,
        MugenCharacter character2,
        CancellationToken ct = default);

    /// <summary>
    /// Trains the prediction model with actual match results.
    /// </summary>
    /// <param name="actualResult">The actual match result to learn from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the training operation.</returns>
    Task<Result> TrainWithResultAsync(
        MugenMatchHistory actualResult,
        CancellationToken ct = default);
}