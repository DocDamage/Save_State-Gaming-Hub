namespace SaveState.Infrastructure.Mugen;

using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;

/// <summary>
/// Implementation of the MUGEN/IKEMEN character loader.
/// Handles scanning directories and loading character files.
/// Includes IKEMEN integration for bundled character packs.
/// </summary>
public class MugenCharacterLoader : IMugenCharacterLoader
{
    private readonly IMugenCharacterParser _characterParser;
    private readonly ILogger<MugenCharacterLoader> _logger;
    private readonly string[] _validExtensions = { ".def", ".DEF" };
    private readonly string[] _characterFileExtensions = { ".cmd", ".cns", ".st", ".stcommon", ".air", ".sff", ".snd", ".act" };

    private readonly MugenOptions _options;

    /// <summary>
    /// Gets the list of common MUGEN character file extensions.
    /// </summary>
    public IReadOnlyList<string> CharacterFileExtensions => _characterFileExtensions;

    /// <summary>
    /// Gets the configured IKEMEN character directories.
    /// </summary>
    public IReadOnlyList<string> IkemenCharacterDirectories => _options.CharacterDirectories;

    /// <summary>
    /// Initializes a new instance of the MugenCharacterLoader.
    /// </summary>
    /// <param name="characterParser">The character definition file parser.</param>
    /// <param name="logger">The logger instance.</param>
    public MugenCharacterLoader(
        IMugenCharacterParser characterParser,
        IOptions<MugenOptions> options,
        ILogger<MugenCharacterLoader> logger)
    {
        _characterParser = characterParser;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Scans a directory for MUGEN characters and loads them into the system.
    /// </summary>
    /// <param name="directoryPath">The directory path to scan.</param>
    /// <param name="recursive">Whether to scan subdirectories recursively.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of loaded MUGEN characters.</returns>
    public async Task<IReadOnlyList<MugenCharacter>> ScanAndLoadCharactersAsync(string directoryPath, bool recursive = true, CancellationToken ct = default)
    {
        if (!Directory.Exists(directoryPath))
        {
            return Array.Empty<MugenCharacter>();
        }

        var characters = new List<MugenCharacter>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        // Find all .def files
        var defFiles = Directory.GetFiles(directoryPath, "*.def", searchOption);

        foreach (var defFile in defFiles)
        {
            ct.ThrowIfCancellationRequested();

            var result = await LoadCharacterFromDefAsync(defFile, ct);
            if (result.IsSuccess && result.Value is not null)
            {
                characters.Add(result.Value);
            }
            else
            {
                // Skip invalid character files but continue processing others
                _logger.LogWarning("Failed to load MUGEN character from {DefFile}: {Error}", defFile, result.Error);
                continue;
            }
        }

        return characters;
    }

    /// <summary>
    /// Scans all IKEMEN character directories (Street Fighter, MVC2, builtin).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of all IKEMEN characters.</returns>
    public async Task<IReadOnlyList<MugenCharacter>> ScanIkemenCharactersAsync(CancellationToken ct = default)
    {
        var allCharacters = new List<MugenCharacter>();

        foreach (var directory in _options.CharacterDirectories)
        {
            if (Directory.Exists(directory))
            {
                var characters = await ScanAndLoadCharactersAsync(directory, recursive: true, ct);
                allCharacters.AddRange(characters);
            }
        }

        return allCharacters;
    }

    /// <summary>
    /// Loads a single MUGEN character from its definition file.
    /// </summary>
    /// <param name="definitionFilePath">Path to the character's .def file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the loaded character or an error if loading failed.</returns>
    public async Task<Result<MugenCharacter>> LoadCharacterFromDefAsync(string definitionFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(definitionFilePath))
        {
            return Result.Failure<MugenCharacter>($"Character definition file not found: {definitionFilePath}", ErrorType.NotFound);
        }

        try
        {
            var characterDirectory = Path.GetDirectoryName(definitionFilePath)!;
            var characterName = ExtractCharacterName(definitionFilePath, characterDirectory);

            // Parse the character metadata
            var metadata = await _characterParser.ParseCharacterAsync(definitionFilePath, characterDirectory, ct);

            // Create the character entity
            var character = MugenCharacter.Create(characterName, definitionFilePath, characterDirectory);
            character.UpdateMetadata(metadata);

            return Result.Success<MugenCharacter>(character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load MUGEN character from {DefinitionFilePath}", definitionFilePath);
            return Result.Failure<MugenCharacter>($"Failed to load character: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Validates whether a directory contains a valid MUGEN character.
    /// </summary>
    /// <param name="directoryPath">The directory path to validate.</param>
    /// <returns>True if the directory contains a valid MUGEN character.</returns>
    public bool IsValidMugenCharacterDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return false;
        }

        // Check for .def file
        var defFiles = Directory.GetFiles(directoryPath, "*.def");
        if (defFiles.Length == 0)
        {
            return false;
        }

        // Check for at least one character file (sff is most common)
        var hasCharacterFile = _characterFileExtensions.Any(ext =>
            Directory.GetFiles(directoryPath, $"*{ext}").Length > 0);

        return hasCharacterFile;
    }

    private static string ExtractCharacterName(string defFilePath, string characterDirectory)
    {
        // Try to get name from directory first
        var directoryName = Path.GetFileName(characterDirectory);
        if (!string.IsNullOrEmpty(directoryName))
        {
            return directoryName;
        }

        // Fallback to filename without extension
        return Path.GetFileNameWithoutExtension(defFilePath);
    }
}

