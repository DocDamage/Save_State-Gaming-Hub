// MobileModels.cs
// Data models for Mobile Companion App

using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.Models.Mobile;

/// <summary>
/// Connection status for mobile companion
/// </summary>
public enum MobileConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Paired,
    Error,
    Timeout
}

/// <summary>
/// Remote control mode options
/// </summary>
public enum RemoteControlMode
{
    Gamepad,
    Touchpad,
    Media,
    Keyboard
}

/// <summary>
/// Mobile device information
/// </summary>
public partial class MobileDevice : ObservableObject
{
    [ObservableProperty] private string _deviceId = string.Empty;
    [ObservableProperty] private string _deviceName = string.Empty;
    [ObservableProperty] private string _deviceType = string.Empty;
    [ObservableProperty] private string _osVersion = string.Empty;
    [ObservableProperty] private string _appVersion = string.Empty;
    [ObservableProperty] private DateTime _pairedAt;
    [ObservableProperty] private DateTime _lastConnectedAt;
    [ObservableProperty] private bool _isOnline;
    [ObservableProperty] private List<string> _permissions = new();
}

/// <summary>
/// System status information sent to mobile companion
/// </summary>
public partial class SystemStatus : ObservableObject
{
    [ObservableProperty] private double _cpuUsage;
    [ObservableProperty] private double _ramUsage;
    [ObservableProperty] private double _temperature;
    [ObservableProperty] private double _diskUsage;
    [ObservableProperty] private TimeSpan _uptime;
    [ObservableProperty] private bool _isGaming;
    [ObservableProperty] private string _currentActivity = string.Empty;
}

/// <summary>
/// Game summary for mobile display
/// </summary>
public partial class GameSummary : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _coverImageUrl;
    [ObservableProperty] private string _platform = string.Empty;
    [ObservableProperty] private TimeSpan _totalPlayTime;
    [ObservableProperty] private DateTime? _lastPlayedAt;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private double _completionPercentage;
}

/// <summary>
/// Save state information for mobile companion
/// </summary>
public partial class SaveStateInfo : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _gameId = string.Empty;
    [ObservableProperty] private string _gameTitle = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _thumbnailUrl;
    [ObservableProperty] private DateTime _createdAt;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _isCloudSynced;
    [ObservableProperty] private long _fileSize;
}

/// <summary>
/// Screenshot information for mobile companion
/// </summary>
public partial class ScreenshotInfo : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _gameId = string.Empty;
    [ObservableProperty] private string _gameTitle = string.Empty;
    [ObservableProperty] private string _imageUrl = string.Empty;
    [ObservableProperty] private DateTime _capturedAt;
    [ObservableProperty] private string _resolution = string.Empty;
    [ObservableProperty] private long _fileSize;
}

/// <summary>
/// Companion notification
/// </summary>
public partial class CompanionNotification : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _type = string.Empty;
    [ObservableProperty] private DateTime _timestamp;
    [ObservableProperty] private bool _isRead;
    [ObservableProperty] private string? _actionUrl;
}

/// <summary>
/// Touchpad input data
/// </summary>
public partial class TouchpadInput : ObservableObject
{
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private bool _isPressed;
    [ObservableProperty] private int _fingerCount;
    [ObservableProperty] private string _gestureType = string.Empty;
}

/// <summary>
/// Pairing request from mobile device
/// </summary>
public partial class PairingRequest : ObservableObject
{
    [ObservableProperty] private string _deviceId = string.Empty;
    [ObservableProperty] private string _deviceName = string.Empty;
    [ObservableProperty] private string _deviceType = string.Empty;
    [ObservableProperty] private string _osVersion = string.Empty;
    [ObservableProperty] private string _appVersion = string.Empty;
    [ObservableProperty] private string _pairingCode = string.Empty;
    [ObservableProperty] private List<string> _requestedPermissions = new();
    [ObservableProperty] private DateTime _requestedAt;
}
