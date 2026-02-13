using System.Collections.Immutable;
using SaveState.Core.Mugen.Services;

namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Types of moves.
/// </summary>
public enum MoveType
{
    Normal,
    Special,
    Super,
    Hyper,
    Throw,
    CommandGrab,
    Movement,
    Other
}

/// <summary>
/// Comprehensive definition of a MUGEN character move.
/// Contains all state data, hitboxes, frame data, and properties.
/// </summary>
public sealed record MugenMoveDefinition(
    string Name,
    string DisplayName,
    string Command,
    MoveType MoveType,
    MoveCategory Category,
    MoveProperties Properties,
    IReadOnlyList<MoveState> States,
    IReadOnlyDictionary<string, string> Parameters,
    MoveMetadata Metadata)
{
    /// <summary>
    /// Gets the total duration of the move in frames.
    /// </summary>
    public int TotalDuration => States.Sum(s => s.Duration);

    /// <summary>
    /// Gets the startup frames (frames before active hitboxes).
    /// </summary>
    public int StartupFrames => Properties.StartupFrames;

    /// <summary>
    /// Gets the active frames (frames with hitboxes).
    /// </summary>
    public int ActiveFrames => Properties.ActiveFrames;

    /// <summary>
    /// Gets the recovery frames (frames after active hitboxes).
    /// </summary>
    public int RecoveryFrames => Properties.RecoveryFrames;

    /// <summary>
    /// Gets the total frame advantage on hit.
    /// </summary>
    public int FrameAdvantageOnHit => Properties.FrameAdvantageOnHit;

    /// <summary>
    /// Gets the total frame advantage on block.
    /// </summary>
    public int FrameAdvantageOnBlock => Properties.FrameAdvantageOnBlock;

    /// <summary>
    /// Validates the move definition for consistency.
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Validate state progression
        if (States.Count == 0)
        {
            errors.Add(new ValidationError("NoStates", "Move must have at least one state"));
        }

        // Validate frame data consistency
        var totalFrames = States.Sum(s => s.Duration);
        var expectedFrames = StartupFrames + ActiveFrames + RecoveryFrames;

        if (totalFrames != expectedFrames)
        {
            warnings.Add(new ValidationWarning("FrameMismatch",
                $"Total state frames ({totalFrames}) don't match frame data ({expectedFrames})"));
        }

        // Validate hitboxes exist during active frames
        var activeStates = States.Where(s => s.HasHitboxes).ToList();
        if (activeStates.Count == 0 && ActiveFrames > 0)
        {
            warnings.Add(new ValidationWarning("NoHitboxes", "Move has active frames but no hitboxes"));
        }

        // Validate command format
        if (string.IsNullOrWhiteSpace(Command))
        {
            errors.Add(new ValidationError("EmptyCommand", "Move command cannot be empty"));
        }

        return new ValidationResult(
            IsValid: !errors.Any(),
            Errors: errors,
            Warnings: warnings,
            Suggestions: Array.Empty<string>());
    }

    /// <summary>
    /// Creates a deep copy with modifications.
    /// </summary>
    public MugenMoveDefinition With(
        string? name = null,
        string? displayName = null,
        string? command = null,
        MoveType? moveType = null,
        MoveCategory? category = null,
        MoveProperties? properties = null,
        IReadOnlyList<MoveState>? states = null,
        IReadOnlyDictionary<string, string>? parameters = null,
        MoveMetadata? metadata = null)
    {
        return new MugenMoveDefinition(
            Name: name ?? Name,
            DisplayName: displayName ?? DisplayName,
            Command: command ?? Command,
            MoveType: moveType ?? MoveType,
            Category: category ?? Category,
            Properties: properties ?? Properties,
            States: states ?? States,
            Parameters: parameters ?? Parameters,
            Metadata: metadata ?? Metadata);
    }
}

/// <summary>
/// Category of move for organization and filtering.
/// </summary>
public enum MoveCategory
{
    Normal,
    CommandNormal,
    Special,
    Super,
    Hyper,
    Throw,
    Counter,
    Parry,
    Taunt,
    Movement
}

/// <summary>
/// Properties that define move behavior and balance.
/// </summary>
public sealed record MoveProperties(
    int Damage,
    int MeterGain,
    int MeterCost,
    int StartupFrames,
    int ActiveFrames,
    int RecoveryFrames,
    int FrameAdvantageOnHit,
    int FrameAdvantageOnBlock,
    int HitStun,
    int BlockStun,
    int HitStop,
    int BlockStop,
    bool CausesKnockdown,
    bool GuardCrush,
    bool CounterHit,
    bool Unblockable,
    bool ArmorBreak,
    KnockdownType KnockdownType,
    HitEffect HitEffect,
    GuardEffect GuardEffect,
    Priority Priority,
    GroundAirType GroundAirType,
    MoveAttribute Attribute,
    IReadOnlyList<string> Flags);

