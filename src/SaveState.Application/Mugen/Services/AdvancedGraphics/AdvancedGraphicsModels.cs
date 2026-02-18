namespace SaveState.Application.Mugen.Services.AdvancedGraphics;

/// <summary>
/// Graphics scene data.
/// </summary>
public class GraphicsScene
{
    public string SceneId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Resolution Dimensions { get; set; } = default!;
    public IReadOnlyList<BackgroundLayer> BackgroundLayers { get; set; } = default!;
    public LightingSetup LightingSetup { get; set; } = default!;
    public IReadOnlyList<string> ParticleSystems { get; set; } = default!;
    public IReadOnlyList<PostProcessingEffect> PostProcessingEffects { get; set; } = default!;
    public CameraSettings CameraSettings { get; set; } = default!;
    public RenderSettings RenderSettings { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Scene creation request.
/// </summary>
public class SceneCreationRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Resolution Dimensions { get; set; } = default!;
    public LightingPreset LightingPreset { get; set; } = default!;
    public IReadOnlyList<BackgroundLayerRequest> BackgroundLayers { get; set; } = default!;
    public IReadOnlyList<string> ParticleSystemIds { get; set; } = default!;
    public IReadOnlyList<PostProcessingEffect> PostProcessingEffects { get; set; } = default!;
}

/// <summary>
/// Background layer data.
/// </summary>
public class BackgroundLayer
{
    public string LayerId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string TexturePath { get; set; } = default!;
    public GraphicsVector2 Position { get; set; } = default!;
    public GraphicsVector2 Scale { get; set; } = default!;
    public float Opacity { get; set; } = default!;
    public GraphicsVector2 ScrollSpeed { get; set; } = default!;
    public float ParallaxFactor { get; set; } = default!;
    public BlendMode BlendMode { get; set; } = default!;
    public string? ShaderId { get; set; } = default!;
}

/// <summary>
/// Background layer request.
/// </summary>
public class BackgroundLayerRequest
{
    public string Name { get; set; } = default!;
    public string TexturePath { get; set; } = default!;
    public GraphicsVector2 Position { get; set; } = default!;
    public GraphicsVector2 Scale { get; set; } = default!;
    public float Opacity { get; set; } = default!;
    public GraphicsVector2 ScrollSpeed { get; set; } = default!;
    public float ParallaxFactor { get; set; } = default!;
    public BlendMode BlendMode { get; set; } = default!;
    public string? ShaderId { get; set; } = default!;
}

/// <summary>
/// Lighting setup data.
/// </summary>
public class LightingSetup
{
    public string SetupId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public AmbientLight AmbientLight { get; set; } = default!;
    public IReadOnlyList<LightSource> Lights { get; set; } = default!;
    public bool ShadowsEnabled { get; set; } = default!;
    public ShadowQuality ShadowQuality { get; set; } = default!;
}

/// <summary>
/// Lighting setup request.
/// </summary>
public class LightingSetupRequest
{
    public string Name { get; set; } = default!;
    public AmbientLight AmbientLight { get; set; } = default!;
    public IReadOnlyList<LightSource> Lights { get; set; } = default!;
    public bool ShadowsEnabled { get; set; } = default!;
    public ShadowQuality ShadowQuality { get; set; } = default!;
}

/// <summary>
/// Lighting preset enumeration.
/// </summary>
public enum LightingPreset
{
    Daylight,
    Night,
    Arena,
    Underground,
    Custom
}

/// <summary>
/// Particle system data.
/// </summary>
public class ParticleSystem
{
    public string SystemId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ParticleEmitterSettings EmitterSettings { get; set; } = default!;
    public ParticleSettings ParticleSettings { get; set; } = default!;
    public ParticleBehaviorSettings BehaviorSettings { get; set; } = default!;
    public ParticleRenderSettings RenderSettings { get; set; } = default!;
    public bool IsActive { get; set; } = default!;
    public int ParticleCount { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}

/// <summary>
/// Particle system request.
/// </summary>
public class ParticleSystemRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ParticleEmitterSettings EmitterSettings { get; set; } = default!;
    public ParticleSettings ParticleSettings { get; set; } = default!;
    public ParticleBehaviorSettings BehaviorSettings { get; set; } = default!;
    public ParticleRenderSettings RenderSettings { get; set; } = default!;
}

/// <summary>
/// Particle emitter settings.
/// </summary>
public class ParticleEmitterSettings
{
    public GraphicsVector3 Position { get; set; } = default!;
    public GraphicsVector3 Direction { get; set; } = default!;
    public float Spread { get; set; } = default!;
    public float Rate { get; set; } = default!;
    public float Duration { get; set; } = default!;
    public int MaxParticles { get; set; } = default!;
}

/// <summary>
/// Particle settings.
/// </summary>
public class ParticleSettings
{
    public GraphicsVector2 Size { get; set; } = default!;
    public GraphicsColor StartColor { get; set; } = default!;
    public GraphicsColor EndColor { get; set; } = default!;
    public float StartAlpha { get; set; } = default!;
    public float EndAlpha { get; set; } = default!;
    public float Lifetime { get; set; } = default!;
}

/// <summary>
/// Particle behavior settings.
/// </summary>
public class ParticleBehaviorSettings
{
    public GraphicsVector3 Gravity { get; set; } = default!;
    public GraphicsVector3 Wind { get; set; } = default!;
    public float Drag { get; set; } = default!;
    public bool CollidesWithWorld { get; set; } = default!;
    public bool AffectedByLighting { get; set; } = default!;
}

/// <summary>
/// Particle render settings.
/// </summary>
public class ParticleRenderSettings
{
    public string TexturePath { get; set; } = default!;
    public BlendMode BlendMode { get; set; } = default!;
    public bool SoftParticles { get; set; } = default!;
    public bool SortByDepth { get; set; } = default!;
}

/// <summary>
/// Shader program data.
/// </summary>
public class ShaderProgram
{
    public string ShaderId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string VertexShader { get; set; } = default!;
    public string FragmentShader { get; set; } = default!;
    public string? GeometryShader { get; set; } = default!;
    public IReadOnlyList<ShaderUniform> Uniforms { get; set; } = default!;
    public IReadOnlyList<ShaderAttribute> Attributes { get; set; } = default!;
    public ShaderCompilationStatus CompilationStatus { get; set; } = default!;
    public DateTime CompiledAt { get; set; } = default!;
    public ShaderPerformanceMetrics PerformanceMetrics { get; set; } = default!;
}

/// <summary>
/// Shader compilation request.
/// </summary>
public class ShaderCompilationRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string VertexShader { get; set; } = default!;
    public string FragmentShader { get; set; } = default!;
    public string? GeometryShader { get; set; } = default!;
}

