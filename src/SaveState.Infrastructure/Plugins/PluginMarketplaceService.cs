using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins.DTOs;
using SaveState.Core.Plugins.Services;

namespace SaveState.Infrastructure.Plugins;

/// <summary>
/// Implementation of plugin marketplace service for discovering and installing plugins.
/// </summary>
public partial class PluginMarketplaceService : IPluginMarketplaceService
{
    private readonly HttpClient _httpClient;
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<PluginMarketplaceService> _logger;
    private readonly string _pluginsDirectory;
    private readonly string _marketplaceUrl = "https://api.savestate.app/plugins/v1"; // Production marketplace API
    private const bool UseRealMarketplace = false; // Toggle for production deployment

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PluginMarketplaceService(
        HttpClient httpClient,
        IPluginManager pluginManager,
        ILogger<PluginMarketplaceService> logger)
    {
        _httpClient = httpClient;
        _pluginManager = pluginManager;
        _logger = logger;

        // Get plugins directory from app data
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _pluginsDirectory = Path.Combine(appDataPath, "SaveState", "Plugins");
        Directory.CreateDirectory(_pluginsDirectory);
    }

    public async Task<Result<List<PluginMarketplaceEntry>>> GetAvailablePluginsAsync(CancellationToken ct = default)
    {
        try
        {
            LogFetchingPlugins(_logger);

            // Use mock data until marketplace API is deployed
            if (!UseRealMarketplace)
            {
                var mockPlugins = GetMockMarketplacePlugins();
                LogPluginsFetched(_logger, mockPlugins.Count);
                return Result.Success(mockPlugins);
            }

            // Production marketplace API integration
            var endpoint = $"{_marketplaceUrl}/plugins";
            LogCallingMarketplaceApi(_logger, endpoint);

            using var response = await _httpClient.GetAsync(endpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = $"Marketplace API returned {response.StatusCode}";
                LogMarketplaceApiFailed(_logger, (int)response.StatusCode, error);
                return Result.Failure<List<PluginMarketplaceEntry>>(error);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var plugins = JsonSerializer.Deserialize<List<PluginMarketplaceEntry>>(json, JsonOptions);

            if (plugins == null)
            {
                LogMarketplaceParsingFailed(_logger);
                return Result.Failure<List<PluginMarketplaceEntry>>("Failed to parse marketplace response");
            }

            LogPluginsFetched(_logger, plugins.Count);
            return Result.Success(plugins);
        }
        catch (HttpRequestException ex)
        {
            LogMarketplaceConnectionFailed(_logger, ex);
            return Result.Failure<List<PluginMarketplaceEntry>>($"Failed to connect to marketplace: {ex.Message}");
        }
        catch (Exception ex)
        {
            LogFetchPluginsFailed(_logger, ex);
            return Result.Failure<List<PluginMarketplaceEntry>>($"Failed to fetch plugins: {ex.Message}");
        }
    }

    public async Task<Result<List<PluginMarketplaceEntry>>> GetFeaturedPluginsAsync(CancellationToken ct = default)
    {
        var allPluginsResult = await GetAvailablePluginsAsync(ct);
        if (!allPluginsResult.IsSuccess || allPluginsResult.Value == null)
        {
            return allPluginsResult;
        }

        // Return top 5 by rating and download count
        var featured = allPluginsResult.Value
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.DownloadCount)
            .Take(5)
            .ToList();

        return Result.Success(featured);
    }

    public async Task<Result<List<PluginMarketplaceEntry>>> GetPluginsByCategoryAsync(string category, CancellationToken ct = default)
    {
        var allPluginsResult = await GetAvailablePluginsAsync(ct);
        if (!allPluginsResult.IsSuccess || allPluginsResult.Value == null)
        {
            return allPluginsResult;
        }

        if (category == PluginCategories.All)
        {
            return allPluginsResult;
        }

        var filtered = allPluginsResult.Value
            .Where(p => p.Category == category)
            .ToList();

        return Result.Success(filtered);
    }

