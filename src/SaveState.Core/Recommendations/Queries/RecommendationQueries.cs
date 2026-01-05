using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Recommendations.DTOs;

namespace SaveState.Core.Recommendations.Queries;

/// <summary>
/// Query to get personalized game recommendations for a user.
/// </summary>
/// <param name="UserId">The user ID to get recommendations for.</param>
/// <param name="Count">Number of recommendations to return.</param>
public record GetGameRecommendationsQuery(
    Guid UserId,
    int Count = 10) : IRequest<Result<IReadOnlyList<SmartGameRecommendation>>>;

/// <summary>
/// Query to get games similar to a specific game.
/// </summary>
/// <param name="GameId">The game to find similar games for.</param>
/// <param name="Count">Number of similar games to return.</param>
public record GetSimilarGamesQuery(
    Guid GameId,
    int Count = 5) : IRequest<Result<IReadOnlyList<SmartSimilarGame>>>;

/// <summary>
/// Query to get trending games based on recent activity.
/// </summary>
/// <param name="Count">Number of trending games to return.</param>
public record GetTrendingGamesQuery(
    int Count = 10) : IRequest<Result<IReadOnlyList<SmartTrendingGame>>>;

/// <summary>
/// Query to get recommended games from user's backlog.
/// </summary>
/// <param name="UserId">The user ID.</param>
/// <param name="Count">Number of backlog recommendations.</param>
public record GetBacklogRecommendationsQuery(
    Guid UserId,
    int Count = 5) : IRequest<Result<IReadOnlyList<SmartBacklogRecommendation>>>;

