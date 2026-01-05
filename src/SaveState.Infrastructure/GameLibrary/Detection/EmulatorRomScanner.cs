using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.GameLibrary.Detection;

/// <summary>
/// Scans directories for emulator ROM files.
/// </summary>
public class EmulatorRomScanner
{
    private readonly ILogger<EmulatorRomScanner> _logger;

    /// <summary>
    /// ROM file extensions mapped to platform names.
    /// </summary>
    private static readonly Dictionary<string, string> ExtensionPlatformMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Nintendo
        [".nes"] = "NES",
        [".sfc"] = "SNES",
        [".smc"] = "SNES",
        [".n64"] = "Nintendo 64",
        [".z64"] = "Nintendo 64",
        [".v64"] = "Nintendo 64",
        [".gba"] = "Game Boy Advance",
        [".gb"] = "Game Boy",
        [".gbc"] = "Game Boy Color",
        [".nds"] = "Nintendo DS",
        [".3ds"] = "Nintendo 3DS",
        [".wbfs"] = "Wii",
        [".iso"] = "Multiple",
        [".gcm"] = "GameCube",
        [".rvz"] = "Wii/GameCube",
        [".nsp"] = "Nintendo Switch",
        [".xci"] = "Nintendo Switch",

        // Sega
        [".md"] = "Sega Genesis",
        [".gen"] = "Sega Genesis",
        [".bin"] = "Sega Genesis",
        [".sms"] = "Sega Master System",
        [".32x"] = "Sega 32X",
        [".gg"] = "Game Gear",
        [".cue"] = "Sega CD/Saturn",

        // Sony
        [".pbp"] = "PSP",
        [".cso"] = "PSP",
        [".pkg"] = "PS3/PS4",

        // Other
        [".a26"] = "Atari 2600",
        [".a78"] = "Atari 7800",
        [".pce"] = "TurboGrafx-16",
        [".zip"] = "Arcade",
        [".7z"] = "Multiple"
    };

    public EmulatorRomScanner(ILogger<EmulatorRomScanner> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanAsync(
        IEnumerable<string> directories,
        CancellationToken ct = default)
    {
        var games = new List<DetectedGame>();

        foreach (var directory in directories)
        {
            if (ct.IsCancellationRequested) break;

            if (!Directory.Exists(directory))
            {
                _logger.LogDebug("ROM directory not found: {Path}", directory);
                continue;
            }

            var dirGames = await ScanDirectoryAsync(directory, ct).ConfigureAwait(false);
            games.AddRange(dirGames);
        }

        _logger.LogInformation("ROM scan complete: found {Count} ROMs", games.Count);
        return games;
    }

    private async Task<IReadOnlyList<DetectedGame>> ScanDirectoryAsync(
        string directory,
        CancellationToken ct)
    {
        var games = new List<DetectedGame>();

        try
        {
            var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(f => ExtensionPlatformMap.ContainsKey(Path.GetExtension(f)));

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    if (ct.IsCancellationRequested) break;

                    var game = ParseRomFile(file);
                    if (game != null)
                    {
                        games.Add(game);
                    }
                }
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning ROM directory {Path}", directory);
        }

        return games;
    }

    private DetectedGame? ParseRomFile(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            if (!ExtensionPlatformMap.TryGetValue(extension, out var platform))
            {
                return null;
            }

            // Clean up ROM name (remove region codes, hashes, etc.)
            var cleanName = CleanRomName(fileName);

            var fileInfo = new FileInfo(filePath);

            var metadata = new Dictionary<string, string>
            {
                ["OriginalFilename"] = fileName,
                ["Extension"] = extension
            };

            return new DetectedGame(
                Title: cleanName,
                ExecutablePath: filePath,
                Source: "ROM",
                PlatformHint: platform,
                ExternalId: $"rom_{Path.GetFileName(filePath).GetHashCode():X8}",
                SizeBytes: fileInfo.Length,
                Metadata: metadata
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse ROM file: {FilePath}", filePath);
            return null;
        }
    }

    private static string CleanRomName(string fileName)
    {
        // Remove common ROM naming patterns
        var name = fileName;

        // Remove region codes like (USA), (Europe), [!], etc.
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*[\[\(][^\]\)]*[\]\)]", "");

        // Remove trailing spaces
        name = name.Trim();

        // If name is empty, use original
        return string.IsNullOrWhiteSpace(name) ? fileName : name;
    }
}
