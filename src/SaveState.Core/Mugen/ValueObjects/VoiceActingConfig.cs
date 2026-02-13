namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Configuration for voice acting tools and character voice lines.
/// </summary>
public record VoiceActingConfig
{
    /// <summary>
    /// Voice actor assignments for characters.
    /// </summary>
    public IReadOnlyDictionary<string, VoiceActor> VoiceActors { get; init; } = new Dictionary<string, VoiceActor>();

    /// <summary>
    /// Voice line categories and their configurations.
    /// </summary>
    public IReadOnlyDictionary<VoiceLineCategory, VoiceLineConfig> VoiceLines { get; init; } = new Dictionary<VoiceLineCategory, VoiceLineConfig>();

    /// <summary>
    /// Recording settings for voice capture.
    /// </summary>
    public RecordingSettings RecordingSettings { get; init; } = new();

    /// <summary>
    /// Voice processing effects (pitch, formant shifting, etc.).
    /// </summary>
    public IReadOnlyList<VoiceEffect> VoiceEffects { get; init; } = Array.Empty<VoiceEffect>();

    /// <summary>
    /// Dialogue timing and synchronization settings.
    /// </summary>
    public DialogueTimingConfig Timing { get; init; } = new();
}

/// <summary>
/// Voice actor information.
/// </summary>
public record VoiceActor
{
    /// <summary>
    /// Name of the voice actor.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gender of the voice actor.
    /// </summary>
    public VoiceGender Gender { get; init; }

    /// <summary>
    /// Age range category.
    /// </summary>
    public AgeCategory Age { get; init; }

    /// <summary>
    /// Voice characteristics.
    /// </summary>
    public VoiceCharacteristics Characteristics { get; init; } = new();

