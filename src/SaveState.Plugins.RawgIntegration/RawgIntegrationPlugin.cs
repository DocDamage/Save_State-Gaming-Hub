using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.RawgIntegration;

public sealed class RawgIntegrationPlugin : IPlugin
{
    private IPluginContext? _context;

    public string Id => "rawg-integration";
    public string Name => "RAWG.io Integration";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Fetch ratings, release dates, and rich metadata from RAWG.";
    public PluginCapabilities Capabilities => PluginCapabilities.MetadataScraper;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("RAWG Integration Initialized");
        }
        return Task.CompletedTask;
    }

    // Future implementation will implement IMetadataScraper methods
    public Task<string> GetRatingAsync(string gameTitle)
    {
        if (_context?.Logger.IsEnabled(LogLevel.Debug) == true)
        {
             _context.Logger.LogDebug("Fetching RAWG rating for {Game}", gameTitle);
        }
        return Task.FromResult("4.5/5");
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
