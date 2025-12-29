namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Mugen.Entities;
using SaveState.Core.Common;

/// <summary>
/// Service interface for loading and managing MUGEN/IKEMEN characters from the filesystem.
/// Handles scanning directories, parsing character files, and managing character collections.
/// Includes IKEMEN integration for bundled character packs.
/// </summary>
public interface IMugenCharacterLoader
{
    /// <summary>
    /// Scans a directory for MUGEN characters and loads them into the system.
    /// </summary>
    /// <param name="directoryPath">The directory path to scan.</param>
    /// <param name="recursive">Whether to scan subdirectories recursively.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of loaded MUGEN characters.</returns>
    Task<IReadOnlyList<MugenCharacter>> ScanAndLoadCharactersAsync(string directoryPath, bool recursive = true, CancellationToken ct = default);

    /// <summary>
    /// Scans all IKEMEN character directories (Street Fighter, MVC2, builtin).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all IKEMEN characters.</returns>
    Task<IReadOnlyList<MugenCharacter>> ScanIkemenCharactersAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads a single MUGEN character from its definition file.
    /// </summary>
    /// <param name="definitionFilePath">Path to the character's .def file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the loaded character or an error if loading failed.</returns>
    Task<Result<MugenCharacter>> LoadCharacterFromDefAsync(string definitionFilePath, CancellationToken ct = default);

    /// <summary>
    /// Validates whether a directory contains a valid MUGEN character.
    /// </summary>
    /// <param name="directoryPath">The directory path to validate.</param>
    /// <returns>True if the directory contains a valid MUGEN character.</returns>
    bool IsValidMugenCharacterDirectory(string directoryPath);

    /// <summary>
    /// Gets the list of common MUGEN character file extensions.
    /// </summary>
    IReadOnlyList<string> CharacterFileExtensions { get; }

    /// <summary>
    /// Gets the configured IKEMEN character directories.
    /// </summary>
    IReadOnlyList<string> IkemenCharacterDirectories { get; }
}
