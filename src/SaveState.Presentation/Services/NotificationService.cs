namespace SaveState.Presentation.Services;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Implementation of the notification service for displaying toast notifications.
/// </summary>
public partial class NotificationService : ObservableObject, INotificationService
{
    /// <summary>
    /// Gets the collection of active notifications.
    /// </summary>
    public ObservableCollection<NotificationViewModel> Notifications { get; } = new();

    /// <summary>
    /// Event raised when a notification is added.
    /// </summary>
    public event EventHandler<NotificationEventArgs>? NotificationAdded;

    public void ShowSuccess(string message, string? title = null, int durationMs = 3000)
    {
        AddNotification(message, title ?? "Success", NotificationType.Success, durationMs);
    }

    public void ShowError(string message, string? title = null, int durationMs = 5000)
    {
        AddNotification(message, title ?? "Error", NotificationType.Error, durationMs);
    }

    public void ShowWarning(string message, string? title = null, int durationMs = 4000)
    {
        AddNotification(message, title ?? "Warning", NotificationType.Warning, durationMs);
    }

    public void ShowInfo(string message, string? title = null, int durationMs = 3000)
    {
        AddNotification(message, title ?? "Info", NotificationType.Info, durationMs);
    }

    public void ClearAll()
    {
        Notifications.Clear();
    }

    private void AddNotification(string message, string title, NotificationType type, int durationMs)
    {
        var notification = new NotificationViewModel
        {
            Message = message,
            Title = title,
            Type = type,
            DurationMs = durationMs
        };

        Notifications.Insert(0, notification);
        NotificationAdded?.Invoke(this, new NotificationEventArgs(message, title, type, durationMs));

        // Auto-remove after duration
        if (durationMs > 0)
        {
            Task.Delay(durationMs).ContinueWith(_ =>
            {
                Notifications.Remove(notification);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}

/// <summary>
/// View model for a single notification.
/// </summary>
public partial class NotificationViewModel : ObservableObject
{
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private NotificationType _type;

    [ObservableProperty]
    private int _durationMs;

    [ObservableProperty]
    private bool _isVisible = true;

    public string Icon => Type switch
    {
        NotificationType.Success => "✓",
        NotificationType.Error => "✗",
        NotificationType.Warning => "⚠",
        NotificationType.Info => "ℹ",
        _ => "ℹ"
    };

    public string BackgroundColor => Type switch
    {
        NotificationType.Success => "#10B981",
        NotificationType.Error => "#EF4444",
        NotificationType.Warning => "#F59E0B",
        NotificationType.Info => "#3B82F6",
        _ => "#6B7280"
    };

    public string BorderColor => Type switch
    {
        NotificationType.Success => "#059669",
        NotificationType.Error => "#DC2626",
        NotificationType.Warning => "#D97706",
        NotificationType.Info => "#2563EB",
        _ => "#4B5563"
    };
}
