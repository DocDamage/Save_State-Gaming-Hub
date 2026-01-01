namespace SaveState.Core.GameLibrary.Services.DTOs;

/// <summary>
/// Represents a complete launch sequence for a game.
/// </summary>
public sealed record LaunchSequence(
    Guid GameId,
    IReadOnlyList<LaunchStep> Steps,
    TimeSpan TotalDuration);

/// <summary>
/// Base class for different types of launch sequence steps.
/// </summary>
public abstract record LaunchStep(string Type, TimeSpan Duration);

/// <summary>
/// Step that shows interesting facts about the game.
/// </summary>
public sealed record GameFactsStep(
    IReadOnlyList<string> Facts) : LaunchStep("GameFacts", TimeSpan.FromSeconds(5));

/// <summary>
/// Step that shows the player's progress in the game.
/// </summary>
public sealed record ProgressSummaryStep(
    TimeSpan TotalPlaytime,
    int AchievementsEarned) : LaunchStep("Progress", TimeSpan.FromSeconds(3));

/// <summary>
/// Step that plays ambient music during the launch.
/// </summary>
public sealed record AmbientMusicStep(
    string? TrackName) : LaunchStep("AmbientMusic", TimeSpan.FromSeconds(3));

/// <summary>
/// Step that shows a loading screen with tips.
/// </summary>
public sealed record LoadingScreenStep(
    IReadOnlyList<string> Tips) : LaunchStep("Loading", TimeSpan.FromSeconds(2));