using SaveState.Core.Common;

namespace SaveState.Core.RetroArch.Services;

/// <summary>
/// Service for integrating with RetroArch emulation frontend.
/// </summary>
public interface IRetroArchService
{
    /// <summary>
    /// Detects RetroArch installation path.
    /// </summary>
    Task<Result<string>> DetectRetroArchPathAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all games from RetroArch playlists.
    /// </summary>
    Task<Result<IReadOnlyList<RetroArchGame>>> GetGamesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets installed RetroArch cores.
    /// </summary>
    Task<Result<IReadOnlyList<RetroArchCore>>> GetInstalledCoresAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets available cores for download.
    /// </summary>
    Task<Result<IReadOnlyList<RetroArchCore>>> GetAvailableCoresAsync(CancellationToken ct = default);

    /// <summary>
    /// Installs a RetroArch core.
    /// </summary>
    Task<Result> InstallCoreAsync(string coreName, CancellationToken ct = default);

    /// <summary>
    /// Updates a RetroArch core.
    /// </summary>
    Task<Result> UpdateCoreAsync(string coreName, CancellationToken ct = default);

    /// <summary>
    /// Gets RetroArch configuration.
    /// </summary>
    Task<Result<RetroArchConfig>> GetConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Syncs save files via RetroArch cloud.
    /// </summary>
    Task<Result> SyncSavesAsync(CancellationToken ct = default);

    /// <summary>
    /// Launches a game in RetroArch.
    /// </summary>
    Task<Result> LaunchGameAsync(string gamePath, string corePath, CancellationToken ct = default);

    /// <summary>
    /// Gets RetroAchievements for a game.
    /// </summary>
    Task<Result<IReadOnlyList<Achievement>>> GetAchievementsAsync(string gameHash, CancellationToken ct = default);

    /// <summary>
    /// Creates a save state via RetroArch network command interface.
    /// </summary>
    Task<Result<string>> CreateSaveStateAsync(int slot = -1, CancellationToken ct = default);

    /// <summary>
    /// Loads a save state via RetroArch network command interface.
    /// </summary>
    Task<Result> LoadSaveStateAsync(int slot, CancellationToken ct = default);

    /// <summary>
    /// Loads a save state from a specific file path via RetroArch network command interface.
    /// </summary>
    Task<Result> LoadSaveStateFromFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Captures a screenshot from running RetroArch instance.
    /// </summary>
    Task<Result<string>> CaptureScreenshotAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a command to running RetroArch instance via network command interface.
    /// </summary>
    Task<Result<string>> SendCommandAsync(string command, CancellationToken ct = default);

    /// <summary>
    /// Checks if a RetroArch instance is currently running.
    /// </summary>
    Task<Result<bool>> IsRunningAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents a RetroAchievement.
/// </summary>
public class Achievement
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required int Points { get; init; }
    public string? BadgeUrl { get; init; }
    public bool IsUnlocked { get; init; }
    public DateTime? UnlockedAt { get; init; }
}
