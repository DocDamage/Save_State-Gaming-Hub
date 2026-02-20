using SaveState.Core.Common;
using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.InputRecording;

/// <summary>
/// Type of input device.
/// </summary>
public enum InputDeviceType
{
    Keyboard,
    Mouse,
    Gamepad,
    ArcadeStick,
    LightGun,
    DancePad
}

/// <summary>
/// Type of input recording.
/// </summary>
public enum RecordingType
{
    /// <summary>
    /// Full gameplay recording.
    /// </summary>
    Gameplay,
    
    /// <summary>
    /// Short sequence for combo practice.
    /// </summary>
    ComboSequence,
    
    /// <summary>
    /// Tool-assisted speedrun with frame-perfect inputs.
    /// </summary>
    TAS,
    
    /// <summary>
    /// Tutorial demonstration.
    /// </summary>
    Tutorial,
    
    /// <summary>
    /// Replay for analysis.
    /// </summary>
    AnalysisReplay
}

/// <summary>
/// Status of an input recording.
/// </summary>
public enum RecordingStatus
{
    Recording,
    Paused,
    Completed,
    Processing,
    Ready,
    Corrupted
}

/// <summary>
/// Playback speed options.
/// </summary>
public enum PlaybackSpeed
{
    Quarter = 25,
    Half = 50,
    ThreeQuarter = 75,
    Normal = 100,
    OneAndHalf = 150,
    Double = 200,
    Quadruple = 400,
    Turbo = 800
}

/// <summary>
/// Single frame input state.
/// </summary>
public sealed class InputFrame
{
    /// <summary>
    /// Frame number in the sequence.
    /// </summary>
    public long FrameNumber { get; set; }
    
    /// <summary>
    /// Timestamp relative to recording start.
    /// </summary>
    public TimeSpan Timestamp { get; set; }
    
    /// <summary>
    /// Pressed buttons/keys (platform-specific codes).
    /// </summary>
    public List<string> PressedInputs { get; set; } = new();
    
    /// <summary>
    /// Analog stick/D-pad X axis (-1.0 to 1.0).
    /// </summary>
    public float? AnalogX { get; set; }
    
    /// <summary>
    /// Analog stick/D-pad Y axis (-1.0 to 1.0).
    /// </summary>
    public float? AnalogY { get; set; }
    
    /// <summary>
    /// Trigger/analog button values (0.0 to 1.0).
    /// </summary>
    public Dictionary<string, float> AnalogInputs { get; set; } = new();
    
    /// <summary>
    /// Mouse X position (if applicable).
    /// </summary>
    public int? MouseX { get; set; }
    
    /// <summary>
    /// Mouse Y position (if applicable).
    /// </summary>
    public int? MouseY { get; set; }
    
    /// <summary>
    /// Whether this frame has any input.
    /// </summary>
    public bool HasInput => PressedInputs.Count > 0 || 
                           AnalogX.HasValue || 
                           AnalogY.HasValue || 
                           AnalogInputs.Count > 0 ||
                           MouseX.HasValue ||
                           MouseY.HasValue;
}

/// <summary>
/// Input recording entity.
/// </summary>
public class InputRecording : EntityBase
{
    /// <summary>
    /// Game this recording is for.
    /// </summary>
    public Guid GameId { get; set; }
    
    /// <summary>
    /// Recording name/title.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Type of recording.
    /// </summary>
    public RecordingType Type { get; set; } = RecordingType.Gameplay;
    
    /// <summary>
    /// Current status.
    /// </summary>
    public RecordingStatus Status { get; set; } = RecordingStatus.Recording;
    
    /// <summary>
    /// Input device used.
    /// </summary>
    public InputDeviceType DeviceType { get; set; } = InputDeviceType.Keyboard;
    
    /// <summary>
    /// Total number of frames recorded.
    /// </summary>
    public long TotalFrames { get; set; }
    
    /// <summary>
    /// Duration of the recording.
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Frames per second (for timing).
    /// </summary>
    public int Fps { get; set; } = 60;
    
