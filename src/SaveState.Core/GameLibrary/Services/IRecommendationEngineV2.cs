using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.Recommendations;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Advanced recommendation engine that provides hybrid recommendations
/// based on time, mood, social factors, and user preferences.
/// </summary>
public interface IRecommendationEngineV2
{
    /// <summary>
    /// Gets personalized game recommendations based on the provided context.
    /// </summary>
    /// <param name="context">The recommendation context including time, mood, and preferences.</param>
    /// <param name="count">The maximum number of recommendations to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of game recommendations.</returns>
    Task<Result<IReadOnlyList<GameRecommendation>>> GetRecommendationsAsync(
        RecommendationContext context,
        int count = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Gets recommendations for what to play next after finishing games.
    /// </summary>
    /// <param name="context">The context including recently finished games and available time.</param>
    /// <param name="count">The maximum number of recommendations to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of game recommendations.</returns>
    Task<Result<IReadOnlyList<GameRecommendation>>> GetPlayNextAsync(
        PlayNextContext context,
        int count = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Gets recommendations based on social factors (friends playing, recommendations).
    /// </summary>
    /// <param name="context">The social context including friend activity.</param>
    /// <param name="count">The maximum number of recommendations to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of game recommendations.</returns>
    Task<Result<IReadOnlyList<GameRecommendation>>> GetSocialRecommendationsAsync(
        SocialRecommendationContext context,
        int count = 5,
        CancellationToken ct = default);

    /// <summary>
    /// Gets currently trending games in the community.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of trending game recommendations.</returns>
    Task<Result<IReadOnlyList<GameRecommendation>>> GetTrendingAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets hidden gem recommendations (high-rated but lesser-known games).
    /// </summary>
    /// <param name="count">The maximum number of recommendations to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of hidden gem recommendations.</returns>
    Task<Result<IReadOnlyList<GameRecommendation>>> GetHiddenGemsAsync(
        int count = 10,
        CancellationToken ct = default);
}
