using Microsoft.Win32;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace SaveState.Core.Providers;

public class XboxProvider : IGameProvider
{
    public string Id => "xbox";
    public string Name => "Xbox / Game Pass";

    private readonly ILogger _logger = Log.ForContext<XboxProvider>();

    public async Task<IEnumerable<Game>> GetInstalledGamesAsync()
    {
        var games = new List<Game>();

        // Xbox games are UWP apps installed via Microsoft Store / Game Pass
        // We scan the XboxGames folder and registry
        
        try
        {
            // Check common Xbox install locations
            var xboxPaths = new[]
            {
                @"C:\XboxGames",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages")
            };

            // Also check registry for Game Pass games
            games.AddRange(GetGamesFromRegistry());

            // Scan XboxGames folder
            var xboxGamesPath = @"C:\XboxGames";
            if (Directory.Exists(xboxGamesPath))
            {
                foreach (var gameDir in Directory.GetDirectories(xboxGamesPath))
                {
                    var contentDir = Path.Combine(gameDir, "Content");
                    if (Directory.Exists(contentDir))
                    {
                        var gameName = Path.GetFileName(gameDir);
                        games.Add(new Game
                        {
                            Title = gameName,
                            SortTitle = gameName,
                            Source = "Xbox",
                            SourceId = gameName,
                            IsInstalled = true,
                            InstallPath = gameDir
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to scan Xbox games");
        }

        _logger.Information("Found {Count} Xbox games", games.Count);
        return await Task.FromResult(games);
    }

    public Task<IEnumerable<Game>> GetOwnedGamesAsync()
    {
        return Task.FromResult(Enumerable.Empty<Game>());
    }

    public Task LaunchGameAsync(Game game)
    {
        if (!string.IsNullOrEmpty(game.LaunchCommand))
        {
            Process.Start(new ProcessStartInfo(game.LaunchCommand) { UseShellExecute = true });
            _logger.Information("Launching Xbox game: {Title}", game.Title);
        }
        return Task.CompletedTask;
    }

    private List<Game> GetGamesFromRegistry()
    {
        var games = new List<Game>();
        try
        {
            // Xbox Game Pass games are registered in the Gaming Services
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\GamingServices\PackageRepository\Root");
            if (key == null) return games;

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using var packageKey = key.OpenSubKey(subKeyName);
                if (packageKey == null) continue;

                var displayName = packageKey.GetValue("DisplayName") as string;
                var installPath = packageKey.GetValue("Root") as string;

                if (!string.IsNullOrEmpty(displayName) && !displayName.Contains("Xbox"))
                {
                    games.Add(new Game
                    {
                        Title = displayName,
                        SortTitle = displayName,
                        Source = "Xbox",
                        SourceId = subKeyName,
                        IsInstalled = true,
                        InstallPath = installPath
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Xbox registry scan - may require elevated permissions");
        }
        return games;
    }
}
