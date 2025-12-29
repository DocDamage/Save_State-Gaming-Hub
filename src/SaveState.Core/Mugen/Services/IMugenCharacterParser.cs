namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service interface for parsing MUGEN character definition files (.def).
/// </summary>
public interface IMugenCharacterParser
{
    /// <summary>
    /// Parses a MUGEN character definition file and extracts metadata.
    /// </summary>
    /// <param name="definitionFilePath">Path to the .def file.</param>
    /// <param name="characterDirectory">Directory containing the character files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed character metadata.</returns>
    Task<CharacterMetadata> ParseCharacterAsync(string definitionFilePath, string characterDirectory, CancellationToken ct = default);

    /// <summary>
    /// Validates whether a file is a valid MUGEN character definition file.
    /// </summary>
    /// <param name="filePath">Path to the potential .def file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the file is a valid character definition.</returns>
    Task<bool> IsValidCharacterDefinitionAsync(string filePath, CancellationToken ct = default);
}
