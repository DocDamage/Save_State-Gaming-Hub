using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.Mobile;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.MobileCompanion;

/// <summary>
/// ViewModel for managing notifications from the mobile companion app.
/// Displays notifications from the gaming hub and allows managing them.
/// </summary>
public partial class MobileNotificationsViewModel : ObservableObject
{
    private readonly ILogger<MobileNotificationsViewModel> _logger;
    private readonly IMobileCompanionService? _companionService;

    [ObservableProperty]
    private ObservableCollection<CompanionNotification> _notifications = new();

    [ObservableProperty]
    private ObservableCollection<CompanionNotification> _filteredNotifications = new();

    [ObservableProperty]
    private bool _hasUnread;

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private string _selectedFilter = "All";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private CompanionNotification? _selectedNotification;

    [ObservableProperty]
    private bool _isDetailOpen;

    public ObservableCollection<string> AvailableFilters { get; } = new()
    {
        "All",
        "Unread",
        "Game",
        "System",
        "Achievement",
        "Save State"
    };

    public MobileNotificationsViewModel(
        ILogger<MobileNotificationsViewModel> logger,
        IMobileCompanionService? companionService = null)
    {
        _logger = logger;
        _companionService = companionService;
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the view model and loads notifications
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            await LoadNotificationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize notifications view");
        }
    }

    /// <summary>
    /// Loads notifications from the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task LoadNotificationsAsync()
    {
        try
        {
            IsLoading = true;
            Notifications.Clear();

            _logger.LogInformation("Loading notifications");

            if (_companionService is not null)
            {
                // TODO: Load from service
            }
            else
            {
                await LoadDemoNotificationsAsync();
            }

            ApplyFilter();
            UpdateUnreadStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notifications");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Clears all notifications
    /// </summary>
    [RelayCommand]
    private async Task ClearAllAsync()
    {
        try
        {
            _logger.LogInformation("Clearing all notifications");

            if (_companionService is not null)
            {
                // TODO: Send clear command
            }

            Notifications.Clear();
            FilteredNotifications.Clear();
            UpdateUnreadStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear notifications");
        }
    }

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    [RelayCommand]
    private async Task MarkAsReadAsync(CompanionNotification? notification)
    {
        if (notification is null) return;

        try
        {
            if (!notification.IsRead)
            {
                notification.IsRead = true;

                if (_companionService is not null)
                {
                    // TODO: Update via service
                }

                UpdateUnreadStatus();
                _logger.LogDebug("Marked notification {Id} as read", notification.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification as read");
        }
    }

    /// <summary>
    /// Marks all notifications as read
    /// </summary>
    [RelayCommand]
    private async Task MarkAllAsReadAsync()
    {
        try
        {
            _logger.LogInformation("Marking all notifications as read");

            foreach (var notification in Notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
            }

            if (_companionService is not null)
            {
                // TODO: Update via service
            }

            UpdateUnreadStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all as read");
        }
    }

    /// <summary>
    /// Deletes a notification
    /// </summary>
    [RelayCommand]
    private async Task DeleteNotificationAsync(CompanionNotification? notification)
    {
        if (notification is null) return;

        try
        {
            _logger.LogInformation("Deleting notification {Id}", notification.Id);

            if (_companionService is not null)
            {
                // TODO: Delete via service
            }

            Notifications.Remove(notification);
            ApplyFilter();
            UpdateUnreadStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification");
        }
    }

    /// <summary>
    /// Opens a notification detail view
    /// </summary>
    [RelayCommand]
    private async Task OpenNotificationAsync(CompanionNotification? notification)
    {
        if (notification is null) return;

        SelectedNotification = notification;
        IsDetailOpen = true;

        // Mark as read when opened
        await MarkAsReadAsync(notification);

        // Handle action if URL is present
        if (!string.IsNullOrEmpty(notification.ActionUrl))
        {
            // TODO: Handle deep link
        }
    }

    /// <summary>
    /// Closes the notification detail view
    /// </summary>
    [RelayCommand]
    private void CloseDetail()
    {
        IsDetailOpen = false;
        SelectedNotification = null;
    }

    /// <summary>
    /// Applies the selected filter to notifications
    /// </summary>
    [RelayCommand]
    private void ApplyFilter()
    {
        FilteredNotifications.Clear();

        IEnumerable<CompanionNotification> filtered = SelectedFilter switch
        {
            "Unread" => Notifications.Where(n => !n.IsRead),
            "Game" => Notifications.Where(n => n.Type == "Game"),
            "System" => Notifications.Where(n => n.Type == "System"),
            "Achievement" => Notifications.Where(n => n.Type == "Achievement"),
            "Save State" => Notifications.Where(n => n.Type == "SaveState"),
            _ => Notifications
        };

        foreach (var notification in filtered.OrderByDescending(n => n.Timestamp))
        {
            FilteredNotifications.Add(notification);
        }
    }

    /// <summary>
    /// Refreshes the notifications list
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadNotificationsAsync();
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
    /// Updates the unread status counters
    /// </summary>
    private void UpdateUnreadStatus()
    {
        UnreadCount = Notifications.Count(n => !n.IsRead);
        HasUnread = UnreadCount > 0;
    }

    /// <summary>
    /// Loads demo notifications
    /// </summary>
    private async Task LoadDemoNotificationsAsync()
    {
        var demoNotifications = new[]
        {
            new CompanionNotification
            {
                Id = "1",
                Title = "Achievement Unlocked!",
                Message = "You earned 'First Blood' in Elden Ring",
                Type = "Achievement",
                Timestamp = DateTime.Now.AddMinutes(-5),
                IsRead = false,
                ActionUrl = "savestate://games/elden-ring/achievements"
            },
            new CompanionNotification
            {
                Id = "2",
                Title = "Save State Created",
                Message = "Auto-save: Before Malenia boss fight",
                Type = "SaveState",
                Timestamp = DateTime.Now.AddHours(-1),
                IsRead = false,
                ActionUrl = "savestate://games/elden-ring/saves"
            },
            new CompanionNotification
            {
                Id = "3",
                Title = "Game Launch Complete",
                Message = "Hades II is now ready to play",
                Type = "Game",
                Timestamp = DateTime.Now.AddHours(-2),
                IsRead = true,
                ActionUrl = "savestate://games/hades-2"
            },
            new CompanionNotification
            {
                Id = "4",
                Title = "Screenshot Captured",
                Message = "Screenshot saved to Cyberpunk 2077 gallery",
                Type = "Game",
                Timestamp = DateTime.Now.AddHours(-3),
                IsRead = true,
                ActionUrl = "savestate://games/cyberpunk/screenshots"
            },
            new CompanionNotification
            {
                Id = "5",
                Title = "System Update Available",
                Message = "SaveState Reborn 2.5.3 is available",
                Type = "System",
                Timestamp = DateTime.Now.AddDays(-1),
                IsRead = true,
                ActionUrl = "savestate://settings/updates"
            },
            new CompanionNotification
            {
                Id = "6",
                Title = "Cloud Sync Complete",
                Message = "3 save states synced successfully",
                Type = "SaveState",
                Timestamp = DateTime.Now.AddDays(-1).AddHours(-2),
                IsRead = true,
                ActionUrl = "savestate://cloud-sync"
            },
            new CompanionNotification
            {
                Id = "7",
                Title = "Playtime Milestone",
                Message = "You've played 100 hours of Baldur's Gate 3!",
                Type = "Achievement",
                Timestamp = DateTime.Now.AddDays(-2),
                IsRead = true,
                ActionUrl = "savestate://games/bg3"
            }
        };

        foreach (var notification in demoNotifications)
        {
            Notifications.Add(notification);
        }
    }
}
