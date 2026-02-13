using SaveState.Core.Common;
using SaveState.Core.RetroArch;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for managing games and playlists.
/// </summary>
public interface IGameManagementEngine
{
    /// <summary>
    /// Gets all games from RetroArch playlists.
    /// </summary>
    Task<Result<IReadOnlyList<RetroArchGame>>> GetGamesAsync(string retroArchPath, string? playlistsPathOverride, CancellationToken ct = default);

    /// <summary>
    /// Parses a playlist file.
    /// </summary>
    Task<IReadOnlyList<RetroArchGame>> ParsePlaylistAsync(string playlistPath, CancellationToken ct = default);

    /// <summary>
    /// Gets games from a specific playlist.
    /// </summary>
    Task<Result<IReadOnlyList<RetroArchGame>>> GetPlaylistGamesAsync(string playlistPath, CancellationToken ct = default);

    /// <summary>
    /// Gets all available playlists.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetPlaylistsAsync(string retroArchPath, string? playlistsPathOverride, CancellationToken ct = default);

    /// <summary>
    /// Launches a game in RetroArch.
    /// </summary>
    Task<Result> LaunchGameAsync(string retroArchPath, string gamePath, string corePath, CancellationToken ct = default);
}
