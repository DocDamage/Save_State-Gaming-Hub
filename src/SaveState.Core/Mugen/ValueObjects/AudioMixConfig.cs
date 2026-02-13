namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Configuration for multi-track audio mixing with effects processing.
/// </summary>
public record AudioMixConfig
{
    /// <summary>
    /// Master volume level (0.0 to 1.0).
    /// </summary>
    public float MasterVolume { get; init; } = 1.0f;

    /// <summary>
    /// Audio tracks in the mix.
    /// </summary>
    public IReadOnlyList<AudioTrack> Tracks { get; init; } = Array.Empty<AudioTrack>();

    /// <summary>
    /// Master effects chain.
    /// </summary>
    public IReadOnlyList<AudioEffect> MasterEffects { get; init; } = Array.Empty<AudioEffect>();

    /// <summary>
    /// Compression settings for the master bus.
    /// </summary>
    public CompressorSettings MasterCompression { get; init; } = new();

    /// <summary>
    /// EQ settings for the master bus.
    /// </summary>
    public EqSettings MasterEq { get; init; } = new();

    /// <summary>
    /// Reverb settings for spatial enhancement.
    /// </summary>
    public ReverbSettings Reverb { get; init; } = new();

    /// <summary>
    /// Limiter settings to prevent clipping.
    /// </summary>
    public LimiterSettings Limiter { get; init; } = new();
}

/// <summary>
/// Individual audio track in the mix.
/// </summary>
public record AudioTrack
{
    /// <summary>
    /// Name of the track.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Type of audio track.
    /// </summary>
    public AudioTrackType TrackType { get; init; }

    /// <summary>
    /// Volume level for this track (0.0 to 1.0).
    /// </summary>
    public float Volume { get; init; } = 1.0f;

    /// <summary>
    /// Pan position (-1.0 left to 1.0 right).
    /// </summary>
    public float Pan { get; init; } = 0.0f;

    /// <summary>
    /// Mute state of the track.
    /// </summary>
    public bool Muted { get; init; } = false;

    /// <summary>
    /// Solo state of the track.
    /// </summary>
    public bool Solo { get; init; } = false;

    /// <summary>
    /// Effects chain for this track.
    /// </summary>
    public IReadOnlyList<AudioEffect> Effects { get; init; } = Array.Empty<AudioEffect>();

    /// <summary>
    /// Send levels to auxiliary buses.
    /// </summary>
    public IReadOnlyDictionary<string, float> Sends { get; init; } = new Dictionary<string, float>();

    /// <summary>
    /// Automation curves for volume, pan, etc.
    /// </summary>
    public IReadOnlyDictionary<string, AutomationCurve> Automation { get; init; } = new Dictionary<string, AutomationCurve>();
}

/// <summary>
/// Types of audio tracks.
/// </summary>
public enum AudioTrackType
{
    Music,
    SoundEffects,
    Voice,
    Ambient,
    Foley,
    Ui,
    Custom
}

/// <summary>
/// Audio effect configuration.
/// </summary>
public record AudioEffect
{
    /// <summary>
    /// Type of audio effect.
    /// </summary>
    public AudioEffectType EffectType { get; init; }

    /// <summary>
    /// Whether the effect is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Wet/dry mix (0.0 = dry, 1.0 = wet).
    /// </summary>
    public float Mix { get; init; } = 1.0f;

    /// <summary>
    /// Effect-specific parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Order in the effects chain.
    /// </summary>
    public int Order { get; init; }
}

/// <summary>
/// Types of audio effects.
/// </summary>
public enum AudioEffectType
{
    Reverb,
    Delay,
    Chorus,
    Flanger,
    Phaser,
    Distortion,
    Overdrive,
    Compressor,
    Equalizer,
    Filter,
    PitchShift,
    Custom
}

/// <summary>
/// Automation curve for parameter control.
/// </summary>
public record AutomationCurve
{
    /// <summary>
    /// Parameter being automated.
    /// </summary>
    public string Parameter { get; init; } = string.Empty;

    /// <summary>
    /// Control points defining the curve.
    /// </summary>
    public IReadOnlyList<AutomationPoint> Points { get; init; } = Array.Empty<AutomationPoint>();

    /// <summary>
    /// Interpolation mode between points.
    /// </summary>
    public InterpolationMode Interpolation { get; init; } = InterpolationMode.Linear;
}

