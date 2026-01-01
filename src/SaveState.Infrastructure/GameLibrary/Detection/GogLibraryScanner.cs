using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.GameLibrary.Detection;

/// <summary>
/// Scans GOG Galaxy library for installed games.
/// </summary>
public class GogLibraryScanner
{
    private readonly ILogger<GogLibraryScanner> _logger;

    private static readonly string GogDbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "GOG.com", "Galaxy", "storage", "galaxy-2.0.db");

    private static readonly string GogGamesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        "GOG Galaxy", "Games");

    public GogLibraryScanner(ILogger<GogLibraryScanner> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanAsync(CancellationToken ct = default)
    {
        var games = new List<DetectedGame>();

        try
        {
            // Try to scan from GOG Galaxy database first
            var dbGames = await ScanFromDatabaseAsync(ct).ConfigureAwait(false);
            games.AddRange(dbGames);

            // Also scan default installation directory
            if (Directory.Exists(GogGamesPath))
            {
                var dirGames = await ScanDirectoryAsync(GogGamesPath, ct).ConfigureAwait(false);

                // Add only games not already found in database
                var existingPaths = games.Select(g => g.ExecutablePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                games.AddRange(dirGames.Where(g => !existingPaths.Contains(g.ExecutablePath)));
            }

            _logger.LogInformation("GOG Galaxy scan complete: found {Count} games", games.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning GOG Galaxy library");
        }

        return games;
    }

    private Task<IReadOnlyList<DetectedGame>> ScanFromDatabaseAsync(CancellationToken ct)
    {
        // GOG Galaxy 2.0 uses SQLite database - for now, fall back to directory scan
        // Full implementation would query the galaxy-2.0.db SQLite database
        _logger.LogDebug("GOG database scanning not implemented, falling back to directory scan");
        return Task.FromResult<IReadOnlyList<DetectedGame>>(Array.Empty<DetectedGame>());
    }

    private async Task<IReadOnlyList<DetectedGame>> ScanDirectoryAsync(string gamesPath, CancellationToken ct)
    {
        var games = new List<DetectedGame>();

        if (!Directory.Exists(gamesPath))
        {
            return games;
        }

        var gameDirs = Directory.GetDirectories(gamesPath);

        foreach (var gameDir in gameDirs)
        {
            if (ct.IsCancellationRequested) break;

            var game = await ScanGameDirectoryAsync(gameDir, ct).ConfigureAwait(false);
            if (game != null)
            {
                games.Add(game);
            }
        }

        return games;
    }

    private Task<DetectedGame?> ScanGameDirectoryAsync(string gameDir, CancellationToken ct)
    {
        try
        {
            var gameName = Path.GetFileName(gameDir);

            // Look for goggame-*.info file
            var infoFiles = Directory.GetFiles(gameDir, "goggame-*.info");

            if (infoFiles.Length > 0)
            {
                return ParseInfoFileAsync(infoFiles[0], gameDir, ct);
            }

            // Fallback: find main executable
            var executable = FindMainExecutable(gameDir);
            if (executable == null)
            {
                return Task.FromResult<DetectedGame?>(null);
            }

            return Task.FromResult<DetectedGame?>(new DetectedGame(
                Title: gameName,
                ExecutablePath: executable,
                Source: "GOG Galaxy",
                PlatformHint: "PC",
                ExternalId: $"gog_{gameName.Replace(" ", "_").ToLowerInvariant()}"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not scan GOG game directory {Path}", gameDir);
            return Task.FromResult<DetectedGame?>(null);
        }
    }

    private async Task<DetectedGame?> ParseInfoFileAsync(string infoPath, string gameDir, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(infoPath, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            var gameId = root.TryGetProperty("gameId", out var idProp) ? idProp.GetString() : null;

            if (string.IsNullOrEmpty(name))
            {
                name = Path.GetFileName(gameDir);
            }

            var executable = FindMainExecutable(gameDir);

            var metadata = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(gameId))
                metadata["GameId"] = gameId;

            return new DetectedGame(
                Title: name,
                ExecutablePath: executable ?? gameDir,
                Source: "GOG Galaxy",
                PlatformHint: "PC",
                ExternalId: $"gog_{gameId ?? name?.Replace(" ", "_").ToLowerInvariant()}",
                LaunchCommand: !string.IsNullOrEmpty(gameId) ? $"goggalaxy://openGameView/{gameId}" : null,
                Metadata: metadata
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse GOG info file {Path}", infoPath);
            return null;
        }
    }

    private static string? FindMainExecutable(string gamePath)
    {
        var exeFiles = Directory.GetFiles(gamePath, "*.exe", SearchOption.AllDirectories);

        return exeFiles
            .OrderBy(f => f.Split(Path.DirectorySeparatorChar).Length)
            .FirstOrDefault(f => !f.Contains("unins", StringComparison.OrdinalIgnoreCase) &&
                                 !f.Contains("crash", StringComparison.OrdinalIgnoreCase) &&
                                 !f.Contains("redist", StringComparison.OrdinalIgnoreCase) &&
                                 !f.Contains("setup", StringComparison.OrdinalIgnoreCase) &&
                                 !f.Contains("galaxy", StringComparison.OrdinalIgnoreCase));
    }
}
