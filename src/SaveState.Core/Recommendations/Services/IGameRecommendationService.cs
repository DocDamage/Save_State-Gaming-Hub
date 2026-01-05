using SaveState.Core.Common;
using SaveState.Core.Recommendations.DTOs;

namespace SaveState.Core.Recommendations.Services;

/// <summary>
/// Service for generating personalized game recommendations based on user preferences and play patterns.
/// </summary>
public interface IGameRecommendationService
{
    /// <summary>
    /// Gets personalized game recommendations for the user.
    /// </summary>
    /// <param name="userId">The user ID to generate recommendations for.</param>
    /// <param name="count">Number of recommendations to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of game recommendations with confidence scores.</returns>
    Task<Result<IReadOnlyList<SmartGameRecommendation>>> GetRecommendationsAsync(
        Guid userId,
        int count = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Gets games similar to the specified game based on tags, genres, and metadata.
    /// </summary>
    /// <param name="gameId">The game to find similar games for.</param>
    /// <param name="count">Number of similar games to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of similar games with similarity scores.</returns>
    Task<Result<IReadOnlyList<SmartSimilarGame>>> GetSimilarGamesAsync(
        Guid gameId,
        int count = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Gets trending games based on recent play activity across all users.
    /// </summary>
    /// <param name="count">Number of trending games to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of trending games.</returns>
    Task<Result<IReadOnlyList<SmartTrendingGame>>> GetTrendingGamesAsync(
        int count = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Gets games from the user's backlog that are recommended to play next.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="count">Number of recommendations.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of backlog recommendations with reasons.</returns>
    Task<Result<IReadOnlyList<SmartBacklogRecommendation>>> GetBacklogRecommendationsAsync(
        Guid userId,
        int count = 5,
        CancellationToken ct = default);
}

