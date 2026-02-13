namespace SaveState.Core.OpenMK.ValueObjects;

/// <summary>
/// Represents a friendship in OpenMK (humorous finishing moves).
/// </summary>
public record OpenMKFriendship(
    string Name,
    string DisplayName,
    string Description,
    string InputCommand,
    string AnimationSequence,
    string? SoundEffect = null,
    string? VoiceLine = null,
    string? ItemUsed = null); // What item/props are used in the friendship