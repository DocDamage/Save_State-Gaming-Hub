namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Types of audio enhancements.
/// </summary>
public enum AudioEnhancementType
{
    AudioMixing,
    VoiceActing,
    DynamicMusic,
    SoundSpatialization,
    AudioAnalysis,
    CustomEffects,
    ProceduralAudio
}

/// <summary>
/// Configuration for dynamic music that changes based on match state.
/// </summary>
public record DynamicMusicConfig
{
    /// <summary>
    /// Whether dynamic music is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Music tracks for different game states.
    /// </summary>
    public IReadOnlyDictionary<GameState, MusicTrack> StateTracks { get; init; } = new Dictionary<GameState, MusicTrack>();

    /// <summary>
    /// Transition settings between tracks.
    /// </summary>
    public MusicTransitionSettings Transitions { get; init; } = new();

    /// <summary>
    /// Intensity-based music layers.
    /// </summary>
    public IReadOnlyList<MusicLayer> IntensityLayers { get; init; } = Array.Empty<MusicLayer>();

    /// <summary>
    /// Stinger sounds for special events.
    /// </summary>
    public IReadOnlyDictionary<SpecialEvent, StingerSound> Stingers { get; init; } = new Dictionary<SpecialEvent, StingerSound>();
}

/// <summary>
/// Game states for dynamic music.
/// </summary>
public enum GameState
{
    Menu,
    CharacterSelect,
    Loading,
    RoundStart,
    Fighting,
    Combo,
    SpecialMove,
    SuperMove,
    Danger,
    Victory,
    Defeat,
    GameOver
}

/// <summary>
/// Music track configuration.
/// </summary>
public record MusicTrack
{
    /// <summary>
    /// Name of the track.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Audio file path.
    /// </summary>
    public string AudioFile { get; init; } = string.Empty;

    /// <summary>
    /// Volume level (0.0 to 1.0).
    /// </summary>
    public float Volume { get; init; } = 1.0f;

    /// <summary>
    /// Loop settings.
    /// </summary>
    public LoopSettings Loop { get; init; } = new();

    /// <summary>
    /// BPM for synchronization.
    /// </summary>
    public float Bpm { get; init; } = 120.0f;
}

/// <summary>
/// Loop settings for music tracks.
/// </summary>
public record LoopSettings
{
    /// <summary>
    /// Whether the track loops.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Start time for looping in seconds.
    /// </summary>
    public float LoopStart { get; init; } = 0.0f;

    /// <summary>
    /// End time for looping in seconds (0 = end of track).
    /// </summary>
    public float LoopEnd { get; init; } = 0.0f;

    /// <summary>
    /// Crossfade time in seconds.
    /// </summary>
    public float Crossfade { get; init; } = 2.0f;
}

/// <summary>
/// Music transition settings.
/// </summary>
public record MusicTransitionSettings
{
    /// <summary>
    /// Fade in time in seconds.
    /// </summary>
    public float FadeInTime { get; init; } = 1.0f;

    /// <summary>
    /// Fade out time in seconds.
    /// </summary>
    public float FadeOutTime { get; init; } = 1.0f;

    /// <summary>
    /// Transition curve type.
    /// </summary>
    public TransitionCurve Curve { get; init; } = TransitionCurve.Linear;

    /// <summary>
    /// Whether to sync transitions to beat.
    /// </summary>
    public bool SyncToBeat { get; init; } = false;
}

/// <summary>
/// Transition curve types.
/// </summary>
public enum TransitionCurve
{
    Linear,
    Smooth,
    Exponential,
    Logarithmic,
    SCurve
}

/// <summary>
/// Music layer for intensity-based mixing.
/// </summary>
public record MusicLayer
{
    /// <summary>
    /// Name of the layer.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Audio file for this layer.
    /// </summary>
    public string AudioFile { get; init; } = string.Empty;

    /// <summary>
    /// Intensity range where this layer is active (0.0 to 1.0).
    /// </summary>
    public FloatRange IntensityRange { get; init; } = new(0.0f, 1.0f);

    /// <summary>
    /// Volume curve based on intensity.
    /// </summary>
    public VolumeCurve VolumeCurve { get; init; } = new();

    /// <summary>
    /// Filter settings that change with intensity.
    /// </summary>
    public IntensityFilter Filters { get; init; } = new();
}

/// <summary>
/// Volume curve for music layers.
/// </summary>
public record VolumeCurve
{
    /// <summary>
    /// Control points for volume curve.
    /// </summary>
    public IReadOnlyList<CurvePoint> Points { get; init; } = Array.Empty<CurvePoint>();
}

