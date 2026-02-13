namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Result of audio analysis and balancing.
/// </summary>
public record AudioAnalysisResult
{
    /// <summary>
    /// Path to the analyzed audio file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Duration of the audio in seconds.
    /// </summary>
    public float Duration { get; init; }

    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    /// Number of channels.
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    /// Bit depth.
    /// </summary>
    public int BitDepth { get; init; }

    /// <summary>
    /// Peak level in dBFS.
    /// </summary>
    public float PeakLevelDb { get; init; }

    /// <summary>
    /// RMS level in dBFS.
    /// </summary>
    public float RmsLevelDb { get; init; }

    /// <summary>
    /// Dynamic range in dB.
    /// </summary>
    public float DynamicRange { get; init; }

    /// <summary>
    /// Crest factor.
    /// </summary>
    public float CrestFactor { get; init; }

    /// <summary>
    /// Frequency analysis data.
    /// </summary>
    public FrequencyAnalysis FrequencyAnalysis { get; init; } = new();

    /// <summary>
    /// Loudness measurements (LUFS).
    /// </summary>
    public LoudnessMeasurements Loudness { get; init; } = new();

    /// <summary>
    /// Recommended processing suggestions.
    /// </summary>
    public IReadOnlyList<ProcessingSuggestion> Suggestions { get; init; } = Array.Empty<ProcessingSuggestion>();
}

/// <summary>
/// Frequency analysis data.
/// </summary>
public record FrequencyAnalysis
{
    /// <summary>
    /// Frequency spectrum data (magnitude by frequency bin).
    /// </summary>
    public IReadOnlyList<SpectrumPoint> Spectrum { get; init; } = Array.Empty<SpectrumPoint>();

    /// <summary>
    /// Dominant frequencies.
    /// </summary>
    public IReadOnlyList<float> DominantFrequencies { get; init; } = Array.Empty<float>();

    /// <summary>
    /// Spectral centroid.
    /// </summary>
    public float SpectralCentroid { get; init; }

    /// <summary>
    /// Spectral rolloff (95% energy).
    /// </summary>
    public float SpectralRolloff { get; init; }
}

/// <summary>
/// Point in frequency spectrum.
/// </summary>
public record SpectrumPoint(float Frequency, float Magnitude);

/// <summary>
/// Loudness measurements.
/// </summary>
public record LoudnessMeasurements
{
    /// <summary>
    /// Integrated loudness (LUFS).
    /// </summary>
    public float Integrated { get; init; }

    /// <summary>
    /// Short-term loudness (LUFS).
    /// </summary>
    public float ShortTerm { get; init; }

    /// <summary>
    /// Momentary loudness (LUFS).
    /// </summary>
    public float Momentary { get; init; }

    /// <summary>
    /// Loudness range (LRA).
    /// </summary>
    public float LoudnessRange { get; init; }

    /// <summary>
    /// True peak level (dBTP).
    /// </summary>
    public float TruePeak { get; init; }
}

/// <summary>
/// Processing suggestion from analysis.
/// </summary>
public record ProcessingSuggestion
{
    /// <summary>
    /// Type of processing suggested.
    /// </summary>
    public SuggestionType Type { get; init; }

    /// <summary>
    /// Description of the suggestion.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Severity of the issue (1-10).
    /// </summary>
    public int Severity { get; init; }

    /// <summary>
    /// Suggested parameter values.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Types of processing suggestions.
/// </summary>
public enum SuggestionType
{
    Normalization,
    Compression,
    Eq,
    NoiseReduction,
    DeEssing,
    StereoWidening,
    LoudnessCorrection
}

/// <summary>
/// Batch processing results.
/// </summary>
public record AudioBatchProcessingResult
{
    /// <summary>
    /// Total number of files processed.
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Number of successfully processed files.
    /// </summary>
    public int ProcessedFiles { get; init; }

    /// <summary>
    /// Number of files that failed processing.
    /// </summary>
    public int FailedFiles { get; init; }

    /// <summary>
    /// Individual file results.
    /// </summary>
    public IReadOnlyList<AudioFileProcessingResult> FileResults { get; init; } = Array.Empty<AudioFileProcessingResult>();

    /// <summary>
    /// Processing statistics.
    /// </summary>
    public BatchProcessingStats Stats { get; init; } = new();
}

/// <summary>
/// Individual file processing result.
/// </summary>
public record AudioFileProcessingResult
{
    /// <summary>
    /// Path to the processed file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Whether processing was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Processing time in seconds.
    /// </summary>
    public float ProcessingTime { get; init; }

    /// <summary>
    /// Before and after analysis results.
    /// </summary>
    public ProcessingComparison Comparison { get; init; } = new();
}

/// <summary>
/// Comparison of before/after processing.
/// </summary>
public record ProcessingComparison
{
    /// <summary>
    /// Analysis before processing.
    /// </summary>
    public AudioAnalysisResult? Before { get; init; }

