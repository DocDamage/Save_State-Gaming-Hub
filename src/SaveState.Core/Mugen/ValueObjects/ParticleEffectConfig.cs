namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Configuration for customizable particle effects on character moves.
/// </summary>
public record ParticleEffectConfig
{
    /// <summary>
    /// Type of particle effect.
    /// </summary>
    public ParticleEffectType EffectType { get; init; }

    /// <summary>
    /// Name of the effect for identification.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Number of particles to emit.
    /// </summary>
    public int ParticleCount { get; init; } = 50;

    /// <summary>
    /// Lifetime of each particle in seconds.
    /// </summary>
    public float Lifetime { get; init; } = 2.0f;

    /// <summary>
    /// Size of particles (width and height).
    /// </summary>
    public ParticleSize ParticleSize { get; init; } = new(4.0f, 4.0f);

    /// <summary>
    /// Color gradient for particles over their lifetime.
    /// </summary>
    public ColorGradient Colors { get; init; } = new();

    /// <summary>
    /// Initial velocity of particles.
    /// </summary>
    public ParticleVelocity InitialVelocity { get; init; } = new();

    /// <summary>
    /// Gravity affecting particles.
    /// </summary>
    public ParticleVelocity Gravity { get; init; } = new(0, -98.0f);

    /// <summary>
    /// Emission shape and pattern.
    /// </summary>
    public EmissionPattern EmissionPattern { get; init; } = new();

    /// <summary>
    /// Texture or sprite to use for particles.
    /// </summary>
    public string TexturePath { get; init; } = string.Empty;

    /// <summary>
    /// Blend mode for rendering particles.
    /// </summary>
    public BlendMode BlendMode { get; init; } = BlendMode.Additive;

    /// <summary>
    /// Whether particles should collide with the environment.
    /// </summary>
    public bool EnableCollision { get; init; } = false;

    /// <summary>
    /// Custom behaviors to apply to particles.
    /// </summary>
    public IReadOnlyList<ParticleBehavior> Behaviors { get; init; } = Array.Empty<ParticleBehavior>();
}

/// <summary>
/// Types of particle effects.
/// </summary>
public enum ParticleEffectType
{
    Explosion,
    Fire,
    Smoke,
    Spark,
    Dust,
    Energy,
    Magic,
    Blood,
    Custom
}

/// <summary>
/// Size dimensions for particles.
/// </summary>
public record ParticleSize(float Width, float Height);

/// <summary>
/// Color gradient for particle lifetime.
/// </summary>
public record ColorGradient
{
    /// <summary>
    /// Starting color.
    /// </summary>
    public string StartColor { get; init; } = "#FFFFFF";

    /// <summary>
    /// Middle color (optional).
    /// </summary>
    public string? MiddleColor { get; init; }

    /// <summary>
    /// End color.
    /// </summary>
    public string EndColor { get; init; } = "#000000";
}

/// <summary>
/// Velocity vector for particles.
/// </summary>
public record ParticleVelocity(float X = 0, float Y = 0);

/// <summary>
/// Pattern for particle emission.
/// </summary>
public record EmissionPattern
{
    /// <summary>
    /// Shape of emission area.
    /// </summary>
    public EmissionShape Shape { get; init; } = EmissionShape.Point;

    /// <summary>
    /// Size of emission area.
    /// </summary>
    public ParticleSize AreaSize { get; init; } = new(10.0f, 10.0f);

    /// <summary>
    /// Angle spread for emission.
    /// </summary>
    public float AngleSpread { get; init; } = 360.0f;

    /// <summary>
    /// Speed variation range.
    /// </summary>
    public FloatRange SpeedRange { get; init; } = new(50.0f, 100.0f);
}

/// <summary>
/// Shapes for particle emission.
/// </summary>
public enum EmissionShape
{
    Point,
    Circle,
    Rectangle,
    Line,
    Cone
}

/// <summary>
/// Float value range.
/// </summary>
public record FloatRange(float Min, float Max);

/// <summary>
/// Blend modes for particle rendering.
/// </summary>
public enum BlendMode
{
    Alpha,
    Additive,
    Multiply,
    Screen
}

/// <summary>
/// Custom behavior for particles.
/// </summary>
public record ParticleBehavior
{
    /// <summary>
    /// Type of behavior.
    /// </summary>
    public BehaviorType Type { get; init; }

    /// <summary>
    /// Parameters for the behavior.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Types of particle behaviors.
/// </summary>
public enum BehaviorType
{
    Fade,
    Scale,
    Rotate,
    Bounce,
    Follow,
    Attract,
    Repel
}
