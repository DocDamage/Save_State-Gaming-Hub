using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins.DTOs;
using SaveState.Core.Plugins.Services;
using SaveState.Presentation.Models.PluginStore;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.PluginStore;

/// <summary>
/// ViewModel for the Plugin Store - browsing, installing, and managing plugins.
/// </summary>
public partial class PluginStoreViewModel : ObservableObject
{
    private readonly IPluginMarketplaceService _marketplaceService;
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<PluginStoreViewModel> _logger;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<PluginListing> _featuredPlugins = new();

    [ObservableProperty]
    private ObservableCollection<PluginListing> _trendingPlugins = new();

    [ObservableProperty]
    private ObservableCollection<PluginListing> _newPlugins = new();

    [ObservableProperty]
    private ObservableCollection<PluginCategory> _categories = new();

    [ObservableProperty]
    private ObservableCollection<PluginListing> _installedPlugins = new();

    [ObservableProperty]
    private ObservableCollection<PluginListing> _updateAvailablePlugins = new();

    [ObservableProperty]
    private ObservableCollection<PluginListing> _searchResults = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private int _totalInstalled;

    [ObservableProperty]
    private bool _hasUpdates;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private PluginListing? _selectedPlugin;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _selectedSort = "Popular";

    public PluginStoreViewModel(
        IPluginMarketplaceService marketplaceService,
        IPluginManager pluginManager,
        ILogger<PluginStoreViewModel> logger,
        IDialogService dialogService)
    {
        _marketplaceService = marketplaceService;
        _pluginManager = pluginManager;
        _logger = logger;
        _dialogService = dialogService;

        InitializeCategories();
        _ = InitializeAsync();
    }

    /// <summary>
    /// Available sort options.
    /// </summary>
    public List<string> SortOptions { get; } = new()
    {
        "Popular",
        "Newest",
        "Rating",
        "Downloads",
        "Name"
    };

