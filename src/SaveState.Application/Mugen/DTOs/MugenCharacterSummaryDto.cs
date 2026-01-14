using SaveState.Core.Mugen.DTOs;

namespace SaveState.Application.Mugen.DTOs;

/// <summary>
/// Backwards compatibility class for MugenCharacterSummary moved to Core layer.
/// Application layer should use the Core DTO directly or map from it.
/// </summary>
[Obsolete("Use SaveState.Core.Mugen.DTOs.MugenCharacterSummary instead", false)]
public class MugenCharacterSummaryDto
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

    public MugenCharacterSummaryDto() { }

    public MugenCharacterSummaryDto(
        Guid id, string name, string displayName, string author, string version,
        bool isValid, DateTime? lastScannedAt, long fileSize)
    {
        Id = id;
        Name = name;
        DisplayName = displayName;
        Author = author;
        Version = version;
        IsValid = isValid;
        LastScannedAt = lastScannedAt ?? DateTime.MinValue; // Handle explicit nullable to non-nullable
        FileSize = fileSize;
    }

    /// <summary>
    /// Create from Core DTO.
    /// </summary>
    public static MugenCharacterSummaryDto FromCore(MugenCharacterSummary core)
    {
        return new MugenCharacterSummaryDto
        {
            Id = core.Id,
            Name = core.Name,
            DisplayName = core.DisplayName,
            Author = core.Author,
            Version = core.Version,
            IsValid = core.IsValid,
            LastScannedAt = core.LastScannedAt,
            FileSize = core.FileSize
        };
    }

    /// <summary>
    /// Convert to Core DTO.
    /// </summary>
    public MugenCharacterSummary ToCore()
    {
        return new MugenCharacterSummary
        {
            Id = Id,
            Name = Name,
            DisplayName = DisplayName,
            Author = Author,
            Version = Version,
            IsValid = IsValid,
            LastScannedAt = LastScannedAt,
            FileSize = FileSize
        };
    }
}
