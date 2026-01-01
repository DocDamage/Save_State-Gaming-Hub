using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Service for automatic detection of games from various sources.
/// </summary>
public interface IGameDetectorService
{
    /// <summary>
    /// Scans all configured sources for installed games.
    /// </summary>
    Task<IReadOnlyList<DetectedGame>> ScanAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Scans Steam library locations for installed games.
    /// </summary>
    Task<IReadOnlyList<DetectedGame>> ScanSteamAsync(CancellationToken ct = default);

    /// <summary>
    /// Scans Epic Games Store library locations for installed games.
    /// </summary>
    Task<IReadOnlyList<DetectedGame>> ScanEpicAsync(CancellationToken ct = default);

    /// <summary>
    /// Scans GOG Galaxy library locations for installed games.
    /// </summary>
    Task<IReadOnlyList<DetectedGame>> ScanGogAsync(CancellationToken ct = default);

    /// <summary>
    /// Scans emulator ROM directories for game files.
    /// </summary>
    Task<IReadOnlyList<DetectedGame>> ScanEmulatorRomsAsync(
        IEnumerable<string> romDirectories,
        CancellationToken ct = default);

    /// <summary>
    /// Scans a custom directory for executable game files.
    /// </summary>
    Task<IReadOnlyList<DetectedGame>> ScanDirectoryAsync(
        string directory,
        bool recursive = true,
        CancellationToken ct = default);
}
