namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Types of graphics enhancements.
/// </summary>
public enum GraphicsEnhancementType
{
    DynamicLighting,
    ParticleEffects,
    ScreenFilters,
    BackgroundEffects,
    CameraSystem
}

/// <summary>
/// Preview data for graphics enhancements.
/// </summary>
public record GraphicsPreview
{
    /// <summary>
    /// Type of enhancement being previewed.
    /// </summary>
    public GraphicsEnhancementType EnhancementType { get; init; }

    /// <summary>
    /// Name of the preview.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Thumbnail image data or path.
    /// </summary>
    public string Thumbnail { get; init; } = string.Empty;

    /// <summary>
    /// Description of the preview.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Whether the preview is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Performance impact rating (Low, Medium, High).
    /// </summary>
    public PerformanceImpact PerformanceImpact { get; init; } = PerformanceImpact.Medium;

    /// <summary>
    /// Compatibility information.
    /// </summary>
    public CompatibilityInfo Compatibility { get; init; } = new();
}

/// <summary>
/// Performance impact levels.
/// </summary>
public enum PerformanceImpact
{
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
    /// Compatibility information for graphics enhancements.
/// </summary>
public record CompatibilityInfo
{
    /// <summary>
    /// Minimum required graphics API version.
    /// </summary>
    public string MinGraphicsApi { get; init; } = "OpenGL 3.3";

    /// <summary>
    /// Minimum required shader model.
    /// </summary>
    public string MinShaderModel { get; init; } = "3.0";

    /// <summary>
    /// Known compatibility issues.
    /// </summary>
    public IReadOnlyList<string> KnownIssues { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether enhancement is supported on current hardware.
    /// </summary>
    public bool IsSupported { get; init; } = true;
}

/// <summary>
/// Saved graphics configuration preset.
/// </summary>
public record GraphicsPreset
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
    /// Dynamic lighting configuration.
    /// </summary>
    public DynamicLightingConfig? LightingConfig { get; init; }

    /// <summary>
    /// Screen filter configuration.
    /// </summary>
    public ScreenFilterConfig? FilterConfig { get; init; }

    /// <summary>
    /// Background effects configuration.
    /// </summary>
    public BackgroundEffectConfig? BackgroundConfig { get; init; }

    /// <summary>
    /// Camera system configuration.
    /// </summary>
    public CameraSystemConfig? CameraConfig { get; init; }

    /// <summary>
    /// Particle effect configurations by move name.
    /// </summary>
    public IReadOnlyDictionary<string, ParticleEffectConfig> ParticleEffects { get; init; } = new Dictionary<string, ParticleEffectConfig>();

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this is a built-in preset.
    /// </summary>
    public bool IsBuiltIn { get; init; } = false;

    /// <summary>
    /// Preview thumbnail.
    /// </summary>
    public string PreviewImage { get; init; } = string.Empty;

    /// <summary>
    /// Performance requirements.
    /// </summary>
    public PerformanceRequirements PerformanceRequirements { get; init; } = new();
}

/// <summary>
/// Performance requirements for graphics presets.
/// </summary>
public record PerformanceRequirements
{
    /// <summary>
    /// Minimum CPU requirements.
    /// </summary>
    public string MinCpu { get; init; } = "Dual-core 2.0GHz";

    /// <summary>
    /// Minimum GPU requirements.
    /// </summary>
    public string MinGpu { get; init; } = "DirectX 10 compatible";

    /// <summary>
    /// Minimum RAM requirements in MB.
    /// </summary>
    public int MinRamMb { get; init; } = 2048;

    /// <summary>
    /// Recommended CPU.
    /// </summary>
    public string RecommendedCpu { get; init; } = "Quad-core 3.0GHz";

    /// <summary>
    /// Recommended GPU.
    /// </summary>
    public string RecommendedGpu { get; init; } = "DirectX 11 compatible";

    /// <summary>
    /// Recommended RAM in MB.
    /// </summary>
    public int RecommendedRamMb { get; init; } = 4096;
}

/// <summary>
/// Current status of the graphics engine.
/// </summary>
public record GraphicsEngineStatus
{
    /// <summary>
    /// Whether the graphics engine is initialized.
    /// </summary>
    public bool IsInitialized { get; init; }

    /// <summary>
    /// Whether the graphics engine is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Currently loaded preset name.
    /// </summary>
    public string CurrentPreset { get; init; } = "Default";

    /// <summary>
    /// Active enhancements.
    /// </summary>
    public IReadOnlyList<GraphicsEnhancementType> ActiveEnhancements { get; init; } = Array.Empty<GraphicsEnhancementType>();

    /// <summary>
    /// Performance metrics.
    /// </summary>
    public GraphicsPerformanceMetrics Performance { get; init; } = new();

    /// <summary>
    /// Any active warnings or errors.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Supported graphics features.
    /// </summary>
    public GraphicsCapabilities Capabilities { get; init; } = new();
}

/// <summary>
/// Performance metrics for graphics engine.
/// </summary>
public record GraphicsPerformanceMetrics
{
    /// <summary>
    /// Current FPS.
    /// </summary>
    public float CurrentFps { get; init; }

    /// <summary>
    /// Average frame time in milliseconds.
    /// </summary>
    public float AverageFrameTime { get; init; }

    /// <summary>
    /// GPU memory usage in MB.
    /// </summary>
    public float GpuMemoryUsageMb { get; init; }

    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    public float CpuUsagePercent { get; init; }

    /// <summary>
    /// Draw call count.
    /// </summary>
    public int DrawCalls { get; init; }

    /// <summary>
    /// Triangle count.
    /// </summary>
    public int TriangleCount { get; init; }
}

/// <summary>
/// Graphics capabilities of the current system.
/// </summary>
public record GraphicsCapabilities
{
    /// <summary>
    /// Maximum texture size.
    /// </summary>
    public int MaxTextureSize { get; init; } = 2048;

    /// <summary>
    /// Supported shader model.
    /// </summary>
    public string ShaderModel { get; init; } = "3.0";

    /// <summary>
    /// Whether geometry shaders are supported.
    /// </summary>
    public bool SupportsGeometryShaders { get; init; }

    /// <summary>
    /// Whether compute shaders are supported.
    /// </summary>
    public bool SupportsComputeShaders { get; init; }

    /// <summary>
    /// Whether multiple render targets are supported.
    /// </summary>
    public bool SupportsMrt { get; init; }

    /// <summary>
    /// Available VRAM in MB.
    /// </summary>
    public int AvailableVramMb { get; init; } = 512;

    /// <summary>
    /// Graphics API version.
    /// </summary>
    public string GraphicsApi { get; init; } = "OpenGL 3.3";
}