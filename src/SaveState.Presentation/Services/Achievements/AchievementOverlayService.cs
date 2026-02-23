using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.RetroAchievements.Services;
using SaveState.Presentation.Models.Achievements;
using Timer = System.Timers.Timer;

namespace SaveState.Presentation.Services.Achievements;

/// <summary>
/// Service implementation for managing achievement overlay notifications.
/// </summary>
public partial class AchievementOverlayService : ObservableObject, IAchievementOverlayService
{
    private readonly IRetroAchievementsService? _retroAchievementsService;
    private readonly ILogger<AchievementOverlayService>? _logger;
    private readonly ITimeProvider? _timeProvider;
    private readonly Dictionary<int, Timer> _autoDismissTimers = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Collection of active achievement notifications.
    /// </summary>
    public ObservableCollection<AchievementNotificationViewModel> ActiveNotifications { get; } = new();

    /// <summary>
    /// The current achievement progress for the active game.
    /// </summary>
    [ObservableProperty]
    private AchievementProgress _currentProgress = new();

    /// <summary>
    /// Whether the progress overlay is visible.
    /// </summary>
    [ObservableProperty]
    private bool _isProgressVisible;

    /// <summary>
    /// Whether there are any active notifications.
    /// </summary>
    public bool HasActiveNotifications => ActiveNotifications.Count > 0;

    /// <summary>
    /// Event raised when an achievement is unlocked and displayed.
    /// </summary>
    public event EventHandler<AchievementNotification>? OnAchievementUnlocked;

    /// <summary>
    /// Event raised when a notification is dismissed.
    /// </summary>
    public event EventHandler<AchievementNotification>? OnNotificationDismissed;

    /// <summary>
    /// Event raised when all notifications are dismissed.
    /// </summary>
    public event EventHandler? OnAllNotificationsDismissed;

    /// <summary>
    /// Design-time constructor.
    /// </summary>
    public AchievementOverlayService()
    {
        _retroAchievementsService = null;
        _logger = null;
        _timeProvider = null;
    }

