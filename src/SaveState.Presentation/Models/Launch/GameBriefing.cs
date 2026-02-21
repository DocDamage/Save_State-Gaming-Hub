namespace SaveState.Presentation.Models.Launch;

/// <summary>
/// Represents an AI-generated briefing shown during game launch.
/// </summary>
public class GameBriefing
{
    /// <summary>
    /// The title of the game being launched.
    /// </summary>
    public string GameTitle { get; set; } = string.Empty;

    /// <summary>
    /// A catchy tagline or description for the game.
    /// </summary>
    public string Tagline { get; set; } = string.Empty;

    /// <summary>
    /// Summary of the last play session.
    /// </summary>
    public string LastSessionSummary { get; set; } = string.Empty;

    /// <summary>
    /// Current objective or recommended next step.
    /// </summary>
    public string CurrentObjective { get; set; } = string.Empty;

    /// <summary>
    /// Overall progress percentage for the game.
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// List of gameplay tips to display.
    /// </summary>
    public List<string> Tips { get; set; } = new();

    /// <summary>
    /// Recent achievements earned in the game.
    /// </summary>
    public List<RecentAchievement> RecentAchievements { get; set; } = new();

    /// <summary>
    /// Total playtime across all sessions.
    /// </summary>
    public TimeSpan TotalPlaytime { get; set; }

    /// <summary>
    /// Path to the game's cover art image.
    /// </summary>
    public string? CoverArtPath { get; set; }

    /// <summary>
    /// Path to the background image for the launch overlay.
    /// </summary>
    public string? BackgroundPath { get; set; }
}

/// <summary>
/// Represents a recently unlocked achievement.
/// </summary>
public class RecentAchievement
{
    /// <summary>
    /// The name of the achievement.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of how to earn the achievement.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Path to the achievement icon.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// When the achievement was unlocked.
    /// </summary>
    public DateTime UnlockedAt { get; set; }
}
