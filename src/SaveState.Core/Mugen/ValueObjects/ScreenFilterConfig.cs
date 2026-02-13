namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Configuration for screen filters like CRT, scanlines, and custom shaders.
/// </summary>
public record ScreenFilterConfig
{
    /// <summary>
    /// Type of screen filter.
    /// </summary>
    public ScreenFilterType FilterType { get; init; }

    /// <summary>
    /// Intensity of the filter effect (0.0 to 1.0).
    /// </summary>
    public float Intensity { get; init; } = 0.5f;

    /// <summary>
    /// Whether the filter is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Custom parameters specific to the filter type.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Types of screen filters.
/// </summary>
public enum ScreenFilterType
{
    None,
    Crt,
    Scanlines,
    Bloom,
    ChromaticAberration,
    FilmGrain,
    Vignette,
    CustomShader
}

/// <summary>
/// CRT filter configuration.
/// </summary>
public record CrtFilterConfig : ScreenFilterConfig
{
    /// <summary>
    /// Curvature of the CRT screen.
    /// </summary>
    public float Curvature { get; init; } = 0.1f;

    /// <summary>
    /// Corner size of the CRT effect.
    /// </summary>
    public float CornerSize { get; init; } = 0.02f;

    /// <summary>
    /// Scanline thickness.
    /// </summary>
    public float ScanlineThickness { get; init; } = 1.0f;

    /// <summary>
    /// Brightness of scanlines.
    /// </summary>
    public float ScanlineBrightness { get; init; } = 0.8f;
}

/// <summary>
/// Scanlines filter configuration.
/// </summary>
public record ScanlinesFilterConfig : ScreenFilterConfig
{
    /// <summary>
    /// Number of scanlines.
    /// </summary>
    public int LineCount { get; init; } = 240;

    /// <summary>
    /// Thickness of scanlines.
    /// </summary>
    public float Thickness { get; init; } = 1.0f;

    /// <summary>
    /// Brightness of scanlines (0.0 to 1.0).
    /// </summary>
    public float Brightness { get; init; } = 0.7f;

    /// <summary>
    /// Speed of scanline animation.
    /// </summary>
    public float AnimationSpeed { get; init; } = 0.0f;
}

/// <summary>
/// Bloom filter configuration.
/// </summary>
public record BloomFilterConfig : ScreenFilterConfig
{
    /// <summary>
    /// Threshold for bloom effect.
    /// </summary>
    public float Threshold { get; init; } = 0.8f;

    /// <summary>
    /// Soft knee for smoother bloom transition.
    /// </summary>
    public float SoftKnee { get; init; } = 0.5f;

    /// <summary>
    /// Radius of bloom effect.
    /// </summary>
    public float Radius { get; init; } = 4.0f;

    /// <summary>
    /// Intensity of bloom.
    /// </summary>
    public float BloomIntensity { get; init; } = 1.0f;
}

/// <summary>
/// Custom shader filter configuration.
/// </summary>
public record CustomShaderFilterConfig : ScreenFilterConfig
{
    /// <summary>
    /// Path to the shader file.
    /// </summary>
    public string ShaderPath { get; init; } = string.Empty;

    /// <summary>
    /// Shader language (GLSL, HLSL, etc.).
    /// </summary>
    public string ShaderLanguage { get; init; } = "GLSL";

    /// <summary>
    /// Custom uniforms to pass to the shader.
    /// </summary>
    public IReadOnlyDictionary<string, ShaderUniform> Uniforms { get; init; } = new Dictionary<string, ShaderUniform>();
}

/// <summary>
/// Shader uniform value.
/// </summary>
public record ShaderUniform
{
    /// <summary>
    /// Type of the uniform value.
    /// </summary>
    public UniformType Type { get; init; }

    /// <summary>
    /// Value of the uniform.
    /// </summary>
    public object Value { get; init; } = new object();
}

/// <summary>
/// Types of shader uniforms.
/// </summary>
public enum UniformType
{
    Float,
    Int,
    Bool,
    Vec2,
    Vec3,
    Vec4,
    Mat3,
    Mat4,
    Texture
}