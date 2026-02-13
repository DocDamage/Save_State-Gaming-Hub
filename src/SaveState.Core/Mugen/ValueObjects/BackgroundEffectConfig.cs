namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Configuration for interactive background effects with parallax and animations.
/// </summary>
public record BackgroundEffectConfig
{
    /// <summary>
    /// Enables parallax scrolling effects.
    /// </summary>
    public bool EnableParallax { get; init; } = true;

    /// <summary>
    /// Parallax scroll speed multiplier.
    /// </summary>
    public float ParallaxSpeed { get; init; } = 0.5f;

    /// <summary>
    /// Layers of background elements with different parallax speeds.
    /// </summary>
    public IReadOnlyList<BackgroundLayer> Layers { get; init; } = Array.Empty<BackgroundLayer>();

    /// <summary>
    /// Interactive elements that respond to match events.
    /// </summary>
    public IReadOnlyList<InteractiveElement> InteractiveElements { get; init; } = Array.Empty<InteractiveElement>();

    /// <summary>
    /// Particle effects for background ambiance.
    /// </summary>
    public BackgroundParticles Particles { get; init; } = new();

    /// <summary>
    /// Dynamic lighting effects for the background.
    /// </summary>
    public BackgroundLighting Lighting { get; init; } = new();

    /// <summary>
    /// Weather effects to apply to the background.
    /// </summary>
    public WeatherEffects Weather { get; init; } = new();
}

/// <summary>
/// Background layer with parallax and animation properties.
/// </summary>
public record BackgroundLayer
{
    /// <summary>
    /// Name of the layer for identification.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Path to the layer image.
    /// </summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>
    /// Parallax speed multiplier for this layer.
    /// </summary>
    public float ParallaxMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Z-depth of the layer (affects rendering order).
    /// </summary>
    public int ZDepth { get; init; }

    /// <summary>
    /// Opacity of the layer (0.0 to 1.0).
    /// </summary>
    public float Opacity { get; init; } = 1.0f;

    /// <summary>
    /// Animation to apply to this layer.
    /// </summary>
    public LayerAnimation Animation { get; init; } = new();

    /// <summary>
    /// Blend mode for this layer.
    /// </summary>
    public BlendMode BlendMode { get; init; } = BlendMode.Alpha;
}

/// <summary>
/// Animation configuration for background layers.
/// </summary>
public record LayerAnimation
{
    /// <summary>
    /// Type of animation.
    /// </summary>
    public LayerAnimationType Type { get; init; } = LayerAnimationType.Static;

    /// <summary>
    /// Speed of animation.
    /// </summary>
    public float Speed { get; init; } = 1.0f;

    /// <summary>
    /// Amplitude of animation effect.
    /// </summary>
    public float Amplitude { get; init; } = 1.0f;

    /// <summary>
    /// Direction of animation movement.
    /// </summary>
    public Direction Direction { get; init; } = Direction.Horizontal;
}

/// <summary>
/// Types of layer animations.
/// </summary>
public enum LayerAnimationType
{
    Static,
    Scroll,
    Wave,
    Rotate,
    Scale,
    Fade
}

/// <summary>
/// Animation direction.
/// </summary>
public enum Direction
{
    Horizontal,
    Vertical,
    Diagonal,
    Circular
}

/// <summary>
/// Interactive background elements that respond to match events.
/// </summary>
public record InteractiveElement
{
    /// <summary>
    /// Type of interactive element.
    /// </summary>
    public InteractiveElementType Type { get; init; }

    /// <summary>
    /// Position of the element.
    /// </summary>
    public BackgroundPosition Position { get; init; } = new();

    /// <summary>
    /// Triggers that activate this element.
    /// </summary>
    public IReadOnlyList<ElementTrigger> Triggers { get; init; } = Array.Empty<ElementTrigger>();

    /// <summary>
    /// Animation to play when triggered.
    /// </summary>
    public ElementAnimation Animation { get; init; } = new();