    /// <summary>
    /// Runtime constructor.
    /// </summary>
    public AchievementOverlayService(
        IRetroAchievementsService retroAchievementsService,
        ILogger<AchievementOverlayService> logger,
        ITimeProvider timeProvider)
    {
        _retroAchievementsService = retroAchievementsService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public void ShowAchievementUnlocked(AchievementNotification achievement)
    {
        lock (_lockObject)
        {
            // Check if this achievement is already being displayed
            var existing = ActiveNotifications.FirstOrDefault(n => n.Notification.AchievementId == achievement.AchievementId);
            if (existing != null)
            {
                _logger?.LogDebug("Achievement {AchievementId} is already being displayed", achievement.AchievementId);
                return;
            }

            // Create view model
            var viewModel = new AchievementNotificationViewModel(achievement);
            ActiveNotifications.Add(viewModel);

            _logger?.LogInformation("Showing achievement unlock notification: {Title} ({GameName})",
                achievement.Title, achievement.GameName);

            // Set up auto-dismiss timer (5 seconds default)
            var timer = new Timer(5000);
            timer.Elapsed += (_, _) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => DismissNotification(viewModel));
            };
            timer.AutoReset = false;
            timer.Start();

            _autoDismissTimers[achievement.AchievementId] = timer;

            // Raise event
            OnAchievementUnlocked?.Invoke(this, achievement);
        }
    }

    /// <inheritdoc />
    public void ShowProgressUpdate(AchievementProgress progress)
    {
        CurrentProgress = progress;
        IsProgressVisible = true;

        _logger?.LogDebug("Showing progress update for {GameName}: {Unlocked}/{Total} achievements",
            progress.GameName, progress.UnlockedAchievements, progress.TotalAchievements);

        // Auto-hide progress after 10 seconds unless it's a new unlock
        if (progress.UnlockedAchievements < progress.TotalAchievements)
        {
            Task.Delay(10000).ContinueWith(_ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (CurrentProgress.GameId == progress.GameId)
                    {
                        IsProgressVisible = false;
                    }
                });
            });
        }
    }

    /// <inheritdoc />
    public void ShowMilestoneReached(string milestone, int value)
    {
        var milestoneObj = new AchievementMilestone
        {
            MilestoneType = milestone,
            Value = value,
            Description = $"Reached {milestone} milestone: {value}",
            ReachedAt = _timeProvider?.Now ?? DateTime.Now
        };

        ShowMilestone(milestoneObj);
    }

    /// <inheritdoc />
    public void ShowMilestone(AchievementMilestone milestone)
    {
        // Create a special notification for the milestone
        var notification = new AchievementNotification
        {
            AchievementId = -milestone.Value, // Negative ID to distinguish from regular achievements
            Title = $"🎯 {milestone.MilestoneType} Milestone!",
            Description = milestone.Description,
            Points = milestone.Value,
            GameName = milestone.GameName,
            UnlockedAt = milestone.ReachedAt,
            Rarity = AchievementRarity.Epic,
            UnlockPercentage = 10.0
        };

        ShowAchievementUnlocked(notification);

        _logger?.LogInformation("Showing milestone notification: {MilestoneType} = {Value}",
            milestone.MilestoneType, milestone.Value);
    }

    /// <inheritdoc />
    public void RegisterAchievementUnlockHandler()
    {
        if (_retroAchievementsService == null)
        {
            _logger?.LogWarning("Cannot register achievement unlock handler: RetroAchievementsService is null");
            return;
        }

        _retroAchievementsService.AchievementUnlocked += OnRetroAchievementUnlocked;
        _logger?.LogInformation("Registered achievement unlock handler");
    }

    /// <inheritdoc />
    public void UnregisterAchievementUnlockHandler()
    {
        if (_retroAchievementsService == null)
        {
            return;
        }

        _retroAchievementsService.AchievementUnlocked -= OnRetroAchievementUnlocked;
        _logger?.LogInformation("Unregistered achievement unlock handler");
    }

    /// <inheritdoc />
    public void DismissAllNotifications()
    {
        lock (_lockObject)
        {
            // Stop all timers
            foreach (var timer in _autoDismissTimers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            _autoDismissTimers.Clear();

            // Clear notifications
            ActiveNotifications.Clear();

            _logger?.LogDebug("Dismissed all achievement notifications");

            // Raise event
            OnAllNotificationsDismissed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Dismisses a specific notification.
    /// </summary>
    /// <param name="viewModel">The notification view model to dismiss.</param>
    public void DismissNotification(AchievementNotificationViewModel viewModel)
    {
        lock (_lockObject)
        {
            if (!ActiveNotifications.Contains(viewModel))
            {
                return;
            }

            // Stop timer if exists
            if (_autoDismissTimers.TryGetValue(viewModel.Notification.AchievementId, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                _autoDismissTimers.Remove(viewModel.Notification.AchievementId);
            }

            // Animate out
            viewModel.Opacity = 0;

            // Remove after animation
            Task.Delay(300).ContinueWith(_ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ActiveNotifications.Remove(viewModel);
                    OnNotificationDismissed?.Invoke(this, viewModel.Notification);
                });
            });
        }
    }

    /// <summary>
    /// Handles achievement unlocked events from RetroAchievements service.
    /// </summary>
    private void OnRetroAchievementUnlocked(object? sender, AchievementUnlockedEventArgs e)
    {
        _logger?.LogDebug("Received achievement unlock event: {AchievementTitle}", e.AchievementTitle);

        // Calculate rarity based on the achievement data (would need additional lookup in real implementation)
        var rarity = AchievementRarity.Common;

        var notification = new AchievementNotification
        {
            AchievementId = e.AchievementId,
            Title = e.AchievementTitle,
            Description = "Achievement Unlocked!", // Would be populated from achievement data
            BadgeUrl = e.BadgeUrl,
            Points = e.Points,
            GameName = e.GameTitle,
            UnlockedAt = _timeProvider?.Now ?? DateTime.Now,
            IsHardcore = e.IsHardcore,
            Rarity = rarity,
            UnlockPercentage = 25.0 // Would be populated from achievement data
        };

        Avalonia.Threading.Dispatcher.UIThread.Post(() => ShowAchievementUnlocked(notification));
    }

    /// <summary>
    /// Updates the progress for the current game.
    /// </summary>
    /// <param name="progress">The new progress.</param>
    public void UpdateProgress(AchievementProgress progress)
    {
        CurrentProgress = progress;
    }

    /// <summary>
    /// Shows the achievement overlay with current notifications.
    /// </summary>
    public void ShowOverlay()
    {
        // This would trigger the view to become visible
        OnPropertyChanged(nameof(ActiveNotifications));
    }

    /// <summary>
    /// Hides the achievement overlay.
    /// </summary>
    public void HideOverlay()
    {
        IsProgressVisible = false;
    }
}
