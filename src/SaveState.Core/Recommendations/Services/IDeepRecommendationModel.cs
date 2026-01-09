using SaveState.Core.Common;

namespace SaveState.Core.Recommendations.Services;

/// <summary>
/// Interface for deep learning-based game recommendation model.
/// Provides neural network embeddings and predictions for games and users.
/// </summary>
public interface IDeepRecommendationModel
{
    /// <summary>
    /// Gets the embedding vector for a game.
    /// </summary>
    /// <param name="gameId">The game ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Embedding vector (typically 128-256 dimensions).</returns>
    Task<Result<float[]>> GetGameEmbeddingAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets the embedding vector for a user based on their play history.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Embedding vector (same dimensions as game embeddings).</returns>
    Task<Result<float[]>> GetUserEmbeddingAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Predicts the rating/affinity a user would have for a game.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="gameId">The game ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Predicted rating (0-100 scale).</returns>
    Task<Result<float>> PredictAffinityAsync(Guid userId, Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Updates the model with new user interaction data (online learning).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="gameId">The game ID.</param>
    /// <param name="interaction">Interaction type (played, liked, etc.).</param>
    /// <param name="value">Interaction value (playtime, rating, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> UpdateModelAsync(Guid userId, Guid gameId, string interaction, float value, CancellationToken ct = default);

    /// <summary>
    /// Trains or retrains the model with historical data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> TrainModelAsync(CancellationToken ct = default);
}
