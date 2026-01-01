using Microsoft.Extensions.Logging;
using SaveState.Core.Plugins;
using SaveState.Core.Plugins.Services;

namespace SaveState.Infrastructure.Plugins;

/// <summary>
/// Implementation of the plugin context.
/// </summary>
public class PluginContext : IPluginContext
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly PluginManager _pluginManager;
    private readonly List<PluginMenuItem> _registeredMenuItems = new();

    public IServiceProvider Services => _services;
    public ILogger Logger => _logger;
    public string DataDirectory { get; }
    public string PluginDirectory { get; }

    public event EventHandler<PluginEventArgs>? EventReceived;

    public PluginContext(
        IServiceProvider services,
        ILogger logger,
        string dataDirectory,
        string pluginDirectory,
        PluginManager pluginManager)
    {
        _services = services;
        _logger = logger;
        _pluginManager = pluginManager;
        DataDirectory = dataDirectory;
        PluginDirectory = pluginDirectory;

        // Ensure data directory exists
        Directory.CreateDirectory(dataDirectory);
    }

    public Task<bool> RegisterMenuItemAsync(PluginMenuItem item)
    {
        try
        {
            _registeredMenuItems.Add(item);
            _logger.LogDebug("Plugin registered menu item: {Label}", item.Label);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register menu item {Label}", item.Label);
            return Task.FromResult(false);
        }
    }

    public Task<bool> RegisterGameProviderAsync(IGameProvider provider)
    {
        try
        {
            // The plugin manager will handle registration when the plugin is loaded
            _logger.LogDebug("Plugin registered game provider: {Name}", provider.ProviderName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register game provider {Name}", provider.ProviderName);
            return Task.FromResult(false);
        }
    }

    public Task<bool> RegisterMetadataScraperAsync(IMetadataScraper scraper)
    {
        try
        {
            // The plugin manager will handle registration when the plugin is loaded
            _logger.LogDebug("Plugin registered metadata scraper: {Name}", scraper.ScraperName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register metadata scraper {Name}", scraper.ScraperName);
            return Task.FromResult(false);
        }
    }

    public Task<bool> RegisterThemeAsync(ITheme theme)
    {
        try
        {
            // The plugin manager will handle registration when the plugin is loaded
            _logger.LogDebug("Plugin registered theme: {Name}", theme.ThemeName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register theme {Name}", theme.ThemeName);
            return Task.FromResult(false);
        }
    }

    public void ReportProgress(string message, float progress)
    {
        _logger.LogInformation("Plugin progress: {Message} ({Progress:P0})", message, progress);
    }

    public void HandleEvent(PluginEventType eventType, object? data = null)
    {
        try
        {
            EventReceived?.Invoke(this, new PluginEventArgs(eventType, data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling plugin event {EventType}", eventType);
        }
    }

    /// <summary>
    /// Gets the registered menu items for this plugin.
    /// </summary>
    public IReadOnlyList<PluginMenuItem> GetRegisteredMenuItems() => _registeredMenuItems;
}