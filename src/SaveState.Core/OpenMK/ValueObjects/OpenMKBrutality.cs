namespace SaveState.Core.OpenMK.ValueObjects;

/// <summary>
/// Represents a brutality in OpenMK (special finishing moves with specific requirements).
/// </summary>
public record OpenMKBrutality(
    string Name,
    string DisplayName,
    string Description,
    string InputCommand,
    string[] Requirements, // Array of conditions that must be met
    string AnimationSequence,
    string? SoundEffect = null,
    string? VoiceLine = null);