/// <summary>
/// Point on a volume curve.
/// </summary>
public record CurvePoint(float Intensity, float Volume);

/// <summary>
/// Filter settings that change with intensity.
/// </summary>
public record IntensityFilter
{
    /// <summary>
    /// Low-pass filter cutoff changes.
    /// </summary>
    public FilterCurve LowPass { get; init; } = new();

    /// <summary>
    /// High-pass filter cutoff changes.
    /// </summary>
    public FilterCurve HighPass { get; init; } = new();

    /// <summary>
    /// Pitch shift based on intensity.
    /// </summary>
    public float PitchShift { get; init; } = 0.0f;
}

/// <summary>
/// Filter curve for intensity-based changes.
/// </summary>
public record FilterCurve
{
    /// <summary>
    /// Whether this filter is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Frequency curve points.
    /// </summary>
    public IReadOnlyList<CurvePoint> FrequencyCurve { get; init; } = Array.Empty<CurvePoint>();
}

/// <summary>
/// Special event types for stingers.
/// </summary>
public enum SpecialEvent
{
    Perfect,
    UltraCombo,
    Comeback,
    TimeUp,
    RingOut,
    CounterHit,
    Custom
}

/// <summary>
/// Stinger sound for special events.
/// </summary>
public record StingerSound
{
    /// <summary>
    /// Audio file for the stinger.
    /// </summary>
    public string AudioFile { get; init; } = string.Empty;

    /// <summary>
    /// Volume level (0.0 to 1.0).
    /// </summary>
    public float Volume { get; init; } = 1.0f;

    /// <summary>
    /// Whether to interrupt current music.
    /// </summary>
    public bool InterruptMusic { get; init; } = true;

    /// <summary>
    /// Fade out time for interrupted music.
    /// </summary>
    public float MusicFadeOut { get; init; } = 0.2f;

    /// <summary>
    /// Resume music after stinger.
    /// </summary>
    public bool ResumeAfter { get; init; } = true;
}

/// <summary>
/// Configuration for 3D sound spatialization.
/// </summary>
public record SoundSpatializationConfig
{
    /// <summary>
    /// Whether spatialization is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Spatialization mode.
    /// </summary>
    public SpatializationMode Mode { get; init; } = SpatializationMode.Hrtf;

    /// <summary>
    /// Listener position and orientation.
    /// </summary>
    public ListenerConfig Listener { get; init; } = new();

    /// <summary>
    /// Sound sources and their positions.
    /// </summary>
    public IReadOnlyDictionary<string, SoundSource> Sources { get; init; } = new Dictionary<string, SoundSource>();

    /// <summary>
    /// Reverb zones for environmental audio.
    /// </summary>
    public IReadOnlyList<ReverbZone> ReverbZones { get; init; } = Array.Empty<ReverbZone>();

    /// <summary>
    /// Occlusion settings for sound blocking.
    /// </summary>
    public OcclusionSettings Occlusion { get; init; } = new();
}

/// <summary>
/// Spatialization modes.
/// </summary>
public enum SpatializationMode
{
    Stereo,
    Surround,
    Hrtf,
    Ambisonics
}

/// <summary>
/// Listener configuration for spatial audio.
/// </summary>
public record ListenerConfig
{
    /// <summary>
    /// Listener position in 3D space.
    /// </summary>
    public Position3D Position { get; init; } = new();

    /// <summary>
    /// Listener orientation (forward and up vectors).
    /// </summary>
    public Orientation Orientation { get; init; } = new();

    /// <summary>
    /// Listener movement settings.
    /// </summary>
    public ListenerMovement Movement { get; init; } = new();
}

/// <summary>
/// 3D position.
/// </summary>
public record Position3D(float X = 0, float Y = 0, float Z = 0);

/// <summary>
/// Orientation with forward and up vectors.
/// </summary>
public record Orientation
{
    /// <summary>
    /// Forward direction vector.
    /// </summary>
    public Vector3D Forward { get; init; } = new(0, 0, 1);

    /// <summary>
    /// Up direction vector.
    /// </summary>
    public Vector3D Up { get; init; } = new(0, 1, 0);
}

/// <summary>
/// 3D vector.
/// </summary>
public record Vector3D(float X, float Y, float Z);

/// <summary>
/// Listener movement settings.
/// </summary>
public record ListenerMovement
{
    /// <summary>
    /// Doppler effect strength.
    /// </summary>
    public float DopplerFactor { get; init; } = 1.0f;

    /// <summary>
    /// Speed of sound for Doppler calculations.
    /// </summary>
    public float SpeedOfSound { get; init; } = 343.0f;

