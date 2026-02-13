using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Advanced graphics engine for MUGEN/IKEMEN visual enhancements.
/// Provides dynamic lighting, particle effects, screen filters, background effects, and camera systems.
/// </summary>
public interface IMugenGraphicsEngine
{
    /// <summary>
    /// Applies dynamic lighting effects to a character or stage.
    /// </summary>
    /// <param name="target">The target (character or stage) to apply lighting to.</param>
    /// <param name="lightingConfig">Lighting configuration settings.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ApplyDynamicLightingAsync(string target, DynamicLightingConfig lightingConfig);

    /// <summary>
    /// Creates and manages particle effects for character moves.
    /// </summary>
    /// <param name="characterId">The character to add particle effects to.</param>
    /// <param name="moveName">The move to enhance with particles.</param>
    /// <param name="effectConfig">Particle effect configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AddParticleEffectAsync(int characterId, string moveName, ParticleEffectConfig effectConfig);

    /// <summary>
    /// Applies screen filters such as CRT, scanlines, or custom shaders.
    /// </summary>
    /// <param name="filterType">The type of screen filter to apply.</param>
    /// <param name="config">Filter-specific configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ApplyScreenFilterAsync(ScreenFilterType filterType, ScreenFilterConfig config);

    /// <summary>
    /// Enhances backgrounds with interactive elements and parallax effects.
    /// </summary>
    /// <param name="stageId">The stage to enhance.</param>
    /// <param name="backgroundConfig">Background enhancement configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> EnhanceBackgroundAsync(int stageId, BackgroundEffectConfig backgroundConfig);

    /// <summary>
    /// Configures dynamic camera angles and cinematic sequences.
    /// </summary>
    /// <param name="cameraConfig">Camera system configuration.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ConfigureCameraSystemAsync(CameraSystemConfig cameraConfig);

    /// <summary>
    /// Previews graphics enhancements before applying them.
    /// </summary>
    /// <param name="enhancementType">The type of enhancement to preview.</param>
    /// <param name="config">Configuration for the preview.</param>
    /// <returns>Result with preview data or error.</returns>
    Task<Result<GraphicsPreview>> PreviewEnhancementAsync(GraphicsEnhancementType enhancementType, object config);

    /// <summary>
    /// Gets available graphics enhancement presets.
    /// </summary>
    /// <returns>Collection of available presets.</returns>
    Task<Result<IReadOnlyCollection<GraphicsPreset>>> GetAvailablePresetsAsync();

    /// <summary>
    /// Saves a custom graphics configuration as a preset.
    /// </summary>
    /// <param name="preset">The preset to save.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SavePresetAsync(GraphicsPreset preset);

    /// <summary>
    /// Loads a graphics preset by name.
    /// </summary>
    /// <param name="presetName">Name of the preset to load.</param>
    /// <returns>Result with the loaded preset or error.</returns>
    Task<Result<GraphicsPreset>> LoadPresetAsync(string presetName);

    /// <summary>
    /// Gets the current graphics engine status and active enhancements.
    /// </summary>
    /// <returns>Current status information.</returns>
    Task<Result<GraphicsEngineStatus>> GetStatusAsync();

    /// <summary>
    /// Resets all graphics enhancements to default state.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ResetEnhancementsAsync();
}