/// <summary>
/// Shader compilation status.
/// </summary>
public enum ShaderCompilationStatus
{
    Pending,
    Success,
    Failed
}

/// <summary>
/// Shader uniform data.
/// </summary>
public class ShaderUniform
{
    public string Name { get; set; } = default!;
    public UniformType Type { get; set; } = default!;
    public object Value { get; set; } = default!;
}

/// <summary>
/// Shader attribute data.
/// </summary>
public class ShaderAttribute
{
    public string Name { get; set; } = default!;
    public AttributeType Type { get; set; } = default!;
    public int Location { get; set; } = default!;
}

/// <summary>
/// Shader performance metrics.
/// </summary>
public class ShaderPerformanceMetrics
{
    public int EstimatedDrawCalls { get; set; } = default!;
    public float EstimatedFillRate { get; set; } = default!;
    public long EstimatedMemoryUsage { get; set; } = default!;
}

/// <summary>
/// Uniform type enumeration.
/// </summary>
public enum UniformType
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
/// Attribute type enumeration.
/// </summary>
public enum AttributeType
{
    Float,
    Vec2,
    Vec3,
    Vec4
}

/// <summary>
/// Post-processing effect data.
/// </summary>
public class PostProcessingEffect
{
    public string EffectId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public PostProcessingType Type { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public string? ShaderId { get; set; } = default!;
    public int Priority { get; set; } = default!;
    public bool Enabled { get; set; } = default!;
}

/// <summary>
/// Post-processing request.
/// </summary>
public class PostProcessingRequest
{
    public string Name { get; set; } = default!;
    public PostProcessingType Type { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public string? ShaderId { get; set; } = default!;
    public int Priority { get; set; } = default!;
}

/// <summary>
/// Post-processing type enumeration.
/// </summary>
public enum PostProcessingType
{
    Bloom,
    ToneMapping,
    ColorGrading,
    MotionBlur,
    DepthOfField,
    Vignette,
    ChromaticAberration,
    FilmGrain
}

/// <summary>
/// Camera settings.
/// </summary>
public class CameraSettings
{
    public GraphicsVector3 Position { get; set; } = default!;
    public GraphicsVector3 Target { get; set; } = default!;
    public GraphicsVector3 Up { get; set; } = default!;
    public float FieldOfView { get; set; } = default!;
    public float NearPlane { get; set; } = default!;
    public float FarPlane { get; set; } = default!;
    public ProjectionMode ProjectionMode { get; set; } = default!;
}

/// <summary>
/// Render settings.
/// </summary>
public class RenderSettings
{
    public Resolution Resolution { get; set; } = default!;
    public AntiAliasingMode AntiAliasing { get; set; } = default!;
    public int AnisotropicFiltering { get; set; } = default!;
    public ShadowQuality ShadowQuality { get; set; } = default!;
    public TextureQuality TextureQuality { get; set; } = default!;
    public EffectQuality EffectQuality { get; set; } = default!;
    public bool VSync { get; set; } = default!;
    public int TargetFrameRate { get; set; } = default!;
}

/// <summary>
/// Render context.
/// </summary>
public class RenderContext
{
    public string ContextId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public IReadOnlyDictionary<string, object> RenderState { get; set; } = default!;
}

/// <summary>
/// Render statistics.
/// </summary>
public class RenderStatistics
{
    public string SceneId { get; set; } = default!;
    public float FrameRate { get; set; } = default!;
    public int DrawCalls { get; set; } = default!;
    public int TriangleCount { get; set; } = default!;
    public long TextureMemoryUsage { get; set; } = default!;
    public int ShaderSwitches { get; set; } = default!;
    public int ParticleCount { get; set; } = default!;
    public int LightingCalculations { get; set; } = default!;
    public TimeSpan PostProcessingTime { get; set; } = default!;
    public TimeSpan TotalRenderTime { get; set; } = default!;
}

/// <summary>
/// Light source base class.
/// </summary>
public abstract record LightSource(
    string LightId,
    LightType Type,
    GraphicsColor Color,
    float Intensity);

/// <summary>
/// Ambient light.
/// </summary>
public record AmbientLight(float Intensity = default, GraphicsColor Color = default) 
    : LightSource("ambient", LightType.Ambient, Color, Intensity);

/// <summary>
/// Directional light.
/// </summary>
public record DirectionalLight(GraphicsVector3 Direction = default, GraphicsColor Color = default, float Intensity = default) 
    : LightSource(Guid.NewGuid().ToString(), LightType.Directional, Color, Intensity);

/// <summary>
/// Point light.
/// </summary>
public record PointLight(GraphicsVector3 Position = default, GraphicsColor Color = default, float Intensity = default, float Range = default) 
    : LightSource(Guid.NewGuid().ToString(), LightType.Point, Color, Intensity);

/// <summary>
/// Spot light.
/// </summary>
public record SpotLight(GraphicsVector3 Position = default, GraphicsVector3 Direction = default, GraphicsColor Color = default, float Intensity = default, float Angle = default) 
    : LightSource(Guid.NewGuid().ToString(), LightType.Spot, Color, Intensity);

/// <summary>
/// Light type enumeration.
/// </summary>
public enum LightType
{
    Ambient,
    Directional,
    Point,
    Spot
}

/// <summary>
/// Blend mode enumeration.
/// </summary>
public enum BlendMode
{
    Normal,
    Additive,
    Multiply,
    Screen,
    Overlay
}

/// <summary>
/// Projection mode enumeration.
/// </summary>
public enum ProjectionMode
{
    Perspective,
    Orthographic
}

/// <summary>
/// Anti-aliasing mode enumeration.
/// </summary>
public enum AntiAliasingMode
{
    None,
    FXAA,
    MSAA2x,
    MSAA4x,
    MSAA8x
}

/// <summary>
/// Shadow quality enumeration.
/// </summary>
public enum ShadowQuality
{
    Off,
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Texture quality enumeration.
/// </summary>
public enum TextureQuality
{
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Effect quality enumeration.
/// </summary>
public enum EffectQuality
{
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Resolution data.
/// </summary>
public class Resolution
{
    public Resolution() { }
    public Resolution(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; set; } = default!;
    public int Height { get; set; } = default!;
}

/// <summary>
/// Vector2 data.
/// </summary>
public class GraphicsVector2
{
    public GraphicsVector2() { }
    public GraphicsVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
}

/// <summary>
/// Vector3 data.
/// </summary>
public class GraphicsVector3
{
    public GraphicsVector3() { }
    public GraphicsVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;
    public float Z { get; set; } = default!;

    // Implicit conversion to the shared Vector3 (double components)
    public static implicit operator Vector3(GraphicsVector3 v)
        => new Vector3(v.X, v.Y, v.Z);

    // Implicit conversion from shared Vector3 to this type
    public static implicit operator GraphicsVector3(Vector3 v)
        => new GraphicsVector3((float)v.X, (float)v.Y, (float)v.Z);
}

/// <summary>
/// Color data.
/// </summary>
public class GraphicsColor
{
    public GraphicsColor() { }
    public GraphicsColor(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public float R { get; set; } = default!;
    public float G { get; set; } = default!;
    public float B { get; set; } = default!;
    public float A { get; set; } = default!;
}