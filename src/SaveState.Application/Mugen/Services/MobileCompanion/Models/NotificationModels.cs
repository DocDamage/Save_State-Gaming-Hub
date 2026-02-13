namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Push notification data.
/// </summary>
public class MobileCompanionServicePushNotification
{
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public MobileCompanionServiceNotificationType Type { get; set; } = default!;
    public MobileCompanionServiceNotificationPriority Priority { get; set; } = default!;
    public IReadOnlyDictionary<string, object>? Data { get; set; } = default!;
    public DateTime? ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Mobile notification.
/// </summary>
public class MobileCompanionServiceMobileNotification
{
    public string NotificationId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public MobileCompanionServiceNotificationType Type { get; set; } = default!;
    public MobileCompanionServiceNotificationPriority Priority { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public string? ActionUrl { get; set; } = default!;
    public DateTime? ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Notification preferences.
/// </summary>
public class MobileCompanionServiceNotificationPreferences
{
    public bool EnableMatchNotifications { get; set; } = true;
    public bool EnableTournamentNotifications { get; set; } = true;
    public bool EnableSocialNotifications { get; set; } = true;
    public bool EnableSystemNotifications { get; set; } = true;
    public MobileCompanionServiceNotificationPriority MinimumPriority { get; set; } = MobileCompanionServiceNotificationPriority.Normal;
    public TimeSpan QuietHoursStart { get; set; }
    public TimeSpan QuietHoursEnd { get; set; }
}
