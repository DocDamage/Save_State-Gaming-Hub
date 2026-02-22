using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Screen Filters Engine interface.
/// </summary>
public interface IScreenFiltersEngine
{
    Task<Result<ScreenFiltersEngineScreenFilterProfile>> CreateFilterProfileAsync(ScreenFiltersEngineFilterProfileRequest request, CancellationToken ct = default);
    Task<Result<ScreenFiltersEngineCustomShader>> CreateCustomShaderAsync(ScreenFiltersEngineCustomShaderRequest request, CancellationToken ct = default);
    Task<Result<ScreenFiltersEngineCRTSettings>> CreateCRTSettingsAsync(ScreenFiltersEngineCRTSettingsRequest request, CancellationToken ct = default);
    Task<Result<ScreenFiltersEngineScanlineSettings>> CreateScanlineSettingsAsync(ScreenFiltersEngineScanlineSettingsRequest request, CancellationToken ct = default);
    Task<Result> ApplyScreenFiltersAsync(string profileId, ScreenFiltersEngineRenderTarget target, CancellationToken ct = default);
    Task<Result<ScreenFiltersEngineFilterChain>> CreateFilterChainAsync(ScreenFiltersEngineFilterChainRequest request, CancellationToken ct = default);
    Task<Result<ScreenFiltersEngineFilterPreset>> CreateFilterPresetAsync(ScreenFiltersEngineFilterPresetRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ScreenFiltersEngineFilterPreset>>> GetFilterPresetsAsync(ScreenFiltersEngineFilterCategory? category, CancellationToken ct = default);
    Task<Result<ScreenFiltersEngineFilterPerformanceReport>> AnalyzeFilterPerformanceAsync(string profileId, CancellationToken ct = default);
}

/// <summary>
/// Screen filter profile data.
/// </summary>
public class ScreenFiltersEngineScreenFilterProfile
{
    public string ProfileId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ScreenFiltersEngineFilterPresetType BasePreset { get; set; } = default!;
    public ScreenFiltersEngineCRTSettings? ScreenFiltersEngineCRTSettings { get; set; } = default!;
    public ScreenFiltersEngineScanlineSettings? ScreenFiltersEngineScanlineSettings { get; set; } = default!;
    public ScreenFiltersEngineColorSettings? ScreenFiltersEngineColorSettings { get; set; } = default!;
    public ScreenFiltersEngineNoiseSettings? ScreenFiltersEngineNoiseSettings { get; set; } = default!;
    public ScreenFiltersEngineBloomSettings? ScreenFiltersEngineBloomSettings { get; set; } = default!;
    public IReadOnlyList<ScreenFiltersEngineCustomEffect> CustomEffects { get; set; } = default!;
    public bool Enabled { get; set; } = default!;
    public int Order { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Filter profile request.
/// </summary>
public class ScreenFiltersEngineFilterProfileRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ScreenFiltersEngineFilterPresetType BasePreset { get; set; } = default!;
    public ScreenFiltersEngineCRTSettings? ScreenFiltersEngineCRTSettings { get; set; } = default!;
    public ScreenFiltersEngineScanlineSettings? ScreenFiltersEngineScanlineSettings { get; set; } = default!;
    public ScreenFiltersEngineColorSettings? ScreenFiltersEngineColorSettings { get; set; } = default!;
    public ScreenFiltersEngineNoiseSettings? ScreenFiltersEngineNoiseSettings { get; set; } = default!;
    public ScreenFiltersEngineBloomSettings? ScreenFiltersEngineBloomSettings { get; set; } = default!;
    public IReadOnlyList<ScreenFiltersEngineCustomEffect> CustomEffects { get; set; } = default!;
    public int Order { get; set; } = default!;
}

/// <summary>
/// Custom shader data.
/// </summary>
public class ScreenFiltersEngineCustomShader
{
    public string ShaderId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string VertexShader { get; set; } = default!;
    public string FragmentShader { get; set; } = default!;
    public IReadOnlyList<ScreenFiltersEngineShaderUniform> Uniforms { get; set; } = default!;
    public ScreenFiltersEngineShaderCompilationStatus CompilationStatus { get; set; } = default!;
    public ScreenFiltersEngineShaderPerformanceRating PerformanceRating { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Custom shader request.
/// </summary>
public class ScreenFiltersEngineCustomShaderRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string VertexShader { get; set; } = default!;
    public string FragmentShader { get; set; } = default!;
}

/// <summary>
/// CRT settings data.
/// </summary>
public class ScreenFiltersEngineCRTSettings
{
    public float Curvature { get; set; } = default!;
    public float VignetteStrength { get; set; } = default!;
    public float PhosphorGlow { get; set; } = default!;
    public float ScanlineOpacity { get; set; } = default!;
    public float ColorBleeding { get; set; } = default!;
    public float Persistence { get; set; } = default!;
    public float Overscan { get; set; } = default!;
    public float CornerRounding { get; set; } = default!;
}

/// <summary>
/// CRT settings request.
/// </summary>
public class ScreenFiltersEngineCRTSettingsRequest
{
    public float Curvature { get; set; } = default!;
    public float VignetteStrength { get; set; } = default!;
    public float PhosphorGlow { get; set; } = default!;
    public float ScanlineOpacity { get; set; } = default!;
    public float ColorBleeding { get; set; } = default!;
    public float Persistence { get; set; } = default!;
    public float Overscan { get; set; } = default!;
    public float CornerRounding { get; set; } = default!;
}

/// <summary>
/// Scanline settings data.
/// </summary>
public class ScreenFiltersEngineScanlineSettings
{
    public float Intensity { get; set; } = default!;
    public float Thickness { get; set; } = default!;
    public float Spacing { get; set; } = default!;
    public float HorizontalShift { get; set; } = default!;
    public float VerticalShift { get; set; } = default!;
    public string Color { get; set; } = default!;
    public float AnimationSpeed { get; set; } = default!;
}

/// <summary>
/// Scanline settings request.
/// </summary>
public class ScreenFiltersEngineScanlineSettingsRequest
{
    public float Intensity { get; set; } = default!;
    public float Thickness { get; set; } = default!;
    public float Spacing { get; set; } = default!;
    public float HorizontalShift { get; set; } = default!;
    public float VerticalShift { get; set; } = default!;
    public string Color { get; set; } = default!;
    public float AnimationSpeed { get; set; } = default!;
}

/// <summary>
/// Color settings data.
/// </summary>
public class ScreenFiltersEngineColorSettings
{
    public float Brightness { get; set; } = default!;
    public float Contrast { get; set; } = default!;
    public float Saturation { get; set; } = default!;
    public float Gamma { get; set; } = default!;
    public int ColorTemperature { get; set; } = default!;
    public IReadOnlyList<string>? Palette { get; set; } = default!;
}

/// <summary>
/// Noise settings data.
/// </summary>
public class ScreenFiltersEngineNoiseSettings
{
    public ScreenFiltersEngineNoiseType Type { get; set; } = default!;
    public float Intensity { get; set; } = default!;
    public float Size { get; set; } = default!;
    public float AnimationSpeed { get; set; } = default!;
}

/// <summary>
/// Bloom settings data.
/// </summary>
public class ScreenFiltersEngineBloomSettings
{
    public float Intensity { get; set; } = default!;
    public float Threshold { get; set; } = default!;
    public float Radius { get; set; } = default!;
    public int Iterations { get; set; } = default!;
    public string TintColor { get; set; } = default!;
}

/// <summary>
/// Custom effect data.
/// </summary>
public class ScreenFiltersEngineCustomEffect
{
    public string EffectId { get; set; } = default!;
    public string ShaderId { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public ScreenFiltersEngineBlendMode ScreenFiltersEngineBlendMode { get; set; } = default!;
    public float Opacity { get; set; } = default!;
}

/// <summary>
/// Filter chain data.
/// </summary>
public class ScreenFiltersEngineFilterChain
{
    public string ChainId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IReadOnlyList<string> FilterIds { get; set; } = default!;
    public IReadOnlyList<ScreenFiltersEngineBlendMode> BlendModes { get; set; } = default!;
    public IReadOnlyList<float> OpacityValues { get; set; } = default!;
    public bool Enabled { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Filter chain request.
/// </summary>
public class ScreenFiltersEngineFilterChainRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IReadOnlyList<string> FilterIds { get; set; } = default!;
    public IReadOnlyList<ScreenFiltersEngineBlendMode> BlendModes { get; set; } = default!;
    public IReadOnlyList<float> OpacityValues { get; set; } = default!;
}

/// <summary>
/// Filter preset data.
/// </summary>
public class ScreenFiltersEngineFilterPreset
{
    public string PresetId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ScreenFiltersEngineFilterCategory Category { get; set; } = default!;
    public string ThumbnailUrl { get; set; } = default!;
    public string? ProfileId { get; set; } = default!;
    public string? ChainId { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public bool IsPublic { get; set; } = default!;
    public string Author { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public int Downloads { get; set; } = default!;
}

/// <summary>
/// Filter preset request.
/// </summary>
public class ScreenFiltersEngineFilterPresetRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ScreenFiltersEngineFilterCategory Category { get; set; } = default!;
    public string ThumbnailUrl { get; set; } = default!;
    public string? ProfileId { get; set; } = default!;
    public string? ChainId { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public bool IsPublic { get; set; } = default!;
    public string Author { get; set; } = default!;
}

/// <summary>
/// Filter performance report.
/// </summary>
public class ScreenFiltersEngineFilterPerformanceReport
{
    public string ProfileId { get; set; } = default!;
    public float FrameRateImpact { get; set; } = default!;
    public long MemoryUsage { get; set; } = default!;
    public float GPUUtilization { get; set; } = default!;
    public int DrawCallsAdded { get; set; } = default!;
    public int ShaderSwitches { get; set; } = default!;
    public IReadOnlyList<string> Recommendations { get; set; } = default!;
    public DateTime AnalyzedAt { get; set; } = default!;
}

/// <summary>
/// Render target data.
/// </summary>
public class ScreenFiltersEngineRenderTarget
{
    public string TargetId { get; set; } = default!;
    public ScreenFiltersEngineResolution ScreenFiltersEngineResolution { get; set; } = default!;
    public ScreenFiltersEnginePixelFormat Format { get; set; } = default!;
    public bool IsHDR { get; set; } = default!;
}

/// <summary>
/// Vector2 data.
/// </summary>
public class ScreenFiltersEngineFilterVector2
{
    public ScreenFiltersEngineFilterVector2() { }
    public ScreenFiltersEngineFilterVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
}

/// <summary>
/// Shader uniform data.
/// </summary>
public class ScreenFiltersEngineShaderUniform
{
    public string Name { get; set; } = default!;
    public ScreenFiltersEngineUniformType Type { get; set; } = default!;
    public object Value { get; set; } = default!;
}

/// <summary>
/// ScreenFiltersEngineResolution data.
/// </summary>
public class ScreenFiltersEngineResolution
{
    public ScreenFiltersEngineResolution() { }
    public ScreenFiltersEngineResolution(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; set; } = default!;
    public int Height { get; set; } = default!;
}

/// <summary>
/// Filter preset type enumeration.
/// </summary>
public enum ScreenFiltersEngineFilterPresetType
{
    CRT_Classic,
    CRT_Trinitron,
    CRT_Aperture,
    Arcade_Perfect,
    Arcade_Dark,
    Handheld_Classic,
    Handheld_Modern,
    Custom
}

/// <summary>
/// Filter category enumeration.
/// </summary>
public enum ScreenFiltersEngineFilterCategory
{
    CRT,
    Arcade,
    Handheld,
    Retro,
    Modern,
    Artistic,
    Custom
}

/// <summary>
/// Noise type enumeration.
/// </summary>
public enum ScreenFiltersEngineNoiseType
{
    FilmGrain,
    TVNoise,
    DigitalGlitch,
    AnalogWarmth
}

/// <summary>
/// Blend mode enumeration.
/// </summary>
public enum ScreenFiltersEngineBlendMode
{
    Normal,
    Multiply,
    Screen,
    Overlay,
    SoftLight,
    HardLight,
    ColorDodge,
    ColorBurn
}

/// <summary>
/// Shader compilation status enumeration.
/// </summary>
public enum ScreenFiltersEngineShaderCompilationStatus
{
    Pending,
    Success,
    Failed
}

/// <summary>
/// Shader performance rating enumeration.
/// </summary>
public enum ScreenFiltersEngineShaderPerformanceRating
{
    Excellent,
    Good,
    Fair,
    Poor
}

/// <summary>
/// Uniform type enumeration.
/// </summary>
public enum ScreenFiltersEngineUniformType
{
    Float,
    Vec2,
    Vec3,
    Vec4,
    Mat4,
    Texture2D,
    Bool
}

/// <summary>
/// Pixel format enumeration.
/// </summary>
public enum ScreenFiltersEnginePixelFormat
{
    RGBA8,
    RGBA16F,
    RGBA32F,
    RGB10A2
}
