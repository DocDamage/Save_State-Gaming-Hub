using Microsoft.Win32;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System.Diagnostics;
using System.Xml.Linq;

namespace SaveState.Core.Providers;

public class EaProvider : IGameProvider
{
    public string Id => "ea";
    public string Name => "EA App";

    private readonly ILogger _logger = Log.ForContext<EaProvider>();

    public async Task<IEnumerable<Game>> GetInstalledGamesAsync()
    {
        var games = new List<Game>();

        // Try EA App first (newer), then Origin (legacy)
        games.AddRange(GetEaAppGames());
        if (games.Count == 0)
        {
            games.AddRange(GetOriginGames());
        }

        _logger.Information("Found {Count} EA games", games.Count);
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
            // EA App uses origin2:// protocol
            var url = $"origin2://game/launch?offerIds={game.SourceId}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _logger.Information("Launching EA game: {Title}", game.Title);
        }
        return Task.CompletedTask;
    }

    private List<Game> GetEaAppGames()
    {
        var games = new List<Game>();
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var installerDataPath = Path.Combine(localAppData, "Electronic Arts", "EA Desktop", "InstallData");

            if (!Directory.Exists(installerDataPath))
                return games;

            foreach (var file in Directory.GetFiles(installerDataPath, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("installLocation", out var loc) &&
                        root.TryGetProperty("displayName", out var name))
                    {
                        var offerId = Path.GetFileNameWithoutExtension(file);
                        games.Add(new Game
                        {
                            Title = name.GetString() ?? "Unknown",
                            SortTitle = name.GetString(),
                            Source = "EA",
                            SourceId = offerId,
                            IsInstalled = true,
                            InstallPath = loc.GetString()
                        });
                    }
                }
                catch (Exception ex) { _logger.Warning(ex, "Failed to parse EA game install data"); }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to scan EA App games");
        }
        return games;
    }

    private List<Game> GetOriginGames()
    {
        var games = new List<Game>();
        try
        {
            // Legacy Origin uses local content XML files
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var originPath = Path.Combine(programData, "Origin", "LocalContent");

            if (!Directory.Exists(originPath))
                return games;

            foreach (var dir in Directory.GetDirectories(originPath))
            {
                var mfstFiles = Directory.GetFiles(dir, "*.mfst");
                foreach (var mfst in mfstFiles)
                {
                    var content = File.ReadAllText(mfst);
                    // Parse simple key=value format
                    var lines = content.Split('\n');
                    var offerId = "";
                    var installPath = "";

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("id="))
                            offerId = line.Substring(3).Trim();
                        if (line.StartsWith("dipinstallpath="))
                            installPath = Uri.UnescapeDataString(line.Substring(15).Trim());
                    }

                    if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                    {
                        var gameName = Path.GetFileName(installPath);
                        games.Add(new Game
                        {
                            Title = gameName,
                            SortTitle = gameName,
                            Source = "EA",
                            SourceId = offerId,
                            IsInstalled = true,
                            InstallPath = installPath
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to scan Origin games");
        }
        return games;
    }
}
