using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;
using SaveState.Core.Plugins.Services;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace SaveState.Infrastructure.Plugins;

/// <summary>
/// Implementation of the plugin manager.
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _pluginsDirectory;
    private readonly ConcurrentDictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly List<IGameProvider> _gameProviders = new();
    private readonly List<IMetadataScraper> _metadataScrapers = new();
    private readonly List<ITheme> _themes = new();
    private readonly List<IImporter> _importers = new();
    private readonly List<IExporter> _exporters = new();
    private readonly List<IUIPanel> _uiPanels = new();

    public PluginManager(
        ILogger<PluginManager> logger,
        IServiceProvider serviceProvider,
        string? pluginsDirectory = null)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _pluginsDirectory = pluginsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
    }

    public async Task<Result<IReadOnlyList<PluginInfo>>> DiscoverPluginsAsync(CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(_pluginsDirectory))
            {
                Directory.CreateDirectory(_pluginsDirectory);
                return Result<IReadOnlyList<PluginInfo>>.Success(Array.Empty<PluginInfo>());
            }

            var pluginFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);
            var discoveredPlugins = new List<PluginInfo>();

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(pluginFile);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                        .ToList();

                    foreach (var pluginType in pluginTypes)
                    {
                        try
                        {
                            // Create a temporary instance to get plugin info
                            if (Activator.CreateInstance(pluginType) is IPlugin tempPlugin)
                            {
                                var pluginInfo = new PluginInfo(
                                    Id: tempPlugin.Id,
                                    Name: tempPlugin.Name,
                                    Version: tempPlugin.Version,
                                    Author: tempPlugin.Author,
                                    Description: tempPlugin.Description,
                                    IsEnabled: _loadedPlugins.ContainsKey(tempPlugin.Id),
                                    IsLoaded: _loadedPlugins.ContainsKey(tempPlugin.Id),
                                    Path: pluginFile,
                                    Capabilities: tempPlugin.Capabilities);

                                discoveredPlugins.Add(pluginInfo);

                                // Clean up the temporary instance
                                if (tempPlugin is IAsyncDisposable asyncDisposable)
                                {
                                    await asyncDisposable.DisposeAsync();
                                }
                                else if (tempPlugin is IDisposable disposable)
                                {
                                    disposable.Dispose();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to load plugin type {Type} from {File}", pluginType.FullName, pluginFile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load assembly {File}", pluginFile);
                }
            }

            return Result<IReadOnlyList<PluginInfo>>.Success(discoveredPlugins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover plugins");
            return Result<IReadOnlyList<PluginInfo>>.Failure("Failed to discover plugins", ErrorType.Internal);
        }
    }

    public async Task<Result<bool>> LoadPluginAsync(string pluginPath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(pluginPath))
            {
                return Result<bool>.Failure("Plugin file not found", ErrorType.NotFound);
            }

            var assembly = Assembly.LoadFrom(pluginPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();

            if (!pluginTypes.Any())
            {
                return Result<bool>.Failure("No plugin types found in assembly", ErrorType.Validation);
            }

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                    var pluginContext = new PluginContext(
                        _serviceProvider,
                        _logger,
                        Path.Combine(_pluginsDirectory, plugin.Id),
                        Path.GetDirectoryName(pluginPath)!,
                        this);

                    await plugin.InitializeAsync(pluginContext, ct);

                    var pluginInfo = new PluginInfo(
                        Id: plugin.Id,
                        Name: plugin.Name,
                        Version: plugin.Version,
                        Author: plugin.Author,
                        Description: plugin.Description,
                        IsEnabled: true,
                        IsLoaded: true,
                        Path: pluginPath,
                        Capabilities: plugin.Capabilities);

                    var loadedPlugin = new LoadedPlugin(plugin, pluginInfo, pluginContext, assembly);
                    _loadedPlugins[plugin.Id] = loadedPlugin;

                    RegisterPluginCapabilities(plugin, pluginContext);

                    _logger.LogInformation("Loaded plugin {Name} v{Version} by {Author}", plugin.Name, plugin.Version, plugin.Author);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize plugin type {Type}", pluginType.FullName);
                    return Result<bool>.Failure($"Failed to initialize plugin: {ex.Message}", ErrorType.Internal);
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from {Path}", pluginPath);
            return Result<bool>.Failure($"Failed to load plugin: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<bool>> UnloadPluginAsync(string pluginId, CancellationToken ct = default)
    {
        try
        {
            if (!_loadedPlugins.TryRemove(pluginId, out var loadedPlugin))
            {
                return Result<bool>.Failure("Plugin not found", ErrorType.NotFound);
            }

            // Shutdown the plugin
            await loadedPlugin.Plugin.ShutdownAsync(ct);

            // Unregister capabilities
            UnregisterPluginCapabilities(loadedPlugin.Plugin);

            // Clean up resources
            if (loadedPlugin.Plugin is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (loadedPlugin.Plugin is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _logger.LogInformation("Unloaded plugin {Name}", loadedPlugin.Plugin.Name);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin {Id}", pluginId);
            return Result<bool>.Failure($"Failed to unload plugin: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<bool>> EnablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            loadedPlugin.IsEnabled = true;
            RegisterPluginCapabilities(loadedPlugin.Plugin, loadedPlugin.Context);
            _logger.LogInformation("Enabled plugin {Name}", loadedPlugin.Plugin.Name);
            return Task.FromResult(Result<bool>.Success(true));
        }

        return Task.FromResult(Result<bool>.Failure("Plugin not found", ErrorType.NotFound));
    }

    public Task<Result<bool>> DisablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            loadedPlugin.IsEnabled = false;
            UnregisterPluginCapabilities(loadedPlugin.Plugin);
            _logger.LogInformation("Disabled plugin {Name}", loadedPlugin.Plugin.Name);
            return Task.FromResult(Result<bool>.Success(true));
        }

        return Task.FromResult(Result<bool>.Failure("Plugin not found", ErrorType.NotFound));
    }

    public IReadOnlyList<PluginInfo> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.Select(lp => lp.Info).ToList();
    }

    public PluginInfo? GetPluginInfo(string pluginId)
    {
        return _loadedPlugins.TryGetValue(pluginId, out var loadedPlugin) ? loadedPlugin.Info : null;
    }

    public Task<Result<bool>> InstallPluginAsync(string packagePath, CancellationToken ct = default)
    {
        // For now, just copy the file to the plugins directory
        // In a real implementation, this would extract from a package format
        try
        {
            var fileName = Path.GetFileName(packagePath);
            var targetPath = Path.Combine(_pluginsDirectory, fileName);

            File.Copy(packagePath, targetPath, true);

            _logger.LogInformation("Installed plugin package to {Path}", targetPath);

            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin package");
            return Task.FromResult(Result<bool>.Failure($"Failed to install plugin: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result<bool>> UninstallPluginAsync(string pluginId, CancellationToken ct = default)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            try
            {
                // First unload the plugin
                var unloadResult = await UnloadPluginAsync(pluginId, ct);
                if (!unloadResult.IsSuccess)
                {
                    return Result<bool>.Failure(unloadResult.Error!, unloadResult.ErrorType);
                }

                // Delete the plugin file
                if (File.Exists(loadedPlugin.Info.Path))
                {
                    File.Delete(loadedPlugin.Info.Path);
                }

                _logger.LogInformation("Uninstalled plugin {Name}", loadedPlugin.Plugin.Name);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to uninstall plugin {Id}", pluginId);
                return Result<bool>.Failure($"Failed to uninstall plugin: {ex.Message}", ErrorType.Internal);
            }
        }

        return Result<bool>.Failure("Plugin not found", ErrorType.NotFound);
    }

    public async Task<Result<bool>> ReloadPluginsAsync(CancellationToken ct = default)
    {
        try
        {
            var loadedPluginIds = _loadedPlugins.Keys.ToList();

            // Unload all plugins
            foreach (var pluginId in loadedPluginIds)
            {
                await UnloadPluginAsync(pluginId, ct);
            }

            // Discover and reload plugins
            var discoveredPlugins = await DiscoverPluginsAsync(ct);
            if (!discoveredPlugins.IsSuccess)
            {
                return Result<bool>.Failure(discoveredPlugins.Error!, discoveredPlugins.ErrorType);
            }

            foreach (var pluginInfo in discoveredPlugins.Value)
            {
                if (pluginInfo.IsEnabled)
                {
                    await LoadPluginAsync(pluginInfo.Path, ct);
                }
            }

            _logger.LogInformation("Reloaded all plugins");

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload plugins");
            return Result<bool>.Failure($"Failed to reload plugins: {ex.Message}", ErrorType.Internal);
        }
    }

    public IReadOnlyList<IGameProvider> GetGameProviders() => _gameProviders;
    public IReadOnlyList<IMetadataScraper> GetMetadataScrapers() => _metadataScrapers;
    public IReadOnlyList<ITheme> GetThemes() => _themes;
    public IReadOnlyList<IImporter> GetImporters() => _importers;
    public IReadOnlyList<IExporter> GetExporters() => _exporters;
    public IReadOnlyList<IUIPanel> GetUIPanels() => _uiPanels;

    public async Task SendEventToPluginsAsync(PluginEventType eventType, object? data = null)
    {
        var eventArgs = new PluginEventArgs(eventType, data);

        foreach (var loadedPlugin in _loadedPlugins.Values.Where(lp => lp.IsEnabled))
        {
            try
            {
                loadedPlugin.Context.HandleEvent(eventType, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {Name} failed to handle event {EventType}",
                    loadedPlugin.Plugin.Name, eventType);
            }
        }

        await Task.CompletedTask; // Make method async
    }

    private void RegisterPluginCapabilities(IPlugin plugin, IPluginContext context)
    {
        if (plugin is IGameProvider gameProvider)
        {
            _gameProviders.Add(gameProvider);
            _logger.LogDebug("Registered game provider: {Name}", gameProvider.ProviderName);
        }

        if (plugin is IMetadataScraper scraper)
        {
            _metadataScrapers.Add(scraper);
            _logger.LogDebug("Registered metadata scraper: {Name}", scraper.ScraperName);
        }

        if (plugin is ITheme theme)
        {
            _themes.Add(theme);
            _logger.LogDebug("Registered theme: {Name}", theme.ThemeName);
        }

        if (plugin is IImporter importer)
        {
            _importers.Add(importer);
            _logger.LogDebug("Registered importer: {Name}", importer.ImporterName);
        }

        if (plugin is IExporter exporter)
        {
            _exporters.Add(exporter);
            _logger.LogDebug("Registered exporter: {Name}", exporter.ExporterName);
        }

        if (plugin is IUIPanel uiPanel)
        {
            _uiPanels.Add(uiPanel);
            _logger.LogDebug("Registered UI panel: {Name}", uiPanel.PanelName);
        }
    }

    private void UnregisterPluginCapabilities(IPlugin plugin)
    {
        if (plugin is IGameProvider gameProvider)
        {
            _gameProviders.Remove(gameProvider);
        }

        if (plugin is IMetadataScraper scraper)
        {
            _metadataScrapers.Remove(scraper);
        }

        if (plugin is ITheme theme)
        {
            _themes.Remove(theme);
        }

        if (plugin is IImporter importer)
        {
            _importers.Remove(importer);
        }

        if (plugin is IExporter exporter)
        {
            _exporters.Remove(exporter);
        }

        if (plugin is IUIPanel uiPanel)
        {
            _uiPanels.Remove(uiPanel);
        }
    }
}
