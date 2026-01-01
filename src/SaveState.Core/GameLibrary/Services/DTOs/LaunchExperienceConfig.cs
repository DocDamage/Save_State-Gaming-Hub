namespace SaveState.Core.GameLibrary.Services.DTOs;

/// <summary>
/// Configuration for game launch experience settings.
/// </summary>
public sealed record LaunchExperienceConfig(
    bool ShowGameFacts,
    bool ShowLastProgress,
    bool ShowAchievementProgress,
    bool PlayAmbientMusic,
    TimeSpan MaxIntroDuration);