using SaveState.Core.Common.ValueObjects;

namespace SaveState.Core.OpenMK.ValueObjects;

/// <summary>
/// Represents a special move in OpenMK (Mortal Kombat-style special attacks).
/// </summary>
public record OpenMKSpecialMove(
    string Name,
    string DisplayName,
    string Description,
    string InputCommand, // e.g., "Down, Forward, Punch"
    OpenMKSpecialMoveType Type,
    int Damage,
    bool RequiresSuperBar = false,
    int SuperBarCost = 0,
    string? AnimationName = null,
    string? SoundEffect = null);

/// <summary>
/// Types of special moves in OpenMK.
/// </summary>
public enum OpenMKSpecialMoveType
{
    /// <summary>
    /// Basic special move.
    /// </summary>
    Special,

    /// <summary>
    /// Enhanced version of a special move.
    /// </summary>
    Enhanced,

    /// <summary>
    /// Super move that requires super bar.
    /// </summary>
    Super,

    /// <summary>
    /// Ultimate move with high damage.
    /// </summary>
    Ultimate,

    /// <summary>
    /// Weapon-based special move.
    /// </summary>
    Weapon,

    /// <summary>
    /// Fatal Blow (mid-match finisher).
    /// </summary>
    FatalBlow
}