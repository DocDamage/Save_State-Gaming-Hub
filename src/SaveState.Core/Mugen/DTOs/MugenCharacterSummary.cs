namespace SaveState.Core.Mugen.DTOs;

/// <summary>
/// Data transfer object for MUGEN character summary information.
/// </summary>
public class MugenCharacterSummary
{
    /// <summary>
    /// Gets or sets the unique character identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the character's unique name identifier.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character's author.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character's version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the character is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the last time this character was scanned.
    /// </summary>
    public DateTime LastScannedAt { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }
}
