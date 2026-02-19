using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Comprehensive service for MUGEN sound design and audio management.
/// Provides sound effect creation, BGM management, voice synthesis, and audio editing.
/// </summary>
public interface ISoundDesignService
{
    #region Sound Effect Management

    /// <summary>
    /// Loads sound effects from a directory.
    /// </summary>
    Task<Result<IReadOnlyList<SoundEffect>>> LoadSoundEffectsAsync(
        string directoryPath,
        CancellationToken ct = default);

    /// <summary>
    /// Imports a sound effect file.
    /// </summary>
    Task<Result<SoundEffect>> ImportSoundEffectAsync(
        string filePath,
        SoundEffectMetadata metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Exports sound effect to file.
    /// </summary>
    Task<Result> ExportSoundEffectAsync(
        SoundEffect soundEffect,
        string outputPath,
        SoundExportFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new sound effect from synthesis.
    /// </summary>
    Task<Result<SoundEffect>> CreateSynthesizedSoundAsync(
        SynthesisParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Edits a sound effect with effects.
    /// </summary>
    Task<Result<SoundEffect>> EditSoundEffectAsync(
        SoundEffect source,
        AudioEffectChain effects,
        CancellationToken ct = default);

    /// <summary>
    /// Trims silence from sound effect.
    /// </summary>
    Task<Result<SoundEffect>> TrimSilenceAsync(
        SoundEffect soundEffect,
        TrimOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Normalizes audio volume.
    /// </summary>
    Task<Result<SoundEffect>> NormalizeAsync(
        SoundEffect soundEffect,
        NormalizationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Changes playback speed without pitch change.
    /// </summary>
    Task<Result<SoundEffect>> TimeStretchAsync(
        SoundEffect soundEffect,
        double ratio,
        CancellationToken ct = default);

    /// <summary>
    /// Changes pitch without affecting speed.
    /// </summary>
    Task<Result<SoundEffect>> PitchShiftAsync(
        SoundEffect soundEffect,
        double semitones,
        CancellationToken ct = default);

    /// <summary>
    /// Applies reverb effect.
    /// </summary>
    Task<Result<SoundEffect>> ApplyReverbAsync(
        SoundEffect soundEffect,
        ReverbParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Applies equalization.
    /// </summary>
    Task<Result<SoundEffect>> ApplyEqualizationAsync(
        SoundEffect soundEffect,
        EqualizerSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Mixes multiple sound effects.
    /// </summary>
    Task<Result<SoundEffect>> MixSoundsAsync(
        IReadOnlyList<SoundEffect> sounds,
        MixOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets sound effect by ID.
    /// </summary>
    Task<Result<SoundEffect>> GetSoundEffectAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a sound effect.
    /// </summary>
    Task<Result> DeleteSoundEffectAsync(
        Guid id,
        CancellationToken ct = default);

    #endregion

    #region BGM Management

    /// <summary>
    /// Loads BGM track.
    /// </summary>
    Task<Result<BackgroundMusic>> LoadBgmAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Creates loop points for BGM.
    /// </summary>
    Task<Result<LoopPoints>> CreateLoopPointsAsync(
        BackgroundMusic bgm,
        TimeSpan start,
        TimeSpan end,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes BGM for beat detection.
    /// </summary>
    Task<Result<BeatAnalysis>> AnalyzeBeatAsync(
        BackgroundMusic bgm,
        CancellationToken ct = default);

    /// <summary>
    /// Converts BGM to different format.
    /// </summary>
    Task<Result> ConvertBgmAsync(
        string sourcePath,
        string destinationPath,
        AudioFormat targetFormat,
        int quality,
        CancellationToken ct = default);

    /// <summary>
    /// Adjusts BGM volume for stages.
    /// </summary>
    Task<Result<BackgroundMusic>> AdjustStageBgmAsync(
        BackgroundMusic bgm,
        StageBgmSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Crossfades between two BGM tracks.
    /// </summary>
    Task<Result<byte[]>> CrossfadeBgmAsync(
        BackgroundMusic from,
        BackgroundMusic to,
        TimeSpan duration,
        CancellationToken ct = default);

    #endregion

    #region Voice Synthesis

    /// <summary>
    /// Synthesizes voice from text.
    /// </summary>
    Task<Result<SoundEffect>> SynthesizeVoiceAsync(
        string text,
        VoiceSynthesisOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Records voice from microphone.
    /// </summary>
    Task<Result<SoundEffect>> RecordVoiceAsync(
        RecordingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Applies voice effects (robot, echo, etc.).
    /// </summary>
    Task<Result<SoundEffect>> ApplyVoiceEffectAsync(
        SoundEffect voice,
        VoiceEffectType effectType,
        VoiceEffectParameters parameters,
        CancellationToken ct = default);

    /// <summary>
    /// Batch generates character voice lines.
    /// </summary>
    Task<Result<IReadOnlyList<SoundEffect>>> BatchGenerateVoicesAsync(
        IReadOnlyList<string> lines,
        VoiceSynthesisOptions options,
        CancellationToken ct = default);

    #endregion

    #region Audio Library

    /// <summary>
    /// Creates sound category.
    /// </summary>
    Task<Result<SoundCategory>> CreateCategoryAsync(
        string name,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets sounds by category.
    /// </summary>
    Task<Result<IReadOnlyList<SoundEffect>>> GetSoundsByCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default);

    /// <summary>
    /// Tags sound effect.
    /// </summary>
    Task<Result> TagSoundAsync(
        Guid soundId,
        IReadOnlyList<string> tags,
        CancellationToken ct = default);

    /// <summary>
    /// Searches sounds by tags.
    /// </summary>
    Task<Result<IReadOnlyList<SoundEffect>>> SearchSoundsAsync(
        string query,
        SearchOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets sound library statistics.
    /// </summary>
    Task<Result<LibraryStatistics>> GetLibraryStatisticsAsync(
        CancellationToken ct = default);

    #endregion

    #region Preview and Testing

    /// <summary>
    /// Plays sound effect for preview.
    /// </summary>
    Task<Result> PreviewSoundAsync(
        Guid soundId,
        SoundPreviewOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Stops preview playback.
    /// </summary>
    Task<Result> StopPreviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets audio visualization data.
    /// </summary>
    Task<Result<VisualizationData>> GetVisualizationDataAsync(
        Guid soundId,
        VisualizationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Tests audio latency.
    /// </summary>
    Task<Result<LatencyTestResult>> TestLatencyAsync(
        CancellationToken ct = default);

    #endregion

    #region Batch Operations

    /// <summary>
    /// Batch processes sound effects.
    /// </summary>
    Task<Result<BatchSoundResult>> BatchProcessAsync(
        IReadOnlyList<Guid> soundIds,
        SoundBatchOperation operation,
        CancellationToken ct = default);

    /// <summary>
    /// Validates sound library.
    /// </summary>
    Task<Result<SoundValidationReport>> ValidateLibraryAsync(
        ValidationSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Optimizes sound files for distribution.
    /// </summary>
    Task<Result<OptimizationReport>> OptimizeLibraryAsync(
        OptimizationSettings settings,
        CancellationToken ct = default);

    #endregion

    #region Project Management

    /// <summary>
    /// Creates sound project.
    /// </summary>
    Task<Result<SoundProject>> CreateProjectAsync(
        string name,
        SoundProjectSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Opens sound project.
    /// </summary>
    Task<Result<SoundProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default);

    /// <summary>
    /// Saves sound project.
    /// </summary>
    Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default);

    /// <summary>
    /// Exports project for MUGEN.
    /// </summary>
    Task<Result> ExportForMugenAsync(
        string outputDirectory,
        MugenExportOptions options,
        CancellationToken ct = default);

    #endregion
}

#region Request/Response Models

/// <summary>
/// Sound effect data.
/// </summary>
public record SoundEffect(
    Guid Id,
    string Name,
    string FilePath,
    AudioFormat Format,
    int SampleRate,
    int Channels,
    TimeSpan Duration,
    long FileSize,
    SoundEffectMetadata Metadata,
    DateTime CreatedAt,
    DateTime ModifiedAt);

/// <summary>
/// Sound effect metadata.
/// </summary>
public record SoundEffectMetadata(
    string? Description,
    Guid? CategoryId,
    IReadOnlyList<string> Tags,
    SoundUsage Usage,
    int? LoopStart,
    int? LoopEnd,
    double Volume);

/// <summary>
/// Sound usage type.
/// </summary>
public enum SoundUsage
{
    Attack,
    Hit,
    Block,
    Special,
    Hyper,
    Voice,
    Step,
    Landing,
    Guard,
    Win,
    Intro,
    Custom
}

/// <summary>
/// Audio format.
/// </summary>
public enum AudioFormat
{
    Wav,
    Mp3,
    Ogg,
    Flac,
    Wma
}

/// <summary>
/// Export format.
/// </summary>
public enum SoundExportFormat
{
    Wav,
    Mp3,
    Ogg,
    Flac
}

/// <summary>
/// Synthesis parameters.
/// </summary>
public record SynthesisParameters(
    SoundWaveform Waveform,
    double Frequency,
    double Duration,
    double Attack,
    double Decay,
    double Sustain,
    double Release,
    IReadOnlyList<Harmonic> Harmonics);

/// <summary>
/// Sound waveform types.
/// </summary>
public enum SoundWaveform
{
    Sine,
    Square,
    Sawtooth,
    Triangle,
    Noise,
    Custom
}

/// <summary>
/// Harmonic for synthesis.
/// </summary>
public record Harmonic(int Number, double Amplitude, double Phase);

/// <summary>
/// Audio effect chain.
/// </summary>
public record AudioEffectChain(
    IReadOnlyList<AudioEffect> Effects);

/// <summary>
/// Audio effect.
/// </summary>
public record AudioEffect(
    EffectType Type,
    EffectParameters Parameters);

/// <summary>
/// Effect types.
/// </summary>
public enum EffectType
{
    Gain,
    Compressor,
    Distortion,
    Delay,
    Reverb,
    Chorus,
    Flanger,
    Phaser,
    Equalizer,
    Filter
}

/// <summary>
/// Effect parameters.
/// </summary>
public record EffectParameters(
    IReadOnlyDictionary<string, double> Values);

/// <summary>
/// Trim options.
/// </summary>
public record TrimOptions(
    double ThresholdDb,
    bool TrimStart,
    bool TrimEnd,
    TimeSpan? MinDuration = null);

/// <summary>
/// Normalization options.
/// </summary>
public record NormalizationOptions(
    double TargetDb,
    bool PeakNormalize,
    bool LoudnessNormalize);

/// <summary>
/// Reverb parameters.
/// </summary>
public record ReverbParameters(
    double RoomSize,
    double Damping,
    double WetLevel,
    double DryLevel,
    double Width);

/// <summary>
/// Equalizer settings.
/// </summary>
public record EqualizerSettings(
    IReadOnlyList<EQBand> Bands);

/// <summary>
/// EQ band.
/// </summary>
public record EQBand(
    double Frequency,
    double Gain,
    double Q,
    FilterType Type);

/// <summary>
/// Filter type.
/// </summary>
public enum FilterType
{
    LowPass,
    HighPass,
    BandPass,
    Notch,
    Peaking,
    LowShelf,
    HighShelf
}

/// <summary>
/// Mix options.
/// </summary>
public record MixOptions(
    IReadOnlyList<double> Volumes,
    bool NormalizeOutput);

/// <summary>
/// Background music.
/// </summary>
public record BackgroundMusic(
    Guid Id,
    string Title,
    string Artist,
    string FilePath,
    AudioFormat Format,
    TimeSpan Duration,
    int Bpm,
    LoopPoints? Loop,
    BgmMetadata Metadata);

/// <summary>
/// BGM metadata.
/// </summary>
public record BgmMetadata(
    string? Album,
    int? Year,
    string? Genre,
    int? TrackNumber);

/// <summary>
/// Loop points.
/// </summary>
public record LoopPoints(
    TimeSpan Start,
    TimeSpan End,
    bool IsSeamless);

/// <summary>
/// Beat analysis.
/// </summary>
public record BeatAnalysis(
    int Bpm,
    IReadOnlyList<TimeSpan> BeatPositions,
    IReadOnlyList<double> EnergyLevels);

/// <summary>
/// Stage BGM settings.
/// </summary>
public record StageBgmSettings(
    double IntroVolume,
    double LoopVolume,
    double FadeInDuration,
    double FadeOutDuration);

/// <summary>
/// Voice synthesis options.
/// </summary>
public record VoiceSynthesisOptions(
    VoiceGender Gender,
    VoiceAge Age,
    string? Language,
    double Pitch,
    double Speed,
    double Volume,
    VoiceEmotion Emotion);

/// <summary>
/// Voice gender.
/// </summary>
public enum VoiceGender
{
    Male,
    Female,
    Neutral
}

/// <summary>
/// Voice age.
/// </summary>
public enum VoiceAge
{
    Child,
    Teen,
    Adult,
    Elder
}

/// <summary>
/// Voice emotion.
/// </summary>
public enum VoiceEmotion
{
    Neutral,
    Happy,
    Angry,
    Sad,
    Excited,
    Serious
}

/// <summary>
/// Recording options.
/// </summary>
public record RecordingOptions(
    int SampleRate,
    int Channels,
    int BitDepth,
    TimeSpan MaxDuration,
    double InputGain);

/// <summary>
/// Voice effect type.
/// </summary>
public enum VoiceEffectType
{
    Robot,
    Echo,
    Reverb,
    Chorus,
    PitchShift,
    Radio,
    Telephone,
    Monster,
    Chipmunk
}

/// <summary>
/// Voice effect parameters.
/// </summary>
public record VoiceEffectParameters(
    double Intensity,
    IReadOnlyDictionary<string, double> CustomValues);

/// <summary>
/// Sound category.
/// </summary>
public record SoundCategory(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt);

/// <summary>
/// Search options.
/// </summary>
public record SearchOptions(
    bool SearchNames,
    bool SearchTags,
    bool SearchDescriptions,
    AudioFormat? FormatFilter,
    TimeSpan? MinDuration,
    TimeSpan? MaxDuration);

/// <summary>
/// Library statistics.
/// </summary>
public record LibraryStatistics(
    int TotalSounds,
    int TotalCategories,
    long TotalSize,
    IReadOnlyDictionary<AudioFormat, int> FormatCounts,
    IReadOnlyDictionary<SoundUsage, int> UsageCounts);

/// <summary>
/// Preview options.
/// </summary>
public record SoundPreviewOptions(
    double Volume,
    bool Loop,
    TimeSpan? StartPosition,
    TimeSpan? EndPosition);

/// <summary>
/// Visualization options.
/// </summary>
public record VisualizationOptions(
    int Resolution,
    VisualizationType Type,
    TimeSpan? StartTime,
    TimeSpan? EndTime);

/// <summary>
/// Visualization type.
/// </summary>
public enum VisualizationType
{
    Waveform,
    Spectrum,
    Spectrogram,
    LevelMeter
}

/// <summary>
/// Visualization data.
/// </summary>
public record VisualizationData(
    IReadOnlyList<double> LeftChannel,
    IReadOnlyList<double> RightChannel,
    IReadOnlyList<double> Frequencies,
    TimeSpan Duration);

/// <summary>
/// Latency test result.
/// </summary>
public record LatencyTestResult(
    double InputLatencyMs,
    double OutputLatencyMs,
    double RoundTripLatencyMs,
    bool IsAcceptable);

/// <summary>
/// Batch operation.
/// </summary>
public record SoundBatchOperation(
    SoundBatchOperationType Type,
    IReadOnlyDictionary<string, object> Parameters);

/// <summary>
/// Batch operation type.
/// </summary>
public enum SoundBatchOperationType
{
    Convert,
    Normalize,
    Trim,
    ApplyEffect,
    Rename,
    Tag
}

/// <summary>
/// Batch sound result.
/// </summary>
public record BatchSoundResult(
    int Processed,
    int Failed,
    IReadOnlyList<string> Errors,
    TimeSpan Duration);

/// <summary>
/// Validation settings.
/// </summary>
public record ValidationSettings(
    bool CheckFileIntegrity,
    bool CheckFormatCompatibility,
    bool CheckVolumeLevels,
    bool CheckLoopPoints,
    bool CheckMetadata);

/// <summary>
/// Sound validation report.
/// </summary>
public record SoundValidationReport(
    bool IsValid,
    int ErrorCount,
    int WarningCount,
    IReadOnlyList<SoundValidationIssue> Issues);

/// <summary>
/// Sound validation issue.
/// </summary>
public record SoundValidationIssue(
    SoundValidationSeverity Severity,
    string Code,
    string Message,
    Guid? SoundId);

/// <summary>
/// Validation severity.
/// </summary>
public enum SoundValidationSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Optimization settings.
/// </summary>
public record OptimizationSettings(
    bool CompressAudio,
    bool RemoveUnused,
    bool NormalizeVolume,
    int TargetQuality,
    AudioFormat PreferredFormat);

/// <summary>
/// Optimization report.
/// </summary>
public record OptimizationReport(
    int FilesOptimized,
    int FilesRemoved,
    long SpaceSaved,
    long NewTotalSize);

/// <summary>
/// Sound project.
/// </summary>
public record SoundProject(
    string Name,
    string FilePath,
    SoundProjectSettings Settings,
    IReadOnlyList<SoundEffect> Sounds,
    IReadOnlyList<BackgroundMusic> BgmTracks,
    IReadOnlyList<SoundCategory> Categories,
    DateTime CreatedAt,
    DateTime ModifiedAt);

/// <summary>
/// Sound project settings.
/// </summary>
public record SoundProjectSettings(
    string CharacterName,
    string Author,
    int DefaultSampleRate,
    int DefaultChannels,
    AudioFormat PreferredFormat,
    double MasterVolume);

/// <summary>
/// MUGEN export options.
/// </summary>
public record MugenExportOptions(
    bool ExportWav,
    bool ExportWithLoopPoints,
    bool GenerateSoundDefs,
    double VolumeScale,
    string Prefix);

#endregion
