using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Presentation.Services;

public class NotificationHistoryService : INotificationHistoryService
{
    private readonly List<NotificationItem> _notifications = new();
    private readonly INotificationService _toastService;

    public NotificationHistoryService(INotificationService toastService)
    {
        _toastService = toastService;
    }

    public IReadOnlyList<NotificationItem> Notifications => _notifications.OrderByDescending(n => n.Timestamp).ToList();

    public void AddNotification(string message, string? title = null, NotificationType type = NotificationType.Info)
    {
        var item = new NotificationItem(
            Guid.NewGuid(),
            message,
            title,
            type,
            DateTime.Now,
            false);

        _notifications.Add(item);

        // Also show a toast
        switch (type)
        {
            case NotificationType.Success:
                _toastService.ShowSuccess(message, title);
                break;
            case NotificationType.Error:
                _toastService.ShowError(message, title);
                break;
            case NotificationType.Warning:
                _toastService.ShowWarning(message, title);
                break;
            case NotificationType.Info:
                _toastService.ShowInfo(message, title);
                break;
        }

        NotificationsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void MarkAsRead(Guid id)
    {
        var index = _notifications.FindIndex(n => n.Id == id);
        if (index != -1)
        {
            var old = _notifications[index];
            _notifications[index] = old with { IsRead = true };
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearAll()
    {
        _notifications.Clear();
        NotificationsChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? NotificationsChanged;
}
