using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Achievements;
using SaveState.Presentation.Services.Achievements;
using SaveState.Presentation.ViewModels.Achievements;
using Timer = System.Timers.Timer;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the achievement overlay that displays unlock notifications and progress.
/// </summary>
public partial class AchievementOverlayViewModel : ObservableObject, IDisposable
{
    private readonly IAchievementOverlayService? _achievementService;
    private readonly ILogger<AchievementOverlayViewModel>? _logger;
    private readonly ITimeProvider? _timeProvider;
    private readonly Dictionary<int, Timer> _notificationTimers = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Collection of active achievement notifications.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AchievementNotificationViewModel> _activeNotifications = new();

    /// <summary>
    /// Whether the overlay is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// The currently highlighted/selected notification.
    /// </summary>
    [ObservableProperty]
    private AchievementNotificationViewModel? _currentNotification;

    /// <summary>
    /// Current achievement progress for the active game.
    /// </summary>
    [ObservableProperty]
    private AchievementProgress _currentProgress = new();

    /// <summary>
    /// Whether to show the progress bar.
    /// </summary>
    [ObservableProperty]
    private bool _showProgressBar;

    /// <summary>
    /// Whether the corner popup is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isCornerPopupVisible;

    /// <summary>
    /// The notification displayed in the corner popup.
    /// </summary>
    [ObservableProperty]
    private AchievementNotificationViewModel? _cornerPopupNotification;

    /// <summary>
    /// Corner popup auto-dismiss timer.
    /// </summary>
    private Timer? _cornerPopupTimer;

    /// <summary>
    /// Design-time constructor with sample data.
    /// </summary>
    public AchievementOverlayViewModel()
    {
        // Sample data for XAML designer
        _activeNotifications = new ObservableCollection<AchievementNotificationViewModel>
        {
            new AchievementNotificationViewModel(new AchievementNotification
            {
                AchievementId = 1,
                Title = "Master Chief",
                Description = "Complete the campaign on Legendary difficulty",
                Points = 50,
                GameName = "Halo: The Master Chief Collection",
                UnlockedAt = DateTime.Now.AddMinutes(-5),
                IsHardcore = true,
                Rarity = AchievementRarity.Rare,
                UnlockPercentage = 12.5
            }),
            new AchievementNotificationViewModel(new AchievementNotification
            {
                AchievementId = 2,
                Title = "War Hero",
                Description = "Complete all missions on Heroic or higher",
                Points = 25,
                GameName = "Halo: The Master Chief Collection",
                UnlockedAt = DateTime.Now.AddHours(-1),
                Rarity = AchievementRarity.Uncommon,
                UnlockPercentage = 35.0
            })
        };

        _currentProgress = new AchievementProgress
        {
            GameId = 1,
            GameName = "Halo: The Master Chief Collection",
            TotalAchievements = 50,
            UnlockedAchievements = 47,
            TotalPoints = 1000,
            CurrentPoints = 985,
            RecentUnlocks = new List<string> { "Master Chief", "War Hero", "Sharpshooter" }
        };

        IsVisible = true;
        ShowProgressBar = true;
    }

    /// <summary>
    /// Runtime constructor.
    /// </summary>
    public AchievementOverlayViewModel(
        IAchievementOverlayService achievementService,
        ILogger<AchievementOverlayViewModel> logger,
        ITimeProvider timeProvider)
    {
        _achievementService = achievementService;
        _logger = logger;
        _timeProvider = timeProvider;

        // Subscribe to service events
        _achievementService.OnAchievementUnlocked += OnAchievementUnlocked;
        _achievementService.OnNotificationDismissed += OnNotificationDismissed;
        _achievementService.OnAllNotificationsDismissed += OnAllNotificationsDismissed;
    }

    #region Commands

    /// <summary>
    /// Shows an achievement notification.
    /// </summary>
    [RelayCommand]
    private async Task ShowAchievementAsync(AchievementNotification achievement)
    {
        var viewModel = new AchievementNotificationViewModel(achievement);

        lock (_lockObject)
        {
            ActiveNotifications.Insert(0, viewModel);

            // Set up auto-dismiss timer (5 seconds)
            var timer = new Timer(5000);
            timer.Elapsed += (_, _) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => DismissNotificationAsync(viewModel));
            };
            timer.AutoReset = false;
            timer.Start();

