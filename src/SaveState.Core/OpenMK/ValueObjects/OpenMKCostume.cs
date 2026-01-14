namespace SaveState.Core.OpenMK.ValueObjects;

/// <summary>
/// Represents an alternative costume for an OpenMK character.
/// </summary>
public class OpenMKCostume
{
    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private OpenMKCostume()
    {
        Name = string.Empty;
        DisplayName = string.Empty;
        Description = string.Empty;
        SpritePath = string.Empty;
    }

    /// <summary>
    /// Creates a new OpenMK costume.
    /// </summary>
    public OpenMKCostume(
        string name,
        string displayName,
        string description,
        string spritePath,
        bool isDefault = false,
        OpenMKUnlockRequirements? unlockRequirements = null)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        SpritePath = spritePath;
        IsDefault = isDefault;
        UnlockRequirements = unlockRequirements;
    }

    /// <summary>
    /// Gets the internal name of the costume.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the display name of the costume.
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Gets the description of the costume.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the path to the costume sprites.
    /// </summary>
    public string SpritePath { get; private set; }

    /// <summary>
    /// Gets whether this is the default costume.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Gets the unlock requirements for this costume, if any.
    /// </summary>
    public OpenMKUnlockRequirements? UnlockRequirements { get; private set; }
}
