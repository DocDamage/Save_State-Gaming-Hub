using Microsoft.Extensions.Logging;

namespace SaveState.Core.Plugins;

/// <summary>
/// Core interface that all plugins must implement.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the unique identifier for this plugin.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the author of the plugin.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets an optional description of the plugin.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the capabilities this plugin provides.
    /// </summary>
    PluginCapabilities Capabilities { get; }

    /// <summary>
    /// Initializes the plugin with the given context.
    /// </summary>
    /// <param name="context">The plugin context providing access to services and utilities.</param>
    /// <param name="ct">Cancellation token.</param>
    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);

    /// <summary>
    /// Shuts down the plugin and cleans up resources.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets whether this plugin supports hot reload.
    /// If true, the plugin can be unloaded and reloaded without restarting the application.
    /// </summary>
    bool CanHotReload => true;

    /// <summary>
    /// Prepares the plugin for hot reload by serializing any state that should be preserved.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Serialized state that will be passed to the new instance, or null if no state.</returns>
    Task<byte[]?> PrepareForHotReloadAsync(CancellationToken ct = default) => Task.FromResult<byte[]?>(null);

    /// <summary>
    /// Restores state after hot reload from a previous instance.
    /// </summary>
    /// <param name="state">State serialized from the previous instance.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RestoreFromHotReloadAsync(byte[]? state, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>
/// Flags enum defining what capabilities a plugin can provide.
/// </summary>
[Flags]
public enum PluginCapabilities
{
    /// <summary>
    /// No capabilities.
    /// </summary>
    None = 0,

    /// <summary>
    /// Plugin can provide games from external sources (e.g., itch.io, Humble Bundle).
    /// </summary>
    GameProvider = 1 << 0,

    /// <summary>
    /// Plugin can scrape metadata from external sources.
    /// </summary>
    MetadataScraper = 1 << 1,

    /// <summary>
    /// Plugin provides UI themes.
    /// </summary>
    ThemeProvider = 1 << 2,

    /// <summary>
    /// Plugin can import data from other applications.
    /// </summary>
    Importer = 1 << 3,

    /// <summary>
    /// Plugin can export data to various formats.
    /// </summary>
    Exporter = 1 << 4,

    /// <summary>
    /// Plugin provides UI extensions.
    /// </summary>
    UIExtension = 1 << 5,

    /// <summary>
    /// Plugin provides AI-powered features.
    /// </summary>
    AIService = 1 << 6,

    /// <summary>
    /// Plugin provides cloud storage integration.
    /// </summary>
    CloudStorage = 1 << 7,

    /// <summary>
    /// Plugin provides social features.
    /// </summary>
    SocialFeatures = 1 << 8,

    /// <summary>
    /// Plugin provides input/controller features.
    /// </summary>
    InputProvider = 1 << 9,

    /// <summary>
    /// Plugin provides performance monitoring.
    /// </summary>
    PerformanceMonitor = 1 << 10,

    /// <summary>
    /// Plugin provides save state management.
    /// </summary>
    SaveStateProvider = 1 << 11,

    /// <summary>
    /// Plugin provides system optimization features.
    /// </summary>
    SystemOptimization = 1 << 12,

    /// <summary>
    /// Plugin provides launch experience enhancements.
    /// </summary>
    LaunchExperience = 1 << 13,

    /// <summary>
    /// Plugin provides macro recording and playback.
    /// </summary>
    MacroSystem = 1 << 14,

    /// <summary>
    /// Plugin provides Steam Deck integration features.
    /// </summary>
    SteamDeckIntegration = 1 << 15,

    /// <summary>
    /// Plugin provides battery optimization features.
    /// </summary>
    BatteryOptimization = 1 << 16,

    /// <summary>
    /// Plugin provides touch control features.
    /// </summary>
    TouchControls = 1 << 17,

    /// <summary>
    /// Plugin provides cloud gaming features.
    /// </summary>
    CloudGaming = 1 << 18,

