using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Sound design studio for MUGEN/IKEMEN audio enhancements.
/// Provides audio mixing, voice acting tools, dynamic music, sound spatialization, and audio analysis.
/// </summary>
public interface IMugenSoundDesignStudio
{
    /// <summary>
    /// Mixes multiple audio tracks with effects processing.
    /// </summary>
    /// <param name="audioMix">The audio mix configuration to apply.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ApplyAudioMixAsync(AudioMixConfig audioMix);

    /// <summary>
    /// Records and integrates voice lines for characters.
    /// </summary>
    /// <param name="characterId">The character to add voice lines to.</param>
    /// <param name="voiceConfig">Voice acting configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RecordVoiceLinesAsync(int characterId, VoiceActingConfig voiceConfig);

    /// <summary>
    /// Sets up dynamic music that changes based on match state.
    /// </summary>
    /// <param name="musicConfig">Dynamic music configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ConfigureDynamicMusicAsync(DynamicMusicConfig musicConfig);

    /// <summary>
    /// Applies 3D sound spatialization effects.
    /// </summary>
    /// <param name="spatialConfig">Sound spatialization configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ApplySoundSpatializationAsync(SoundSpatializationConfig spatialConfig);

    /// <summary>
    /// Performs automatic audio balancing and normalization.
    /// </summary>
    /// <param name="audioFile">Path to the audio file to analyze.</param>
    /// <returns>Result with audio analysis data.</returns>
    Task<Result<AudioAnalysisResult>> AnalyzeAudioAsync(string audioFile);

    /// <summary>
    /// Batch processes multiple audio files with analysis and normalization.
    /// </summary>
    /// <param name="audioFiles">Collection of audio files to process.</param>
    /// <param name="processingConfig">Batch processing configuration.</param>
    /// <returns>Result with batch processing results.</returns>
    Task<Result<AudioBatchProcessingResult>> BatchProcessAudioAsync(
        IEnumerable<string> audioFiles,
        AudioBatchProcessingConfig processingConfig);

    /// <summary>
    /// Creates custom audio effects and filters.
    /// </summary>
    /// <param name="effectConfig">Custom effect configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CreateCustomAudioEffectAsync(CustomAudioEffectConfig effectConfig);

    /// <summary>
    /// Generates procedural audio based on character moves or match events.
    /// </summary>
    /// <param name="proceduralConfig">Procedural audio generation configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> GenerateProceduralAudioAsync(ProceduralAudioConfig proceduralConfig);

    /// <summary>
    /// Previews audio enhancements before applying them.
    /// </summary>
    /// <param name="enhancementType">The type of audio enhancement to preview.</param>
    /// <param name="config">Configuration for the preview.</param>
    /// <returns>Result with preview data or error.</returns>
    Task<Result<AudioPreview>> PreviewEnhancementAsync(AudioEnhancementType enhancementType, object config);

    /// <summary>
    /// Gets available audio enhancement presets.
    /// </summary>
    /// <returns>Collection of available presets.</returns>
    Task<Result<IReadOnlyCollection<AudioPreset>>> GetAvailablePresetsAsync();

    /// <summary>
    /// Saves a custom audio configuration as a preset.
    /// </summary>
    /// <param name="preset">The preset to save.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SavePresetAsync(AudioPreset preset);

    /// <summary>
    /// Loads an audio preset by name.
    /// </summary>
    /// <param name="presetName">Name of the preset to load.</param>
    /// <returns>Result with the loaded preset or error.</returns>
    Task<Result<AudioPreset>> LoadPresetAsync(string presetName);

    /// <summary>
    /// Gets the current sound design studio status and active enhancements.
    /// </summary>
    /// <returns>Current status information.</returns>
    Task<Result<SoundStudioStatus>> GetStatusAsync();

    /// <summary>
    /// Resets all audio enhancements to default state.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ResetEnhancementsAsync();

    /// <summary>
    /// Exports audio configuration for sharing or backup.
    /// </summary>
    /// <param name="exportPath">Path to export the configuration to.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ExportConfigurationAsync(string exportPath);

    /// <summary>
    /// Imports audio configuration from file.
    /// </summary>
    /// <param name="importPath">Path to import the configuration from.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ImportConfigurationAsync(string importPath);
}