    public async Task<Result<List<PluginMarketplaceEntry>>> SearchPluginsAsync(string query, CancellationToken ct = default)
    {
        var allPluginsResult = await GetAvailablePluginsAsync(ct);
        if (!allPluginsResult.IsSuccess || allPluginsResult.Value == null)
        {
            return allPluginsResult;
        }

        var searchResults = allPluginsResult.Value
            .Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Result.Success(searchResults);
    }

    public async Task<Result<PluginMarketplaceEntry>> GetPluginDetailsAsync(string pluginId, CancellationToken ct = default)
    {
        var allPluginsResult = await GetAvailablePluginsAsync(ct);
        if (!allPluginsResult.IsSuccess || allPluginsResult.Value == null)
        {
            return Result.Failure<PluginMarketplaceEntry>("Failed to fetch plugins");
        }

        var plugin = allPluginsResult.Value.FirstOrDefault(p => p.Id == pluginId);
        if (plugin == null)
        {
            return Result.Failure<PluginMarketplaceEntry>($"Plugin '{pluginId}' not found");
        }

        return Result.Success(plugin);
    }

    public async Task<Result<string>> InstallPluginAsync(string pluginId, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            LogInstallingPlugin(_logger, pluginId);
            progress?.Report(0.1);

            // Get plugin details
            var pluginResult = await GetPluginDetailsAsync(pluginId, ct);
            if (!pluginResult.IsSuccess || pluginResult.Value == null)
            {
                LogPluginNotFound(_logger, pluginId);
                return Result.Failure<string>("Plugin not found");
            }

            var plugin = pluginResult.Value;
            progress?.Report(0.2);

            // Download plugin package
            LogDownloadingPlugin(_logger, plugin.DownloadUrl);
            var downloadPath = Path.Combine(Path.GetTempPath(), $"{pluginId}_{Guid.NewGuid()}.zip");

            try
            {
                using var response = await _httpClient.GetAsync(plugin.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var fileStream = File.Create(downloadPath);
                await CopyStreamWithProgressAsync(
                    await response.Content.ReadAsStreamAsync(ct),
                    fileStream,
                    totalBytes,
                    progress,
                    0.2, // Start progress
                    0.7, // End progress
                    ct);

                LogPluginDownloaded(_logger, pluginId, new FileInfo(downloadPath).Length);
            }
            catch (HttpRequestException ex)
            {
                LogDownloadFailed(_logger, pluginId, ex);
                return Result.Failure<string>($"Download failed: {ex.Message}");
            }

            progress?.Report(0.7);

            // Extract to plugins directory
            var installPath = Path.Combine(_pluginsDirectory, pluginId);
            if (Directory.Exists(installPath))
            {
                LogRemovingOldVersion(_logger, pluginId);
                Directory.Delete(installPath, recursive: true);
            }

            Directory.CreateDirectory(installPath);
            LogExtractingPlugin(_logger, installPath);
            ZipFile.ExtractToDirectory(downloadPath, installPath);
            progress?.Report(0.9);

            // Clean up download
            File.Delete(downloadPath);

            // Register with plugin manager (if available)
            try
            {
                await _pluginManager.LoadPluginAsync(installPath, ct);
            }
            catch (Exception ex)
            {
                LogPluginLoadFailed(_logger, pluginId, ex);
                // Continue - plugin is installed even if load fails
            }

            progress?.Report(1.0);

            LogPluginInstalled(_logger, pluginId, installPath);
            return Result.Success(installPath);
        }
        catch (Exception ex)
        {
            LogInstallPluginFailed(_logger, pluginId, ex);
            return Result.Failure<string>($"Installation failed: {ex.Message}");
        }
    }

    public Task<Result> UninstallPluginAsync(string pluginId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Uninstalling plugin: {PluginId}", pluginId);

            var pluginPath = Path.Combine(_pluginsDirectory, pluginId);
            if (!Directory.Exists(pluginPath))
            {
                return Task.FromResult(Result.Failure($"Plugin '{pluginId}' is not installed"));
            }

            // Unload from plugin manager
            // await _pluginManager.UnloadPluginAsync(pluginId);

            // Delete plugin directory
            Directory.Delete(pluginPath, recursive: true);

            _logger.LogInformation("Successfully uninstalled plugin: {PluginId}", pluginId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall plugin: {PluginId}", pluginId);
            return Task.FromResult(Result.Failure($"Uninstallation failed: {ex.Message}"));
        }
    }

