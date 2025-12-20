using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace SaveState.Core.Providers;

public class EpicProvider : IGameProvider
{
    public string Id => "epic";
    public string Name => "Epic Games";

    private readonly ILogger _logger = Log.ForContext<EpicProvider>();

    public async Task<IEnumerable<Game>> GetInstalledGamesAsync()
    {
        var games = new List<Game>();
        var manifestsPath = GetManifestsPath();

        if (string.IsNullOrEmpty(manifestsPath) || !Directory.Exists(manifestsPath))
        {
            _logger.Warning("Epic Games manifests folder not found");
            return games;
        }

        var manifestFiles = Directory.GetFiles(manifestsPath, "*.item");
        foreach (var manifestFile in manifestFiles)
        {
            var game = await ParseManifest(manifestFile);
            if (game != null)
            {
                games.Add(game);
            }
        }

        _logger.Information("Found {Count} Epic games", games.Count);
        return games;
    }

    public Task<IEnumerable<Game>> GetOwnedGamesAsync()
    {
        return Task.FromResult(Enumerable.Empty<Game>());
    }

    public Task LaunchGameAsync(Game game)
    {
        if (!string.IsNullOrEmpty(game.SourceId))
        {
            var url = $"com.epicgames.launcher://apps/{game.SourceId}?action=launch";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _logger.Information("Launching Epic game: {Title}", game.Title);
        }
        return Task.CompletedTask;
    }

    private string? GetManifestsPath()
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var path = Path.Combine(programData, "Epic", "EpicGamesLauncher", "Data", "Manifests");
        return Directory.Exists(path) ? path : null;
    }

    private async Task<Game?> ParseManifest(string manifestPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var displayName = root.TryGetProperty("DisplayName", out var dn) ? dn.GetString() : null;
            var installLocation = root.TryGetProperty("InstallLocation", out var il) ? il.GetString() : null;
            var appName = root.TryGetProperty("AppName", out var an) ? an.GetString() : null;
            var catalogItemId = root.TryGetProperty("CatalogItemId", out var ci) ? ci.GetString() : null;

            if (string.IsNullOrEmpty(displayName))
                return null;

            // Skip UE and launcher components
            if (displayName.Contains("Unreal Engine", StringComparison.OrdinalIgnoreCase) ||
                displayName.Contains("Launcher", StringComparison.OrdinalIgnoreCase))
                return null;

            return new Game
            {
                Title = displayName,
                SortTitle = displayName,
                Source = "Epic",
                SourceId = appName ?? catalogItemId,
                IsInstalled = true,
                InstallPath = installLocation
            };
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse Epic manifest: {Path}", manifestPath);
            return null;
        }
    }
}
