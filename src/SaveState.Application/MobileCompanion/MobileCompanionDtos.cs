using SaveState.Core.MobileCompanion.Models;

namespace SaveState.Application.MobileCompanion;

/// <summary>
/// Data transfer objects for Mobile Companion API.
/// These are shared between Commands, Queries, and Controllers.
/// </summary>

public sealed record PairingRequestDto
{
    public Guid Id { get; init; }
    public string PairingCode { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record DeviceInfoDto
{
    public string DeviceName { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string? DeviceModel { get; init; }
    public string? OsVersion { get; init; }
    public string? AppVersion { get; init; }
    public string? PushNotificationToken { get; init; }
}

public sealed record MobileDeviceDto
{
    public Guid Id { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string? DeviceModel { get; init; }
    public string? OsVersion { get; init; }
    public string? AppVersion { get; init; }
    public DateTime PairedAt { get; init; }
    public DateTime? LastConnectedAt { get; init; }
    public bool IsConnected { get; init; }
    public ConnectionStatus Status { get; init; }
    public List<string> Permissions { get; init; } = new();
}

public sealed record RemoteSessionDto
{
    public Guid Id { get; init; }
    public Guid DeviceId { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public RemoteControlMode CurrentMode { get; init; }
    public bool IsActive { get; init; }
    public string ConnectionId { get; init; } = string.Empty;
}

public sealed record LibrarySyncInfoDto
{
    public int TotalGames { get; init; }
    public int RecentlyPlayedCount { get; init; }
    public int InstalledCount { get; init; }
    public DateTime LastSyncAt { get; init; }
    public List<GameSummaryDto> RecentlyPlayed { get; init; } = new();
}

public sealed record GameSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? CoverImage { get; init; }
    public string Platform { get; init; } = string.Empty;
    public TimeSpan PlayTime { get; init; }
    public DateTime? LastPlayed { get; init; }
    public GameStatus Status { get; init; }
}

public sealed record SystemStatusDto
{
    public bool IsOnline { get; init; }
    public float CpuUsage { get; init; }
    public float MemoryUsage { get; init; }
    public string? CurrentlyPlayingGame { get; init; }
    public string? CurrentlyPlayingGameCover { get; init; }
    public TimeSpan SessionDuration { get; init; }
    public bool IsRecording { get; init; }
    public bool IsStreaming { get; init; }
}

public sealed record GamepadInputDto
{
    public string Button { get; init; } = string.Empty;
    public bool IsPressed { get; init; }
    public float? AxisX { get; init; }
    public float? AxisY { get; init; }
}

public sealed record TouchpadInputDto
{
    public float X { get; init; }
    public float Y { get; init; }
    public TouchAction Action { get; init; }
    public int? FingerId { get; init; }
}

public sealed record KeyboardInputDto
{
    public string Key { get; init; } = string.Empty;
    public bool IsPressed { get; init; }
    public bool IsModifier { get; init; }
    public List<string> Modifiers { get; init; } = new();
}
