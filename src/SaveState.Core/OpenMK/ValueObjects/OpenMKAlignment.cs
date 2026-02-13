namespace SaveState.Core.OpenMK.ValueObjects;

/// <summary>
/// Represents character alignment in OpenMK.
/// </summary>
public enum OpenMKAlignment
{
    /// <summary>
    /// Good alignment (heroes, defenders).
    /// </summary>
    Good,

    /// <summary>
    /// Evil alignment (villains, conquerors).
    /// </summary>
    Evil,

    /// <summary>
    /// Neutral alignment (anti-heroes, mercenaries).
    /// </summary>
    Neutral,

    /// <summary>
    /// Chaotic alignment (unpredictable, wild cards).
    /// </summary>
    Chaotic,

    /// <summary>
    /// Unknown alignment.
    /// </summary>
    Unknown
}