    /// <summary>
    /// Whether listener follows camera.
    /// </summary>
    public bool FollowCamera { get; init; } = true;
}

/// <summary>
/// Sound source configuration.
/// </summary>
public record SoundSource
{
    /// <summary>
    /// Position of the sound source.
    /// </summary>
    public Position3D Position { get; init; } = new();

    /// <summary>
    /// Inner and outer radius for distance attenuation.
    /// </summary>
    public DistanceAttenuation Attenuation { get; init; } = new();

    /// <summary>
    /// Directivity pattern of the sound source.
    /// </summary>
    public DirectivityPattern Directivity { get; init; } = new();

    /// <summary>
    /// Air absorption settings.
    /// </summary>
    public AirAbsorption Absorption { get; init; } = new();
}

/// <summary>
/// Distance attenuation settings.
/// </summary>
public record DistanceAttenuation
{
    /// <summary>
    /// Inner radius where volume is maximum.
    /// </summary>
    public float InnerRadius { get; init; } = 1.0f;

    /// <summary>
    /// Outer radius where volume reaches minimum.
    /// </summary>
    public float OuterRadius { get; init; } = 100.0f;

    /// <summary>
    /// Volume at outer radius (0.0 to 1.0).
    /// </summary>
    public float OuterVolume { get; init; } = 0.0f;

    /// <summary>
    /// Attenuation curve type.
    /// </summary>
    public AttenuationCurve Curve { get; init; } = AttenuationCurve.Linear;
}

/// <summary>
/// Attenuation curve types.
/// </summary>
public enum AttenuationCurve
{
    Linear,
    Logarithmic,
    Inverse,
    InverseSquare,
    Custom
}

/// <summary>
/// Directivity pattern for sound sources.
/// </summary>
public record DirectivityPattern
{
    /// <summary>
    /// Pattern type.
    /// </summary>
    public DirectivityType Type { get; init; } = DirectivityType.Omni;

    /// <summary>
    /// Directivity factor (0 = omni, 1 = directional).
    /// </summary>
    public float Sharpness { get; init; } = 0.0f;

    /// <summary>
    /// Orientation of the directivity pattern.
    /// </summary>
    public Orientation PatternOrientation { get; init; } = new();
}

/// <summary>
/// Types of directivity patterns.
/// </summary>
public enum DirectivityType
{
    Omni,
    Cardioid,
    FigureEight,
    Custom
}

/// <summary>
/// Air absorption settings.
/// </summary>
public record AirAbsorption
{
    /// <summary>
    /// Whether air absorption is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Absorption coefficient.
    /// </summary>
    public float Coefficient { get; init; } = 0.0002f;

    /// <summary>
    /// Frequency-dependent absorption.
    /// </summary>
    public bool FrequencyDependent { get; init; } = true;
}

/// <summary>
/// Reverb zone for environmental audio.
/// </summary>
public record ReverbZone
{
    /// <summary>
    /// Name of the reverb zone.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Position of the zone center.
    /// </summary>
    public Position3D Center { get; init; } = new();

    /// <summary>
    /// Radius of the reverb zone.
    /// </summary>
    public float Radius { get; init; } = 50.0f;

    /// <summary>
    /// Reverb settings for this zone.
    /// </summary>
    public ReverbSettings Reverb { get; init; } = new();

    /// <summary>
    /// Transition settings for zone boundaries.
    /// </summary>
    public ZoneTransition Transition { get; init; } = new();
}

/// <summary>
/// Zone transition settings.
/// </summary>
public record ZoneTransition
{
    /// <summary>
    /// Transition width at zone boundaries.
    /// </summary>
    public float Width { get; init; } = 10.0f;

    /// <summary>
    /// Transition curve.
    /// </summary>
    public TransitionCurve Curve { get; init; } = TransitionCurve.Linear;
}

/// <summary>
/// Occlusion settings for sound blocking.
/// </summary>
public record OcclusionSettings
{
    /// <summary>
    /// Whether occlusion is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Occlusion factor (0 = no occlusion, 1 = complete blocking).
    /// </summary>
    public float Factor { get; init; } = 0.5f;

    /// <summary>
    /// Frequency-dependent occlusion.
    /// </summary>
    public bool FrequencyDependent { get; init; } = true;

    /// <summary>
    /// Low frequency occlusion factor.
    /// </summary>
    public float LowFreqFactor { get; init; } = 0.3f;

    /// <summary>
    /// High frequency occlusion factor.
    /// </summary>
    public float HighFreqFactor { get; init; } = 0.8f;
}