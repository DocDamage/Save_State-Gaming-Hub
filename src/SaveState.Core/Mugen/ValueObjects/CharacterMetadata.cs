namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Value object containing parsed metadata from a MUGEN character definition file.
/// </summary>
public record CharacterMetadata(
    string? DisplayName = null,
    string? Version = null,
    string? Author = null,
    string? CommandFile = null,
    string? ConstantsFile = null,
    string? StatesFile = null,
    string? CommonStatesFile = null,
    CharacterDirectories? Directories = null,
    PaletteInfo? PaletteInfo = null,
    ArcadeInfo? ArcadeInfo = null,
    long FileSize = 0
);
