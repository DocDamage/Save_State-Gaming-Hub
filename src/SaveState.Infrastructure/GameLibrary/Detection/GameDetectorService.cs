using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Detection;

/// <summary>
/// Orchestrates game detection from all available sources.
/// </summary>
public class GameDetectorService : IGameDetectorService
{
    private readonly SteamLibraryScanner _steamScanner;
    private readonly EpicLibraryScanner _epicScanner;
    private readonly GogLibraryScanner _gogScanner;
    private readonly EmulatorRomScanner _romScanner;
    private readonly ILogger<GameDetectorService> _logger;

    public GameDetectorService(
        SteamLibraryScanner steamScanner,
        EpicLibraryScanner epicScanner,
        GogLibraryScanner gogScanner,
        EmulatorRomScanner romScanner,
        ILogger<GameDetectorService> logger)
    {
        _steamScanner = steamScanner;
        _epicScanner = epicScanner;
        _gogScanner = gogScanner;
        _romScanner = romScanner;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanAllAsync(CancellationToken ct = default)
    {
        var allGames = new List<DetectedGame>();

        _logger.LogInformation("Starting full game library scan...");

        // Scan all sources in parallel
        var steamTask = ScanSteamAsync(ct);
        var epicTask = ScanEpicAsync(ct);
        var gogTask = ScanGogAsync(ct);

        await Task.WhenAll(steamTask, epicTask, gogTask).ConfigureAwait(false);

        allGames.AddRange(await steamTask.ConfigureAwait(false));
        allGames.AddRange(await epicTask.ConfigureAwait(false));
        allGames.AddRange(await gogTask.ConfigureAwait(false));

        _logger.LogInformation("Full scan complete: found {Count} total games", allGames.Count);

        return allGames;
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanSteamAsync(CancellationToken ct = default)
    {
        try
        {
            return await _steamScanner.ScanAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Steam scan failed");
            return Array.Empty<DetectedGame>();
        }
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanEpicAsync(CancellationToken ct = default)
    {
        try
        {
            return await _epicScanner.ScanAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Epic Games scan failed");
            return Array.Empty<DetectedGame>();
        }
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanGogAsync(CancellationToken ct = default)
    {
        try
        {
            return await _gogScanner.ScanAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GOG Galaxy scan failed");
            return Array.Empty<DetectedGame>();
        }
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanEmulatorRomsAsync(
        IEnumerable<string> romDirectories,
        CancellationToken ct = default)
    {
        try
        {
            return await _romScanner.ScanAsync(romDirectories, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ROM scan failed");
            return Array.Empty<DetectedGame>();
        }
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanDirectoryAsync(
        string directory,
        bool recursive = true,
        CancellationToken ct = default)
    {
        var games = new List<DetectedGame>();

        try
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogWarning("Directory not found: {Path}", directory);
                return games;
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var exeFiles = Directory.GetFiles(directory, "*.exe", searchOption);

            await Task.Run(() =>
            {
                foreach (var exePath in exeFiles)
                {
                    if (ct.IsCancellationRequested) break;

                    // Skip common non-game executables
                    var fileName = Path.GetFileName(exePath);
                    if (IsLikelyNotGame(fileName))
                    {
                        continue;
                    }

                    var gameDir = Path.GetDirectoryName(exePath) ?? directory;
                    var gameName = Path.GetFileNameWithoutExtension(exePath);

                    // Try to derive a better name from the directory
                    if (gameName.Equals("launcher", StringComparison.OrdinalIgnoreCase) ||
                        gameName.Equals("game", StringComparison.OrdinalIgnoreCase))
                    {
                        gameName = Path.GetFileName(gameDir);
                    }

                    var fileInfo = new FileInfo(exePath);

                    var game = new DetectedGame(
                        Title: gameName,
                        ExecutablePath: exePath,
                        Source: "Custom Directory",
                        PlatformHint: "PC",
                        SizeBytes: fileInfo.Length,
                        Metadata: new Dictionary<string, string>
                        {
                            ["ScannedDirectory"] = directory
                        }
                    );

                    games.Add(game);
                }
            }, ct).ConfigureAwait(false);

            _logger.LogInformation("Directory scan complete: found {Count} potential games in {Path}",
                games.Count, directory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory {Path}", directory);
        }

        return games;
    }

    private static bool IsLikelyNotGame(string fileName)
    {
        var lowerName = fileName.ToLowerInvariant();

        var excludePatterns = new[]
        {
            "unins", "uninst", "setup", "install", "crash", "report",
            "redist", "vcredist", "dxsetup", "directx", "dotnet",
            "updater", "launcher", "patcher", "helper", "service",
            "ue4prereq", "easyanticheat", "battleye"
        };

        return excludePatterns.Any(pattern => lowerName.Contains(pattern));
    }
}
