using System;
using System.Collections.Generic;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for managing the history of notifications.
/// </summary>
public interface INotificationHistoryService
{
    /// <summary>
    /// Gets the list of current notifications.
    /// </summary>
    IReadOnlyList<NotificationItem> Notifications { get; }

    /// <summary>
    /// Adds a notification to the history.
    /// </summary>
    void AddNotification(string message, string? title = null, NotificationType type = NotificationType.Info);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    void MarkAsRead(Guid id);

    /// <summary>
    /// Clears all notifications.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Raised when the notification list changes.
    /// </summary>
    event EventHandler? NotificationsChanged;
}

/// <summary>
/// Represents a single notification in the history.
/// </summary>
public record NotificationItem(
    Guid Id,
    string Message,
    string? Title,
    NotificationType Type,
    DateTime Timestamp,
    bool IsRead);
