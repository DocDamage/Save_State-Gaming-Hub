using System;
using System.Collections.Generic;
using System.Numerics;

namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Fusion modes for merged MUGEN characters.
/// </summary>
public enum FusionType
{
    Balanced,
    Dominant,
    GodLike
}

/// <summary>
/// Options that control the fusion process.
/// </summary>
public sealed class FusionOptions
{
    public bool PreserveSprites { get; init; } = true;
    public bool PreserveAnimations { get; init; } = true;
    public bool PreserveSound { get; init; } = true;
    public bool PreserveStates { get; init; } = true;
}

/// <summary>
/// Result of creating a fusion character.
/// </summary>
public sealed record FusionResult(Guid Id, string Name, string Directory, MugenCharacterStats Stats);

/// <summary>
/// Metadata exposed for available fusions.
/// </summary>
public sealed record FusionMetadata(Guid Id, string Name, DateTime CreatedAt, IReadOnlyList<string> SourceCharacters, int PowerLevel);

/// <summary>
/// Simple stats used in fusion calculations.
/// </summary>
public sealed record MugenCharacterStats(int Health, int Attack, int Defense, float Speed, int PowerLevel);

/// <summary>
/// Types of hitboxes that can appear in a move.
/// </summary>
public enum HitboxType
{
    Attack,
    Projectile,
    Throw
}

/// <summary>
/// Hit levels for hitboxes.
/// </summary>
public enum HitLevel
{
    Light,
    Medium,
    Heavy
}

/// <summary>
/// Rectangle-like type used by hitboxes and hurtboxes.
/// </summary>
public sealed class Rectangle
{
    public Rectangle() { }
    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

/// <summary>
/// Represents a hitbox used during move export.
/// </summary>
public sealed class Hitbox
{
    public HitboxType Type { get; init; } = HitboxType.Attack;
    public Rectangle Bounds { get; init; } = new();
    public HitLevel HitLevel { get; init; } = HitLevel.Light;
    public IReadOnlyList<string> HitFlags { get; init; } = Array.Empty<string>();
    public int Damage { get; init; }
    public int GuardDamage { get; init; }
    public int HitPause { get; init; }
    public int GuardPause { get; init; }
    public int PlayerHitPause { get; init; }
    public int PlayerGuardPause { get; init; }
    public Vector2 Velocity { get; init; } = Vector2.Zero;
}

/// <summary>
/// Represents a projectile spawned during a move.
/// </summary>
public sealed class Projectile
{
    public int Id { get; init; }
    public string Animation { get; init; } = string.Empty;
    public Vector2 Position { get; init; } = Vector2.Zero;
    public int Damage { get; init; }
    public IReadOnlyList<string> HitFlags { get; init; } = Array.Empty<string>();
    public int Hits { get; init; }
    public bool RemoveOnHit { get; init; }
    public bool RemoveOnBlock { get; init; }
    public int Time { get; init; }
    public Vector2 Velocity { get; init; } = Vector2.Zero;
    public Vector2 Acceleration { get; init; } = Vector2.Zero;
}

/// <summary>
/// Represents a simple particle effect or explosion.
/// </summary>
public sealed class ParticleEffect
{
    public string Type { get; init; } = string.Empty;
    public string Animation { get; init; } = string.Empty;
    public Vector2 Position { get; init; } = Vector2.Zero;
    public int Duration { get; init; }
    public Dictionary<string, string> Parameters { get; } = new();
}

/// <summary>
/// Options provided to the move exporter.
/// </summary>
public sealed class ExportOptions
{
    public string OutputDirectory { get; init; } = "./output";
    public bool GenerateAirVersion { get; init; } = false;
    public bool IncludeComments { get; init; } = true;

    public ExportOptions()
    {
    }

