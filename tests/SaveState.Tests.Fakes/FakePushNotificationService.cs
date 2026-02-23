using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of IPushNotificationService for integration testing.
/// </summary>
public class FakePushNotificationService : IPushNotificationService
{
    private readonly Dictionary<Guid, RegisteredDeviceInfo> _registeredDevices = new();

    public Task<Result> RegisterDeviceAsync(Guid deviceId, string token, string platform)
    {
        _registeredDevices[deviceId] = new RegisteredDeviceInfo
        {
            DeviceId = deviceId,
            Platform = platform,
            RegisteredAt = DateTime.UtcNow,
            IsActive = true,
            BadgeCount = 0
        };
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UnregisterDeviceAsync(Guid deviceId)
    {
        _registeredDevices.Remove(deviceId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SendNotificationAsync(Guid deviceId, CompanionNotification notification)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SendNotificationAsync(List<Guid> deviceIds, CompanionNotification notification)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> BroadcastNotificationAsync(CompanionNotification notification)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UpdateBadgeCountAsync(Guid deviceId, int badgeCount)
    {
        if (_registeredDevices.TryGetValue(deviceId, out var device))
        {
            device.BadgeCount = badgeCount;
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result<List<RegisteredDeviceInfo>>> GetRegisteredDevicesAsync()
    {
        var devices = _registeredDevices.Values.ToList();
        return Task.FromResult(Result<List<RegisteredDeviceInfo>>.Success(devices));
    }
}