/// <summary>
/// Point on an automation curve.
/// </summary>
public record AutomationPoint
{
    /// <summary>
    /// Time position in seconds.
    /// </summary>
    public float Time { get; init; }

    /// <summary>
    /// Parameter value at this point.
    /// </summary>
    public float Value { get; init; }

    /// <summary>
    /// Curve shape before this point.
    /// </summary>
    public CurveType CurveIn { get; init; } = CurveType.Linear;

    /// <summary>
    /// Curve shape after this point.
    /// </summary>
    public CurveType CurveOut { get; init; } = CurveType.Linear;
}

/// <summary>
/// Interpolation modes for automation.
/// </summary>
public enum InterpolationMode
{
    Linear,
    Smooth,
    Step
}

/// <summary>
/// Curve types for automation points.
/// </summary>
public enum CurveType
{
    Linear,
    Smooth,
    Step,
    Exponential,
    Logarithmic
}

/// <summary>
/// Compressor settings.
/// </summary>
public record CompressorSettings
{
    /// <summary>
    /// Whether compression is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Threshold in dB.
    /// </summary>
    public float ThresholdDb { get; init; } = -12.0f;

    /// <summary>
    /// Ratio (1:1 to infinity:1).
    /// </summary>
    public float Ratio { get; init; } = 4.0f;

    /// <summary>
    /// Attack time in milliseconds.
    /// </summary>
    public float AttackMs { get; init; } = 10.0f;

    /// <summary>
    /// Release time in milliseconds.
    /// </summary>
    public float ReleaseMs { get; init; } = 100.0f;

    /// <summary>
    /// Makeup gain in dB.
    /// </summary>
    public float MakeupGainDb { get; init; } = 0.0f;

    /// <summary>
    /// Knee softness.
    /// </summary>
    public float Knee { get; init; } = 2.0f;
}

/// <summary>
/// EQ settings.
/// </summary>
public record EqSettings
{
    /// <summary>
    /// Whether EQ is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// EQ bands.
    /// </summary>
    public IReadOnlyList<EqBand> Bands { get; init; } = Array.Empty<EqBand>();
}

/// <summary>
/// EQ frequency band.
/// </summary>
public record EqBand
{
    /// <summary>
    /// Frequency in Hz.
    /// </summary>
    public float Frequency { get; init; }

    /// <summary>
    /// Gain in dB.
    /// </summary>
    public float GainDb { get; init; } = 0.0f;

    /// <summary>
    /// Q factor (bandwidth).
    /// </summary>
    public float Q { get; init; } = 1.0f;

    /// <summary>
    /// Filter type.
    /// </summary>
    public FilterType Type { get; init; } = FilterType.Peak;
}

/// <summary>
/// Types of EQ filters.
/// </summary>
public enum FilterType
{
    Peak,
    LowShelf,
    HighShelf,
    LowPass,
    HighPass,
    BandPass,
    Notch
}

/// <summary>
/// Reverb settings.
/// </summary>
public record ReverbSettings
{
    /// <summary>
    /// Whether reverb is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Room size (0.0 to 1.0).
    /// </summary>
    public float RoomSize { get; init; } = 0.5f;

    /// <summary>
    /// Damping factor (0.0 to 1.0).
    /// </summary>
    public float Damping { get; init; } = 0.5f;

    /// <summary>
    /// Wet level (0.0 to 1.0).
    /// </summary>
    public float WetLevel { get; init; } = 0.3f;

    /// <summary>
    /// Dry level (0.0 to 1.0).
    /// </summary>
    public float DryLevel { get; init; } = 0.7f;

    /// <summary>
    /// Pre-delay in milliseconds.
    /// </summary>
    public float PreDelayMs { get; init; } = 20.0f;
}

/// <summary>
/// Limiter settings.
/// </summary>
public record LimiterSettings
{
    /// <summary>
    /// Whether limiter is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Threshold in dB.
    /// </summary>
    public float ThresholdDb { get; init; } = -0.1f;

    /// <summary>
    /// Release time in milliseconds.
    /// </summary>
    public float ReleaseMs { get; init; } = 50.0f;

    /// <summary>
    /// Lookahead time in milliseconds.
    /// </summary>
    public float LookaheadMs { get; init; } = 1.0f;
}