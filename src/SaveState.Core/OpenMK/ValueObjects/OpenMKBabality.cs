namespace SaveState.Core.OpenMK.ValueObjects;

/// <summary>
/// Represents a babality in OpenMK (turns opponent into a baby).
/// </summary>
public record OpenMKBabality(
    string Name,
    string DisplayName,
    string Description,
    string InputCommand,
    string AnimationSequence,
    string? SoundEffect = null,
    string? VoiceLine = null,
    string? BabyItem = null); // What the baby version holds