    /// <summary>
    /// File path to stored recording data.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Starting game state/checkpoint.
    /// </summary>
    public string? StartingState { get; set; }
    
    /// <summary>
    /// Hash of the ROM/game file for validation.
    /// </summary>
    public string? RomHash { get; set; }
    
    /// <summary>
    /// Emulator core used (if applicable).
    /// </summary>
    public string? EmulatorCore { get; set; }
    
    /// <summary>
    /// Game region (NTSC/PAL/etc).
    /// </summary>
    public string? Region { get; set; }
    
    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Number of views/plays.
    /// </summary>
    public int PlayCount { get; set; }
    
    /// <summary>
    /// Personal best time achieved (for speedruns).
    /// </summary>
    public TimeSpan? PersonalBestTime { get; set; }
    
    /// <summary>
    /// Whether this is a verified TAS submission.
    /// </summary>
    public bool IsVerifiedTAS { get; set; }
    
    /// <summary>
    /// TAS authors.
    /// </summary>
    public List<string> Authors { get; set; } = new();
    
    /// <summary>
    /// Recording started at.
    /// </summary>
    public DateTime RecordedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;
    
    /// <summary>
    /// Last played at.
    /// </summary>
    public DateTime? LastPlayedAt { get; set; }
    
    /// <summary>
    /// Last updated at.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;
    
    /// <summary>
    /// Whether the recording is bookmarked.
    /// </summary>
    public bool IsBookmarked { get; set; }
    
    /// <summary>
    /// Bookmarks at specific frames.
    /// </summary>
    public List<RecordingBookmark> Bookmarks { get; set; } = new();
}

/// <summary>
/// Bookmark at a specific frame in a recording.
/// </summary>
public sealed class RecordingBookmark
{
    /// <summary>
    /// Frame number.
    /// </summary>
    public long FrameNumber { get; set; }
    
    /// <summary>
    /// Bookmark label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when bookmark was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Active recording session.
/// </summary>
public sealed class RecordingSession
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Recording being created.
    /// </summary>
    public Guid? RecordingId { get; set; }
    
    /// <summary>
    /// Game being recorded.
    /// </summary>
    public Guid GameId { get; set; }
    
    /// <summary>
    /// When recording started.
    /// </summary>
    public DateTime StartedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;
    
    /// <summary>
    /// Current frame number.
    /// </summary>
    public long CurrentFrame { get; set; }
    
    /// <summary>
    /// Whether currently recording.
    /// </summary>
    public bool IsRecording { get; set; }
    
    /// <summary>
    /// Whether currently paused.
    /// </summary>
    public bool IsPaused { get; set; }
    
    /// <summary>
    /// Collected frames in current session.
    /// </summary>
    public List<InputFrame> BufferedFrames { get; set; } = new();
}

/// <summary>
/// Active playback session.
/// </summary>
public sealed class PlaybackSession
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Recording being played.
    /// </summary>
    public Guid RecordingId { get; set; }
    
    /// <summary>
    /// When playback started.
    /// </summary>
    public DateTime StartedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;
    
    /// <summary>
    /// Current frame position.
    /// </summary>
    public long CurrentFrame { get; set; }
    
    /// <summary>
    /// Total frames in recording.
    /// </summary>
    public long TotalFrames { get; set; }
    
    /// <summary>
    /// Current playback speed.
    /// </summary>
    public PlaybackSpeed Speed { get; set; } = PlaybackSpeed.Normal;
    
    /// <summary>
    /// Whether currently playing.
    /// </summary>
    public bool IsPlaying { get; set; }
    
    /// <summary>
    /// Whether paused.
    /// </summary>
    public bool IsPaused { get; set; }
    
    /// <summary>
    /// Current frame input being sent.
    /// </summary>
    public InputFrame? CurrentInput { get; set; }
}

/// <summary>
/// TAS movie header metadata (for .fm2, .bk2 compatibility).
/// </summary>
public sealed class TASMovieHeader
{
    /// <summary>
    /// Emulator used.
    /// </summary>
    public string Emulator { get; set; } = string.Empty;
    
