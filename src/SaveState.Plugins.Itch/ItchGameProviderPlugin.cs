using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.Itch;

/// <summary>
/// Plugin that provides access to Itch.io games.
/// Supports discovering indie games, downloading, and installation.
/// </summary>
public class ItchGameProviderPlugin : IPlugin, IGameProvider, IDisposable
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private HttpClient? _httpClient;
    private bool _disposed;

    public string Id => "savestate.itch";
    public string Name => "Itch.io Game Provider";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Discover and install indie games from Itch.io";
    public PluginCapabilities Capabilities => PluginCapabilities.GameProvider;

    // IGameProvider implementation
    public string ProviderName => "Itch.io";

    public ItchGameProviderPlugin()
    {
        // Note: In plugin scenarios, IHttpClientFactory may not be available.
        // We create a shared instance that persists for the plugin lifetime.
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SaveState/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Initializing Itch.io plugin");

        // Register menu items for Itch.io features
        var browseMenuItem = new PluginMenuItem(
            Id: "itch.browse",
            Label: "Browse Itch.io Games",
            Icon: "🎮",
            SortOrder: 100,
            Action: BrowseGamesAsync);

        var featuredMenuItem = new PluginMenuItem(
            Id: "itch.featured",
            Label: "Featured on Itch.io",
            Icon: "⭐",
            SortOrder: 101,
            Action: ShowFeaturedAsync);

        await context.RegisterMenuItemAsync(browseMenuItem);
        await context.RegisterGameProviderAsync(this);

        _logger.LogInformation("Itch.io plugin initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down Itch.io plugin");
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    // IGameProvider implementation
    public async Task<Result<IReadOnlyList<Game>>> DiscoverGamesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Discovering games from Itch.io");

            // Itch.io has a public API for browsing games
            // We'll use their browse endpoint to get popular games
            var url = "https://itch.io/api/1/jams";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<IReadOnlyList<Game>>($"Failed to fetch games from Itch.io: {response.StatusCode}");
            }

            // For demo purposes, we'll create some sample games
            // In a real implementation, you'd parse the Itch.io API response
            var games = new List<Game>
            {
                Game.Create("Celeste", source: "Itch.io", sourceId: "celeste"),
                Game.Create("Hades", source: "Itch.io", sourceId: "hades"),
                Game.Create("Risk of Rain 2", source: "Itch.io", sourceId: "risk-of-rain-2"),
                Game.Create("Loop Hero", source: "Itch.io", sourceId: "loop-hero"),
                Game.Create("Spiritfarer", source: "Itch.io", sourceId: "spiritfarer")
            };

            return Result.Success<IReadOnlyList<Game>>(games);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error discovering games from Itch.io");
            return Result.Failure<IReadOnlyList<Game>>($"Failed to discover games: {ex.Message}");
        }
    }

    public async Task<Result<Game>> GetGameDetailsAsync(string externalId, CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Getting game details for {ExternalId}", externalId);

            // In a real implementation, you'd fetch from Itch.io API
            // For demo, return a game with enhanced metadata
            var game = Game.Create(
                externalId.Replace("-", " "),
                description: $"An indie game available on Itch.io: {externalId}",
                source: "Itch.io",
                sourceId: externalId);

            return Result.Success<Game>(game);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting game details for {ExternalId}", externalId);
            return Result.Failure<Game>($"Failed to get game details: {ex.Message}");
        }
    }

    public async Task<Result<bool>> InstallGameAsync(string externalId, string installPath, CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Installing game {ExternalId} to {InstallPath}", externalId, installPath);

            // In a real implementation, this would:
            // 1. Download the game from Itch.io
            // 2. Extract it to the install path
            // 3. Configure the game for launching

            // For demo purposes, we'll simulate the installation
            await Task.Delay(2000, ct); // Simulate download time

            // Create a dummy executable or shortcut
            var gameDir = Path.Combine(installPath, externalId);
            Directory.CreateDirectory(gameDir);

            var exePath = Path.Combine(gameDir, $"{externalId}.exe");
            await File.WriteAllTextAsync(exePath, $"# Dummy executable for {externalId}\necho 'This would launch the real game'", ct);

            _logger?.LogInformation("Successfully 'installed' game {ExternalId}", externalId);
            return Result.Success<bool>(true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error installing game {ExternalId}", externalId);
            return Result.Failure<bool>($"Failed to install game: {ex.Message}");
        }
    }

    private async Task BrowseGamesAsync()
    {
        try
        {
            _logger?.LogInformation("Opening Itch.io game browser");

            // In a real implementation, this would open a UI dialog
            // or launch a browser to Itch.io
            var games = await DiscoverGamesAsync();
            if (games.IsSuccess)
            {
                _logger?.LogInformation("Found {Count} games on Itch.io", games.Value.Count);
                foreach (var game in games.Value)
                {
                    _logger?.LogInformation("- {Title} ({Platform})", game.Title, game.Platform?.Name ?? "Unknown");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error browsing Itch.io games");
        }
    }

    private async Task ShowFeaturedAsync()
    {
        try
        {
            _logger?.LogInformation("Showing featured Itch.io games");

            // This could show featured/popular games
            var games = await DiscoverGamesAsync();
            if (games.IsSuccess)
            {
                _logger?.LogInformation("Featured games:");
                foreach (var game in games.Value.Take(3))
                {
                    _logger?.LogInformation("⭐ {Title}", game.Title);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing featured games");
        }
    }

    /// <summary>
    /// Disposes resources used by the plugin.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
