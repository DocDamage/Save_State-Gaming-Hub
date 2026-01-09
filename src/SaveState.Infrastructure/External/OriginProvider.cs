using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Services;
using System.Diagnostics;
using System.Text.Json;

namespace SaveState.Infrastructure.External;

/// <summary>
/// Game provider for Origin/EA App platform.
/// </summary>
public class OriginProvider : IGameProvider
{
    private readonly ILogger<OriginProvider> _logger;
    private const string OriginProtocol = "origin2://";
    private const string EAAppProtocol = "ea-desktop://";

    public string Name => "Origin/EA";
    public ProviderCapabilities Capabilities => ProviderCapabilities.Discovery | ProviderCapabilities.Launch;

    public OriginProvider(ILogger<OriginProvider> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<GameInfo>();

            // Try EA Desktop App first (newer)
            var eaAppGames = await GetEADesktopGamesAsync(ct);
            games.AddRange(eaAppGames);

            // Fallback to Origin (legacy)
            if (games.Count == 0)
            {
                var originGames = await GetOriginGamesAsync(ct);
                games.AddRange(originGames);
            }

            _logger.LogInformation("Found {Count} Origin/EA games", games.Count);
            return games;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Origin/EA games");
            return Array.Empty<GameInfo>();
        }
    }

    private async Task<List<GameInfo>> GetEADesktopGamesAsync(CancellationToken ct)
    {
        var games = new List<GameInfo>();

        try
        {
            // EA Desktop stores game data in LocalAppData
            var eaAppDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Electronic Arts", "EA Desktop", "Catalog");

            if (!Directory.Exists(eaAppDataPath))
            {
                return games;
            }

            // Look for catalog files
            var catalogFiles = Directory.GetFiles(eaAppDataPath, "*.json", SearchOption.AllDirectories);

            foreach (var catalogFile in catalogFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(catalogFile, ct);
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("games", out var gamesArray))
                    {
                        foreach (var gameElement in gamesArray.EnumerateArray())
                        {
                            var gameInfo = ParseEAGameElement(gameElement);
                            if (gameInfo != null)
                            {
                                games.Add(gameInfo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse EA catalog file: {File}", catalogFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read EA Desktop games");
        }

        return games;
    }

    private async Task<List<GameInfo>> GetOriginGamesAsync(CancellationToken ct)
    {
        var games = new List<GameInfo>();

        try
        {
            // Origin stores game data in ProgramData
            var originDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Origin", "LocalContent");

            if (!Directory.Exists(originDataPath))
            {
                return games;
            }

            // Look for .mfst files (manifest files)
            var manifestFiles = Directory.GetFiles(originDataPath, "*.mfst", SearchOption.AllDirectories);

            foreach (var manifestFile in manifestFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(manifestFile, ct);
                    var gameInfo = ParseOriginManifest(content, Path.GetDirectoryName(manifestFile));
                    if (gameInfo != null)
                    {
                        games.Add(gameInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse Origin manifest: {File}", manifestFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read Origin games");
        }

        return games;
    }

    private GameInfo? ParseEAGameElement(JsonElement gameElement)
    {
        try
        {
            if (!gameElement.TryGetProperty("title", out var titleElement))
            {
                return null;
            }

            var title = titleElement.GetString();
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            var gameId = gameElement.TryGetProperty("id", out var idElement)
                ? idElement.GetString() ?? title
                : title;

            var installPath = gameElement.TryGetProperty("installPath", out var pathElement)
                ? pathElement.GetString()
                : null;

            return new GameInfo
            {
                Source = "EA",
                SourceId = gameId,
                Title = title,
                InstallPath = installPath,
                Platform = "PC"
            };
        }
        catch
        {
            return null;
        }
    }

    private GameInfo? ParseOriginManifest(string content, string? directory)
    {
        try
        {
            // Origin manifests are not JSON, they're custom format
            // Extract title from dipinstallpath or id.gameIssueTrackerId
            var lines = content.Split('\n');
            string? title = null;
            string? gameId = null;
            string? installPath = directory;

            foreach (var line in lines)
            {
                if (line.Contains("dipinstallpath", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('=');
                    if (parts.Length > 1)
                    {
                        title = parts[1].Trim().Trim('"');
                    }
                }
                else if (line.Contains("id.gameIssueTrackerId", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('=');
                    if (parts.Length > 1)
                    {
                        gameId = parts[1].Trim().Trim('"');
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return new GameInfo
            {
                Source = "Origin",
                SourceId = gameId ?? title,
                Title = title,
                InstallPath = installPath,
                Platform = "PC"
            };
        }
        catch
        {
            return null;
        }
    }

    public Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
    {
        // Origin doesn't provide a public API for metadata
        // Return basic metadata from game ID
        return Task.FromResult(new GameMetadata
        {
            Title = gameId
        });
    }

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            // Try EA Desktop protocol first
            var eaAppUri = $"{EAAppProtocol}launch/{gameId}";
            Process.Start(new ProcessStartInfo(eaAppUri) { UseShellExecute = true });

            _logger.LogInformation("Launched EA game: {GameId}", gameId);
            return Task.FromResult(true);
        }
        catch
        {
            try
            {
                // Fallback to Origin protocol
                var originUri = $"{OriginProtocol}game/launch?offerIds={gameId}";
                Process.Start(new ProcessStartInfo(originUri) { UseShellExecute = true });

                _logger.LogInformation("Launched Origin game: {GameId}", gameId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch Origin/EA game: {GameId}", gameId);
                return Task.FromResult(false);
            }
        }
    }
}
