namespace SaveState.Core.Recommendations.DTOs;

/// <summary>
/// Represents a smart game recommendation with confidence score and reasoning.
/// </summary>
/// <param name="GameId">The ID of the recommended game.</param>
/// <param name="GameTitle">The title of the game.</param>
/// <param name="ConfidenceScore">Confidence score (0-100) indicating how confident the recommendation is.</param>
/// <param name="Reason">Human-readable explanation for why this game is recommended.</param>
/// <param name="Genres">List of genres for this game.</param>
/// <param name="Tags">List of tags for this game.</param>
/// <param name="EstimatedPlayTime">Estimated time to complete (if available).</param>
public record SmartGameRecommendation(
    Guid GameId,
    string GameTitle,
    float ConfidenceScore,
    string Reason,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Tags,
    TimeSpan? EstimatedPlayTime);

/// <summary>
/// Represents a game similar to another game.
/// </summary>
/// <param name="GameId">The ID of the similar game.</param>
/// <param name="GameTitle">The title of the game.</param>
/// <param name="SimilarityScore">Similarity score (0-100).</param>
/// <param name="SharedGenres">Genres shared with the source game.</param>
/// <param name="SharedTags">Tags shared with the source game.</param>
public record SmartSimilarGame(
    Guid GameId,
    string GameTitle,
    float SimilarityScore,
    IReadOnlyList<string> SharedGenres,
    IReadOnlyList<string> SharedTags);

/// <summary>
/// Represents a trending game based on recent activity.
/// </summary>
/// <param name="GameId">The ID of the trending game.</param>
/// <param name="GameTitle">The title of the game.</param>
/// <param name="TrendScore">Trend score based on recent play activity.</param>
/// <param name="ActivePlayers">Number of active players in the last 7 days.</param>
/// <param name="AverageRating">Average user rating.</param>
public record SmartTrendingGame(
    Guid GameId,
    string GameTitle,
    float TrendScore,
    int ActivePlayers,
    float? AverageRating);

/// <summary>
/// Represents a backlog game recommendation.
/// </summary>
/// <param name="GameId">The ID of the backlog game.</param>
/// <param name="GameTitle">The title of the game.</param>
/// <param name="Priority">Priority score (0-100).</param>
/// <param name="Reason">Why this game should be played next.</param>
/// <param name="EstimatedTimeToComplete">Estimated time to complete.</param>
/// <param name="AddedToBacklogDate">When the game was added to backlog.</param>
public record SmartBacklogRecommendation(
    Guid GameId,
    string GameTitle,
    float Priority,
    string Reason,
    TimeSpan? EstimatedTimeToComplete,
    DateTime AddedToBacklogDate);