    public ExportOptions(string outputDirectory, bool generateAirVersion = false, bool includeComments = true)
    {
        OutputDirectory = outputDirectory;
        GenerateAirVersion = generateAirVersion;
        IncludeComments = includeComments;
    }
}

/// <summary>
/// Result returned by the export process.
/// </summary>
public sealed record MoveExportResult(
    string CnsFilePath,
    string CmdFilePath,
    string? AirFilePath,
    long CnsFileSize,
    long CmdFileSize,
    long AirFileSize,
    IReadOnlyList<string> GeneratedStates,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Move categories used during conversion.
/// </summary>
public enum MoveCategory
{
    Movement,
    Attack,
    Throw,
    Utility,
    Support,
    Undefined
}

/// <summary>
/// Types of moves recognized in the MUGEN toolchain.
/// </summary>
public enum MoveType
{
    Normal,
    Special,
    Super,
    Hyper,
    Throw,
    Other
}

/// <summary>
/// Ground or air classification.
/// </summary>
public enum GroundAirType
{
    Ground,
    Air
}

/// <summary>
/// Metadata stored for a move.
/// </summary>
public sealed class MoveMetadata
{
    public string Version { get; init; } = "1.0";
    public string Author { get; init; } = "Unknown";
    public DateTime Created { get; init; } = DateTime.UtcNow;
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Properties that describe a move's frame data and behavior.
/// </summary>
public sealed class MoveProperties
{
    public int Damage { get; init; }
    public float MeterGain { get; init; }
    public int MeterCost { get; init; }
    public int StartupFrames { get; init; }
    public int ActiveFrames { get; init; }
    public int RecoveryFrames { get; init; }
    public int FrameAdvantageOnHit { get; init; }
    public int FrameAdvantageOnBlock { get; init; }
    public int HitStun { get; init; }
    public int BlockStun { get; init; }
    public int HitStop { get; init; }
    public int BlockStop { get; init; }
    public bool CausesKnockdown { get; init; }
    public bool GuardCrush { get; init; }
    public bool CounterHit { get; init; }
    public bool Unblockable { get; init; }
    public bool ArmorBreak { get; init; }
    public string KnockdownType { get; init; } = string.Empty;
    public string HitEffect { get; init; } = string.Empty;
    public string GuardEffect { get; init; } = string.Empty;
    public int Priority { get; init; }
    public GroundAirType GroundAirType { get; init; }
    public string Attribute { get; init; } = string.Empty;
    public string Flags { get; init; } = string.Empty;
    public Vector2 Velocity { get; init; } = Vector2.Zero;
    public Vector2 Acceleration { get; init; } = Vector2.Zero;
    public bool Invincible { get; init; }

    public MoveProperties(
        int Damage,
        float MeterGain,
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
        string KnockdownType,
        string HitEffect,
        string GuardEffect,
        int Priority,
        GroundAirType GroundAirType,
        string Attribute,
        string Flags)
    {
        this.Damage = Damage;
        this.MeterGain = MeterGain;
        this.MeterCost = MeterCost;
        this.StartupFrames = StartupFrames;
        this.ActiveFrames = ActiveFrames;
        this.RecoveryFrames = RecoveryFrames;
        this.FrameAdvantageOnHit = FrameAdvantageOnHit;
        this.FrameAdvantageOnBlock = FrameAdvantageOnBlock;
        this.HitStun = HitStun;
        this.BlockStun = BlockStun;
        this.HitStop = HitStop;
        this.BlockStop = BlockStop;
        this.CausesKnockdown = CausesKnockdown;
        this.GuardCrush = GuardCrush;
        this.CounterHit = CounterHit;
        this.Unblockable = Unblockable;
        this.ArmorBreak = ArmorBreak;
        this.KnockdownType = KnockdownType;
        this.HitEffect = HitEffect;
        this.GuardEffect = GuardEffect;
        this.Priority = Priority;
        this.GroundAirType = GroundAirType;
        this.Attribute = Attribute;
        this.Flags = Flags;
    }
}

/// <summary>
/// Represents a single state within a move definition.
/// </summary>
public sealed class MoveState
{
    public int StateNumber { get; init; }
    public string AnimationElement { get; init; } = string.Empty;
    public MoveStateProperties Properties { get; init; } = new();
    public IReadOnlyList<Hitbox> Hitboxes { get; init; } = Array.Empty<Hitbox>();
    public IReadOnlyList<Hitbox> Hurtboxes { get; init; } = Array.Empty<Hitbox>();
    public IReadOnlyList<Projectile> Projectiles { get; init; } = Array.Empty<Projectile>();
    public IReadOnlyList<ParticleEffect> Effects { get; init; } = Array.Empty<ParticleEffect>();
    public bool HasHitboxes => Hitboxes.Count > 0;
    public int Duration => Properties.StartupFrames + Properties.ActiveFrames + Properties.RecoveryFrames;
}

/// <summary>
/// Additional properties for move states.
/// </summary>
public sealed class MoveStateProperties
{
    public Vector2 Velocity { get; init; } = Vector2.Zero;
    public Vector2 Acceleration { get; init; } = Vector2.Zero;
    public bool Invincible { get; init; }
    public Dictionary<string, string> CustomProperties { get; } = new();
    public int StartupFrames { get; init; }
    public int ActiveFrames { get; init; }
    public int RecoveryFrames { get; init; }
}

/// <summary>
/// Represents a move definition containing states.
/// </summary>
public sealed record MugenMoveDefinition(
    string Name,
    string DisplayName,
    string Command,
    MoveType MoveType,
    MoveCategory Category,
    MoveMetadata Metadata,
    MoveProperties Properties,
    IReadOnlyList<MoveState> States)
{
    public MugenMoveDefinition With(
        string? name = null,
        string? displayName = null,
        MoveProperties? properties = null)
    {
        return this with
        {
            Name = name ?? Name,
            DisplayName = displayName ?? DisplayName,
            Properties = properties ?? Properties
        };
    }
}
