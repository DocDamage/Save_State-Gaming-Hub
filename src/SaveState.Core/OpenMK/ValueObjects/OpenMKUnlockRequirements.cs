namespace SaveState.Core.OpenMK.ValueObjects;

/// <summary>
/// Represents unlock requirements for OpenMK characters or content.
/// </summary>
public class OpenMKUnlockRequirements
{
    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private OpenMKUnlockRequirements()
    {
        Description = string.Empty;
    }

    /// <summary>
    /// Creates new unlock requirements.
    /// </summary>
    public OpenMKUnlockRequirements(
        string description,
        OpenMKUnlockType type,
        int? requiredValue = null,
        string? requiredCharacter = null,
        string? requiredStage = null)
    {
        Description = description;
        Type = type;
        RequiredValue = requiredValue;
        RequiredCharacter = requiredCharacter;
        RequiredStage = requiredStage;
    }

    /// <summary>
    /// Gets the description of the unlock requirement.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the type of unlock requirement.
    /// </summary>
    public OpenMKUnlockType Type { get; private set; }

    /// <summary>
    /// Gets the required value (e.g., win count, level, etc.).
    /// </summary>
    public int? RequiredValue { get; private set; }

    /// <summary>
    /// Gets the required character name, if applicable.
    /// </summary>
    public string? RequiredCharacter { get; private set; }

    /// <summary>
    /// Gets the required stage name, if applicable.
    /// </summary>
    public string? RequiredStage { get; private set; }
}

/// <summary>
/// Types of unlock requirements in OpenMK.
/// </summary>
public enum OpenMKUnlockType
{
    /// <summary>
    /// Unlock by completing story mode.
    /// </summary>
    StoryMode,

    /// <summary>
    /// Unlock by reaching a certain character level.
    /// </summary>
    CharacterLevel,

    /// <summary>
    /// Unlock by winning a certain number of matches.
    /// </summary>
    WinCount,

    /// <summary>
    /// Unlock by performing a certain number of fatalities.
    /// </summary>
    FatalityCount,

    /// <summary>
    /// Unlock by defeating a specific character.
    /// </summary>
    DefeatCharacter,

    /// <summary>
    /// Unlock by completing a specific stage/mission.
    /// </summary>
    CompleteStage,

    /// <summary>
    /// Unlock by collecting a certain number of koins.
    /// </summary>
    KoinCount,

    /// <summary>
    /// Unlock by time-based progression.
    /// </summary>
    TimeBased,

    /// <summary>
    /// Always unlocked (default).
    /// </summary>
    AlwaysUnlocked
}
