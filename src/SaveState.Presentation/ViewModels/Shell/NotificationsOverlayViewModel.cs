using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class NotificationsOverlayViewModel : ObservableObject
{
    private readonly INotificationHistoryService _notificationHistory;
    private readonly IOverlayService _overlayService;

    [ObservableProperty]
    private bool _hasUnread;

    public ObservableCollection<NotificationItemViewModel> Items { get; } = new();

    public NotificationsOverlayViewModel(
        INotificationHistoryService notificationHistory,
        IOverlayService overlayService)
    {
        _notificationHistory = notificationHistory;
        _overlayService = overlayService;

        _notificationHistory.NotificationsChanged += (s, e) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        var notifications = _notificationHistory.Notifications;
        Items.Clear();
        foreach (var n in notifications)
        {
            Items.Add(new NotificationItemViewModel(n, _notificationHistory));
        }

        HasUnread = notifications.Any(n => !n.IsRead);
    }

    [RelayCommand]
    private void ClearAll()
    {
        _notificationHistory.ClearAll();
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideNotificationsOverlay();
    }
}

public partial class NotificationItemViewModel : ObservableObject
{
    private readonly NotificationItem _item;
    private readonly INotificationHistoryService _history;

    public NotificationItemViewModel(NotificationItem item, INotificationHistoryService history)
    {
        _item = item;
        _history = history;
    }

    public Guid Id => _item.Id;
    public string Message => _item.Message;
    public string? Title => _item.Title;
    public string Icon => _item.Type switch
    {
        NotificationType.Success => "✅",
        NotificationType.Error => "❌",
        NotificationType.Warning => "⚠️",
        _ => "ℹ️"
    };
    public string TimeAgo => FormatTimeAgo(_item.Timestamp);
    public bool IsRead => _item.IsRead;

    [RelayCommand]
    private void MarkAsRead()
    {
        _history.MarkAsRead(Id);
    }

    private static string FormatTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt.ToUniversalTime();
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return dt.ToString("g");
    }
}
