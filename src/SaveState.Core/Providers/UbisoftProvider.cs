using Microsoft.Win32;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SaveState.Core.Providers;

public class UbisoftProvider : IGameProvider
{
    public string Id => "ubisoft";
    public string Name => "Ubisoft Connect";

    private readonly ILogger _logger = Log.ForContext<UbisoftProvider>();

    public async Task<IEnumerable<Game>> GetInstalledGamesAsync()
    {
        var games = new List<Game>();

        games.AddRange(GetGamesFromRegistry());
        
        if (games.Count == 0)
        {
            games.AddRange(GetGamesFromConfig());
        }

        _logger.Information("Found {Count} Ubisoft games", games.Count);
        return await Task.FromResult(games);
    }

    public Task<IEnumerable<Game>> GetOwnedGamesAsync()
    {
        return Task.FromResult(Enumerable.Empty<Game>());
    }

    public Task LaunchGameAsync(Game game)
    {
        if (!string.IsNullOrEmpty(game.SourceId))
        {
            var url = $"uplay://launch/{game.SourceId}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _logger.Information("Launching Ubisoft game: {Title}", game.Title);
        }
        return Task.CompletedTask;
    }

    private List<Game> GetGamesFromRegistry()
    {
        var games = new List<Game>();
        try
        {
            // Check both 32-bit and 64-bit registry
            var registryPaths = new[]
            {
                @"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs",
                @"SOFTWARE\Ubisoft\Launcher\Installs"
            };

            foreach (var regPath in registryPaths)
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                if (key == null) continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var gameKey = key.OpenSubKey(subKeyName);
                    if (gameKey == null) continue;

                    var installDir = gameKey.GetValue("InstallDir") as string;
                    if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                        continue;

                    var gameName = Path.GetFileName(installDir.TrimEnd('\\', '/'));
                    games.Add(new Game
                    {
                        Title = gameName,
                        SortTitle = gameName,
                        Source = "Ubisoft",
                        SourceId = subKeyName,
                        IsInstalled = true,
                        InstallPath = installDir,
                        LaunchCommand = $"uplay://launch/{subKeyName}"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to scan Ubisoft registry");
        }
        return games;
    }

    private List<Game> GetGamesFromConfig()
    {
        var games = new List<Game>();
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = Path.Combine(localAppData, "Ubisoft Game Launcher", "settings.yaml");

            if (!File.Exists(configPath))
                return games;

            // Simple YAML parsing for game paths
            var content = File.ReadAllText(configPath);
            var pathMatches = Regex.Matches(content, @"game_installation_path:\s*""?([^""\n]+)""?");
            
            foreach (Match match in pathMatches)
            {
                var path = match.Groups[1].Value;
                if (Directory.Exists(path))
                {
                    foreach (var gameDir in Directory.GetDirectories(path))
                    {
                        var gameName = Path.GetFileName(gameDir);
                        if (!string.IsNullOrEmpty(gameName) && !gameName.StartsWith("."))
                        {
                            games.Add(new Game
                            {
                                Title = gameName,
                                SortTitle = gameName,
                                Source = "Ubisoft",
                                SourceId = gameName,
                                IsInstalled = true,
                                InstallPath = gameDir
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to scan Ubisoft config");
        }
        return games;
    }
}
