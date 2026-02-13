namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Enhanced spectator session with additional controls.
/// </summary>
public class SpectatorSession
{
    public string SessionId { get; set; } = default!;
    public string MatchId { get; set; } = default!;
    public string StreamUrl { get; set; } = default!;
    public IReadOnlyList<SpectatorControl> Controls { get; set; } = default!;
    public IReadOnlyList<string> CameraAngles { get; set; } = default!;
    public IReadOnlyList<string> Overlays { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public string? CurrentCameraAngle { get; set; } = default!;
    public DateTime? LastCameraChange { get; set; } = default!;
    public IReadOnlyList<string>? ActiveOverlays { get; set; } = default!;
}

/// <summary>
/// Spectator control definition.
/// </summary>
public class SpectatorControl
{
    public string ControlType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool Enabled { get; set; } = default!;
}

/// <summary>
/// Match spectator data.
/// </summary>
public class MatchSpectatorData
{
    public string MatchId { get; set; } = string.Empty;
    public IReadOnlyList<string> Spectators { get; set; } = new List<string>();
    public int ViewerCount { get; set; }
    public TimeSpan TotalWatchTime { get; set; }
    public int PeakViewerCount { get; set; }
    public bool ChatEnabled { get; set; }
    public IReadOnlyList<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}

/// <summary>
/// Spectator chat message.
/// </summary>
public class ChatMessage
{
    public string MessageId { get; set; } = default!;
    public string MatchId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public string SenderName { get; set; } = default!;
    public string Message { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public SpectatorMessageType MessageType { get; set; } = default!;
}

/// <summary>
/// Spectator message types.
/// </summary>
public enum SpectatorMessageType
{
    Chat,
    System,
    Highlight,
    Reaction
}

/// <summary>
/// Match statistics for spectators.
/// </summary>
public class MatchStatistics
{
    public string MatchId { get; set; } = default!;
    public int ViewerCount { get; set; } = default!;
    public TimeSpan TotalWatchTime { get; set; } = default!;
    public IReadOnlyDictionary<string, int> PopularCameraAngles { get; set; } = default!;
    public int ChatMessageCount { get; set; } = default!;
    public int PeakViewerCount { get; set; } = default!;
    public TimeSpan AverageSessionLength { get; set; } = default!;
}

/// <summary>
/// Match highlights.
/// </summary>
public class MatchHighlights
{
    public string MatchId { get; set; } = default!;
    public IReadOnlyList<SpectatorHighlightMoment> Highlights { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Individual highlight moment.
/// </summary>
public class SpectatorHighlightMoment
{
    public TimeSpan TimeStamp { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public HighlightType HighlightType { get; set; } = default!;
}

/// <summary>
/// Types of highlights.
/// </summary>
public enum HighlightType
{
    Combo,
    Comeback,
    Finisher,
    SpecialMove,
    Throw,
    Counter,
    Perfect
}

/// <summary>
/// Replay request data.
/// </summary>
public class ReplayRequest
{
    public string RequestId { get; set; } = default!;
    public string MatchId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public TimeSpan StartTime { get; set; } = default!;
    public TimeSpan EndTime { get; set; } = default!;
    public DateTime RequestedAt { get; set; } = default!;
    public ReplayStatus Status { get; set; } = default!;
}

/// <summary>
/// Replay request status.
/// </summary>
public enum ReplayStatus
{
    Queued,
    Processing,
    Ready,
    Failed
}
