namespace SaveState.Application.Mugen.Models.NetworkFeatures;

/// <summary>
/// Spectator session.
/// </summary>
public record SpectatorSession(
    string SessionId,
    string MatchId,
    string StreamUrl,
    IReadOnlyList<SpectatorControls> Controls);

/// <summary>
/// Controls available to spectators.
/// </summary>
public record SpectatorControls(
    string ControlType,
    string Description,
    bool Enabled);

/// <summary>
/// Spectator stream information.
/// </summary>
public class SpectatorStream
{
    public string StreamId { get; set; } = default!;
    public string MatchId { get; set; } = default!;
    public string StreamUrl { get; set; } = default!;
    public List<string> SpectatorIds { get; set; } = new();
    public int ViewerCount => SpectatorIds.Count;
    public DateTime StartedAt { get; set; }
    public TimeSpan Duration => DateTime.UtcNow - StartedAt;
    public bool IsLive { get; set; }
    public StreamQuality Quality { get; set; }
}

/// <summary>
/// Stream quality settings.
/// </summary>
public enum StreamQuality
{
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Spectator event types.
/// </summary>
public enum SpectatorEventType
{
    Joined,
    Left,
    CameraChanged,
    PlaybackCommand,
    ChatMessage,
    Reaction
}

/// <summary>
/// Spectator event.
/// </summary>
public record SpectatorEvent(
    string EventId,
    string SessionId,
    string MatchId,
    SpectatorEventType EventType,
    string Data,
    DateTime Timestamp);
