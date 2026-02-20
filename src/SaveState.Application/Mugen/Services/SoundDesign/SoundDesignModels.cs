namespace SaveState.Application.Mugen.Services.SoundDesign;

/// <summary>
/// Audio project data.
/// </summary>
public class SoundDesignStudioAudioProject
{
    public string ProjectId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int SampleRate { get; set; } = default!;
    public int BitDepth { get; set; } = default!;
    public int Channels { get; set; } = default!;
    public float Tempo { get; set; } = default!;
    public SoundDesignStudioTimeSignature SoundDesignStudioTimeSignature { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioAudioTrack> Tracks { get; set; } = default!;
    public SoundDesignStudioAudioBus MasterBus { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioMixSnapshot> MixSnapshots { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastModified { get; set; } = default!;
}

/// <summary>
/// Audio project request.
/// </summary>
public class SoundDesignStudioAudioProjectRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int SampleRate { get; set; } = default!;
    public int BitDepth { get; set; } = default!;
    public int Channels { get; set; } = default!;
    public float Tempo { get; set; } = default!;
    public SoundDesignStudioTimeSignature SoundDesignStudioTimeSignature { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioAudioTrackRequest> InitialTracks { get; set; } = default!;
}

/// <summary>
/// Audio track data.
/// </summary>
public class SoundDesignStudioAudioTrack
{
    public string TrackId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public SoundDesignStudioTrackType Type { get; set; } = default!;
    public string Color { get; set; } = default!;
    public float Volume { get; set; } = default!;
    public float Pan { get; set; } = default!;
    public bool Mute { get; set; } = default!;
    public bool Solo { get; set; } = default!;
    public bool RecordArmed { get; set; } = default!;
    public SoundDesignStudioAudioInputSource InputSource { get; set; } = default!;
    public SoundDesignStudioAudioBus OutputBus { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioAudioEffect> EffectsChain { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioAudioClip> Clips { get; set; } = default!;
    public IReadOnlyDictionary<string , SoundDesignStudioAutomationCurve> Automation { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Audio track request.
/// </summary>
public class SoundDesignStudioAudioTrackRequest
{
    public string Name { get; set; } = default!;
    public SoundDesignStudioTrackType Type { get; set; } = default!;
    public string Color { get; set; } = default!;
    public float Volume { get; set; } = default!;
    public float Pan { get; set; } = default!;
    public SoundDesignStudioAudioInputSource InputSource { get; set; } = default!;
    public SoundDesignStudioAudioBus OutputBus { get; set; } = default!;
    public IReadOnlyList<string> EffectIds { get; set; } = default!;
}

/// <summary>
/// Audio effect data.
/// </summary>
public class SoundDesignStudioAudioEffect
{
    public string EffectId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public SoundDesignStudioAudioEffectType Type { get; set; } = default!;
    public IReadOnlyDictionary<string , object> Parameters { get; set; } = default!;
    public bool Enabled { get; set; } = default!;
    public float WetDryMix { get; set; } = default!;
    public bool Bypass { get; set; } = default!;
    public string? PresetName { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Audio effect request.
/// </summary>
public class SoundDesignStudioAudioEffectRequest
{
    public string Name { get; set; } = default!;
    public SoundDesignStudioAudioEffectType Type { get; set; } = default!;
    public IReadOnlyDictionary<string , object> Parameters { get; set; } = default!;
    public float WetDryMix { get; set; } = default!;
    public string? PresetName { get; set; } = default!;
}

/// <summary>
/// Audio clip data.
/// </summary>
public class SoundDesignStudioAudioClip
{
    public string ClipId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public TimeSpan StartTime { get; set; } = default!;
    public TimeSpan Duration { get; set; } = default!;
    public int SampleRate { get; set; } = default!;
    public int Channels { get; set; } = default!;
    public int BitDepth { get; set; } = default!;
    public IReadOnlyList<float> WaveformData { get; set; } = default!;
    public float Tempo { get; set; } = default!;
    public string Key { get; set; } = default!;
    public DateTime ImportedAt { get; set; } = default!;
}

/// <summary>
/// Audio import request.
/// </summary>
public class SoundDesignStudioAudioImportRequest
{
    public string FilePath { get; set; } = default!;
    public string? Name { get; set; } = default!;
    public TimeSpan StartTime { get; set; } = default!;
}

/// <summary>
/// Audio bus data.
/// </summary>
public class SoundDesignStudioAudioBus
{
    public string BusId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public float Volume { get; set; } = default!;
    public float Pan { get; set; } = default!;
    public bool Mute { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioAudioEffect> EffectsChain { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioAudioSend> Sends { get; set; } = default!;
}

/// <summary>
/// Audio send data.
/// </summary>
public class SoundDesignStudioAudioSend
{
    public string SendId { get; set; } = default!;
    public string TargetBusId { get; set; } = default!;
    public float SendLevel { get; set; } = default!;
    public SoundDesignStudioAudioEffect? PreEffect { get; set; } = default!;
}

/// <summary>
/// Mix snapshot data.
/// </summary>
public class SoundDesignStudioMixSnapshot
{
    public string SnapshotId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string ProjectId { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioTrackState> TrackStates { get; set; } = default!;
    public SoundDesignStudioMasterState SoundDesignStudioMasterState { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Track state data.
/// </summary>
public class SoundDesignStudioTrackState
{
    public string TrackId { get; set; } = default!;
    public float Volume { get; set; } = default!;
    public float Pan { get; set; } = default!;
    public bool Mute { get; set; } = default!;
    public bool Solo { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioEffectState> EffectsStates { get; set; } = default!;
}

/// <summary>
/// Effect state data.
/// </summary>
public class SoundDesignStudioEffectState
{
    public string EffectId { get; set; } = default!;
    public IReadOnlyDictionary<string , object> Parameters { get; set; } = default!;
    public bool Enabled { get; set; } = default!;
    public float WetDryMix { get; set; } = default!;
}

/// <summary>
/// Master state data.
/// </summary>
public class SoundDesignStudioMasterState
{
    public float Volume { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioEffectState> EffectsStates { get; set; } = default!;
}

/// <summary>
/// Audio analysis data.
/// </summary>
public class SoundDesignStudioAudioAnalysis
{
    public string ProjectId { get; set; } = default!;
    public SoundDesignStudioFrequencyAnalysis SoundDesignStudioFrequencyAnalysis { get; set; } = default!;
    public SoundDesignStudioFloatRange DynamicRange { get; set; } = default!;
    public float SpectralCentroid { get; set; } = default!;
    public float ZeroCrossingRate { get; set; } = default!;
    public SoundDesignStudioFloatRange RMSLevels { get; set; } = default!;
    public SoundDesignStudioFloatRange PeakLevels { get; set; } = default!;
    public float LUFS { get; set; } = default!;
    public float StereoWidth { get; set; } = default!;
    public DateTime AnalyzedAt { get; set; } = default!;
}

/// <summary>
/// Render settings for audio export.
/// </summary>
public class SoundDesignStudioRenderSettings
{
    public string Format { get; set; } = default!;
    public int SampleRate { get; set; } = default!;
    public int BitDepth { get; set; } = default!;
    public int BitrateKbps { get; set; } = default!;
    public bool Normalize { get; set; } = default!;
}

/// <summary>
/// Frequency analysis data.
/// </summary>
public class SoundDesignStudioFrequencyAnalysis
{
    public IReadOnlyList<float> Spectrum { get; set; } = default!;
    public IReadOnlyList<float> PeakFrequencies { get; set; } = default!;
    public float DominantFrequency { get; set; } = default!;
    public SoundDesignStudioFloatRange FrequencyRange { get; set; } = default!;
}

/// <summary>
/// Spatial audio setup.
/// </summary>
public class SoundDesignStudioSpatialAudioSetup
{
    public string SetupId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public SoundDesignStudioAudioVector3 ListenerPosition { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioSpatialAudioSource> AudioSources { get; set; } = default!;
    public SoundDesignStudioEnvironmentPreset SoundDesignStudioEnvironmentPreset { get; set; } = default!;
    public SoundDesignStudioReverbSettings SoundDesignStudioReverbSettings { get; set; } = default!;
    public SoundDesignStudioOcclusionSettings SoundDesignStudioOcclusionSettings { get; set; } = default!;
}

/// <summary>
/// Spatial audio request.
/// </summary>
public class SoundDesignStudioSpatialAudioRequest
{
    public string Name { get; set; } = default!;
    public SoundDesignStudioAudioVector3 ListenerPosition { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioSpatialAudioSource> AudioSources { get; set; } = default!;
    public SoundDesignStudioEnvironmentPreset SoundDesignStudioEnvironmentPreset { get; set; } = default!;
    public SoundDesignStudioReverbSettings SoundDesignStudioReverbSettings { get; set; } = default!;
    public SoundDesignStudioOcclusionSettings SoundDesignStudioOcclusionSettings { get; set; } = default!;
}

/// <summary>
/// Spatial audio source.
/// </summary>
public class SoundDesignStudioSpatialAudioSource
{
    public string SourceId { get; set; } = default!;
    public string AudioFile { get; set; } = default!;
    public SoundDesignStudioAudioVector3 Position { get; set; } = default!;
    public float Volume { get; set; } = default!;
    public float MinDistance { get; set; } = default!;
    public float MaxDistance { get; set; } = default!;
}

/// <summary>
/// Reverb settings.
/// </summary>
public class SoundDesignStudioReverbSettings
{
    public float RoomSize { get; set; } = default!;
    public float Damping { get; set; } = default!;
    public float WetLevel { get; set; } = default!;
    public float DryLevel { get; set; } = default!;
    public float PreDelay { get; set; } = default!;
}

/// <summary>
/// Occlusion settings.
/// </summary>
public class SoundDesignStudioOcclusionSettings
{
    public bool Enabled { get; set; } = default!;
    public float OcclusionStrength { get; set; } = default!;
    public float TransmissionLoss { get; set; } = default!;
}

/// <summary>
/// Audio file analysis.
/// </summary>
public class SoundDesignStudioAudioFileAnalysis
{
    public TimeSpan Duration { get; set; } = default!;
    public int SampleRate { get; set; } = default!;
    public int Channels { get; set; } = default!;
    public int BitDepth { get; set; } = default!;
    public IReadOnlyList<float> WaveformData { get; set; } = default!;
    public float Tempo { get; set; } = default!;
    public string Key { get; set; } = default!;
}

/// <summary>
/// Automation curve.
/// </summary>
public class SoundDesignStudioAutomationCurve
{
    public string ParameterName { get; set; } = default!;
    public IReadOnlyList<SoundDesignStudioAutomationPoint> Points { get; set; } = default!;
}

/// <summary>
/// Automation point.
/// </summary>
public class SoundDesignStudioAutomationPoint
{
    public TimeSpan Time { get; set; } = default!;
    public float Value { get; set; } = default!;
    public SoundDesignStudioInterpolationType Interpolation { get; set; } = default!;
}

/// <summary>
/// Audio input source.
/// </summary>
public class SoundDesignStudioAudioInputSource
{
    public string SourceId { get; set; } = default!;
    public SoundDesignStudioAudioInputType Type { get; set; } = default!;
    public string DeviceName { get; set; } = default!;
    public int Channel { get; set; } = default!;
}

/// <summary>
/// Time signature.
/// </summary>
public class SoundDesignStudioTimeSignature
{
    public int Numerator { get; set; } = default!;
    public int Denominator { get; set; } = default!;
}

/// <summary>
/// Float range.
/// </summary>
public class SoundDesignStudioFloatRange
{
    public SoundDesignStudioFloatRange() { }
    public SoundDesignStudioFloatRange(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public float Min { get; set; } = default!;
    public float Max { get; set; } = default!;
}

/// <summary>
/// Track type enumeration.
/// </summary>
public enum SoundDesignStudioTrackType
{
    Audio,
    MIDI,
    Instrument,
    Bus,
    Master
}

/// <summary>
/// Audio effect type enumeration.
/// </summary>
public enum SoundDesignStudioAudioEffectType
{
    EQ,
    Dynamics,
    Reverb,
    Delay,
    Modulation,
    Distortion,
    Filter,
    PitchShift,
    Spatial
}

/// <summary>
/// Audio input type enumeration.
/// </summary>
public enum SoundDesignStudioAudioInputType
{
    Microphone,
    LineIn,
    Instrument,
    Loopback,
    File
}

/// <summary>
/// Environment preset enumeration.
/// </summary>
public enum SoundDesignStudioEnvironmentPreset
{
    Indoor,
    Outdoor,
    Cave,
    Hall,
    Cathedral,
    Stadium,
    Custom
}

/// <summary>
/// Interpolation type enumeration.
/// </summary>
public enum SoundDesignStudioInterpolationType
{
    Linear,
    Smooth,
    Step
}

/// <summary>
/// Vector3 for spatial positioning.
/// </summary>
public class SoundDesignStudioAudioVector3
{
    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
    public float Z { get; set; } = default!;
}
