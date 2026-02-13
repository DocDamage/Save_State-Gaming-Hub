namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Configuration for dynamic lighting effects including shadows and environmental lighting.
/// </summary>
public record DynamicLightingConfig
{
    /// <summary>
    /// Enables real-time shadow casting.
    /// </summary>
    public bool EnableShadows { get; init; } = true;

    /// <summary>
    /// Quality level for shadow rendering (Low, Medium, High, Ultra).
    /// </summary>
    public ShadowQuality ShadowQuality { get; init; } = ShadowQuality.Medium;

    /// <summary>
    /// Enables dynamic environmental lighting based on stage conditions.
    /// </summary>
    public bool EnableEnvironmentalLighting { get; init; } = true;

    /// <summary>
    /// Intensity of ambient lighting (0.0 to 1.0).
    /// </summary>
    public float AmbientIntensity { get; init; } = 0.3f;

    /// <summary>
    /// Color of ambient lighting.
    /// </summary>
    public string AmbientColor { get; init; } = "#FFFFFF";

    /// <summary>
    /// Enables volumetric lighting effects.
    /// </summary>
    public bool EnableVolumetricLighting { get; init; } = false;

    /// <summary>
    /// Intensity of volumetric lighting.
    /// </summary>
    public float VolumetricIntensity { get; init; } = 0.5f;

    /// <summary>
    /// Custom lighting effects to apply.
    /// </summary>
    public IReadOnlyList<LightEffect> CustomEffects { get; init; } = Array.Empty<LightEffect>();
}

/// <summary>
/// Quality levels for shadow rendering.
/// </summary>
public enum ShadowQuality
{
    Low,
    Medium,
    High,
    Ultra
}

/// <summary>
/// Represents a custom light effect.
/// </summary>
public record LightEffect
{
    /// <summary>
    /// Type of light effect.
    /// </summary>
    public LightEffectType Type { get; init; }

    /// <summary>
    /// Position of the light source.
    /// </summary>
    public LightingPosition Position { get; init; } = new();

    /// <summary>
    /// Color of the light.
    /// </summary>
    public string Color { get; init; } = "#FFFFFF";

    /// <summary>
    /// Intensity of the light (0.0 to 1.0).
    /// </summary>
    public float Intensity { get; init; } = 1.0f;

    /// <summary>
    /// Range of the light effect.
    /// </summary>
    public float Range { get; init; } = 100.0f;

    /// <summary>
    /// Animation pattern for the light.
    /// </summary>
    public LightAnimation Animation { get; init; } = new();
}

/// <summary>
/// Types of light effects.
/// </summary>
public enum LightEffectType
{
    Point,
    Directional,
    Spot,
    Ambient,
    Rim
}

/// <summary>
/// Animation configuration for light effects.
/// </summary>
public record LightAnimation
{
    /// <summary>
    /// Type of animation.
    /// </summary>
    public AnimationType Type { get; init; } = AnimationType.Static;

    /// <summary>
    /// Speed of animation.
    /// </summary>
    public float Speed { get; init; } = 1.0f;

    /// <summary>
    /// Amplitude of animation effect.
    /// </summary>
    public float Amplitude { get; init; } = 1.0f;
}

/// <summary>
/// Types of light animations.
/// </summary>
public enum AnimationType
{
    Static,
    Pulse,
    Flicker,
    Rotate,
    Wave
}

/// <summary>
/// 2D position for effects.
/// </summary>
public record LightingPosition
{
    public float X { get; init; }
    public float Y { get; init; }
}