    /// <summary>
    /// Sound effect to play when triggered.
    /// </summary>
    public string SoundEffect { get; init; } = string.Empty;
}

/// <summary>
/// Types of interactive elements.
/// </summary>
public enum InteractiveElementType
{
    Sprite,
    ParticleEmitter,
    LightSource,
    Decal,
    Effect
}

/// <summary>
/// Trigger conditions for interactive elements.
/// </summary>
public record ElementTrigger
{
    /// <summary>
    /// Type of trigger event.
    /// </summary>
    public TriggerType EventType { get; init; }

    /// <summary>
    /// Specific conditions for the trigger.
    /// </summary>
    public string Condition { get; init; } = string.Empty;

    /// <summary>
    /// Probability of trigger activation (0.0 to 1.0).
    /// </summary>
    public float Probability { get; init; } = 1.0f;
}

/// <summary>
/// Types of trigger events.
/// </summary>
public enum TriggerType
{
    MatchStart,
    RoundStart,
    Hit,
    SpecialMove,
    SuperMove,
    Combo,
    HealthLow,
    TimeLow,
    PlayerVictory
}

/// <summary>
/// Animation for interactive elements.
/// </summary>
public record ElementAnimation
{
    /// <summary>
    /// Animation sequence name.
    /// </summary>
    public string SequenceName { get; init; } = string.Empty;

    /// <summary>
    /// Duration of animation in seconds.
    /// </summary>
    public float Duration { get; init; } = 1.0f;

    /// <summary>
    /// Whether animation loops.
    /// </summary>
    public bool Loop { get; init; } = false;
}

/// <summary>
/// Background particle effects.
/// </summary>
public record BackgroundParticles
{
    /// <summary>
    /// Type of particles.
    /// </summary>
    public ParticleEffectType ParticleType { get; init; } = ParticleEffectType.Dust;

    /// <summary>
    /// Number of particles.
    /// </summary>
    public int Count { get; init; } = 20;

    /// <summary>
    /// Density of particle spawning.
    /// </summary>
    public float Density { get; init; } = 1.0f;

    /// <summary>
    /// Wind effect on particles.
    /// </summary>
    public BackgroundVelocity Wind { get; init; } = new();
}

/// <summary>
/// Background lighting configuration.
/// </summary>
public record BackgroundLighting
{
    /// <summary>
    /// Ambient light color.
    /// </summary>
    public string AmbientColor { get; init; } = "#FFFFFF";

    /// <summary>
    /// Ambient light intensity.
    /// </summary>
    public float AmbientIntensity { get; init; } = 0.3f;

    /// <summary>
    /// Dynamic lights in the background.
    /// </summary>
    public IReadOnlyList<LightEffect> Lights { get; init; } = Array.Empty<LightEffect>();
}

/// <summary>
/// Weather effects for backgrounds.
/// </summary>
public record WeatherEffects
{
    /// <summary>
    /// Type of weather effect.
    /// </summary>
    public WeatherType Type { get; init; } = WeatherType.None;

    /// <summary>
    /// Intensity of weather effect.
    /// </summary>
    public float Intensity { get; init; } = 1.0f;

    /// <summary>
    /// Speed of weather animation.
    /// </summary>
    public float Speed { get; init; } = 1.0f;

    /// <summary>
    /// Custom weather particle configuration.
    /// </summary>
    public ParticleEffectConfig? CustomParticles { get; init; }
}

/// <summary>
/// Types of weather effects.
/// </summary>
public enum WeatherType
{
    None,
    Rain,
    Snow,
    Fog,
    Wind,
    Custom
}

/// <summary>
/// Position for background elements.
/// </summary>
public record BackgroundPosition(float X = 0, float Y = 0);

/// <summary>
/// Velocity for background elements.
/// </summary>
public record BackgroundVelocity(float X = 0, float Y = 0);
