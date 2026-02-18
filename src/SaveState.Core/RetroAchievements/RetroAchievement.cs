using SaveState.Core.Common.Base;

namespace SaveState.Core.RetroAchievements;

/// <summary>
/// Represents a RetroAchievements achievement for a game.
/// </summary>
public class RetroAchievement : EntityBase
{
    /// <summary>
    /// RetroAchievements ID for this achievement.
    /// </summary>
    public int RetroId { get; set; }
    
    /// <summary>
    /// Game ID in RetroAchievements system.
    /// </summary>
    public int GameId { get; set; }
    
    /// <summary>
    /// Title of the achievement.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of how to unlock.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Point value (typically 1-50 points).
    /// </summary>
    public int Points { get; set; }
    
    /// <summary>
    /// Type of achievement (win, progression, etc.).
    /// </summary>
    public RetroAchievementType Type { get; set; }
    
    /// <summary>
    /// URL to achievement badge image.
    /// </summary>
    public string BadgeUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of players who have earned this.
    /// </summary>
    public int EarnedCount { get; set; }
    
    /// <summary>
    /// Percentage of players who have earned this.
    /// </summary>
    public decimal EarnedRate { get; set; }
    
    /// <summary>
    /// When this achievement was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this was last synced from RetroAchievements.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }
}

/// <summary>
/// Types of RetroAchievements achievements.
/// </summary>
public enum RetroAchievementType
{
    /// <summary>
    /// Standard achievement.
    /// </summary>
    Standard,
    
    /// <summary>
    /// Progression achievement (story-related).
    /// </summary>
    Progression,
    
    /// <summary>
    /// Win condition achievement.
    /// </summary>
    WinCondition,
    
    /// <summary>
    /// Missable achievement.
    /// </summary>
    Missable
}

/// <summary>
/// Represents a user's progress on a specific achievement.
/// </summary>
public class UserRetroAchievementProgress : EntityBase
{
    /// <summary>
    /// User ID.
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Achievement ID.
    /// </summary>
    public int AchievementId { get; set; }
    
    /// <summary>
    /// Whether the achievement is unlocked.
    /// </summary>
    public bool IsUnlocked { get; set; }
    
    /// <summary>
    /// When it was unlocked (if applicable).
    /// </summary>
    public DateTime? UnlockedAt { get; set; }
    
    /// <summary>
    /// Hardcore mode unlock (no save states).
    /// </summary>
    public bool IsHardcore { get; set; }
    
    /// <summary>
    /// Current progress value (for progressive achievements).
    /// </summary>
    public int CurrentProgress { get; set; }
    
    /// <summary>
    /// Target value for unlock.
    /// </summary>
    public int TargetProgress { get; set; }
    
    /// <summary>
    /// When this progress was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// Game information from RetroAchievements.
/// </summary>
public class RetroGameInfo
{
    /// <summary>
    /// RetroAchievements game ID.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Title of the game.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Console ID.
    /// </summary>
    public int ConsoleId { get; set; }
    
    /// <summary>
    /// Console name (e.g., "SNES", "Genesis").
    /// </summary>
    public string ConsoleName { get; set; } = string.Empty;
    
    /// <summary>
    /// URL to game icon.
    /// </summary>
    public string IconUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Total achievements count.
    /// </summary>
    public int AchievementCount { get; set; }
    
    /// <summary>
    /// Total possible points.
    /// </summary>
    public int TotalPoints { get; set; }
    
    /// <summary>
    /// Number of players who have mastered the game.
    /// </summary>
    public int PlayersMastered { get; set; }
}

/// <summary>
/// User summary from RetroAchievements.
/// </summary>
public class RetroUserSummary
{
    /// <summary>
    /// Username on RetroAchievements.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Total achievements earned.
    /// </summary>
    public int TotalAchievements { get; set; }
    
    /// <summary>
    /// Total points earned.
    /// </summary>
    public int TotalPoints { get; set; }
    
    /// <summary>
    /// Site rank.
    /// </summary>
    public int Rank { get; set; }
    
    /// <summary>
    /// User motto/tagline.
    /// </summary>
    public string? Motto { get; set; }
    
    /// <summary>
    /// URL to user avatar.
    /// </summary>
    public string AvatarUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Recently played games.
    /// </summary>
    public List<RetroRecentlyPlayedGame> RecentlyPlayed { get; set; } = new();
}

/// <summary>
/// Recently played game entry.
/// </summary>
public class RetroRecentlyPlayedGame
{
    /// <summary>
    /// Game ID.
    /// </summary>
    public int GameId { get; set; }
    
    /// <summary>
    /// Game title.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Console name.
    /// </summary>
    public string ConsoleName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last played timestamp.
    /// </summary>
    public DateTime LastPlayed { get; set; }
    
    /// <summary>
    /// Achievements earned in this session.
    /// </summary>
    public int AchievementsEarned { get; set; }
}

/// <summary>
/// Achievement unlock event.
/// </summary>
public class AchievementUnlockEvent
{
    /// <summary>
    /// Achievement ID.
    /// </summary>
    public int AchievementId { get; set; }
    
    /// <summary>
    /// When it was unlocked.
    /// </summary>
    public DateTime UnlockedAt { get; set; }
    
    /// <summary>
    /// Whether it was hardcore mode.
    /// </summary>
    public bool IsHardcore { get; set; }
}