    /// <summary>
    /// ROM name.
    /// </summary>
    public string RomName { get; set; } = string.Empty;
    
    /// <summary>
    /// ROM SHA1 hash.
    /// </summary>
    public string RomSha1 { get; set; } = string.Empty;
    
    /// <summary>
    /// Authors.
    /// </summary>
    public List<string> Authors { get; set; } = new();
    
    /// <summary>
    /// Total frames.
    /// </summary>
    public long FrameCount { get; set; }
    
    /// <summary>
    /// Frames per second.
    /// </summary>
    public int Fps { get; set; }
    
    /// <summary>
    /// Controller ports used.
    /// </summary>
    public int ControllerCount { get; set; } = 1;
    
    /// <summary>
    /// Whether the movie begins from power-on or savestate.
    /// </summary>
    public bool StartsFromPowerOn { get; set; } = true;
    
    /// <summary>
    /// Starting savestate data (if not from power-on).
    /// </summary>
    public byte[]? StartingSavestate { get; set; }
}

/// <summary>
/// Statistics for an input recording.
/// </summary>
public sealed class InputRecordingStatistics
{
    /// <summary>
    /// Total recordings.
    /// </summary>
    public int TotalRecordings { get; set; }
    
    /// <summary>
    /// Total duration of all recordings.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }
    
    /// <summary>
    /// Total storage used.
    /// </summary>
    public long TotalStorageBytes { get; set; }
    
    /// <summary>
    /// Recordings by type.
    /// </summary>
    public Dictionary<RecordingType, int> RecordingsByType { get; set; } = new();
    
    /// <summary>
    /// Most active recording day.
    /// </summary>
    public DateTime? MostActiveDay { get; set; }
    
    /// <summary>
    /// Average recording duration.
    /// </summary>
    public TimeSpan AverageDuration { get; set; }
    
    /// <summary>
    /// Longest recording.
    /// </summary>
    public TimeSpan LongestRecording { get; set; }
}

/// <summary>
/// Request to start recording.
/// </summary>
public sealed class StartRecordingRequest
{
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RecordingType Type { get; set; } = RecordingType.Gameplay;
    public InputDeviceType DeviceType { get; set; } = InputDeviceType.Keyboard;
    public int Fps { get; set; } = 60;
    public string? StartingState { get; set; }
    public string? RomHash { get; set; }
    public string? EmulatorCore { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request to start playback.
/// </summary>
public sealed class StartPlaybackRequest
{
    public Guid RecordingId { get; set; }
    public PlaybackSpeed Speed { get; set; } = PlaybackSpeed.Normal;
    public long StartFrame { get; set; } = 0;
    public bool Loop { get; set; } = false;
}

/// <summary>
/// Request to export a recording.
/// </summary>
public sealed class ExportRecordingRequest
{
    public Guid RecordingId { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public RecordingExportFormat Format { get; set; } = RecordingExportFormat.Native;
    public bool IncludeMetadata { get; set; } = true;
}

/// <summary>
/// Export format options.
/// </summary>
public enum RecordingExportFormat
{
    Native,
    FM2,      // FCEUX movie format
    BK2,      // BizHawk movie format
    LSMV,     // lsnes movie format
    M64,      // Mupen64 movie format
    TASPROJ   // TAS project file
}

/// <summary>
/// Request to import a recording.
/// </summary>
public sealed class ImportRecordingRequest
{
    public string FilePath { get; set; } = string.Empty;
    public Guid GameId { get; set; }
    public string? CustomName { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Filter for input recordings.
/// </summary>
public sealed class InputRecordingFilter
{
    public Guid? GameId { get; set; }
    public RecordingType? Type { get; set; }
    public RecordingStatus? Status { get; set; }
    public InputDeviceType? DeviceType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string>? Tags { get; set; }
    public bool? OnlyBookmarked { get; set; }
    public bool? OnlyVerifiedTAS { get; set; }
    public string? SearchQuery { get; set; }
}