    /// <summary>
    /// Analysis after processing.
    /// </summary>
    public AudioAnalysisResult? After { get; init; }
}

/// <summary>
/// Batch processing statistics.
/// </summary>
public record BatchProcessingStats
{
    /// <summary>
    /// Total processing time in seconds.
    /// </summary>
    public float TotalTime { get; init; }

    /// <summary>
    /// Average processing time per file.
    /// </summary>
    public float AverageTimePerFile { get; init; }

    /// <summary>
    /// Average improvement in peak level.
    /// </summary>
    public float AveragePeakImprovement { get; init; }

    /// <summary>
    /// Average improvement in RMS level.
    /// </summary>
    public float AverageRmsImprovement { get; init; }
}

/// <summary>
/// Configuration for batch audio processing.
/// </summary>
public record AudioBatchProcessingConfig
{
    /// <summary>
    /// Processing operations to apply.
    /// </summary>
    public IReadOnlyList<ProcessingOperation> Operations { get; init; } = Array.Empty<ProcessingOperation>();

    /// <summary>
    /// Whether to create backup files.
    /// </summary>
    public bool CreateBackups { get; init; } = true;

    /// <summary>
    /// Output format for processed files.
    /// </summary>
    public AudioFormat OutputFormat { get; init; } = AudioFormat.Wav;

    /// <summary>
    /// Quality settings for lossy formats.
    /// </summary>
    public int Quality { get; init; } = 320;

    /// <summary>
    /// Whether to analyze files before processing.
    /// </summary>
    public bool PreAnalysis { get; init; } = true;

    /// <summary>
    /// Whether to analyze files after processing.
    /// </summary>
    public bool PostAnalysis { get; init; } = true;

    /// <summary>
    /// Parallel processing settings.
    /// </summary>
    public ParallelProcessingSettings Parallel { get; init; } = new();
}

/// <summary>
/// Processing operation to apply.
/// </summary>
public record ProcessingOperation
{
    /// <summary>
    /// Type of operation.
    /// </summary>
    public ProcessingOperationType Type { get; init; }

    /// <summary>
    /// Whether this operation is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Order of execution.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Operation-specific parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Types of processing operations.
/// </summary>
public enum ProcessingOperationType
{
    Normalize,
    Compress,
    Equalize,
    DeNoise,
    DeEss,
    StereoEnhance,
    LoudnessNormalize,
    TrimSilence,
    FadeInOut
}

/// <summary>
/// Parallel processing settings.
/// </summary>
public record ParallelProcessingSettings
{
    /// <summary>
    /// Whether to use parallel processing.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Maximum number of concurrent operations.
    /// </summary>
    public int MaxConcurrency { get; init; } = 4;

    /// <summary>
    /// Whether to process files in order.
    /// </summary>
    public bool MaintainOrder { get; init; } = false;
}

/// <summary>
/// Custom audio effect configuration.
/// </summary>
public record CustomAudioEffectConfig
{
    /// <summary>
    /// Name of the custom effect.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of the effect.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Effect implementation script or code.
    /// </summary>
    public string Implementation { get; init; } = string.Empty;

    /// <summary>
    /// Language of the implementation.
    /// </summary>
    public string Language { get; init; } = "Lua";

    /// <summary>
    /// User-adjustable parameters.
    /// </summary>
    public IReadOnlyList<EffectParameter> Parameters { get; init; } = Array.Empty<EffectParameter>();

    /// <summary>
    /// Effect category for organization.
    /// </summary>
    public string Category { get; init; } = "Custom";

    /// <summary>
    /// Author of the effect.
    /// </summary>
    public string Author { get; init; } = string.Empty;
}

/// <summary>
/// Parameter for custom audio effects.
/// </summary>
public record EffectParameter
{
    /// <summary>
    /// Parameter name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Parameter type.
    /// </summary>
    public ParameterType Type { get; init; }

    /// <summary>
    /// Default value.
    /// </summary>
    public object DefaultValue { get; init; } = new object();

    /// <summary>
    /// Minimum value (for numeric types).
    /// </summary>
    public object? MinValue { get; init; }

    /// <summary>
    /// Maximum value (for numeric types).
    /// </summary>
    public object? MaxValue { get; init; }

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Units for the parameter.
    /// </summary>
    public string Units { get; init; } = string.Empty;
}

/// <summary>
/// Types of effect parameters.
/// </summary>
public enum ParameterType
{
    Float,
    Int,
    Bool,
    String,
    Choice
}

/// <summary>
/// Procedural audio generation configuration.
/// </summary>
public record ProceduralAudioConfig
{
    /// <summary>
    /// Type of procedural audio to generate.
    /// </summary>
    public ProceduralAudioType Type { get; init; }

    /// <summary>
    /// Generation parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Duration of generated audio in seconds.
    /// </summary>
    public float Duration { get; init; } = 1.0f;

    /// <summary>
    /// Seed for reproducible generation.
    /// </summary>
    public int Seed { get; init; } = 0;

