using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;

namespace SaveState.Core.MobileCompanion.Services;

/// <summary>
/// Interface for sending push notifications to mobile devices.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Registers a device for push notifications.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="token">The push notification token from the device.</param>
    /// <param name="platform">The device platform (iOS, Android, Web).</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RegisterDeviceAsync(Guid deviceId, string token, string platform);

    /// <summary>
    /// Unregisters a device from push notifications.
    /// </summary>
    /// <param name="deviceId">The device ID to unregister.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UnregisterDeviceAsync(Guid deviceId);

    /// <summary>
    /// Sends a notification to a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="notification">The notification to send.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SendNotificationAsync(Guid deviceId, CompanionNotification notification);

    /// <summary>
    /// Sends a notification to multiple devices.
    /// </summary>
    /// <param name="deviceIds">The list of device IDs.</param>
    /// <param name="notification">The notification to send.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SendNotificationAsync(List<Guid> deviceIds, CompanionNotification notification);

    /// <summary>
    /// Sends a notification to all registered devices.
    /// </summary>
    /// <param name="notification">The notification to send.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> BroadcastNotificationAsync(CompanionNotification notification);

    /// <summary>
    /// Updates the notification badge count for a device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="badgeCount">The new badge count.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdateBadgeCountAsync(Guid deviceId, int badgeCount);

    /// <summary>
    /// Gets a list of registered devices.
    /// </summary>
    /// <returns>A list of registered device information.</returns>
    Task<Result<List<RegisteredDeviceInfo>>> GetRegisteredDevicesAsync();
}

/// <summary>
/// Information about a registered push notification device.
/// </summary>
public class RegisteredDeviceInfo
{
    public Guid DeviceId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastNotificationAt { get; set; }
    public bool IsActive { get; set; }
    public int BadgeCount { get; set; }
}

/// <summary>
/// Configuration options for push notification services.
/// </summary>
public class PushNotificationOptions
{
    /// <summary>
    /// Firebase Cloud Messaging server key.
    /// </summary>
    public string? FcmServerKey { get; set; }

    /// <summary>
    /// Firebase Cloud Messaging sender ID.
    /// </summary>
    public string? FcmSenderId { get; set; }

    /// <summary>
    /// Apple Push Notification Service certificate path.
    /// </summary>
    public string? ApnsCertificatePath { get; set; }

    /// <summary>
    /// Apple Push Notification Service certificate password.
    /// </summary>
    public string? ApnsCertificatePassword { get; set; }

    /// <summary>
    /// Whether to use APNS sandbox environment.
    /// </summary>
    public bool ApnsUseSandbox { get; set; } = true;

    /// <summary>
    /// VAPID public key for Web Push.
    /// </summary>
    public string? WebPushPublicKey { get; set; }

    /// <summary>
    /// VAPID private key for Web Push.
    /// </summary>
    public string? WebPushPrivateKey { get; set; }

    /// <summary>
    /// VAPID subject for Web Push.
    /// </summary>
    public string? WebPushSubject { get; set; }

    /// <summary>
    /// Maximum number of retry attempts for failed notifications.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to enable notification batching.
    /// </summary>
    public bool EnableBatching { get; set; } = true;
}
