using Microsoft.Win32;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SaveState.Core.Providers;

public class SteamProvider : IGameProvider
{
    public string Id => "steam";
    public string Name => "Steam";

    private readonly ILogger _logger = Log.ForContext<SteamProvider>();

    public async Task<IEnumerable<Game>> GetInstalledGamesAsync()
    {
        var games = new List<Game>();
        var steamPath = GetSteamPath();
        
        if (string.IsNullOrEmpty(steamPath))
        {
            _logger.Warning("Steam installation not found");
            return games;
        }

        // Normalize path (Steam registry uses forward slashes)
        steamPath = steamPath.Replace("/", "\\");
        _logger.Information("Found Steam at: {Path}", steamPath);

        var libraryPaths = GetLibraryPaths(steamPath);
        foreach (var libraryPath in libraryPaths)
        {
            var steamAppsPath = Path.Combine(libraryPath, "steamapps");
            if (!Directory.Exists(steamAppsPath))
                continue;

            var manifestFiles = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");
            foreach (var manifestFile in manifestFiles)
            {
                var game = ParseAppManifest(manifestFile, steamAppsPath);
                if (game != null)
                {
                    games.Add(game);
                }
            }
        }

        _logger.Information("Found {Count} Steam games", games.Count);
        return await Task.FromResult(games);
    }

    public Task<IEnumerable<Game>> GetOwnedGamesAsync()
    {
        // Would require Steam Web API with user auth
        return Task.FromResult(Enumerable.Empty<Game>());
    }

    public Task LaunchGameAsync(Game game)
    {
        if (!string.IsNullOrEmpty(game.SourceId))
        {
            var url = $"steam://rungameid/{game.SourceId}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _logger.Information("Launching Steam game: {Title} ({AppId})", game.Title, game.SourceId);
        }
        return Task.CompletedTask;
    }

    private string? GetSteamPath()
    {
        try
        {
            // Try 64-bit registry first
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key != null)
            {
                return key.GetValue("SteamPath") as string;
            }

            // Fallback to 32-bit
            using var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            return key32?.GetValue("InstallPath") as string;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to read Steam registry");
            return null;
        }
    }

    private List<string> GetLibraryPaths(string steamPath)
    {
        var paths = new List<string> { steamPath };
        var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

        if (!File.Exists(libraryFoldersPath))
            return paths;

        try
        {
            var content = File.ReadAllText(libraryFoldersPath);
            // Parse VDF manually - look for "path" values
            var pathMatches = Regex.Matches(content, @"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);
            foreach (Match match in pathMatches)
            {
                var path = match.Groups[1].Value.Replace("\\\\", "\\");
                if (Directory.Exists(path) && !paths.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    paths.Add(path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to parse libraryfolders.vdf");
        }

        return paths;
    }

    private Game? ParseAppManifest(string manifestPath, string steamAppsPath)
    {
        try
        {
            var content = File.ReadAllText(manifestPath);
            
            var appId = ExtractVdfValue(content, "appid");
            var name = ExtractVdfValue(content, "name");
            var installDir = ExtractVdfValue(content, "installdir");

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(name))
                return null;

            // Skip Steam tools and redistributables
            if (name.Contains("Steamworks", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Proton", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Redistributable", StringComparison.OrdinalIgnoreCase))
                return null;

            var installPath = !string.IsNullOrEmpty(installDir) 
                ? Path.Combine(steamAppsPath, "common", installDir) 
                : null;

            return new Game
            {
                Title = name,
                SortTitle = name,
                Source = "Steam",
                SourceId = appId,
                IsInstalled = true,
                InstallPath = installPath,
                LaunchCommand = $"steam://rungameid/{appId}"
            };
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse manifest: {Path}", manifestPath);
            return null;
        }
    }

    private string? ExtractVdfValue(string content, string key)
    {
        var match = Regex.Match(content, $@"""{key}""\s+""([^""]+)""", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}
