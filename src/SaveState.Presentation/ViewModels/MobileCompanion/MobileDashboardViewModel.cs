using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.Mobile;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.MobileCompanion;

/// <summary>
/// ViewModel for the mobile dashboard screen.
/// Main interface after successful connection to the gaming hub.
/// </summary>
public partial class MobileDashboardViewModel : ObservableObject
{
    private readonly ILogger<MobileDashboardViewModel> _logger;
    private readonly IMobileCompanionService? _companionService;

    [ObservableProperty]
    private SystemStatus _systemStatus = new();

    [ObservableProperty]
    private ObservableCollection<GameSummary> _recentGames = new();

    [ObservableProperty]
    private GameSummary? _currentlyPlaying;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private RemoteControlMode _currentMode = RemoteControlMode.Gamepad;

    [ObservableProperty]
    private MobileDevice? _connectedDevice;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private int _notificationCount;

    public MobileDashboardViewModel(
        ILogger<MobileDashboardViewModel> logger,
        IMobileCompanionService? companionService = null)
    {
        _logger = logger;
        _companionService = companionService;
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the dashboard with current data
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            IsConnected = true;
            await RefreshDataAsync();
            _ = StartPeriodicRefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize mobile dashboard");
        }
    }

    /// <summary>
    /// Refreshes all dashboard data
    /// </summary>
    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            IsRefreshing = true;
            _logger.LogDebug("Refreshing dashboard data");

            // Load system status
            await LoadSystemStatusAsync();

            // Load recent games
            await LoadRecentGamesAsync();

            // Load currently playing game
            await LoadCurrentlyPlayingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh dashboard data");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Launches a game on the connected gaming hub
    /// </summary>
    [RelayCommand]
    private async Task LaunchGameAsync(GameSummary? game)
    {
        if (game is null) return;

        try
        {
            _logger.LogInformation("Launching game {GameTitle}", game.Title);

            if (_companionService is not null)
            {
                // TODO: Implement game launch via service
            }

            // Update currently playing after short delay
            await Task.Delay(2000);
            CurrentlyPlaying = game;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch game {GameTitle}", game.Title);
        }
    }

    /// <summary>
    /// Opens the remote control interface
    /// </summary>
    [RelayCommand]
    private async Task OpenRemoteControlAsync()
    {
        try
        {
            _logger.LogInformation("Opening remote control in {Mode} mode", CurrentMode);
            // Navigation to remote control view would happen here
            // This would typically use a navigation service
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open remote control");
        }
    }

    /// <summary>
    /// Opens the save states management interface
    /// </summary>
    [RelayCommand]
    private async Task OpenSaveStatesAsync()
    {
        try
        {
            _logger.LogInformation("Opening save states view");
            // Navigation to save states view
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open save states");
        }
    }

    /// <summary>
    /// Opens the screenshots gallery
    /// </summary>
    [RelayCommand]
    private async Task OpenScreenshotsAsync()
    {
        try
        {
            _logger.LogInformation("Opening screenshots gallery");
            // Navigation to screenshots view
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open screenshots");
        }
    }

    /// <summary>
    /// Disconnects from the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try
        {
            _logger.LogInformation("Disconnecting from gaming hub");

            if (_companionService is not null && ConnectedDevice is not null)
            {
                await _companionService.DisconnectAsync(ConnectedDevice.DeviceId);
            }

            IsConnected = false;
            CurrentlyPlaying = null;
            RecentGames.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnect");
        }
    }

    /// <summary>
    /// Opens the notifications panel
    /// </summary>
    [RelayCommand]
    private async Task OpenNotificationsAsync()
    {
        try
        {
            _logger.LogInformation("Opening notifications");
            // Navigation to notifications view
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open notifications");
        }
    }

    /// <summary>
    /// Opens the settings panel
    /// </summary>
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        try
        {
            _logger.LogInformation("Opening settings");
            // Navigation to settings view
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open settings");
        }
    }

    /// <summary>
    /// Pauses the currently playing game
    /// </summary>
    [RelayCommand]
    private async Task PauseGameAsync()
    {
        try
        {
            _logger.LogInformation("Pausing current game");
            // Send pause command to gaming hub
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause game");
        }
    }

    /// <summary>
    /// Takes a screenshot of the currently playing game
    /// </summary>
    [RelayCommand]
    private async Task TakeScreenshotAsync()
    {
        try
        {
            _logger.LogInformation("Taking screenshot");
            // Send screenshot command to gaming hub
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot");
        }
    }

    /// <summary>
    /// Starts/stops recording gameplay
    /// </summary>
    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        try
        {
            _logger.LogInformation("Toggling game recording");
            // Send recording toggle command to gaming hub
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle recording");
        }
    }

    /// <summary>
    /// Loads the current system status from the gaming hub
    /// </summary>
    private async Task LoadSystemStatusAsync()
    {
        try
        {
            // TODO: Load from actual service
            // For now, generate demo data
            SystemStatus = new SystemStatus
            {
                CpuUsage = Random.Shared.Next(20, 80),
                RamUsage = Random.Shared.Next(40, 85),
                Temperature = Random.Shared.Next(55, 85),
                IsGaming = CurrentlyPlaying is not null,
                CurrentActivity = CurrentlyPlaying?.Title ?? "Idle"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load system status");
        }
    }

    /// <summary>
    /// Loads recently played games from the gaming hub
    /// </summary>
    private async Task LoadRecentGamesAsync()
    {
        try
        {
            RecentGames.Clear();

            // TODO: Load from actual service
            // For now, generate demo data
            var games = new[]
            {
                new GameSummary
                {
                    Id = "1",
                    Title = "Elden Ring",
                    Platform = "Steam",
                    TotalPlayTime = TimeSpan.FromHours(45),
                    LastPlayedAt = DateTime.Now.AddHours(-2)
                },
                new GameSummary
                {
                    Id = "2",
                    Title = "Hades II",
                    Platform = "Steam",
                    TotalPlayTime = TimeSpan.FromHours(23),
                    LastPlayedAt = DateTime.Now.AddDays(-1)
                },
                new GameSummary
                {
                    Id = "3",
                    Title = "Baldur's Gate 3",
                    Platform = "Steam",
                    TotalPlayTime = TimeSpan.FromHours(120),
                    LastPlayedAt = DateTime.Now.AddDays(-3)
                },
                new GameSummary
                {
                    Id = "4",
                    Title = "Cyberpunk 2077",
                    Platform = "Steam",
                    TotalPlayTime = TimeSpan.FromHours(67),
                    LastPlayedAt = DateTime.Now.AddDays(-5)
                }
            };

            foreach (var game in games)
            {
                RecentGames.Add(game);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent games");
        }
    }

    /// <summary>
    /// Loads the currently playing game from the gaming hub
    /// </summary>
    private async Task LoadCurrentlyPlayingAsync()
    {
        try
        {
            // TODO: Load from actual service
            // For demo, set first recent game as currently playing
            CurrentlyPlaying = Random.Shared.Next(2) == 0 ? RecentGames.FirstOrDefault() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load currently playing game");
        }
    }

    /// <summary>
    /// Starts periodic refresh of dashboard data
    /// </summary>
    private async Task StartPeriodicRefreshAsync()
    {
        while (IsConnected)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                if (IsConnected)
                {
                    await LoadSystemStatusAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic refresh");
            }
        }
    }
}
