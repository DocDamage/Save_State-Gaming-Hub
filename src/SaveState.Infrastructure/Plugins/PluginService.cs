using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Generic;

namespace SaveState.Infrastructure.Plugins;

/// <summary>
/// Plugin system for extending SaveState functionality.
/// PHASE 7: REQUIRED - Plugin System (Session 5)
/// </summary>
public class PluginService
{
    private readonly ILogger<PluginService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, PluginMetadata> _plugins = new();
    private readonly Dictionary<string, PluginInstance> _loadedPlugins = new();

    public PluginService(ILogger<PluginService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Registers a plugin.
    /// </summary>
    public async Task<Result<PluginMetadata>> RegisterPluginAsync(
        string pluginName,
        string version,
        string author,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Registering plugin: {PluginName}", pluginName);

            var metadata = new PluginMetadata(
                id: Guid.NewGuid().ToString(),
                name: pluginName,
                version: version,
                author: author,
                description: description,
                registeredAt: _timeProvider.UtcNow,
                isEnabled: true,
                minimumVersion: "1.0.0");

            _plugins[pluginName] = metadata;

            _logger.LogInformation("Plugin registered: {PluginName}", pluginName);
            return Result.Success(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register plugin: {PluginName}", pluginName);
            return Result.Failure<PluginMetadata>(
                $"Registration failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Loads a plugin.
    /// </summary>
    public async Task<Result> LoadPluginAsync(
        string pluginName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_plugins.TryGetValue(pluginName, out var metadata))
            {
                return Result.Failure($"Plugin not found: {pluginName}", ErrorType.Validation);
            }

            _logger.LogInformation("Loading plugin: {PluginName}", pluginName);

            var instance = new PluginInstance(
                id: Guid.NewGuid().ToString(),
                metadata: metadata,
                loadedAt: _timeProvider.UtcNow,
                isRunning: true);

            _loadedPlugins[pluginName] = instance;

            _logger.LogInformation("Plugin loaded: {PluginName}", pluginName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin: {PluginName}", pluginName);
            return Result.Failure($"Load failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Unloads a plugin.
    /// </summary>
    public async Task<Result> UnloadPluginAsync(
        string pluginName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_loadedPlugins.TryGetValue(pluginName, out var instance))
            {
                return Result.Failure($"Plugin not loaded: {pluginName}", ErrorType.Validation);
            }

            _logger.LogInformation("Unloading plugin: {PluginName}", pluginName);

            instance.IsRunning = false;
            _loadedPlugins.Remove(pluginName);

            _logger.LogInformation("Plugin unloaded: {PluginName}", pluginName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin: {PluginName}", pluginName);
            return Result.Failure($"Unload failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Enables a plugin.
    /// </summary>
    public async Task<Result> EnablePluginAsync(
        string pluginName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_plugins.TryGetValue(pluginName, out var metadata))
            {
                return Result.Failure("Plugin not found", ErrorType.Validation);
            }

            metadata.IsEnabled = true;
            _logger.LogInformation("Plugin enabled: {PluginName}", pluginName);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable plugin: {PluginName}", pluginName);
            return Result.Failure($"Enable failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Disables a plugin.
    /// </summary>
    public async Task<Result> DisablePluginAsync(
        string pluginName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_plugins.TryGetValue(pluginName, out var metadata))
            {
                return Result.Failure("Plugin not found", ErrorType.Validation);
            }

            metadata.IsEnabled = false;

            if (_loadedPlugins.TryGetValue(pluginName, out var instance))
            {
                instance.IsRunning = false;
            }

            _logger.LogInformation("Plugin disabled: {PluginName}", pluginName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable plugin: {PluginName}", pluginName);
            return Result.Failure($"Disable failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Lists all plugins.
    /// </summary>
    public List<PluginMetadata> GetAllPlugins()
    {
        return _plugins.Values.ToList();
    }

    /// <summary>
    /// Lists loaded plugins.
    /// </summary>
    public List<PluginInstance> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.Where(p => p.IsRunning).ToList();
    }

    /// <summary>
    /// Gets plugin metadata.
    /// </summary>
    public async Task<Result<PluginMetadata>> GetPluginMetadataAsync(
        string pluginName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_plugins.TryGetValue(pluginName, out var metadata))
            {
                return Result.Failure<PluginMetadata>(
                    "Plugin not found",
                    ErrorType.Validation);
            }

            return Result.Success(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get plugin metadata");
            return Result.Failure<PluginMetadata>(
                $"Failed: {ex.Message}",
                ErrorType.Internal);
        }
    }
}

/// <summary>
/// Plugin metadata.
/// </summary>
public class PluginMetadata
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string? Description { get; set; }
    public DateTime RegisteredAt { get; set; }
    public bool IsEnabled { get; set; }
    public string MinimumVersion { get; set; }

    public PluginMetadata(
        string id,
        string name,
        string version,
        string author,
        string? description,
        DateTime registeredAt,
        bool isEnabled,
        string minimumVersion)
    {
        Id = id;
        Name = name;
        Version = version;
        Author = author;
        Description = description;
        RegisteredAt = registeredAt;
        IsEnabled = isEnabled;
        MinimumVersion = minimumVersion;
    }
}

/// <summary>
/// Plugin instance (loaded plugin).
/// </summary>
public class PluginInstance
{
    public string Id { get; set; }
    public PluginMetadata Metadata { get; set; }
    public DateTime LoadedAt { get; set; }
    public bool IsRunning { get; set; }

    public PluginInstance(
        string id,
        PluginMetadata metadata,
        DateTime loadedAt,
        bool isRunning)
    {
        Id = id;
        Metadata = metadata;
        LoadedAt = loadedAt;
        IsRunning = isRunning;
    }
}
