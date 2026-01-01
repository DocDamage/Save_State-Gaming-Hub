using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services.DTOs;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Service for managing immersive game launch experiences with cinematic sequences.
/// </summary>
public interface ILaunchExperienceManager
{
    /// <summary>
    /// Configures the launch experience settings for a specific game.
    /// </summary>
    Task<Result> ConfigureLaunchExperienceAsync(
        Guid gameId,
        LaunchExperienceConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a launch sequence for a game based on configured settings and game data.
    /// </summary>
    Task<Result<LaunchSequence>> GenerateLaunchSequenceAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Executes the complete launch sequence, including cinematic elements and game startup.
    /// </summary>
    Task ExecuteLaunchSequenceAsync(
        LaunchSequence sequence,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current launch experience configuration for a game.
    /// </summary>
    Task<Result<LaunchExperienceConfig?>> GetLaunchExperienceConfigAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Resets launch experience configuration to defaults for a game.
    /// </summary>
    Task<Result> ResetLaunchExperienceConfigAsync(
        Guid gameId,
        CancellationToken ct = default);
}