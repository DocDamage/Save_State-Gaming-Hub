using SaveState.Core.Common;
using SaveState.Core.Plugins.DTOs;

namespace SaveState.Core.Plugins.Services;

/// <summary>
/// Service for discovering, browsing, and installing plugins from the marketplace.
/// </summary>
public interface IPluginMarketplaceService
{
    /// <summary>
    /// Gets all available plugins from the marketplace.
    /// </summary>
    Task<Result<List<PluginMarketplaceEntry>>> GetAvailablePluginsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets featured plugins from the marketplace.
    /// </summary>
    Task<Result<List<PluginMarketplaceEntry>>> GetFeaturedPluginsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets plugins by category.
    /// </summary>
    Task<Result<List<PluginMarketplaceEntry>>> GetPluginsByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>
    /// Searches for plugins by keyword.
    /// </summary>
    Task<Result<List<PluginMarketplaceEntry>>> SearchPluginsAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Gets details for a specific plugin.
    /// </summary>
    Task<Result<PluginMarketplaceEntry>> GetPluginDetailsAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Installs a plugin from the marketplace.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to install</param>
    /// <param name="progress">Progress callback for installation (0.0 to 1.0)</param>
    Task<Result<string>> InstallPluginAsync(string pluginId, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Uninstalls a previously installed plugin.
    /// </summary>
    Task<Result> UninstallPluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Updates an installed plugin to the latest version.
    /// </summary>
    Task<Result> UpdatePluginAsync(string pluginId, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Gets list of installed plugins.
    /// </summary>
    Task<Result<List<string>>> GetInstalledPluginsAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if a plugin has an available update.
    /// </summary>
    Task<Result<bool>> HasUpdateAsync(string pluginId, CancellationToken ct = default);
}
