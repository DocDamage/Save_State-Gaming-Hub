namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Mobile session data.
/// </summary>
public class MobileCompanionServiceMobileSession
{
    public string SessionId { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public MobileCompanionServiceMobilePlatform Platform { get; set; } = default!;
    public string AppVersion { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServicePermission> Permissions { get; set; } = default!;
    public MobileCompanionServiceSessionStatus Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastActivity { get; set; } = default!;
    public MobileCompanionServiceSessionFeatures Features { get; set; } = default!;
}

/// <summary>
/// Companion device data.
/// </summary>
public class MobileCompanionServiceCompanionDevice
{
    public string DeviceId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public MobileCompanionServiceMobilePlatform Platform { get; set; } = default!;
    public string DeviceName { get; set; } = default!;
    public string? PushToken { get; set; } = default!;
    public DateTime RegisteredAt { get; set; } = default!;
    public DateTime LastSeen { get; set; } = default!;
}

/// <summary>
/// Device registration request.
/// </summary>
public class MobileCompanionServiceDeviceRegistrationRequest
{
    public string DeviceId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string DeviceName { get; set; } = default!;
    public MobileCompanionServiceMobilePlatform Platform { get; set; } = default!;
    public string AppVersion { get; set; } = default!;
    public string? PushToken { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServicePermission> RequestedPermissions { get; set; } = default!;
}

/// <summary>
/// Session features.
/// </summary>
public class MobileCompanionServiceSessionFeatures
{
    public bool RemoteControl { get; set; } = default!;
    public bool RealTimeStats { get; set; } = default!;
    public bool Notifications { get; set; } = default!;
    public bool SocialFeatures { get; set; } = default!;
    public bool ContentManagement { get; set; } = default!;
}