    public async Task<Result> UpdatePluginAsync(string pluginId, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating plugin: {PluginId}", pluginId);

            // Uninstall old version
            var uninstallResult = await UninstallPluginAsync(pluginId, ct);
            if (!uninstallResult.IsSuccess)
            {
                return uninstallResult;
            }

            progress?.Report(0.3);

            // Install new version
            var installResult = await InstallPluginAsync(pluginId, progress, ct);
            if (!installResult.IsSuccess)
            {
                return Result.Failure($"Update failed: {installResult.Error}");
            }

            _logger.LogInformation("Successfully updated plugin: {PluginId}", pluginId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update plugin: {PluginId}", pluginId);
            return Result.Failure($"Update failed: {ex.Message}");
        }
    }

    public Task<Result<List<string>>> GetInstalledPluginsAsync(CancellationToken ct = default)
    {
        try
        {
            var installed = Directory.GetDirectories(_pluginsDirectory)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .ToList();

            return Task.FromResult(Result.Success(installed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get installed plugins");
            return Task.FromResult(Result.Failure<List<string>>($"Failed to get installed plugins: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> HasUpdateAsync(string pluginId, CancellationToken ct = default)
    {
        try
        {
            LogCheckingForUpdate(_logger, pluginId);

            // Check if plugin is installed
            var installedResult = await GetInstalledPluginsAsync(ct);
            if (!installedResult.IsSuccess || installedResult.Value == null)
            {
                LogUpdateCheckFailed(_logger, pluginId, "Failed to get installed plugins");
                return Result.Failure<bool>("Failed to check installed plugins");
            }

            if (!installedResult.Value.Contains(pluginId))
            {
                LogPluginNotInstalled(_logger, pluginId);
                return Result.Success(false);
            }

            // Read installed plugin version from plugin.json manifest
            var pluginPath = Path.Combine(_pluginsDirectory, pluginId);
            var manifestPath = Path.Combine(pluginPath, "plugin.json");

            if (!File.Exists(manifestPath))
            {
                LogManifestNotFound(_logger, pluginId);
                return Result.Success(false);
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath, ct);
            var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson, JsonOptions);

            if (manifest == null || string.IsNullOrEmpty(manifest.Version))
            {
                LogInvalidManifest(_logger, pluginId);
                return Result.Success(false);
            }

            // Fetch marketplace version
            var marketplaceResult = await GetPluginDetailsAsync(pluginId, ct);
            if (!marketplaceResult.IsSuccess || marketplaceResult.Value == null)
            {
                LogMarketplaceVersionCheckFailed(_logger, pluginId);
                return Result.Failure<bool>("Failed to fetch marketplace version");
            }

            // Compare versions using semantic versioning
            var hasUpdate = CompareVersions(manifest.Version, marketplaceResult.Value.Version) < 0;

            if (hasUpdate)
            {
                LogUpdateAvailable(_logger, pluginId, manifest.Version, marketplaceResult.Value.Version);
            }
            else
            {
                LogNoUpdateAvailable(_logger, pluginId, manifest.Version);
            }

            return Result.Success(hasUpdate);
        }
        catch (Exception ex)
        {
            LogUpdateCheckFailed(_logger, pluginId, ex.Message);
            LogUpdateCheckException(_logger, pluginId, ex);
            return Result.Failure<bool>($"Update check failed: {ex.Message}");
        }
    }

    private List<PluginMarketplaceEntry> GetMockMarketplacePlugins()
    {
        return new List<PluginMarketplaceEntry>
        {
            new("steam-enhanced", "Steam Enhanced",
                "Enhanced Steam integration with achievements, playtime tracking, and cloud save sync",
                "SaveState Team", "1.2.0", PluginCategories.Integration,
                "https://example.com/icons/steam.png", "https://example.com/downloads/steam-enhanced.zip",
                5420, 4.8, 156, DateTime.Now.AddMonths(-6), DateTime.Now.AddDays(-10),
                new List<string> { "steam", "integration", "cloud-sync" },
                new List<string> { "https://example.com/screenshots/1.png" },
                "https://github.com/savestate/steam-enhanced",
                "https://github.com/savestate/steam-enhanced",
                true, true, "2.0.0", 2_450_000),

            new("dark-mode-plus", "Dark Mode Plus",
                "Advanced dark theme with customizable colors and multiple variants",
                "ThemeCreators", "2.1.0", PluginCategories.Themes,
                "https://example.com/icons/dark-mode.png", "https://example.com/downloads/dark-mode.zip",
                12340, 4.9, 432, DateTime.Now.AddMonths(-3), DateTime.Now.AddDays(-5),
                new List<string> { "theme", "dark-mode", "ui" },
                new List<string> { "https://example.com/screenshots/2.png" },
                "https://github.com/themes/dark-mode-plus",
                "https://github.com/themes/dark-mode-plus",
                true, true, "2.0.0", 850_000),

            new("retroarch-connector", "RetroArch Connector",
                "Seamless RetroArch integration for emulated games",
                "EmuDev", "1.5.2", PluginCategories.Emulation,
                "https://example.com/icons/retroarch.png", "https://example.com/downloads/retroarch.zip",
                8900, 4.7, 287, DateTime.Now.AddMonths(-8), DateTime.Now.AddDays(-15),
                new List<string> { "retroarch", "emulation", "retro" },
                new List<string> { "https://example.com/screenshots/3.png" },
                "https://github.com/emulation/retroarch-connector",
                "https://github.com/emulation/retroarch-connector",
                true, true, "2.0.0", 1_750_000),

            new("twitch-alerts", "Twitch Stream Alerts",
                "Live streaming notifications and viewer stats for Twitch streamers",
                "StreamTools", "1.0.5", PluginCategories.Social,
                "https://example.com/icons/twitch.png", "https://example.com/downloads/twitch-alerts.zip",
                3210, 4.6, 98, DateTime.Now.AddMonths(-2), DateTime.Now.AddDays(-3),
                new List<string> { "twitch", "streaming", "alerts" },
                new List<string> { "https://example.com/screenshots/4.png" },
                "https://github.com/streaming/twitch-alerts",
                "https://github.com/streaming/twitch-alerts",
                false, true, "2.0.0", 920_000),

            new("advanced-analytics", "Advanced Analytics Pro",
                "Deep analytics with AI-powered insights and predictions",
                "DataViz Studio", "3.0.1", PluginCategories.Analytics,
                "https://example.com/icons/analytics.png", "https://example.com/downloads/analytics.zip",
                6780, 4.9, 234, DateTime.Now.AddMonths(-4), DateTime.Now.AddDays(-7),
                new List<string> { "analytics", "ai", "statistics", "insights" },
                new List<string> { "https://example.com/screenshots/5.png" },
                "https://github.com/analytics/advanced-pro",
                "https://github.com/analytics/advanced-pro",
                true, true, "2.3.0", 3_200_000)
        };
    }

    #region Helper Methods

    private static async Task CopyStreamWithProgressAsync(
        Stream source,
        Stream destination,
        long totalBytes,
        IProgress<double>? progress,
        double progressStart,
        double progressEnd,
        CancellationToken ct)
    {
        var buffer = new byte[81920]; // 80KB buffer
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;

            if (progress != null && totalBytes > 0)
            {
                var downloadProgress = (double)totalRead / totalBytes;
                var overallProgress = progressStart + (downloadProgress * (progressEnd - progressStart));
                progress.Report(overallProgress);
            }
        }
    }

    private static int CompareVersions(string version1, string version2)
    {
        try
        {
            var v1 = ParseVersion(version1);
            var v2 = ParseVersion(version2);

            // Compare major.minor.patch
            if (v1.Major != v2.Major) return v1.Major.CompareTo(v2.Major);
            if (v1.Minor != v2.Minor) return v1.Minor.CompareTo(v2.Minor);
            return v1.Patch.CompareTo(v2.Patch);
        }
        catch
        {
            // Fallback to string comparison if parsing fails
            return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        // Remove any leading 'v' and trim
        version = version.TrimStart('v').Trim();

        var parts = version.Split('.');
        var major = parts.Length > 0 && int.TryParse(parts[0], out var m) ? m : 0;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2].Split('-')[0], out var p) ? p : 0;

        return (major, minor, patch);
    }

    #endregion

    #region LoggerMessage Definitions

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching available plugins from marketplace")]
    private static partial void LogFetchingPlugins(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetched {Count} plugins from marketplace")]
    private static partial void LogPluginsFetched(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling marketplace API: {Endpoint}")]
    private static partial void LogCallingMarketplaceApi(ILogger logger, string endpoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Marketplace API failed with status {StatusCode}: {Error}")]
    private static partial void LogMarketplaceApiFailed(ILogger logger, int statusCode, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse marketplace response")]
    private static partial void LogMarketplaceParsingFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to connect to marketplace")]
    private static partial void LogMarketplaceConnectionFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to fetch plugins")]
    private static partial void LogFetchPluginsFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Installing plugin: {PluginId}")]
    private static partial void LogInstallingPlugin(ILogger logger, string pluginId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin not found: {PluginId}")]
    private static partial void LogPluginNotFound(ILogger logger, string pluginId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Downloading plugin from {DownloadUrl}")]
    private static partial void LogDownloadingPlugin(ILogger logger, string downloadUrl);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plugin {PluginId} downloaded ({Size} bytes)")]
    private static partial void LogPluginDownloaded(ILogger logger, string pluginId, long size);

    [LoggerMessage(Level = LogLevel.Error, Message = "Download failed for plugin {PluginId}")]
    private static partial void LogDownloadFailed(ILogger logger, string pluginId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Removing old version of plugin: {PluginId}")]
    private static partial void LogRemovingOldVersion(ILogger logger, string pluginId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracting plugin to {InstallPath}")]
    private static partial void LogExtractingPlugin(ILogger logger, string installPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load plugin {PluginId} into manager")]
    private static partial void LogPluginLoadFailed(ILogger logger, string pluginId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plugin {PluginId} installed successfully at {InstallPath}")]
    private static partial void LogPluginInstalled(ILogger logger, string pluginId, string installPath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to install plugin {PluginId}")]
    private static partial void LogInstallPluginFailed(ILogger logger, string pluginId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Checking for updates for plugin: {PluginId}")]
    private static partial void LogCheckingForUpdate(ILogger logger, string pluginId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin {PluginId} is not installed")]
    private static partial void LogPluginNotInstalled(ILogger logger, string pluginId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Manifest not found for plugin: {PluginId}")]
    private static partial void LogManifestNotFound(ILogger logger, string pluginId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid or missing version in manifest for plugin: {PluginId}")]
    private static partial void LogInvalidManifest(ILogger logger, string pluginId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch marketplace version for plugin: {PluginId}")]
    private static partial void LogMarketplaceVersionCheckFailed(ILogger logger, string pluginId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Update available for plugin {PluginId}: {InstalledVersion} -> {MarketplaceVersion}")]
    private static partial void LogUpdateAvailable(ILogger logger, string pluginId, string installedVersion, string marketplaceVersion);

    [LoggerMessage(Level = LogLevel.Information, Message = "No update available for plugin {PluginId} (current version: {Version})")]
    private static partial void LogNoUpdateAvailable(ILogger logger, string pluginId, string version);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Update check failed for plugin {PluginId}: {Error}")]
    private static partial void LogUpdateCheckFailed(ILogger logger, string pluginId, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Update check exception for plugin {PluginId}")]
    private static partial void LogUpdateCheckException(ILogger logger, string pluginId, Exception ex);

    #endregion
}

/// <summary>
/// Simplified plugin manifest for version checking.
/// </summary>
internal class PluginManifest
{
    public string? Name { get; set; }
    public string? Version { get; set; }
}

