using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Core.Plugins.Services;

namespace SaveState.Infrastructure.Plugins;

/// <summary>
/// Background service that automatically loads plugins at application startup.
/// </summary>
public class PluginLoaderBackgroundService : BackgroundService
{
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<PluginLoaderBackgroundService> _logger;

    public PluginLoaderBackgroundService(
        IPluginManager pluginManager,
        ILogger<PluginLoaderBackgroundService> logger)
    {
        _pluginManager = pluginManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting plugin discovery and loading");

            // Discover available plugins
            var discoverResult = await _pluginManager.DiscoverPluginsAsync(stoppingToken).ConfigureAwait(false);
            if (!discoverResult.IsSuccess)
            {
                _logger.LogError("Failed to discover plugins: {Error}", discoverResult.Error);
                return;
            }

            var discoveredPlugins = discoverResult.Value;
            _logger.LogInformation("Discovered {Count} plugins", discoveredPlugins.Count);

            // Load all enabled plugins
            foreach (var pluginInfo in discoveredPlugins.Where(p => p.IsEnabled))
            {
                try
                {
                    _logger.LogInformation("Loading plugin: {Name} v{Version} by {Author}",
                        pluginInfo.Name, pluginInfo.Version, pluginInfo.Author);

                    var loadResult = await _pluginManager.LoadPluginAsync(pluginInfo.Path, stoppingToken).ConfigureAwait(false);
                    if (!loadResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to load plugin {Name}: {Error}",
                            pluginInfo.Name, loadResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error loading plugin {Name}", pluginInfo.Name);
                }
            }

            var loadedPlugins = _pluginManager.GetLoadedPlugins();
            _logger.LogInformation("Plugin loading completed. {Count} plugins loaded", loadedPlugins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize plugin system");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Plugin loader background service stopping");

        try
        {
            // Unload all plugins
            var loadedPlugins = _pluginManager.GetLoadedPlugins();
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    await _pluginManager.UnloadPluginAsync(plugin.Id, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error unloading plugin {Id}", plugin.Id);
                }
            }

            _logger.LogInformation("All plugins unloaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during plugin cleanup");
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}