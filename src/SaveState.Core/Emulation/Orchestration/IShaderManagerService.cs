using SaveState.Core.Common;

namespace SaveState.Core.Emulation.Orchestration;

/// <summary>
/// Service for managing shader presets and configurations.
/// </summary>
public interface IShaderManagerService
{
    /// <summary>
    /// Gets available shader presets.
    /// </summary>
    Task<Result<IReadOnlyList<ShaderPresetInfo>>> GetAvailablePresetsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets available shaders.
    /// </summary>
    Task<Result<IReadOnlyList<ShaderInfo>>> GetAvailableShadersAsync(ShaderType? type = null, CancellationToken ct = default);

    /// <summary>
    /// Applies a shader preset.
    /// </summary>
    Task<Result<ShaderConfiguration>> ApplyPresetAsync(ShaderPreset preset, CancellationToken ct = default);

    /// <summary>
    /// Creates a custom shader configuration.
    /// </summary>
    Task<Result<ShaderConfiguration>> CreateCustomConfigurationAsync(CreateShaderConfigurationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a shader configuration.
    /// </summary>
    Task<Result<ShaderConfiguration>> GetConfigurationAsync(string configId, CancellationToken ct = default);

    /// <summary>
    /// Updates a shader configuration.
    /// </summary>
    Task<Result<ShaderConfiguration>> UpdateConfigurationAsync(string configId, UpdateShaderConfigurationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a shader configuration.
    /// </summary>
    Task<Result> DeleteConfigurationAsync(string configId, CancellationToken ct = default);

    /// <summary>
    /// Imports a shader from file.
    /// </summary>
    Task<Result<ShaderInfo>> ImportShaderAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Exports a shader configuration.
    /// </summary>
    Task<Result<string>> ExportConfigurationAsync(string configId, string outputPath, CancellationToken ct = default);

    /// <summary>
    /// Gets shader parameters.
    /// </summary>
    Task<Result<IReadOnlyList<ShaderParameter>>> GetShaderParametersAsync(string shaderPath, CancellationToken ct = default);

    /// <summary>
    /// Updates shader parameter values.
    /// </summary>
    Task<Result> UpdateParameterAsync(string configId, string parameterName, object value, CancellationToken ct = default);

    /// <summary>
    /// Previews a shader configuration on a sample image.
    /// </summary>
    Task<Result<ShaderPreview>> PreviewShaderAsync(ShaderConfiguration config, CancellationToken ct = default);

    /// <summary>
    /// Gets recommended shaders for a specific game/system.
    /// </summary>
    Task<Result<IReadOnlyList<ShaderRecommendation>>> GetRecommendedShadersAsync(string systemName, string? gameName = null, CancellationToken ct = default);

    /// <summary>
    /// Compares shader performance.
    /// </summary>
    Task<Result<ShaderPerformanceComparison>> ComparePerformanceAsync(IReadOnlyList<string> configIds, int durationSeconds = 30, CancellationToken ct = default);

    /// <summary>
    /// Downloads shaders from an online repository.
    /// </summary>
    Task<Result<IReadOnlyList<ShaderInfo>>> DownloadShadersAsync(string repositoryUrl, CancellationToken ct = default);

    /// <summary>
    /// Gets shader categories.
    /// </summary>
    Task<Result<IReadOnlyList<ShaderCategory>>> GetCategoriesAsync(CancellationToken ct = default);
}

/// <summary>
/// Shader preset information.
/// </summary>
public sealed record ShaderPresetInfo(
    ShaderPreset Preset,
    string Name,
    string Description,
    string ThumbnailUrl,
    ShaderType Type,
    bool IsBuiltIn = true);

/// <summary>
/// Shader information.
/// </summary>
public sealed record ShaderInfo(
    string Id,
    string Name,
    string Path,
    ShaderType Type,
    string? Author,
    string? Description,
    string? Version,
    IReadOnlyList<ShaderParameter>? Parameters,
    DateTime? InstalledAt = null);

/// <summary>
/// Request to create shader configuration.
/// </summary>
public sealed record CreateShaderConfigurationRequest(
    string Name,
    List<ShaderPass> Passes,
    Dictionary<string, object>? Parameters = null);

/// <summary>
/// Request to update shader configuration.
/// </summary>
public sealed record UpdateShaderConfigurationRequest(
    string? Name = null,
    List<ShaderPass>? Passes = null,
    Dictionary<string, object>? Parameters = null);

/// <summary>
/// Shader parameter definition.
/// </summary>
public sealed record ShaderParameter(
    string Name,
    string Type,
    object DefaultValue,
    object? MinValue = null,
    object? MaxValue = null,
    object? Step = null,
    string? Description = null);

/// <summary>
/// Shader preview result.
/// </summary>
public sealed record ShaderPreview(
    string ConfigId,
    byte[] PreviewImage,
    int Width,
    int Height,
    double RenderTimeMs);

/// <summary>
/// Shader recommendation.
/// </summary>
public sealed record ShaderRecommendation(
    string ShaderId,
    string ShaderName,
    ShaderType Type,
    string Reason,
    int Score,
    bool IsCommunityFavorite);

/// <summary>
/// Shader performance comparison.
/// </summary>
public sealed record ShaderPerformanceComparison(
    IReadOnlyList<ShaderPerformanceResult> Results,
    int TestDurationSeconds,
    DateTime ComparedAt);

/// <summary>
/// Individual shader performance result.
/// </summary>
public sealed record ShaderPerformanceResult(
    string ConfigId,
    string ShaderName,
    double AverageFps,
    double MinFps,
    double FrameTimeMs,
    double GpuUsagePercent,
    int Score);

/// <summary>
/// Shader category.
/// </summary>
public sealed record ShaderCategory(
    string Id,
    string Name,
    string Description,
    int ShaderCount);

/// <summary>
/// Shader types.
/// </summary>
public enum ShaderType
{
    Crt,
    Lcd,
    Upscaler,
    Downscaler,
    AntiAliasing,
    Sharpening,
    Smoothing,
    ColorCorrection,
    Effect,
    Custom
}
