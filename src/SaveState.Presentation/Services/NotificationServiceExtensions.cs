using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

public static class NotificationServiceExtensions
{
    public static Task ShowNotificationAsync(this INotificationService service, string message, string title = "Notification")
    {
        service.ShowInfo(message, title);
        return Task.CompletedTask;
    }

    public static Task ShowErrorAsync(this INotificationService service, string message, string title = "Error")
    {
        service.ShowError(message, title);
        return Task.CompletedTask;
    }

    public static Task ShowWarningAsync(this INotificationService service, string message, string title = "Warning")
    {
        service.ShowWarning(message, title);
        return Task.CompletedTask;
    }
}