            _notificationTimers[achievement.AchievementId] = timer;
        }

        // Show corner popup
        ShowCornerPopup(viewModel);

        _logger?.LogDebug("Showing achievement notification: {Title}", achievement.Title);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Dismisses a specific notification.
    /// </summary>
    [RelayCommand]
    private async Task DismissNotificationAsync(AchievementNotificationViewModel notification)
    {
        if (notification == null)
        {
            return;
        }

        lock (_lockObject)
        {
            // Stop and remove timer
            if (_notificationTimers.TryGetValue(notification.Notification.AchievementId, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                _notificationTimers.Remove(notification.Notification.AchievementId);
            }

            // Animate out
            notification.Opacity = 0;
        }

        // Remove after animation delay
        await Task.Delay(300);

        lock (_lockObject)
        {
            ActiveNotifications.Remove(notification);
        }

        _logger?.LogDebug("Dismissed achievement notification: {Title}", notification.Notification.Title);
    }

    /// <summary>
    /// Dismisses all notifications.
    /// </summary>
    [RelayCommand]
    private async Task DismissAllAsync()
    {
        lock (_lockObject)
        {
            // Stop all timers
            foreach (var timer in _notificationTimers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            _notificationTimers.Clear();

            // Animate all out
            foreach (var notification in ActiveNotifications)
            {
                notification.Opacity = 0;
            }
        }

        // Wait for animation
        await Task.Delay(300);

        lock (_lockObject)
        {
            ActiveNotifications.Clear();
        }

        HideCornerPopup();

        _logger?.LogDebug("Dismissed all achievement notifications");
    }

    /// <summary>
    /// Opens the achievement details view for the selected achievement.
    /// </summary>
    [RelayCommand]
    private async Task ViewAchievementDetailsAsync(AchievementNotificationViewModel notification)
    {
        if (notification == null)
        {
            return;
        }

        CurrentNotification = notification;

        // In a real implementation, this would open the achievement details dialog
        // or navigate to the achievements page
        _logger?.LogInformation("Viewing achievement details: {Title}", notification.Notification.Title);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Shares the achievement to social media or clipboard.
    /// </summary>
    [RelayCommand]
    private async Task ShareAchievementAsync(AchievementNotificationViewModel notification)
    {
        if (notification == null)
        {
            return;
        }

        var shareText = $"🏆 Just unlocked \"{notification.Notification.Title}\" in {notification.Notification.GameName}! " +
                       $"({notification.Notification.Points} points - {notification.RarityDisplayName})";

        // In a real implementation, this would open a share dialog or copy to clipboard
        _logger?.LogInformation("Sharing achievement: {Title}", notification.Notification.Title);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Dismisses the corner popup.
    /// </summary>
    [RelayCommand]
    private void DismissCornerPopup()
    {
        HideCornerPopup();
    }

    /// <summary>
    /// Handles the Escape key press to dismiss notifications.
    /// </summary>
    [RelayCommand]
    private void HandleEscapeKey()
    {
        if (IsCornerPopupVisible)
        {
            HideCornerPopup();
        }
        else if (ActiveNotifications.Count > 0)
        {
            _ = DismissAllAsync();
        }
    }

    #endregion

    #region Event Handlers

    private void OnAchievementUnlocked(object? sender, AchievementNotification achievement)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _ = ShowAchievementAsync(achievement);
        });
    }

    private void OnNotificationDismissed(object? sender, AchievementNotification achievement)
    {
        // Handled in DismissNotificationAsync
    }

    private void OnAllNotificationsDismissed(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            lock (_lockObject)
            {
                ActiveNotifications.Clear();
            }
            HideCornerPopup();
        });
    }

    #endregion

    #region Helper Methods

    private void ShowCornerPopup(AchievementNotificationViewModel notification)
    {
        // Cancel existing timer
        _cornerPopupTimer?.Stop();
        _cornerPopupTimer?.Dispose();

        CornerPopupNotification = notification;
        IsCornerPopupVisible = true;

        // Set up auto-dismiss timer (5 seconds)
        _cornerPopupTimer = new Timer(5000);
        _cornerPopupTimer.Elapsed += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => HideCornerPopup());
        };
        _cornerPopupTimer.AutoReset = false;
        _cornerPopupTimer.Start();
    }

    private void HideCornerPopup()
    {
        _cornerPopupTimer?.Stop();
        _cornerPopupTimer?.Dispose();
        _cornerPopupTimer = null;

        IsCornerPopupVisible = false;
        CornerPopupNotification = null;
    }

    /// <summary>
    /// Shows the full achievement overlay panel.
    /// </summary>
    public void ShowOverlay()
    {
        IsVisible = true;
    }

    /// <summary>
    /// Hides the full achievement overlay panel.
    /// </summary>
    public void HideOverlay()
    {
        IsVisible = false;
    }

    /// <summary>
    /// Updates the achievement progress display.
    /// </summary>
    public void UpdateProgress(AchievementProgress progress)
    {
        CurrentProgress = progress;
        ShowProgressBar = true;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        lock (_lockObject)
        {
            foreach (var timer in _notificationTimers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            _notificationTimers.Clear();
        }

        _cornerPopupTimer?.Stop();
        _cornerPopupTimer?.Dispose();

        if (_achievementService != null)
        {
            _achievementService.OnAchievementUnlocked -= OnAchievementUnlocked;
            _achievementService.OnNotificationDismissed -= OnNotificationDismissed;
            _achievementService.OnAllNotificationsDismissed -= OnAllNotificationsDismissed;
        }
    }

    #endregion
}
