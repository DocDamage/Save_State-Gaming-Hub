namespace SaveState.Application.Mugen.Services.Training;

/// <summary>
/// Recording data.
/// </summary>
public class Recording
{
    public string RecordingId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string? CharacterId { get; set; }
    public string? StageId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime RecordedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public int FrameCount { get; set; }
    public IReadOnlyList<RecordedFrame> Frames { get; set; } = Array.Empty<RecordedFrame>();
    public RecordingMetadata Metadata { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Recorded frame data.
/// </summary>
public class RecordedFrame
{
    public int FrameNumber { get; set; }
    public TimeSpan Timestamp { get; set; }
    public Dictionary<string, object> Inputs { get; set; } = new();
    public Dictionary<string, object> State { get; set; } = new();
}

/// <summary>
/// Recording metadata.
/// </summary>
public class RecordingMetadata
{
    public string? GameVersion { get; set; }
    public string? InputMethod { get; set; }
    public int? MaxComboHits { get; set; }
    public double? TotalDamage { get; set; }
    public bool HasMeterUse { get; set; }
    public string? Difficulty { get; set; }
}

/// <summary>
/// Playback options.
/// </summary>
public class PlaybackOptions
{
    public PlaybackMode Mode { get; set; } = PlaybackMode.Once;
    public float PlaybackSpeed { get; set; } = 1.0f;
    public int LoopCount { get; set; } = 1;
    public bool ShowInputOverlay { get; set; } = false;
    public bool ShowFrameData { get; set; } = false;
    public int StartFrame { get; set; } = 0;
    public int? EndFrame { get; set; }
    public bool MirrorInputs { get; set; } = false;
}

/// <summary>
/// Playback session state.
/// </summary>
public class PlaybackSession
{
    public string SessionId { get; set; } = default!;
    public string RecordingId { get; set; } = default!;
    public PlaybackOptions Options { get; set; } = new();
    public PlaybackStatus Status { get; set; } = PlaybackStatus.Stopped;
    public int CurrentFrame { get; set; }
    public int CurrentLoop { get; set; }
    public DateTime StartedAt { get; set; }
    public TimeSpan ElapsedTime { get; set; }
}

/// <summary>
/// Playback status.
/// </summary>
public enum PlaybackStatus
{
    Stopped,
    Playing,
    Paused,
    Finished
}
