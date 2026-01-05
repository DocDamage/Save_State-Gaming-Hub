namespace SaveState.Presentation.Services;

/// <summary>
/// Service for displaying toast notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a success notification.
    /// </summary>
    void ShowSuccess(string message, string? title = null, int durationMs = 3000);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    void ShowError(string message, string? title = null, int durationMs = 5000);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    void ShowWarning(string message, string? title = null, int durationMs = 4000);

    /// <summary>
    /// Shows an informational notification.
    /// </summary>
    void ShowInfo(string message, string? title = null, int durationMs = 3000);

    /// <summary>
    /// Clears all active notifications.
    /// </summary>
    void ClearAll();
}

/// <summary>
/// Types of notifications that can be displayed.
/// </summary>
public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// Event arguments for notification events.
/// </summary>
public class NotificationEventArgs : EventArgs
{
    public string Message { get; }
    public string? Title { get; }
    public NotificationType Type { get; }
    public int DurationMs { get; }

    public NotificationEventArgs(string message, string? title, NotificationType type, int durationMs)
    {
        Message = message;
        Title = title;
        Type = type;
        DurationMs = durationMs;
    }
}
