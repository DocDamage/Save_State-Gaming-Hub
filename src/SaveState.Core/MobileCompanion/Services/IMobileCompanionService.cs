using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;

namespace SaveState.Core.MobileCompanion.Services;

public interface IMobileCompanionService
{
    // Pairing
    Task<Result<PairingRequest>> CreatePairingRequestAsync(CancellationToken ct = default);
    Task<Result<MobileDevice>> CompletePairingAsync(string pairingCode, DeviceInfo deviceInfo, CancellationToken ct = default);
    Task<Result> UnpairDeviceAsync(Guid deviceId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MobileDevice>>> GetPairedDevicesAsync(CancellationToken ct = default);
    Task<Result<MobileDevice>> GetDeviceAsync(Guid deviceId, CancellationToken ct = default);

    // Session Management
    Task<Result<RemoteSession>> StartSessionAsync(Guid deviceId, string connectionId, CancellationToken ct = default);
    Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<Result<RemoteSession>> GetActiveSessionAsync(Guid deviceId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RemoteSession>>> GetActiveSessionsAsync(CancellationToken ct = default);

    // Remote Control
    Task<Result> SendCommandAsync(Guid deviceId, RemoteCommandMessage command, CancellationToken ct = default);
    Task<Result> SendGamepadInputAsync(Guid deviceId, GamepadInput input, CancellationToken ct = default);
    Task<Result> SendTouchpadInputAsync(Guid deviceId, TouchpadInput input, CancellationToken ct = default);
    Task<Result> SendKeyboardInputAsync(Guid deviceId, KeyboardInput input, CancellationToken ct = default);
    Task<Result> SetControlModeAsync(Guid deviceId, RemoteControlMode mode, CancellationToken ct = default);

    // Notifications
    Task<Result> SendNotificationAsync(Guid deviceId, CompanionNotification notification, CancellationToken ct = default);
    Task<Result> BroadcastNotificationAsync(CompanionNotification notification, CancellationToken ct = default);

    // Data Sync
    Task<Result<LibrarySyncInfo>> GetLibrarySyncInfoAsync(CancellationToken ct = default);
    Task<Result<SystemStatus>> GetSystemStatusAsync(CancellationToken ct = default);
    Task<Result<GameSummary>> GetGameDetailsAsync(Guid gameId, CancellationToken ct = default);

    // Permissions
    Task<Result> UpdateDevicePermissionsAsync(Guid deviceId, List<string> permissions, CancellationToken ct = default);
    Task<Result<bool>> CheckPermissionAsync(Guid deviceId, string permission, CancellationToken ct = default);
}

public record DeviceInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? PushNotificationToken { get; set; }
}