    /// <summary>
    /// Audio format for output.
    /// </summary>
    public AudioFormat OutputFormat { get; init; } = AudioFormat.Wav;

    /// <summary>
    /// Post-processing effects.
    /// </summary>
    public IReadOnlyList<AudioEffect> PostEffects { get; init; } = Array.Empty<AudioEffect>();
}

/// <summary>
/// Types of procedural audio.
/// </summary>
public enum ProceduralAudioType
{
    Impact,
    Swipe,
    Explosion,
    Footstep,
    Whoosh,
    Spark,
    Custom
}

/// <summary>
/// Audio preview data.
/// </summary>
public record AudioPreview
{
    /// <summary>
    /// Type of enhancement being previewed.
    /// </summary>
    public AudioEnhancementType EnhancementType { get; init; }

    /// <summary>
    /// Name of the preview.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Audio data for preview playback.
    /// </summary>
    public byte[] AudioData { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Duration of the preview.
    /// </summary>
    public float Duration { get; init; }

    /// <summary>
    /// Whether the preview is currently playing.
    /// </summary>
    public bool IsPlaying { get; init; }

    /// <summary>
    /// Performance impact rating.
    /// </summary>
    public PerformanceImpact PerformanceImpact { get; init; } = PerformanceImpact.Medium;
}

/// <summary>
/// Saved audio configuration preset.
/// </summary>
public record AudioPreset
{
    /// <summary>
    /// Unique identifier for the preset.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name of the preset.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of the preset.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Author of the preset.
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// Version of the preset.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Audio mix configuration.
    /// </summary>
    public AudioMixConfig? MixConfig { get; init; }

    /// <summary>
    /// Voice acting configuration.
    /// </summary>
    public VoiceActingConfig? VoiceConfig { get; init; }

    /// <summary>
    /// Dynamic music configuration.
    /// </summary>
    public DynamicMusicConfig? MusicConfig { get; init; }

    /// <summary>
    /// Sound spatialization configuration.
    /// </summary>
    public SoundSpatializationConfig? SpatialConfig { get; init; }

    /// <summary>
    /// Custom audio effects.
    /// </summary>
    public IReadOnlyList<CustomAudioEffectConfig> CustomEffects { get; init; } = Array.Empty<CustomAudioEffectConfig>();

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this is a built-in preset.
    /// </summary>
    public bool IsBuiltIn { get; init; } = false;

    /// <summary>
    /// Preview audio file.
    /// </summary>
    public string PreviewAudio { get; init; } = string.Empty;
}

/// <summary>
/// Current status of the sound design studio.
/// </summary>
public record SoundStudioStatus
{
    /// <summary>
    /// Whether the studio is initialized.
    /// </summary>
    public bool IsInitialized { get; init; }

    /// <summary>
    /// Whether the studio is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Currently loaded preset name.
    /// </summary>
    public string CurrentPreset { get; init; } = "Default";

    /// <summary>
    /// Active enhancements.
    /// </summary>
    public IReadOnlyList<AudioEnhancementType> ActiveEnhancements { get; init; } = Array.Empty<AudioEnhancementType>();

    /// <summary>
    /// Current audio metrics.
    /// </summary>
    public AudioMetrics Metrics { get; init; } = new();

    /// <summary>
    /// Any active warnings or errors.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Audio capabilities.
    /// </summary>
    public AudioCapabilities Capabilities { get; init; } = new();
}

/// <summary>
/// Current audio metrics.
/// </summary>
public record AudioMetrics
{
    /// <summary>
    /// Number of active audio sources.
    /// </summary>
    public int ActiveSources { get; init; }

    /// <summary>
    /// CPU usage for audio processing.
    /// </summary>
    public float CpuUsagePercent { get; init; }

    /// <summary>
    /// Audio latency in milliseconds.
    /// </summary>
    public float LatencyMs { get; init; }

    /// <summary>
    /// Current master volume.
    /// </summary>
    public float MasterVolume { get; init; }

    /// <summary>
    /// Currently playing music track.
    /// </summary>
    public string CurrentMusicTrack { get; init; } = string.Empty;
}

/// <summary>
/// Audio capabilities of the system.
/// </summary>
public record AudioCapabilities
{
    /// <summary>
    /// Maximum number of audio sources.
    /// </summary>
    public int MaxSources { get; init; } = 32;

    /// <summary>
    /// Supported sample rates.
    /// </summary>
    public IReadOnlyList<int> SupportedSampleRates { get; init; } = new[] { 44100, 48000, 96000 };

    /// <summary>
    /// Whether 3D spatialization is supported.
    /// </summary>
    public bool SupportsSpatialization { get; init; } = true;

    /// <summary>
    /// Whether HRTF is supported.
    /// </summary>
    public bool SupportsHrtf { get; init; } = true;

    /// <summary>
    /// Maximum reverb zones.
    /// </summary>
    public int MaxReverbZones { get; init; } = 8;

    /// <summary>
    /// Audio API version.
    /// </summary>
    public string AudioApi { get; init; } = "OpenAL 1.1";
}