    /// <summary>
    /// Initializes the plugin store by loading all data.
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await LoadFeaturedPluginsAsync();
        await LoadTrendingPluginsAsync();
        await LoadNewPluginsAsync();
        await LoadInstalledPluginsAsync();
    }

    private void InitializeCategories()
    {
        Categories = new ObservableCollection<PluginCategory>
        {
            new() { Id = "themes", Name = "Themes", Icon = "🎨", PluginCount = 42 },
            new() { Id = "games", Name = "Games", Icon = "🎮", PluginCount = 156 },
            new() { Id = "cloud", Name = "Cloud", Icon = "☁️", PluginCount = 28 },
            new() { Id = "media", Name = "Media", Icon = "🎬", PluginCount = 35 },
            new() { Id = "tools", Name = "Tools", Icon = "🛠️", PluginCount = 89 },
            new() { Id = "integration", Name = "Integration", Icon = "🔗", PluginCount = 67 },
            new() { Id = "analytics", Name = "Analytics", Icon = "📊", PluginCount = 23 },
            new() { Id = "social", Name = "Social", Icon = "💬", PluginCount = 31 }
        };
    }

    /// <summary>
    /// Loads plugin categories from the marketplace.
    /// </summary>
    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        try
        {
            // Categories are pre-defined for now, could be loaded from service
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin categories");
        }
    }

    /// <summary>
    /// Loads featured plugins from the marketplace.
    /// </summary>
    [RelayCommand]
    private async Task LoadFeaturedPluginsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _marketplaceService.GetFeaturedPluginsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                FeaturedPlugins = new ObservableCollection<PluginListing>(
                    result.Value.Select(MapToPluginListing));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load featured plugins");
            ErrorMessage = "Failed to load featured plugins";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads trending plugins.
    /// </summary>
    [RelayCommand]
    private async Task LoadTrendingPluginsAsync()
    {
        try
        {
            var result = await _marketplaceService.GetAvailablePluginsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                // Sort by download count for trending
                var trending = result.Value
                    .OrderByDescending(p => p.DownloadCount)
                    .Take(6)
                    .Select(MapToPluginListing);

                TrendingPlugins = new ObservableCollection<PluginListing>(trending);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load trending plugins");
        }
    }

    /// <summary>
    /// Loads new plugins (recently published).
    /// </summary>
    [RelayCommand]
    private async Task LoadNewPluginsAsync()
    {
        try
        {
            var result = await _marketplaceService.GetAvailablePluginsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                // Sort by publication date for new plugins
                var newPlugins = result.Value
                    .OrderByDescending(p => p.PublishedAt)
                    .Take(6)
                    .Select(MapToPluginListing);

                NewPlugins = new ObservableCollection<PluginListing>(newPlugins);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load new plugins");
        }
    }

    /// <summary>
    /// Loads installed plugins and checks for updates.
    /// </summary>
    [RelayCommand]
    private async Task LoadInstalledPluginsAsync()
    {
        try
        {
            var result = await _marketplaceService.GetInstalledPluginsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                var installed = new List<PluginListing>();
                var updates = new List<PluginListing>();

                foreach (var pluginId in result.Value)
                {
                    var detailsResult = await _marketplaceService.GetPluginDetailsAsync(pluginId);
                    var hasUpdateResult = await _marketplaceService.HasUpdateAsync(pluginId);

                    if (detailsResult.IsSuccess && detailsResult.Value != null)
                    {
                        var listing = MapToPluginListing(detailsResult.Value);
                        listing.IsInstalled = true;
                        listing.IsUpdateAvailable = hasUpdateResult.IsSuccess && hasUpdateResult.Value;

                        installed.Add(listing);

                        if (listing.IsUpdateAvailable)
                        {
                            updates.Add(listing);
                        }
                    }
                }

                InstalledPlugins = new ObservableCollection<PluginListing>(installed);
                UpdateAvailablePlugins = new ObservableCollection<PluginListing>(updates);
                TotalInstalled = installed.Count;
                HasUpdates = updates.Count > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load installed plugins");
        }
    }

    /// <summary>
    /// Opens the plugin details view.
    /// </summary>
    [RelayCommand]
    private async Task OpenPluginDetailsAsync(PluginListing? plugin)
    {
        if (plugin == null) return;

        SelectedPlugin = plugin;

        // Show plugin details in an information dialog
        var details = $"{plugin.Name}\n\n" +
                      $"Author: {plugin.Author}\n" +
                      $"Version: {plugin.Version}\n" +
                      $"Downloads: {plugin.FormattedDownloads}\n" +
                      $"Rating: {plugin.StarRating} ({plugin.ReviewCount} reviews)\n\n" +
                      $"{plugin.Description}";
        await _dialogService.ShowInformationAsync("Plugin Details", details);
    }

    /// <summary>
    /// Installs a plugin.
    /// </summary>
    [RelayCommand]
    private async Task InstallPluginAsync(PluginListing? plugin)
    {
        if (plugin == null) return;

        try
        {
            // Confirm installation
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "Install Plugin",
                $"Do you want to install {plugin.Name} v{plugin.Version}?",
                confirmText: "Install",
                cancelText: "Cancel");

            if (!confirmed) return;

            // Show installing message
            await _dialogService.ShowInformationAsync("Installing", $"Installing {plugin.Name}...");

            var result = await _marketplaceService.InstallPluginAsync(plugin.Id);

            if (result.IsSuccess)
            {
                plugin.IsInstalled = true;
                plugin.InstalledVersion = plugin.Version;
                await _dialogService.ShowSuccessAsync($"{plugin.Name} installed successfully!");
                await LoadInstalledPluginsAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Installation failed";
                await _dialogService.ShowErrorAsync("Installation Failed", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin {PluginId}", plugin.Id);
            ErrorMessage = $"Installation failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Uninstalls a plugin.
    /// </summary>
    [RelayCommand]
    private async Task UninstallPluginAsync(PluginListing? plugin)
    {
        if (plugin == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Uninstall Plugin",
            $"Are you sure you want to uninstall {plugin.Name}?",
            confirmText: "Uninstall",
            cancelText: "Keep");

        if (!confirmed) return;

        try
        {
            var result = await _marketplaceService.UninstallPluginAsync(plugin.Id);
            if (result.IsSuccess)
            {
                plugin.IsInstalled = false;
                plugin.InstalledVersion = null;
                await LoadInstalledPluginsAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Uninstallation failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall plugin {PluginId}", plugin.Id);
            ErrorMessage = $"Uninstallation failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates a plugin to the latest version.
    /// </summary>
    [RelayCommand]
    private async Task UpdatePluginAsync(PluginListing? plugin)
    {
        if (plugin == null) return;

        try
        {
            var progress = new Progress<double>(p => { });
            var result = await _marketplaceService.UpdatePluginAsync(plugin.Id, progress);

            if (result.IsSuccess)
            {
                plugin.IsUpdateAvailable = false;
                plugin.InstalledVersion = plugin.Version;
                await LoadInstalledPluginsAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Update failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update plugin {PluginId}", plugin.Id);
            ErrorMessage = $"Update failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates all plugins with available updates.
    /// </summary>
    [RelayCommand]
    private async Task UpdateAllAsync()
    {
        foreach (var plugin in UpdateAvailablePlugins.ToList())
        {
            await UpdatePluginAsync(plugin);
        }
    }

    /// <summary>
    /// Toggles a plugin's enabled state.
    /// </summary>
    [RelayCommand]
    private async Task TogglePluginAsync(PluginListing? plugin)
    {
        if (plugin == null) return;

        try
        {
            if (plugin.IsEnabled)
            {
                await _pluginManager.DisablePluginAsync(plugin.Id);
                plugin.IsEnabled = false;
            }
            else
            {
                await _pluginManager.EnablePluginAsync(plugin.Id);
                plugin.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle plugin {PluginId}", plugin.Id);
            ErrorMessage = $"Failed to toggle plugin: {ex.Message}";
        }
    }

    /// <summary>
    /// Browses plugins by category.
    /// </summary>
    [RelayCommand]
    private async Task BrowseCategoryAsync(PluginCategory? category)
    {
        if (category == null) return;

        SelectedCategory = category.Name;

        IsLoading = true;
        try
        {
            var result = await _marketplaceService.GetPluginsByCategoryAsync(category.Name);
            if (result.IsSuccess && result.Value != null)
            {
                SearchResults = new ObservableCollection<PluginListing>(
                    result.Value.Select(MapToPluginListing));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Searches for plugins.
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _marketplaceService.SearchPluginsAsync(SearchQuery);
            if (result.IsSuccess && result.Value != null)
            {
                SearchResults = new ObservableCollection<PluginListing>(
                    result.Value.Select(MapToPluginListing));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search plugins");
            ErrorMessage = "Search failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes all plugin data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await InitializeAsync();
    }

    /// <summary>
    /// Checks for plugin updates.
    /// </summary>
    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        await LoadInstalledPluginsAsync();
    }

    /// <summary>
    /// Installs a plugin from a local file.
    /// </summary>
    [RelayCommand]
    private async Task InstallFromFileAsync()
    {
        // Open file picker for plugin package
        var filePath = await _dialogService.ShowOpenFileDialogAsync(
            "Select Plugin Package",
            "Plugin Package",
            new[] { "*.plugin", "*.zip" });

        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            var result = await _pluginManager.InstallPluginAsync(filePath);
            if (result.IsSuccess && result.Value)
            {
                await _dialogService.ShowSuccessAsync("Plugin installed successfully!");
                await LoadInstalledPluginsAsync();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Installation Failed", result.Error ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin from file");
            await _dialogService.ShowErrorAsync("Installation Failed", ex.Message);
        }
    }

    /// <summary>
    /// Opens plugin source management.
    /// </summary>
    [RelayCommand]
    private async Task ManageSourcesAsync()
    {
        // Show information about plugin sources
        await _dialogService.ShowInformationAsync(
            "Plugin Sources",
            "Plugin source management is available in the application settings.\n\n" +
            "You can add custom plugin repositories or configure marketplace settings there.");
    }

    private static PluginListing MapToPluginListing(PluginMarketplaceEntry entry)
    {
        return new PluginListing
        {
            Id = entry.Id,
            Name = entry.Name,
            Description = entry.Description,
            Author = entry.Author,
            Version = entry.Version,
            Icon = entry.IconUrl,
            Screenshots = entry.Screenshots ?? new List<string>(),
            Categories = string.IsNullOrEmpty(entry.Category) ? new List<string>() : new List<string> { entry.Category },
            Tags = entry.Tags ?? new List<string>(),
            DownloadCount = (int)entry.DownloadCount,
            Rating = (float)entry.AverageRating,
            ReviewCount = entry.ReviewCount,
            FileSize = entry.SizeInBytes,
            PublishedAt = entry.PublishedAt,
            UpdatedAt = entry.UpdatedAt,
            MinimumAppVersion = entry.MinimumAppVersion,
            Pricing = new PluginPricing { Type = PricingType.Free }
        };
    }
}

/// <summary>
/// Result of a plugin installation.
/// </summary>
public record PluginInstallationResult
{
    /// <summary>Whether the installation was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; set; }
}
