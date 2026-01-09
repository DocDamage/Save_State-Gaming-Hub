using SaveState.Core.Plugins;
using Microsoft.Extensions.Logging;

namespace SaveState.Plugins.Example;

/// <summary>
/// Example plugin demonstrating basic plugin functionality.
/// </summary>
public class ExamplePlugin : IPlugin, IGameProvider, IMetadataScraper
{
    private IPluginContext? _context;
    private ILogger? _logger;

    public string Id => "savestate.example";
    public string Name => "Example Plugin";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "A sample plugin demonstrating plugin capabilities.";
    public PluginCapabilities Capabilities => PluginCapabilities.GameProvider | PluginCapabilities.MetadataScraper;

    // IGameProvider implementation
    public string ProviderName => "Example Game Provider";

    // IMetadataScraper implementation
    public string ScraperName => "Example Metadata Scraper";

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Example plugin initialized");

        // Register a menu item
        var menuItem = new PluginMenuItem(
            Id: "example.hello",
            Label: "Say Hello",
            Icon: "👋",
            SortOrder: 100,
            Action: () => SayHelloAsync());

        await context.RegisterMenuItemAsync(menuItem);

        _logger.LogInformation("Example plugin registered menu item");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Example plugin shutting down");
        return Task.CompletedTask;
    }

    // IGameProvider implementation
    public Task<Result<IReadOnlyList<Game>>> DiscoverGamesAsync(CancellationToken ct = default)
    {
        var games = new List<Game>
        {
            new Game("Example Game 1", Platform.Create("PC", "Personal Computer")),
            new Game("Example Game 2", Platform.Create("PC", "Personal Computer"))
        };

        return Task.FromResult(Result.Success<IReadOnlyList<Game>>(games));
    }

    public Task<Result<Game>> GetGameDetailsAsync(string externalId, CancellationToken ct = default)
    {
        var game = new Game($"Example Game {externalId}", Platform.Create("PC", "Personal Computer"));
        return Task.FromResult(Result.Success<Game>(game));
    }

    public Task<Result<bool>> InstallGameAsync(string externalId, string installPath, CancellationToken ct = default)
    {
        _logger?.LogInformation("Installing example game {Id} to {Path}", externalId, installPath);
        return Task.FromResult(Result.Success<bool>(true));
    }

    // IMetadataScraper implementation
    public Task<Result<IReadOnlyList<MetadataSearchResult>>> SearchGamesAsync(string title, CancellationToken ct = default)
    {
        var results = new List<MetadataSearchResult>
        {
            new MetadataSearchResult("example-1", "Example Game", "A sample game", 2024, "https://example.com/cover.jpg")
        };

        return Task.FromResult(Result.Success<IReadOnlyList<MetadataSearchResult>>(results));
    }

    public Task<Result<GameMetadata>> GetGameMetadataAsync(string externalId, CancellationToken ct = default)
    {
        var metadata = new GameMetadata(
            Title: "Example Game",
            Description: "A sample game for demonstration purposes.",
            Developer: "Example Developer",
            Publisher: "Example Publisher",
            ReleaseYear: 2024,
            Genre: "Adventure",
            TimeToBeatMain: TimeSpan.FromHours(5),
            TimeToBeatPlus: TimeSpan.FromHours(8),
            TimeToBeat100: TimeSpan.FromHours(12),
            CoverImageUrl: "https://example.com/cover.jpg",
            BackgroundImageUrl: "https://example.com/background.jpg",
            Screenshots: new[] { "https://example.com/screenshot1.jpg" },
            UserScore: 8.5f);

        return Task.FromResult(Result.Success<GameMetadata>(metadata));
    }

    private async Task SayHelloAsync()
    {
        _logger?.LogInformation("Hello from Example Plugin!");
        // In a real plugin, this might show a dialog or perform some action
        await Task.CompletedTask;
    }
}
