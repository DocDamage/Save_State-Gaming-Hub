using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.Mobile;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.MobileCompanion;

/// <summary>
/// ViewModel for managing screenshots from the mobile companion app.
/// Allows viewing, downloading, and sharing screenshots remotely.
/// </summary>
public partial class MobileScreenshotsViewModel : ObservableObject
{
    private readonly ILogger<MobileScreenshotsViewModel> _logger;
    private readonly IMobileCompanionService? _companionService;

    [ObservableProperty]
    private ObservableCollection<ScreenshotInfo> _screenshots = new();

    [ObservableProperty]
    private ObservableCollection<ScreenshotInfo> _filteredScreenshots = new();

    [ObservableProperty]
    private ScreenshotInfo? _selectedScreenshot;

    [ObservableProperty]
    private bool _isViewerOpen;

    [ObservableProperty]
    private string _selectedGame = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availableGames = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private int _currentImageIndex;

    [ObservableProperty]
    private bool _isFullscreen;

    public MobileScreenshotsViewModel(
        ILogger<MobileScreenshotsViewModel> logger,
        IMobileCompanionService? companionService = null)
    {
        _logger = logger;
        _companionService = companionService;
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the view model
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            await LoadAvailableGamesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize screenshots view");
        }
    }

    /// <summary>
    /// Takes a screenshot on the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task TakeScreenshotAsync()
    {
        try
        {
            _logger.LogInformation("Taking screenshot on gaming hub");
            IsLoading = true;

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
                await Task.Delay(1000);
            }

            // Add demo screenshot
            var newScreenshot = new ScreenshotInfo
            {
                Id = Guid.NewGuid().ToString(),
                GameId = SelectedGame,
                GameTitle = SelectedGame,
                CapturedAt = DateTime.Now,
                Resolution = "1920x1080",
                FileSize = 1024 * 1024 * 2
            };

            Screenshots.Insert(0, newScreenshot);
            ApplySearchFilter();

            _logger.LogInformation("Screenshot taken successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Opens a screenshot in the viewer
    /// </summary>
    [RelayCommand]
    private void ViewScreenshotAsync(ScreenshotInfo? screenshot)
    {
        if (screenshot is null) return;

        SelectedScreenshot = screenshot;
        CurrentImageIndex = Screenshots.IndexOf(screenshot);
        IsViewerOpen = true;
        _logger.LogDebug("Viewing screenshot {Id}", screenshot.Id);
    }

    /// <summary>
    /// Downloads a screenshot to the mobile device
    /// </summary>
    [RelayCommand]
    private async Task DownloadScreenshotAsync(ScreenshotInfo? screenshot)
    {
        if (screenshot is null) return;

        try
        {
            _logger.LogInformation("Downloading screenshot {Id}", screenshot.Id);

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
            }

            // Trigger native download
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download screenshot");
        }
    }

    /// <summary>
    /// Shares a screenshot via native mobile share sheet
    /// </summary>
    [RelayCommand]
    private async Task ShareScreenshotAsync(ScreenshotInfo? screenshot)
    {
        if (screenshot is null) return;

        try
        {
            _logger.LogInformation("Sharing screenshot {Id}", screenshot.Id);

            // Trigger native share sheet
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share screenshot");
        }
    }

    /// <summary>
    /// Deletes a screenshot from the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task DeleteScreenshotAsync(ScreenshotInfo? screenshot)
    {
        if (screenshot is null) return;

        try
        {
            _logger.LogInformation("Deleting screenshot {Id}", screenshot.Id);

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
            }

            Screenshots.Remove(screenshot);
            ApplySearchFilter();

            if (SelectedScreenshot == screenshot)
            {
                SelectedScreenshot = null;
                IsViewerOpen = false;
            }

            _logger.LogInformation("Screenshot deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete screenshot");
        }
    }

    /// <summary>
    /// Closes the screenshot viewer
    /// </summary>
    [RelayCommand]
    private void CloseViewer()
    {
        IsViewerOpen = false;
        SelectedScreenshot = null;
        IsFullscreen = false;
    }

    /// <summary>
    /// Navigates to the next screenshot in the viewer
    /// </summary>
    [RelayCommand]
    private void NextImage()
    {
        if (Screenshots.Count == 0) return;

        CurrentImageIndex = (CurrentImageIndex + 1) % Screenshots.Count;
        SelectedScreenshot = Screenshots[CurrentImageIndex];
    }

    /// <summary>
    /// Navigates to the previous screenshot in the viewer
    /// </summary>
    [RelayCommand]
    private void PreviousImage()
    {
        if (Screenshots.Count == 0) return;

        CurrentImageIndex = (CurrentImageIndex - 1 + Screenshots.Count) % Screenshots.Count;
        SelectedScreenshot = Screenshots[CurrentImageIndex];
    }

    /// <summary>
    /// Toggles fullscreen mode in the viewer
    /// </summary>
    [RelayCommand]
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }

    /// <summary>
    /// Loads screenshots for the selected game
    /// </summary>
    [RelayCommand]
    private async Task LoadScreenshotsAsync()
    {
        try
        {
            IsLoading = true;
            Screenshots.Clear();
            FilteredScreenshots.Clear();

            _logger.LogInformation("Loading screenshots for {Game}", SelectedGame);

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
            }
            else
            {
                await LoadDemoScreenshotsAsync();
            }

            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load screenshots");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the screenshots list
    /// </summary>
    [RelayCommand]
    private async Task RefreshScreenshotsAsync()
    {
        await LoadScreenshotsAsync();
    }

    /// <summary>
    /// Searches screenshots
    /// </summary>
    [RelayCommand]
    private void SearchScreenshots()
    {
        ApplySearchFilter();
    }

    /// <summary>
    /// Clears the search query
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        ApplySearchFilter();
    }

    /// <summary>
    /// Navigates back to the dashboard
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        // Navigation would happen here
        await Task.CompletedTask;
    }

    /// <summary>
    /// Loads available games with screenshots
    /// </summary>
    private async Task LoadAvailableGamesAsync()
    {
        try
        {
            AvailableGames.Clear();

            var games = new[] { "All Games", "Elden Ring", "Hades II", "Baldur's Gate 3", "Cyberpunk 2077" };
            foreach (var game in games)
            {
                AvailableGames.Add(game);
            }

            if (AvailableGames.Count > 0)
            {
                SelectedGame = AvailableGames[0];
                await LoadScreenshotsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available games");
        }
    }

    /// <summary>
    /// Loads demo screenshots
    /// </summary>
    private async Task LoadDemoScreenshotsAsync()
    {
        var demoScreenshots = new[]
        {
            new ScreenshotInfo
            {
                Id = "1",
                GameId = "elden-ring",
                GameTitle = "Elden Ring",
                CapturedAt = DateTime.Now.AddHours(-1),
                Resolution = "2560x1440",
                FileSize = 1024 * 1024 * 3
            },
            new ScreenshotInfo
            {
                Id = "2",
                GameId = "elden-ring",
                GameTitle = "Elden Ring",
                CapturedAt = DateTime.Now.AddHours(-3),
                Resolution = "2560x1440",
                FileSize = 1024 * 1024 * 2
            },
            new ScreenshotInfo
            {
                Id = "3",
                GameId = "hades-2",
                GameTitle = "Hades II",
                CapturedAt = DateTime.Now.AddDays(-1),
                Resolution = "1920x1080",
                FileSize = 1024 * 1024 * 1
            },
            new ScreenshotInfo
            {
                Id = "4",
                GameId = "bg3",
                GameTitle = "Baldur's Gate 3",
                CapturedAt = DateTime.Now.AddDays(-2),
                Resolution = "3840x2160",
                FileSize = 1024 * 1024 * 5
            },
            new ScreenshotInfo
            {
                Id = "5",
                GameId = "cyberpunk",
                GameTitle = "Cyberpunk 2077",
                CapturedAt = DateTime.Now.AddDays(-5),
                Resolution = "2560x1440",
                FileSize = 1024 * 1024 * 4
            },
            new ScreenshotInfo
            {
                Id = "6",
                GameId = "cyberpunk",
                GameTitle = "Cyberpunk 2077",
                CapturedAt = DateTime.Now.AddDays(-6),
                Resolution = "2560x1440",
                FileSize = 1024 * 1024 * 3
            }
        };

        foreach (var screenshot in demoScreenshots)
        {
            if (SelectedGame == "All Games" || screenshot.GameTitle == SelectedGame)
            {
                Screenshots.Add(screenshot);
            }
        }
    }

    /// <summary>
    /// Applies the search filter
    /// </summary>
    private void ApplySearchFilter()
    {
        FilteredScreenshots.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchQuery)
            ? Screenshots
            : Screenshots.Where(s =>
                s.GameTitle.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

        foreach (var screenshot in filtered)
        {
            FilteredScreenshots.Add(screenshot);
        }
    }
}
