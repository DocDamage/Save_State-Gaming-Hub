namespace SaveState.Core.GameLibrary.Services.DTOs;

/// <summary>
/// Animation duration options for the launch experience.
/// </summary>
public enum AnimationDuration
{
    /// <summary>
    /// Short animation (5 seconds).
    /// </summary>
    Short = 5,

    /// <summary>
    /// Medium animation (10 seconds).
    /// </summary>
    Medium = 10,

    /// <summary>
    /// Long animation (15 seconds).
    /// </summary>
    Long = 15,

    /// <summary>
    /// Manual - user must dismiss the overlay.
    /// </summary>
    Manual = 0
}

/// <summary>
/// Background style options for the launch experience overlay.
/// </summary>
public enum BackgroundStyle
{
    /// <summary>
    /// Use the game's cover art as background.
    /// </summary>
    GameArt,

    /// <summary>
    /// Use a solid color background.
    /// </summary>
    SolidColor,

    /// <summary>
    /// Use an animated gradient background.
    /// </summary>
    Animated
}

/// <summary>
/// Represents the complete configuration for the immersive launch experience.
/// </summary>
public sealed record LaunchExperienceSettings
{
    /// <summary>
    /// Whether the cinematic launch experience is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// The duration of the launch animation.
    /// </summary>
    public AnimationDuration Duration { get; init; } = AnimationDuration.Medium;

    /// <summary>
    /// Whether to show the AI-generated game briefing.
    /// </summary>
    public bool ShowAiBriefing { get; init; } = true;

    /// <summary>
    /// Whether to show gameplay tips during launch.
    /// </summary>
    public bool ShowTips { get; init; } = true;

    /// <summary>
    /// Whether to show the last session summary.
    /// </summary>
    public bool ShowLastSession { get; init; } = true;

    /// <summary>
    /// The background style for the overlay.
    /// </summary>
    public BackgroundStyle BackgroundStyle { get; init; } = BackgroundStyle.GameArt;

    /// <summary>
    /// Whether the user can skip the launch experience.
    /// </summary>
    public bool AllowSkip { get; init; } = true;

    /// <summary>
    /// Whether to show recent achievements.
    /// </summary>
    public bool ShowAchievements { get; init; } = true;

    /// <summary>
    /// Whether to show total playtime.
    /// </summary>
    public bool ShowPlaytime { get; init; } = true;

    /// <summary>
    /// Gets the duration in seconds based on the selected option.
    /// </summary>
    public int DurationSeconds => Duration switch
    {
        AnimationDuration.Short => 5,
        AnimationDuration.Medium => 10,
        AnimationDuration.Long => 15,
        AnimationDuration.Manual => 0,
        _ => 10
    };
}
