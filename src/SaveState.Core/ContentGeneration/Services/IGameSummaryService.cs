using SaveState.Core.Common;

namespace SaveState.Core.ContentGeneration.Services;

/// <summary>
/// Service for generating AI-powered summaries of gaming journeys and stats.
/// </summary>
public interface IGameSummaryService
{
    /// <summary>
    /// Generates a narrative summary of the player's journey in a game.
    /// </summary>
    /// <param name="gameId">ID of the game.</param>
    /// <param name="userId">ID of the user/player.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the journey summary or failure.</returns>
    Task<Result<GameJourneySummary>> GenerateJourneySummaryAsync(
        Guid gameId,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a narrative story from game statistics.
    /// </summary>
    /// <param name="stats">Game statistics to narrativize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated story text or failure.</returns>
    Task<Result<string>> GenerateStatsStoryAsync(
        GameStats stats,
        CancellationToken ct = default);
}

/// <summary>
/// A narrative summary of a player's journey through a game.
/// </summary>
public record GameJourneySummary
{
    public required string Narrative { get; init; }
    public required IReadOnlyList<Milestone> KeyMoments { get; init; }
    public required string PlaytimeSummary { get; init; }
    public required string AchievementSummary { get; init; }
    public required string? FunnyMoment { get; init; }
}

/// <summary>
/// Represents a significant moment in the player's journey.
/// </summary>
public record Milestone
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTime Date { get; init; }
    public required string Icon { get; init; }
}

/// <summary>
/// Statistics for a game session or overall gameplay.
/// </summary>
public record GameStats
{
    public required string GameTitle { get; init; }
    public required TimeSpan TotalPlaytime { get; init; }
    public required int SessionsCount { get; init; }
    public required int AchievementsUnlocked { get; init; }
    public required int TotalAchievements { get; init; }
    public required DateTime FirstPlayed { get; init; }
    public required DateTime LastPlayed { get; init; }
    public required IReadOnlyList<string> FavoriteActivities { get; init; }
}
