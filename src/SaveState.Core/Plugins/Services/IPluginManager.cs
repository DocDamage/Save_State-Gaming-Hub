using SaveState.Core.Common;

namespace SaveState.Core.Plugins.Services;

/// <summary>
/// Service for managing plugins.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Gets all discovered plugins.
    /// </summary>
    Task<Result<IReadOnlyList<PluginInfo>>> DiscoverPluginsAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads a plugin from the specified path.
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if loading was successful.</returns>
    Task<Result<bool>> LoadPluginAsync(string pluginPath, CancellationToken ct = default);

    /// <summary>
    /// Unloads a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to unload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if unloading was successful.</returns>
    Task<Result<bool>> UnloadPluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Enables a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to enable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if enabling was successful.</returns>
    Task<Result<bool>> EnablePluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Disables a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to disable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if disabling was successful.</returns>
    Task<Result<bool>> DisablePluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Gets information about all loaded plugins.
    /// </summary>
    /// <returns>List of plugin information.</returns>
    IReadOnlyList<PluginInfo> GetLoadedPlugins();

    /// <summary>
    /// Gets information about a specific plugin.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin.</param>
    /// <returns>Plugin information or null if not found.</returns>
    PluginInfo? GetPluginInfo(string pluginId);

    /// <summary>
    /// Installs a plugin from a package file.
    /// </summary>
    /// <param name="packagePath">Path to the plugin package.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if installation was successful.</returns>
    Task<Result<bool>> InstallPluginAsync(string packagePath, CancellationToken ct = default);

    /// <summary>
    /// Uninstalls a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to uninstall.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if uninstallation was successful.</returns>
    Task<Result<bool>> UninstallPluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Reloads all plugins.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if reloading was successful.</returns>
    Task<Result<bool>> ReloadPluginsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all registered game providers.
    /// </summary>
    /// <returns>List of game providers.</returns>
    IReadOnlyList<IGameProvider> GetGameProviders();

    /// <summary>
    /// Gets all registered metadata scrapers.
    /// </summary>
    /// <returns>List of metadata scrapers.</returns>
    IReadOnlyList<IMetadataScraper> GetMetadataScrapers();

    /// <summary>
    /// Gets all registered themes.
    /// </summary>
    /// <returns>List of themes.</returns>
    IReadOnlyList<ITheme> GetThemes();

    /// <summary>
    /// Gets all registered importers.
    /// </summary>
    /// <returns>List of importers.</returns>
    IReadOnlyList<IImporter> GetImporters();

    /// <summary>
    /// Gets all registered exporters.
    /// </summary>
    /// <returns>List of exporters.</returns>
    IReadOnlyList<IExporter> GetExporters();

    /// <summary>
    /// Gets all registered UI panels.
    /// </summary>
    /// <returns>List of UI panels.</returns>
    IReadOnlyList<IUIPanel> GetUIPanels();

    /// <summary>
    /// Gets all menu items currently registered by enabled plugins.
    /// </summary>
    /// <returns>Plugin menu registrations.</returns>
    IReadOnlyList<PluginMenuRegistration> GetRegisteredMenuItems();

    /// <summary>
    /// Sends an event to all loaded plugins.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <param name="data">Optional event data.</param>
    Task SendEventToPluginsAsync(PluginEventType eventType, object? data = null);
}

/// <summary>
/// Represents a loaded plugin instance.
/// </summary>
public sealed class LoadedPlugin
{
    /// <summary>
    /// Gets the plugin instance.
    /// </summary>
    public IPlugin Plugin { get; }

    /// <summary>
    /// Gets the plugin information.
    /// </summary>
    public PluginInfo Info { get; }

    /// <summary>
    /// Gets the plugin context.
    /// </summary>
    public IPluginContext Context { get; }

    /// <summary>
    /// Gets the assembly this plugin was loaded from.
    /// </summary>
    public System.Reflection.Assembly Assembly { get; }

    /// <summary>
    /// Gets whether this plugin is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    public LoadedPlugin(IPlugin plugin, PluginInfo info, IPluginContext context, System.Reflection.Assembly assembly)
    {
        Plugin = plugin;
        Info = info;
        Context = context;
        Assembly = assembly;
        IsEnabled = true;
    }
}

/// <summary>
/// Represents a plugin menu item registration with plugin metadata.
/// </summary>
/// <param name="PluginId">The plugin identifier.</param>
/// <param name="PluginName">The display name of the plugin.</param>
/// <param name="MenuItem">The registered plugin menu item.</param>
public sealed record PluginMenuRegistration(
    string PluginId,
    string PluginName,
    PluginMenuItem MenuItem);
