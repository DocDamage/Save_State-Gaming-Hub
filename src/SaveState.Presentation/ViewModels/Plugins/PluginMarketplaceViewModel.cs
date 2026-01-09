using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Plugins.DTOs;
using SaveState.Core.Plugins.Services;

namespace SaveState.Presentation.ViewModels.Plugins;

/// <summary>
/// ViewModel for the Plugin Marketplace UI.
/// </summary>
public partial class PluginMarketplaceViewModel : ObservableObject
{
    private readonly IPluginMarketplaceService _marketplaceService;

    [ObservableProperty]
    private ObservableCollection<PluginMarketplaceEntry> _availablePlugins = new();

    [ObservableProperty]
    private ObservableCollection<PluginMarketplaceEntry> _featuredPlugins = new();

    [ObservableProperty]
    private ObservableCollection<string> _installedPlugins = new();

    [ObservableProperty]
    private string _selectedCategory = PluginCategories.All;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private PluginMarketplaceEntry? _selectedPlugin;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private double _installProgress;

    [ObservableProperty]
    private bool _isInstalling;

    public ObservableCollection<string> Categories { get; } = new()
    {
        PluginCategories.All,
        PluginCategories.GameDetection,
        PluginCategories.Themes,
        PluginCategories.Integration,
        PluginCategories.Analytics,
        PluginCategories.Social,
        PluginCategories.Emulation,
        PluginCategories.Utilities
    };

    public PluginMarketplaceViewModel(IPluginMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    /// <summary>
    /// Initializes the marketplace by loading plugins.
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadAvailablePluginsAsync();
        await LoadFeaturedPluginsAsync();
        await LoadInstalledPluginsAsync();
    }

    /// <summary>
    /// Loads all available plugins from the marketplace.
    /// </summary>
    [RelayCommand]
    private async Task LoadAvailablePluginsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _marketplaceService.GetAvailablePluginsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                AvailablePlugins = new ObservableCollection<PluginMarketplaceEntry>(result.Value);
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load plugins";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading plugins: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads featured plugins.
    /// </summary>
    [RelayCommand]
    private async Task LoadFeaturedPluginsAsync()
    {
        try
        {
            var result = await _marketplaceService.GetFeaturedPluginsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                FeaturedPlugins = new ObservableCollection<PluginMarketplaceEntry>(result.Value);
            }
        }
        catch (Exception ex)
        {
            // Featured plugins failure is not critical
            System.Diagnostics.Debug.WriteLine($"Failed to load featured plugins: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads list of installed plugins.
    /// </summary>
    [RelayCommand]
    private async Task LoadInstalledPluginsAsync()
    {
        try
        {
            var result = await _marketplaceService.GetInstalledPluginsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InstalledPlugins = new ObservableCollection<string>(result.Value);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load installed plugins: {ex.Message}");
        }
    }

    /// <summary>
    /// Filters plugins by selected category.
    /// </summary>
    [RelayCommand]
    private async Task FilterByCategoryAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _marketplaceService.GetPluginsByCategoryAsync(SelectedCategory);
            if (result.IsSuccess && result.Value != null)
            {
                AvailablePlugins = new ObservableCollection<PluginMarketplaceEntry>(result.Value);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Searches for plugins by query.
    /// </summary>
    [RelayCommand]
    private async Task SearchPluginsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadAvailablePluginsAsync();
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _marketplaceService.SearchPluginsAsync(SearchQuery);
            if (result.IsSuccess && result.Value != null)
            {
                AvailablePlugins = new ObservableCollection<PluginMarketplaceEntry>(result.Value);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Installs a plugin from the marketplace.
    /// </summary>
    [RelayCommand]
    private async Task InstallPluginAsync(string pluginId)
    {
        IsInstalling = true;
        InstallProgress = 0;
        ErrorMessage = null;

        try
        {
            var progress = new Progress<double>(p => InstallProgress = p * 100);
            var result = await _marketplaceService.InstallPluginAsync(pluginId, progress);

            if (result.IsSuccess)
            {
                // Refresh installed plugins list
                await LoadInstalledPluginsAsync();
                InstallProgress = 0;
            }
            else
            {
                ErrorMessage = result.Error ?? "Installation failed";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Installation error: {ex.Message}";
        }
        finally
        {
            IsInstalling = false;
        }
    }

    /// <summary>
    /// Uninstalls a plugin.
    /// </summary>
    [RelayCommand]
    private async Task UninstallPluginAsync(string pluginId)
    {
        try
        {
            var result = await _marketplaceService.UninstallPluginAsync(pluginId);
            if (result.IsSuccess)
            {
                await LoadInstalledPluginsAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Uninstallation failed";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Uninstallation error: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates a plugin to the latest version.
    /// </summary>
    [RelayCommand]
    private async Task UpdatePluginAsync(string pluginId)
    {
        IsInstalling = true;
        InstallProgress = 0;

        try
        {
            var progress = new Progress<double>(p => InstallProgress = p * 100);
            var result = await _marketplaceService.UpdatePluginAsync(pluginId, progress);

            if (result.IsSuccess)
            {
                await LoadInstalledPluginsAsync();
                InstallProgress = 0;
            }
            else
            {
                ErrorMessage = result.Error ?? "Update failed";
            }
        }
        finally
        {
            IsInstalling = false;
        }
    }

    /// <summary>
    /// Checks if a plugin is currently installed.
    /// </summary>
    public bool IsPluginInstalled(string pluginId) => InstalledPlugins.Contains(pluginId);
}
