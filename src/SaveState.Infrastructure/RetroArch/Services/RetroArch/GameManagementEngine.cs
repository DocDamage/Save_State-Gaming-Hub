using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using System.Diagnostics;
using System.Text.Json;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for managing games and playlists.
/// </summary>
public partial class GameManagementEngine : IGameManagementEngine
{
    private readonly ILogger<GameManagementEngine> _logger;

    public GameManagementEngine(ILogger<GameManagementEngine> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RetroArchGame>>> GetGamesAsync(
        string retroArchPath,
        string? playlistsPathOverride,
        CancellationToken ct = default)
    {
        try
        {
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            var playlistsDir = !string.IsNullOrEmpty(playlistsPathOverride)
                ? playlistsPathOverride
                : Path.Combine(retroArchDir, "playlists");

            if (!Directory.Exists(playlistsDir))
            {
                LogPlaylistsNotFound(_logger, playlistsDir);
                return Result.Success<IReadOnlyList<RetroArchGame>>(Array.Empty<RetroArchGame>());
            }

            var games = new List<RetroArchGame>();
            var playlistFiles = Directory.GetFiles(playlistsDir, "*.lpl");

            foreach (var playlistFile in playlistFiles)
            {
                try
                {
                    var playlistGames = await ParsePlaylistAsync(playlistFile, ct);
                    games.AddRange(playlistGames);
                }
                catch (JsonException ex)
                {
                    LogPlaylistParseFailed(_logger, playlistFile, ex);
                }
                catch (IOException ex)
                {
                    LogPlaylistParseFailed(_logger, playlistFile, ex);
                }
            }

            LogGamesFoundCount(_logger, games.Count, playlistFiles.Length);

            return Result.Success<IReadOnlyList<RetroArchGame>>(games);
        }
        catch (Exception ex)
        {
            LogGetGamesError(_logger, ex);
            return Result.Failure<IReadOnlyList<RetroArchGame>>($"Error getting games: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetroArchGame>> ParsePlaylistAsync(string playlistPath, CancellationToken ct)
    {
        var games = new List<RetroArchGame>();
        var json = await File.ReadAllTextAsync(playlistPath, ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                try
                {
                    var game = new RetroArchGame
                    {
                        Path = item.GetProperty("path").GetString() ?? "",
                        Label = item.GetProperty("label").GetString() ?? "",
                        CorePath = item.GetProperty("core_path").GetString() ?? "",
                        CoreName = item.GetProperty("core_name").GetString() ?? "",
                        Crc32 = item.TryGetProperty("crc32", out var crc) ? crc.GetString() : null,
                        DbName = item.TryGetProperty("db_name", out var db) ? db.GetString() : null
                    };

                    games.Add(game);
                }
                catch (KeyNotFoundException ex)
                {
                    LogPlaylistItemParseFailed(_logger, ex);
                }
                catch (InvalidOperationException ex)
                {
                    LogPlaylistItemParseFailed(_logger, ex);
                }
            }
        }

        return games;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RetroArchGame>>> GetPlaylistGamesAsync(string playlistPath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(playlistPath))
            {
                return Result.Failure<IReadOnlyList<RetroArchGame>>($"Playlist not found: {playlistPath}");
            }

            var games = await ParsePlaylistAsync(playlistPath, ct);
            return Result.Success(games);
        }
        catch (JsonException ex)
        {
            LogPlaylistParseFailed(_logger, playlistPath, ex);
            return Result.Failure<IReadOnlyList<RetroArchGame>>($"Error parsing playlist: {ex.Message}");
        }
        catch (IOException ex)
        {
            LogPlaylistParseFailed(_logger, playlistPath, ex);
            return Result.Failure<IReadOnlyList<RetroArchGame>>($"Error reading playlist: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<string>>> GetPlaylistsAsync(
        string retroArchPath,
        string? playlistsPathOverride,
        CancellationToken ct = default)
    {
        try
        {
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            var playlistsDir = !string.IsNullOrEmpty(playlistsPathOverride)
                ? playlistsPathOverride
                : Path.Combine(retroArchDir, "playlists");

            if (!Directory.Exists(playlistsDir))
            {
                return Task.FromResult(Result.Success<IReadOnlyList<string>>(Array.Empty<string>()));
            }

            var playlists = Directory.GetFiles(playlistsDir, "*.lpl")
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>()
                .ToList();

            return Task.FromResult(Result.Success<IReadOnlyList<string>>(playlists));
        }
        catch (Exception ex)
        {
            LogGetPlaylistsError(_logger, ex);
            return Task.FromResult(Result.Failure<IReadOnlyList<string>>($"Error getting playlists: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> LaunchGameAsync(string retroArchPath, string gamePath, string corePath, CancellationToken ct = default)
    {
        try
        {
            LogLaunchingGame(_logger, gamePath, corePath);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = retroArchPath,
                    Arguments = $"-L \"{corePath}\" \"{gamePath}\"",
                    UseShellExecute = true
                }
            };

            process.Start();
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            LogLaunchGameError(_logger, ex);
            return Task.FromResult(Result.Failure($"Error launching game: {ex.Message}"));
        }
    }

    #region Logging

    [LoggerMessage(EventId = 601, Level = LogLevel.Warning, Message = "RetroArch playlists directory not found: {Path}")]
    static partial void LogPlaylistsNotFound(ILogger logger, string path);

    [LoggerMessage(EventId = 602, Level = LogLevel.Warning, Message = "Failed to parse playlist: {File}")]
    static partial void LogPlaylistParseFailed(ILogger logger, string file, Exception ex);

    [LoggerMessage(EventId = 603, Level = LogLevel.Information, Message = "Found {Count} RetroArch games across {PlaylistCount} playlists")]
    static partial void LogGamesFoundCount(ILogger logger, int count, int playlistCount);

    [LoggerMessage(EventId = 604, Level = LogLevel.Error, Message = "Error getting RetroArch games")]
    static partial void LogGetGamesError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 605, Level = LogLevel.Debug, Message = "Failed to parse playlist item")]
    static partial void LogPlaylistItemParseFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 606, Level = LogLevel.Error, Message = "Error getting playlists")]
    static partial void LogGetPlaylistsError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 607, Level = LogLevel.Information, Message = "Launching game in RetroArch: {Game} with core: {Core}")]
    static partial void LogLaunchingGame(ILogger logger, string game, string core);

    [LoggerMessage(EventId = 608, Level = LogLevel.Error, Message = "Error launching game")]
    static partial void LogLaunchGameError(ILogger logger, Exception ex);

    #endregion
}