    /// <summary>
    /// Sample voice clips for reference.
    /// </summary>
    public IReadOnlyList<string> SampleClips { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Voice gender options.
/// </summary>
public enum VoiceGender
{
    Male,
    Female,
    NonBinary,
    Unknown
}

/// <summary>
/// Age categories for voice actors.
/// </summary>
public enum AgeCategory
{
    Child,
    Teen,
    YoungAdult,
    Adult,
    MiddleAged,
    Senior
}

/// <summary>
/// Voice characteristics.
/// </summary>
public record VoiceCharacteristics
{
    /// <summary>
    /// Pitch range (low to high).
    /// </summary>
    public FloatRange PitchRange { get; init; } = new(85.0f, 255.0f);

    /// <summary>
    /// Tone quality.
    /// </summary>
    public ToneQuality Tone { get; init; } = ToneQuality.Neutral;

    /// <summary>
    /// Accent or regional variation.
    /// </summary>
    public string Accent { get; init; } = "Neutral";

    /// <summary>
    /// Emotional expressiveness (0.0 to 1.0).
    /// </summary>
    public float Expressiveness { get; init; } = 0.7f;

    /// <summary>
    /// Unique voice traits.
    /// </summary>
    public IReadOnlyList<string> Traits { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Tone quality descriptions.
/// </summary>
public enum ToneQuality
{
    Deep,
    Rich,
    Bright,
    Warm,
    Harsh,
    Smooth,
    Neutral
}

/// <summary>
/// Categories of voice lines.
/// </summary>
public enum VoiceLineCategory
{
    Taunts,
    Victory,
    Defeat,
    SpecialMoves,
    Combos,
    Grunts,
    Pain,
    Effort,
    Dialogue
}

/// <summary>
/// Configuration for voice lines in a category.
/// </summary>
public record VoiceLineConfig
{
    /// <summary>
    /// Whether this category is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Volume multiplier for this category (0.0 to 1.0).
    /// </summary>
    public float VolumeMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Voice lines for this category.
    /// </summary>
    public IReadOnlyList<VoiceLine> Lines { get; init; } = Array.Empty<VoiceLine>();

    /// <summary>
    /// Playback rules for this category.
    /// </summary>
    public PlaybackRules Rules { get; init; } = new();
}

/// <summary>
/// Individual voice line.
/// </summary>
public record VoiceLine
{
    /// <summary>
    /// Unique identifier for the voice line.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Trigger event for this voice line.
    /// </summary>
    public string Trigger { get; init; } = string.Empty;

    /// <summary>
    /// Audio file path.
    /// </summary>
    public string AudioFile { get; init; } = string.Empty;

    /// <summary>
    /// Text transcription for reference.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public float Duration { get; init; }

    /// <summary>
    /// Priority for overlapping lines.
    /// </summary>
    public int Priority { get; init; } = 1;

    /// <summary>
    /// Whether this line can interrupt others.
    /// </summary>
    public bool CanInterrupt { get; init; } = false;
}

/// <summary>
/// Playback rules for voice line categories.
/// </summary>
public record PlaybackRules
{
    /// <summary>
    /// Cooldown between playing lines in this category.
    /// </summary>
    public float CooldownSeconds { get; init; } = 2.0f;

    /// <summary>
    /// Maximum concurrent lines from this category.
    /// </summary>
    public int MaxConcurrent { get; init; } = 1;

    /// <summary>
    /// Whether lines can overlap within the category.
    /// </summary>
    public bool AllowOverlap { get; init; } = false;

    /// <summary>
    /// Randomization settings.
    /// </summary>
    public RandomizationSettings Randomization { get; init; } = new();
}

/// <summary>
/// Randomization settings for voice lines.
/// </summary>
public record RandomizationSettings
{
    /// <summary>
    /// Whether to randomize line selection.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Weights for different lines (higher = more likely).
    /// </summary>
    public IReadOnlyDictionary<string, float> Weights { get; init; } = new Dictionary<string, float>();

    /// <summary>
    /// Prevent same line from playing consecutively.
    /// </summary>
    public bool PreventRepeats { get; init; } = true;
}

/// <summary>
/// Recording settings for voice capture.
/// </summary>
public record RecordingSettings
{
    /// <summary>
    /// Audio format for recordings.
    /// </summary>
    public AudioFormat Format { get; init; } = AudioFormat.Wav;

    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public int SampleRate { get; init; } = 44100;

    /// <summary>
    /// Bit depth.
    /// </summary>
    public int BitDepth { get; init; } = 16;

    /// <summary>
    /// Number of channels.
    /// </summary>
    public int Channels { get; init; } = 1;

    /// <summary>
    /// Input device for recording.
    /// </summary>
    public string InputDevice { get; init; } = "Default";

    /// <summary>
    /// Pre-recording buffer settings.
    /// </summary>
    public BufferSettings Buffer { get; init; } = new();
}

/// <summary>
/// Audio format options.
/// </summary>
public enum AudioFormat
{
    Wav,
    Mp3,
    Ogg,
    Flac
}

/// <summary>
/// Buffer settings for recording.
/// </summary>
public record BufferSettings
{
    /// <summary>
    /// Pre-record buffer length in seconds.
    /// </summary>
    public float PreRecordSeconds { get; init; } = 0.5f;

    /// <summary>
    /// Buffer size for low-latency recording.
    /// </summary>
    public int BufferSize { get; init; } = 1024;

    /// <summary>
    /// Number of buffers.
    /// </summary>
    public int BufferCount { get; init; } = 2;
}

/// <summary>
/// Voice processing effects.
/// </summary>
public record VoiceEffect
{
    /// <summary>
    /// Type of voice effect.
    /// </summary>
    public VoiceEffectType Type { get; init; }

    /// <summary>
    /// Whether the effect is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Effect strength (0.0 to 1.0).
    /// </summary>
    public float Strength { get; init; } = 0.5f;

    /// <summary>
    /// Effect-specific parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Types of voice effects.
/// </summary>
public enum VoiceEffectType
{
    PitchShift,
    FormantShift,
    Robot,
    Whisper,
    Megaphone,
    Telephone,
    Reverb,
    Echo,
    Distortion
}

/// <summary>
/// Dialogue timing and synchronization configuration.
/// </summary>
public record DialogueTimingConfig
{
    /// <summary>
    /// Lip sync settings for animated characters.
    /// </summary>
    public LipSyncSettings LipSync { get; init; } = new();

    /// <summary>
    /// Timing offset in milliseconds.
    /// </summary>
    public float TimingOffsetMs { get; init; } = 0.0f;

    /// <summary>
    /// Synchronization with game events.
    /// </summary>
    public EventSyncSettings EventSync { get; init; } = new();
}

/// <summary>
/// Lip sync settings for character animations.
/// </summary>
public record LipSyncSettings
{
    /// <summary>
    /// Whether lip sync is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Sensitivity of lip sync detection.
    /// </summary>
    public float Sensitivity { get; init; } = 0.7f;

    /// <summary>
    /// Smoothing factor for lip movements.
    /// </summary>
    public float Smoothing { get; init; } = 0.3f;

    /// <summary>
    /// Maximum mouth openness (0.0 to 1.0).
    /// </summary>
    public float MaxOpenness { get; init; } = 1.0f;
}

/// <summary>
/// Event synchronization settings.
/// </summary>
public record EventSyncSettings
{
    /// <summary>
    /// Whether to sync with game state changes.
    /// </summary>
    public bool SyncWithGameState { get; init; } = true;

    /// <summary>
    /// Priority for voice lines during intense moments.
    /// </summary>
    public int PriorityDuringAction { get; init; } = 2;

    /// <summary>
    /// Fade out time for interrupted lines.
    /// </summary>
    public float InterruptFadeMs { get; init; } = 100.0f;
}