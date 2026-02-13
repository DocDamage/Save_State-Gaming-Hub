namespace SaveState.Core.OpenMK.ValueObjects;

/// <summary>
/// Represents a fatality in OpenMK (Mortal Kombat-style finishing moves).
/// </summary>
public record OpenMKFatality(
    string Name,
    string DisplayName,
    string Description,
    string InputCommand, // e.g., "Forward, Down, Forward, Punch"
    OpenMKFatalityType Type,
    string AnimationSequence,
    string? SoundEffect = null,
    string? VoiceLine = null,
    int RequiredDistance = 1, // 1 = close, 2 = anywhere
    bool RequiresSweep = false); // Whether opponent must be swept

/// <summary>
/// Types of fatalities in OpenMK.
/// </summary>
public enum OpenMKFatalityType
{
    /// <summary>
    /// Standard fatality.
    /// </summary>
    Standard,

    /// <summary>
    /// Stage fatality (uses environment).
    /// </summary>
    Stage,

    /// <summary>
    /// Brutality (alternate finisher).
    /// </summary>
    Brutality,

    /// <summary>
    /// Friendship (humorous ending).
    /// </summary>
    Friendship,

    /// <summary>
    /// Babality (turns opponent into baby).
    /// </summary>
    Babality,

    /// <summary>
    /// Hara-Kiri (self-destruct fatality).
    /// </summary>
    HaraKiri,

    /// <summary>
    /// Animalities (turns into animal).
    /// </summary>
    Animalities
}