/// <summary>
/// Type of knockdown caused by the move.
/// </summary>
public enum KnockdownType
{
    None,
    Soft,
    Hard,
    WallBounce,
    GroundBounce,
    WallSplat
}

/// <summary>
/// Visual effect when move hits.
/// </summary>
public enum HitEffect
{
    Light,
    Medium,
    Heavy,
    Special,
    Custom
}

/// <summary>
/// Visual effect when move is blocked.
/// </summary>
public enum GuardEffect
{
    Light,
    Medium,
    Heavy,
    Special
}

/// <summary>
/// Priority level for hit conflicts.
/// </summary>
public enum Priority
{
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Whether move works on ground, air, or both.
/// </summary>
public enum GroundAirType
{
    Ground,
    Air,
    Both
}

/// <summary>
/// Move attribute for counter systems.
/// </summary>
public enum MoveAttribute
{
    Normal,
    Special,
    Projectile,
    Throw,
    CommandGrab
}

/// <summary>
/// Individual state in a move's animation sequence.
/// </summary>
public sealed record MoveState(
    int StateNumber,
    int Duration,
    int AnimationElement,
    string SpriteGroup,
    string SpriteNumber,
    Position Position,
    IReadOnlyList<Hitbox> Hitboxes,
    IReadOnlyList<Hurtbox> Hurtboxes,
    IReadOnlyList<Projectile> Projectiles,
    IReadOnlyList<ParticleEffect> Effects,
    StateProperties Properties)
{
    /// <summary>
    /// Whether this state has any hitboxes.
    /// </summary>
    public bool HasHitboxes => Hitboxes.Count > 0;

    /// <summary>
    /// Whether this state has any projectiles.
    /// </summary>
    public bool HasProjectiles => Projectiles.Count > 0;
}

/// <summary>
/// Properties specific to a state.
/// </summary>
public sealed record StateProperties(
    bool Invincible,
    bool SuperArmor,
    bool CounterHitState,
    bool ProjectileInvincible,
    bool HeadInvincible,
    bool ThrowInvincible,
    Velocity Velocity,
    Acceleration Acceleration,
    IReadOnlyDictionary<string, string> CustomProperties);

/// <summary>
/// Hitbox definition for collision detection.
/// </summary>
public sealed record Hitbox(
    HitboxType Type,
    Rectangle Bounds,
    int HitId,
    int Damage,
    int GuardDamage,
    int HitPause,
    int GuardPause,
    int PlayerHitPause,
    int PlayerGuardPause,
    HitLevel HitLevel,
    bool GroundHit,
    bool AirHit,
    bool DownHit,
    bool GuardReversal,
    IReadOnlyList<string> HitFlags);

/// <summary>
/// Type of hitbox.
/// </summary>
public enum HitboxType
{
    Attack,
    Projectile,
    Throw
}

/// <summary>
/// Level of hit for combo scaling.
/// </summary>
public enum HitLevel
{
    Light,
    Medium,
    Heavy,
    Special
}

/// <summary>
/// Hurtbox definition for damageable area.
/// </summary>
public sealed record Hurtbox(
    HurtboxType Type,
    Rectangle Bounds,
    int Id);

/// <summary>
/// Type of hurtbox.
/// </summary>
public enum HurtboxType
{
    Body,
    Head,
    Legs,
    Projectile
}

/// <summary>
/// Projectile definition.
/// </summary>
public sealed record Projectile(
    int Id,
    string Animation,
    Position Position,
    Velocity Velocity,
    Acceleration Acceleration,
    int Damage,
    int Hits,
    int Time,
    bool RemoveOnHit,
    bool RemoveOnBlock,
    IReadOnlyList<string> HitFlags);

/// <summary>
/// Particle effect definition.
/// </summary>
public sealed record ParticleEffect(
    string Type,
    Position Position,
    string Animation,
    int Duration,
    IReadOnlyDictionary<string, string> Parameters);


/// <summary>
/// Metadata about the move for organization and search.
/// </summary>
public sealed record MoveMetadata(
    string Author,
    string Description,
    DateTime Created,
    DateTime Modified,
    IReadOnlyList<string> Tags,
    DifficultyLevel Difficulty,
    IReadOnlyList<string> Prerequisites,
    string Version,
    IReadOnlyDictionary<string, string> CustomMetadata);

/// <summary>
/// Difficulty level for move creation.
/// </summary>
public enum DifficultyLevel
{
    Beginner,
    Intermediate,
    Advanced,
    Expert,

    // Additional granular levels used by AI systems and difficulty mapping
    VeryEasy,
    Easy,
    Medium,
    Hard,
    VeryHard
}

/// <summary>
/// Result of validation.
/// </summary>
public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings,
    IReadOnlyList<string> Suggestions);

/// <summary>
/// Validation error.
/// </summary>
public sealed record ValidationError(string Code, string Message);

/// <summary>
/// Validation warning.
/// </summary>
public sealed record ValidationWarning(string Code, string Message);
