using SaveState.Core.Common;

namespace SaveState.Core.RetroAchievements.Services;

/// <summary>
/// Service for interacting with RetroAchievements.org API.
/// </summary>
public interface IRetroAchievementsService
{
    /// <summary>
    /// Validates API credentials.
    /// </summary>
    Task<Result<bool>> ValidateCredentialsAsync(string username, string apiKey, CancellationToken ct = default);
    
    /// <summary>
    /// Gets user summary and stats.
    /// </summary>
    Task<Result<RetroUserSummary>> GetUserSummaryAsync(string username, CancellationToken ct = default);
    
    /// <summary>
    /// Gets game information and achievement list.
    /// </summary>
    Task<Result<RetroGameInfo>> GetGameInfoAsync(int gameId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all achievements for a game.
    /// </summary>
    Task<Result<List<RetroAchievement>>> GetGameAchievementsAsync(int gameId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets user's progress for a specific game.
    /// </summary>
    Task<Result<List<UserRetroAchievementProgress>>> GetUserGameProgressAsync(
        string username, int gameId, CancellationToken ct = default);
    
    /// <summary>
    /// Searches for games by title.
    /// </summary>
    Task<Result<List<RetroGameInfo>>> SearchGamesAsync(string query, int? consoleId = null, CancellationToken ct = default);
    
    /// <summary>
    /// Gets recently unlocked achievements for a user.
    /// </summary>
    Task<Result<List<AchievementUnlockEvent>>> GetRecentUnlocksAsync(
        string username, int count = 10, CancellationToken ct = default);
    
    /// <summary>
    /// Starts rich presence monitoring for a game.
    /// </summary>
    Task<Result> StartRichPresenceAsync(int gameId, CancellationToken ct = default);
    
    /// <summary>
    /// Stops rich presence monitoring.
    /// </summary>
    Task<Result> StopRichPresenceAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets leaderboard entries for a game.
    /// </summary>
    Task<Result<List<RetroLeaderboardEntry>>> GetLeaderboardAsync(
        int gameId, int leaderboardId, CancellationToken ct = default);
    
    /// <summary>
    /// Awards an achievement (called when triggered by emulator integration).
    /// </summary>
    Task<Result> AwardAchievementAsync(
        string username, int achievementId, int? hash = null, CancellationToken ct = default);
    
    /// <summary>
    /// Gets completion progress for all games for a user.
    /// </summary>
    Task<Result<List<GameCompletionStatus>>> GetUserCompletionProgressAsync(
        string username, CancellationToken ct = default);
    
    /// <summary>
    /// Event raised when an achievement is unlocked during gameplay.
    /// </summary>
    event EventHandler<AchievementUnlockedEventArgs>? AchievementUnlocked;
    
    /// <summary>
    /// Event raised when progress is updated.
    /// </summary>
    event EventHandler<ProgressUpdatedEventArgs>? ProgressUpdated;
}

/// <summary>
/// Event args for achievement unlock.
/// </summary>
public class AchievementUnlockedEventArgs : EventArgs
{
    public int AchievementId { get; set; }
    public string AchievementTitle { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public int Points { get; set; }
    public string BadgeUrl { get; set; } = string.Empty;
    public bool IsHardcore { get; set; }
}

/// <summary>
/// Event args for progress update.
/// </summary>
public class ProgressUpdatedEventArgs : EventArgs
{
    public int AchievementId { get; set; }
    public int CurrentProgress { get; set; }
    public int TargetProgress { get; set; }
    public decimal PercentComplete => TargetProgress > 0 ? (decimal)CurrentProgress / TargetProgress * 100 : 0;
}

/// <summary>
/// Leaderboard entry from RetroAchievements.
/// </summary>
public class RetroLeaderboardEntry
{
    public string Username { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Rank { get; set; }
    public DateTime DateAchieved { get; set; }
}

/// <summary>
/// Game completion status.
/// </summary>
public class GameCompletionStatus
{
    public int GameId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ConsoleName { get; set; } = string.Empty;
    public int AchievementsEarned { get; set; }
    public int TotalAchievements { get; set; }
    public int PointsEarned { get; set; }
    public int TotalPoints { get; set; }
    public decimal CompletionPercentage { get; set; }
    public bool IsMastered => AchievementsEarned == TotalAchievements && TotalAchievements > 0;
    public DateTime? LastPlayed { get; set; }
}