    /// <summary>
    /// Plugin provides game memory intelligence features.
    /// </summary>
    MemoryIntelligence = 1 << 19,

    /// <summary>
    /// Plugin provides emulation features.
    /// </summary>
    Emulation = 1 << 20
}

/// <summary>
/// Context provided to plugins during initialization.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Gets access to the service provider for dependency injection.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets a logger for the plugin.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the directory where plugin data should be stored.
    /// </summary>
    string DataDirectory { get; }

    /// <summary>
    /// Gets the directory where the plugin assembly is located.
    /// </summary>
    string PluginDirectory { get; }

    /// <summary>
    /// Registers a menu item in the application's UI.
    /// </summary>
    /// <param name="item">The menu item to register.</param>
    /// <returns>True if registration was successful.</returns>
    Task<bool> RegisterMenuItemAsync(PluginMenuItem item);

    /// <summary>
    /// Registers a game provider with the system.
    /// </summary>
    /// <param name="provider">The game provider to register.</param>
    /// <returns>True if registration was successful.</returns>
    Task<bool> RegisterGameProviderAsync(IGameProvider provider);

    /// <summary>
    /// Registers a metadata scraper with the system.
    /// </summary>
    /// <param name="scraper">The metadata scraper to register.</param>
    /// <returns>True if registration was successful.</returns>
    Task<bool> RegisterMetadataScraperAsync(IMetadataScraper scraper);

    /// <summary>
    /// Registers a UI theme with the system.
    /// </summary>
    /// <param name="theme">The theme to register.</param>
    /// <returns>True if registration was successful.</returns>
    Task<bool> RegisterThemeAsync(ITheme theme);

    /// <summary>
    /// Reports progress during long-running operations.
    /// </summary>
    /// <param name="message">Progress message.</param>
    /// <param name="progress">Progress value between 0 and 1.</param>
    void ReportProgress(string message, float progress);

    /// <summary>
    /// Handles an event sent to the plugin.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <param name="data">Optional event data.</param>
    void HandleEvent(PluginEventType eventType, object? data = null);

    /// <summary>
    /// Event raised when the plugin wants to send events back to the system.
    /// </summary>
    event EventHandler<PluginEventArgs>? EventReceived;
}

/// <summary>
/// Arguments for plugin events.
/// </summary>
public sealed class PluginEventArgs : EventArgs
{
    /// <summary>
    /// Gets the event type.
    /// </summary>
    public PluginEventType EventType { get; }

    /// <summary>
    /// Gets optional event data.
    /// </summary>
    public object? Data { get; }

    public PluginEventArgs(PluginEventType eventType, object? data = null)
    {
        EventType = eventType;
        Data = data;
    }
}

/// <summary>
/// Types of events that can be sent to plugins.
/// </summary>
public enum PluginEventType
{
    /// <summary>
    /// Application is starting up.
    /// </summary>
    ApplicationStarting,

    /// <summary>
    /// Application is shutting down.
    /// </summary>
    ApplicationShutdown,

    /// <summary>
    /// A game was launched.
    /// </summary>
    GameLaunched,

    /// <summary>
    /// A game was closed.
    /// </summary>
    GameClosed,

    /// <summary>
    /// User preferences changed.
    /// </summary>
    PreferencesChanged,

    /// <summary>
    /// Custom plugin-specific event.
    /// </summary>
    Custom
}

/// <summary>
/// Represents a menu item that can be added to the UI by plugins.
/// </summary>
public sealed record PluginMenuItem(
    string Id,
    string Label,
    string? Icon,
    int SortOrder,
    Func<Task> Action);

/// <summary>
/// Information about a loaded plugin.
/// </summary>
public sealed record PluginInfo(
    string Id,
    string Name,
    string Version,
    string Author,
    string? Description,
    bool IsEnabled,
    bool IsLoaded,
    string Path,
    PluginCapabilities Capabilities,
    IReadOnlyList<PluginDependency>? Dependencies = null,
    IReadOnlyList<PluginConflict>? Conflicts = null,
    bool CanHotReload = true,
    bool HasSettings = false);

