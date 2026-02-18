namespace SaveState.Application.Mugen.Services.MobileCompanion.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for managing mobile notifications.
/// </summary>
public class NotificationEngine
{
    private readonly ILogger<NotificationEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, List<MobileCompanionServiceMobileNotification>> _userNotifications = new();

    public NotificationEngine(ILogger<NotificationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets pending notifications for a user.
    /// </summary>
    public Task<List<MobileCompanionServiceMobileNotification>> GetPendingNotificationsAsync(string userId, CancellationToken ct = default)
    {
        var notifications = _userNotifications.GetValueOrDefault(userId) ?? new List<MobileCompanionServiceMobileNotification>();
        return Task.FromResult(notifications.Where(n => !n.IsRead).ToList());
    }

    /// <summary>
    /// Sends a platform notification to a device.
    /// </summary>
    public Task SendPlatformNotificationAsync(
        MobileCompanionServiceCompanionDevice device,
        MobileCompanionServicePushNotification notification,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Sending {Platform} notification to device {DeviceId}: {Title}",
            device.Platform, device.DeviceId, notification.Title);

        // Simulate sending notification
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs a notification for tracking.
    /// </summary>
    public Task LogNotificationAsync(string userId, MobileCompanionServicePushNotification notification, CancellationToken ct = default)
    {
        if (!_userNotifications.ContainsKey(userId))
            _userNotifications[userId] = new List<MobileCompanionServiceMobileNotification>();

        _userNotifications[userId].Add(new MobileCompanionServiceMobileNotification
        {
            NotificationId = Guid.NewGuid().ToString(),
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            CreatedAt = _timeProvider.UtcNow,
            IsRead = false
        });

        return Task.CompletedTask;
    }
}
