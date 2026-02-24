namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a MUGEN character in the game library.
/// Contains metadata extracted from character definition files and file information.
/// </summary>
public class MugenCharacter : EntityBase, ISoftDelete
{
    private static DateTime UtcNow => SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// The display name of the character.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The display name (localized).
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// The version of the character.
    /// </summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>
    /// The author/creator of the character.
    /// </summary>
    public string Author { get; private set; } = string.Empty;

    /// <summary>
    /// The full path to the character definition file (.def).
    /// </summary>
    public string DefinitionFilePath { get; private set; } = string.Empty;

    /// <summary>
    /// The directory containing the character's files.
    /// </summary>
    public string CharacterDirectory { get; private set; } = string.Empty;

    /// <summary>
    /// The command file path (if different from default).
    /// </summary>
    public string? CommandFile { get; private set; }

    /// <summary>
    /// The constants/states file path.
    /// </summary>
    public string? ConstantsFile { get; private set; }

    /// <summary>
    /// The states file path.
    /// </summary>
    public string? StatesFile { get; private set; }

    /// <summary>
    /// The common states file path.
    /// </summary>
    public string? CommonStatesFile { get; private set; }

    /// <summary>
    /// The sprite/stage/music directories.
    /// </summary>
    public CharacterDirectories Directories { get; private set; } = new();

    /// <summary>
    /// The character's palette information.
    /// </summary>
    public PaletteInfo PaletteInfo { get; private set; } = new();

    /// <summary>
    /// The character's arcade mode information.
    /// </summary>
    public ArcadeInfo ArcadeInfo { get; private set; } = new();

    // Basic combat attributes used by AI and analytics engines. These default to typical values
    // and may be populated during character parsing or extended metadata processing in the future.
    public int Health { get; private set; } = 1000;
    public int Attack { get; private set; } = 100;
    public int Defense { get; private set; } = 100;
    public int Speed { get; private set; } = 10;

    // Broad character archetypes detected from character scripts (e.g., projectile, rushdown, zoning)
    public bool IsProjectileCharacter { get; private set; }
    public bool IsRushdownCharacter { get; private set; }
    public bool IsZoningCharacter { get; private set; }

    // Misc mechanics presence flags
    public bool HasSuperArts { get; private set; }
    public bool HasThrows { get; private set; }
    public bool HasCommandGrab { get; private set; }

    /// <summary>
    /// The character's filesize in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// When the character was last scanned.
    /// </summary>
    public DateTime LastScannedAt { get; private set; }

    /// <summary>
    /// Whether the character is currently available/valid.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Any validation errors found during scanning.
    /// </summary>
    public string? ValidationErrors { get; private set; }

    /// <summary>
    /// Whether this character is marked as a favorite.
    /// </summary>
    public bool IsFavorite { get; private set; }

    // ISoftDelete implementation
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Creates a new MugenCharacter entity.
    /// </summary>
    /// <param name="name">The character name.</param>
    /// <param name="definitionFilePath">Path to the .def file.</param>
    /// <param name="characterDirectory">Directory containing character files.</param>
    /// <returns>A new MugenCharacter instance.</returns>
    public static MugenCharacter Create(string name, string definitionFilePath, string characterDirectory)
    {
        var character = new MugenCharacter
        {
            Name = name,
            DisplayName = name, // Default to same as name
            DefinitionFilePath = definitionFilePath,
            CharacterDirectory = characterDirectory,
            LastScannedAt = UtcNow,
            IsValid = true,
            PaletteInfo = new PaletteInfo(),
            ArcadeInfo = new ArcadeInfo(),
            Directories = new CharacterDirectories(),

            // Default combat attributes (can be updated later from parsed metadata)
            Health = 1000,
            Attack = 100,
            Defense = 100,
            Speed = 10,

            // Defaults for archetype/mechanics flags
            IsProjectileCharacter = false,
            IsRushdownCharacter = false,
            IsZoningCharacter = false,
            HasSuperArts = false,
            HasThrows = false,
            HasCommandGrab = false
        };

        return character;
    }

    /// <summary>
    /// Updates the character's metadata from parsed definition file data.
    /// </summary>
    /// <param name="metadata">The parsed character metadata.</param>
    public void UpdateMetadata(CharacterMetadata metadata)
    {
        DisplayName = metadata.DisplayName ?? DisplayName;
        Version = metadata.Version ?? Version;
        Author = metadata.Author ?? Author;
        CommandFile = metadata.CommandFile;
        ConstantsFile = metadata.ConstantsFile;
        StatesFile = metadata.StatesFile;
        CommonStatesFile = metadata.CommonStatesFile;
        Directories = metadata.Directories ?? Directories;
        PaletteInfo = metadata.PaletteInfo ?? PaletteInfo;
        ArcadeInfo = metadata.ArcadeInfo ?? ArcadeInfo;
        FileSize = metadata.FileSize;
        LastScannedAt = UtcNow;
    }

    /// <summary>
    /// Marks the character as invalid with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public void MarkInvalid(string errorMessage)
    {
        IsValid = false;
        ValidationErrors = errorMessage;
        LastScannedAt = UtcNow;
    }

    /// <summary>
    /// Updates the last scanned timestamp.
    /// </summary>
    public void UpdateLastScanned()
    {
        LastScannedAt = UtcNow;
    }

    /// <summary>
    /// Sets the favorite status of this character.
    /// </summary>
    /// <param name="isFavorite">Whether the character should be marked as favorite.</param>
    public void SetFavorite(bool isFavorite)
    {
        IsFavorite = isFavorite;
    }

    // EF Core constructor
    private MugenCharacter() { }
}
