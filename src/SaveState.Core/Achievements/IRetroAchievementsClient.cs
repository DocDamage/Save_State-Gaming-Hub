namespace SaveState.Core.Achievements;
using SaveState.Core.Common;

/// <summary>
/// Client for RetroAchievements.org API integration.
/// </summary>
public interface IRetroAchievementsClient
{
    /// <summary>
    /// Gets whether the client is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Authenticates with RetroAchievements using username and API key.
    /// </summary>
    Task<bool> AuthenticateAsync(string username, string apiKey, CancellationToken ct = default);

    /// <summary>
    /// Gets the authenticated user's profile.
    /// </summary>
    Task<Result<RAUserProfile>> GetUserProfileAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches for a game by ROM hash.
    /// </summary>
    Task<Result<RAGameInfo>> GetGameByHashAsync(string romHash, CancellationToken ct = default);

    /// <summary>
    /// Gets game information by RetroAchievements game ID.
    /// </summary>
    Task<Result<RAGameInfo>> GetGameInfoAsync(int gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets achievements for a game.
    /// </summary>
    Task<Result<IReadOnlyList<RAAchievement>>> GetGameAchievementsAsync(int gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets the user's progress on a specific game.
    /// </summary>
    Task<Result<RAGameProgress>> GetUserGameProgressAsync(int gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets the user's recently earned achievements.
    /// </summary>
    Task<Result<IReadOnlyList<RAEarnedAchievement>>> GetRecentAchievementsAsync(int count = 50, CancellationToken ct = default);
}

/// <summary>
/// RetroAchievements user profile.
/// </summary>
public sealed record RAUserProfile(
    string Username,
    int TotalPoints,
    int TotalTruePoints,
    int Rank,
    int TotalGamesPlayed,
    string? AvatarUrl);

/// <summary>
/// RetroAchievements game information.
/// </summary>
public sealed record RAGameInfo(
    int Id,
    string Title,
    string ConsoleName,
    int ConsoleId,
    string? ImageIcon,
    int TotalAchievements,
    int TotalPoints);

/// <summary>
/// RetroAchievements achievement definition.
/// </summary>
public sealed record RAAchievement(
    int Id,
    string Title,
    string Description,
    int Points,
    string? BadgeUrl,
    bool IsHardcore,
    int NumAwarded,
    float RarityPercent);

/// <summary>
/// User's progress on a specific game.
/// </summary>
public sealed record RAGameProgress(
    int GameId,
    int AchievementsEarned,
    int AchievementsTotal,
    int PointsEarned,
    int PointsTotal,
    float CompletionPercentage,
    IReadOnlyList<int> EarnedAchievementIds);

/// <summary>
/// An achievement earned by the user.
/// </summary>
public sealed record RAEarnedAchievement(
    int AchievementId,
    string Title,
    string GameTitle,
    int Points,
    DateTime EarnedAt,
    bool IsHardcore);
