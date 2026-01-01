using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.GameLibrary.Detection;

/// <summary>
/// Scans Steam library locations for installed games.
/// </summary>
public partial class SteamLibraryScanner
{
    private readonly ILogger<SteamLibraryScanner> _logger;

    private static readonly string[] DefaultSteamPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
        @"C:\Steam",
        @"D:\Steam",
        @"D:\SteamLibrary"
    };

    public SteamLibraryScanner(ILogger<SteamLibraryScanner> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectedGame>> ScanAsync(CancellationToken ct = default)
    {
        var games = new List<DetectedGame>();

        try
        {
            var libraryPaths = await GetLibraryPathsAsync(ct).ConfigureAwait(false);

            foreach (var libraryPath in libraryPaths)
            {
                if (ct.IsCancellationRequested) break;

                var steamAppsPath = Path.Combine(libraryPath, "steamapps");
                if (!Directory.Exists(steamAppsPath)) continue;

                var manifests = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");

                foreach (var manifest in manifests)
                {
                    if (ct.IsCancellationRequested) break;

                    var game = await ParseManifestAsync(manifest, steamAppsPath, ct).ConfigureAwait(false);
                    if (game != null)
                    {
                        games.Add(game);
                    }
                }
            }

            _logger.LogInformation("Steam scan complete: found {Count} games", games.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning Steam library");
        }

        return games;
    }

    private async Task<IReadOnlyList<string>> GetLibraryPathsAsync(CancellationToken ct)
    {
        var paths = new List<string>();

        foreach (var steamPath in DefaultSteamPaths)
        {
            if (!Directory.Exists(steamPath)) continue;

            var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFoldersPath)) continue;

            try
            {
                var content = await File.ReadAllTextAsync(libraryFoldersPath, ct).ConfigureAwait(false);
                var pathMatches = PathRegex().Matches(content);

                foreach (Match match in pathMatches)
                {
                    var libPath = match.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(libPath))
                    {
                        paths.Add(libPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse libraryfolders.vdf at {Path}", libraryFoldersPath);
            }
        }

        // Also include default steam paths directly
        paths.AddRange(DefaultSteamPaths.Where(Directory.Exists));

        return paths.Distinct().ToList();
    }

    private async Task<DetectedGame?> ParseManifestAsync(string manifestPath, string steamAppsPath, CancellationToken ct)
    {
        try
        {
            var content = await File.ReadAllTextAsync(manifestPath, ct).ConfigureAwait(false);

            var appIdMatch = AppIdRegex().Match(content);
            var nameMatch = NameRegex().Match(content);
            var installDirMatch = InstallDirRegex().Match(content);
            var sizeMatch = SizeRegex().Match(content);

            if (!appIdMatch.Success || !nameMatch.Success || !installDirMatch.Success)
            {
                return null;
            }

            var appId = appIdMatch.Groups[1].Value;
            var name = nameMatch.Groups[1].Value;
            var installDir = installDirMatch.Groups[1].Value;
            var gamePath = Path.Combine(steamAppsPath, "common", installDir);

            if (!Directory.Exists(gamePath))
            {
                return null;
            }

            // Try to find main executable
            var executable = FindMainExecutable(gamePath);

            long? sizeBytes = null;
            if (sizeMatch.Success && long.TryParse(sizeMatch.Groups[1].Value, out var size))
            {
                sizeBytes = size;
            }

            var metadata = new Dictionary<string, string>
            {
                ["SteamAppId"] = appId,
                ["InstallDir"] = installDir
            };

            return new DetectedGame(
                Title: name,
                ExecutablePath: executable ?? gamePath,
                Source: "Steam",
                PlatformHint: "PC",
                ExternalId: $"steam_{appId}",
                SizeBytes: sizeBytes,
                IconPath: $"steam://icons/{appId}",
                LaunchCommand: $"steam://rungameid/{appId}",
                Metadata: metadata
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse manifest {Path}", manifestPath);
            return null;
        }
    }

    private static string? FindMainExecutable(string gamePath)
    {
        var exeFiles = Directory.GetFiles(gamePath, "*.exe", SearchOption.AllDirectories);

        // Prefer executables in root or first level
        var rootExe = exeFiles
            .OrderBy(f => f.Split(Path.DirectorySeparatorChar).Length)
            .FirstOrDefault(f => !f.Contains("unins", StringComparison.OrdinalIgnoreCase) &&
                                 !f.Contains("crash", StringComparison.OrdinalIgnoreCase) &&
                                 !f.Contains("redist", StringComparison.OrdinalIgnoreCase) &&
                                 !f.Contains("setup", StringComparison.OrdinalIgnoreCase));

        return rootExe;
    }

    [GeneratedRegex(@"""path""\s+""([^""]+)""")]
    private static partial Regex PathRegex();

    [GeneratedRegex(@"""appid""\s+""(\d+)""")]
    private static partial Regex AppIdRegex();

    [GeneratedRegex(@"""name""\s+""([^""]+)""")]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"""installdir""\s+""([^""]+)""")]
    private static partial Regex InstallDirRegex();

    [GeneratedRegex(@"""SizeOnDisk""\s+""(\d+)""")]
    private static partial Regex SizeRegex();
}
