using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Services;
using System.Diagnostics;
using System.Text.Json;

namespace SaveState.Infrastructure.External;

/// <summary>
/// Game provider for Battle.net (Blizzard) platform.
/// </summary>
public class BattleNetProvider : IGameProvider
{
    private readonly ILogger<BattleNetProvider> _logger;
    private const string BattleNetProtocol = "battlenet://";

    public string Name => "Battle.net";
    public ProviderCapabilities Capabilities => ProviderCapabilities.Discovery | ProviderCapabilities.Launch;

    // Known Battle.net game mappings
    private static readonly Dictionary<string, string> KnownGames = new()
    {
        { "wow", "World of Warcraft" },
        { "wow_classic", "World of Warcraft Classic" },
        { "d3", "Diablo III" },
        { "d2r", "Diablo II: Resurrected" },
        { "hs", "Hearthstone" },
        { "heroes", "Heroes of the Storm" },
        { "sc2", "StarCraft II" },
        { "scr", "StarCraft: Remastered" },
        { "ow", "Overwatch" },
        { "ow2", "Overwatch 2" },
        { "codmw", "Call of Duty: Modern Warfare" },
        { "codmw2", "Call of Duty: Modern Warfare II" },
        { "codvanguard", "Call of Duty: Vanguard" },
        { "w3", "Warcraft III: Reforged" },
        { "d4", "Diablo IV" },
        { "crash4", "Crash Bandicoot 4" }
    };

    public BattleNetProvider(ILogger<BattleNetProvider> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<GameInfo>();

            // Battle.net stores game data in ProgramData
            var battleNetDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Battle.net");

            if (!Directory.Exists(battleNetDataPath))
            {
                _logger.LogDebug("Battle.net data directory not found");
                return Array.Empty<GameInfo>();
            }

            // Look for product database
            var productDbPath = Path.Combine(battleNetDataPath, "Agent", "product.db");
            if (File.Exists(productDbPath))
            {
                var dbGames = await ParseProductDatabaseAsync(productDbPath, ct);
                games.AddRange(dbGames);
            }

            // Also scan for installed games in common paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var installPaths = new[]
            {
                Path.Combine(programFiles, "Overwatch"),
                Path.Combine(programFiles, "Hearthstone"),
                Path.Combine(programFiles, "StarCraft II"),
                Path.Combine(programFiles, "Diablo III"),
                Path.Combine(programFiles, "World of Warcraft"),
                Path.Combine(programFiles, "Heroes of the Storm")
            };

            foreach (var installPath in installPaths)
            {
                if (Directory.Exists(installPath))
                {
                    var gameName = Path.GetFileName(installPath);
                    if (!games.Any(g => g.Title.Equals(gameName, StringComparison.OrdinalIgnoreCase)))
                    {
                        games.Add(new GameInfo
                        {
                            Source = "Battle.net",
                            SourceId = gameName.ToLower().Replace(" ", ""),
                            Title = gameName,
                            InstallPath = installPath,
                            Platform = "PC"
                        });
                    }
                }
            }

            _logger.LogInformation("Found {Count} Battle.net games", games.Count);
            return games;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Battle.net games");
            return Array.Empty<GameInfo>();
        }
    }

    private async Task<List<GameInfo>> ParseProductDatabaseAsync(string dbPath, CancellationToken ct)
    {
        var games = new List<GameInfo>();

        try
        {
            // Battle.net product.db is a JSON-like text file
            var content = await File.ReadAllTextAsync(dbPath, ct);

            // Try to extract game entries (format varies, this is a best-effort parse)
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                try
                {
                    if (line.Contains("\"uid\"") && line.Contains("\"productCode\""))
                    {
                        using var doc = JsonDocument.Parse("{" + line + "}");
                        var root = doc.RootElement;

                        if (root.TryGetProperty("productCode", out var codeElement))
                        {
                            var productCode = codeElement.GetString();
                            if (!string.IsNullOrWhiteSpace(productCode) &&
                                KnownGames.TryGetValue(productCode.ToLower(), out var gameName))
                            {
                                string? installPath = null;
                                if (root.TryGetProperty("installPath", out var pathElement))
                                {
                                    installPath = pathElement.GetString();
                                }

                                games.Add(new GameInfo
                                {
                                    Source = "Battle.net",
                                    SourceId = productCode,
                                    Title = gameName,
                                    InstallPath = installPath,
                                    Platform = "PC"
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Skip malformed lines
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse Battle.net product database");
        }

        return games;
    }

    public Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
    {
        // Battle.net doesn't provide a public API for metadata
        // Return title from known games or gameId
        var title = KnownGames.TryGetValue(gameId.ToLower(), out var name) ? name : gameId;

        return Task.FromResult(new GameMetadata
        {
            Title = title
        });
    }

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
    {
        try
        {
            // Battle.net uses battlenet:// protocol
            // Format varies by game, common format: battlenet://[product]
            var uri = $"{BattleNetProtocol}{gameId}";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });

            _logger.LogInformation("Launched Battle.net game: {GameId}", gameId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch Battle.net game: {GameId}", gameId);
            return Task.FromResult(false);
        }
    }
}
