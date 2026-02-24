namespace SaveState.Core.MobileCompanion.Models;

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Authenticated,
    Error
}

public enum RemoteControlCommand
{
    // Game Library
    LaunchGame,
    CloseGame,
    PauseGame,
    ResumeGame,

    // Save States
    CreateSaveState,
    LoadSaveState,
    DeleteSaveState,

    // Media Controls
    Play,
    Pause,
    Stop,
    Next,
    Previous,
    VolumeUp,
    VolumeDown,
    Mute,

    // Navigation
    NavigateUp,
    NavigateDown,
    NavigateLeft,
    NavigateRight,
    Select,
    Back,
    Home,

    // Screenshots/Recording
    TakeScreenshot,
    StartRecording,
    StopRecording,

    // Big Picture Mode
    EnterBigPicture,
    ExitBigPicture,

    // Voice
    StartVoiceCommand,
    StopVoiceCommand
}

public enum RemoteControlMode
{
    Gamepad,
    Touchpad,
    MediaControls,
    Keyboard,
    Voice
}

public record MobileDevice
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // iOS, Android, Web
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public DateTime PairedAt { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public string? PushNotificationToken { get; set; }
    public bool IsConnected { get; set; }
    public ConnectionStatus Status { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public record PairingRequest
{
    public Guid Id { get; set; }
    public string PairingCode { get; set; } = string.Empty; // 6-digit code
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public bool IsUsed { get; set; }
    public Guid? PairedDeviceId { get; set; }
}

public record RemoteSession
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public required MobileDevice Device { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public RemoteControlMode CurrentMode { get; set; }
    public bool IsActive { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
}

public record RemoteCommandMessage
{
    public Guid Id { get; set; }
    public RemoteControlCommand Command { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public DateTime Timestamp { get; set; }
    public string? GameId { get; set; }
}

public record GamepadInput
{
    public string Button { get; set; } = string.Empty; // A, B, X, Y, LB, RB, etc.
    public bool IsPressed { get; set; }
    public float? AxisX { get; set; }
    public float? AxisY { get; set; }
}

public record TouchpadInput
{
    public float X { get; set; }
    public float Y { get; set; }
    public TouchAction Action { get; set; }
    public int? FingerId { get; set; }
}

public enum TouchAction
{
    Down,
    Move,
    Up,
    Tap,
    DoubleTap,
    LongPress,
    SwipeLeft,
    SwipeRight,
    SwipeUp,
    SwipeDown,
    Pinch,
    Spread
}

public record KeyboardInput
{
    public string Key { get; set; } = string.Empty;
    public bool IsPressed { get; set; }
    public bool IsModifier { get; set; }
    public List<string> Modifiers { get; set; } = new(); // Ctrl, Alt, Shift
}

public record CompanionNotification
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ActionUrl { get; set; }
    public Dictionary<string, string>? Data { get; set; }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Achievement,
    FriendRequest,
    GameInvite,
    SaveStateReady,
    TournamentStarting
}

public record SystemStatus
{
    public bool IsOnline { get; set; }
    public float CpuUsage { get; set; }
    public float MemoryUsage { get; set; }
    public string? CurrentlyPlayingGame { get; set; }
    public string? CurrentlyPlayingGameCover { get; set; }
    public TimeSpan SessionDuration { get; set; }
    public bool IsRecording { get; set; }
    public bool IsStreaming { get; set; }
}

public record LibrarySyncInfo
{
    public int TotalGames { get; set; }
    public int RecentlyPlayedCount { get; set; }
    public int InstalledCount { get; set; }
    public DateTime LastSyncAt { get; set; }
    public List<GameSummary> RecentlyPlayed { get; set; } = new();
}

public record GameSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string Platform { get; set; } = string.Empty;
    public TimeSpan PlayTime { get; set; }
    public DateTime? LastPlayed { get; set; }
    public GameStatus Status { get; set; }
}

public enum GameStatus
{
    NotInstalled,
    Installing,
    Installed,
    Running,
    Updating
}
