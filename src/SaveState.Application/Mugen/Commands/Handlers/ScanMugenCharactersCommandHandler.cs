namespace SaveState.Application.Mugen.Commands.Handlers;

using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;

/// <summary>
/// Handles the ScanMugenCharactersCommand by scanning directories for MUGEN characters.
/// </summary>
public class ScanMugenCharactersCommandHandler : IRequestHandler<ScanMugenCharactersCommand, Unit>
{
    private readonly IMugenCharacterRepository _characterRepository;
    private readonly IMugenCharacterParser _characterParser;
    private readonly ILogger<ScanMugenCharactersCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the ScanMugenCharactersCommandHandler.
    /// </summary>
    /// <param name="characterRepository">The character repository.</param>
    /// <param name="characterParser">The character definition file parser.</param>
    /// <param name="logger">The logger instance.</param>
    public ScanMugenCharactersCommandHandler(
        IMugenCharacterRepository characterRepository,
        IMugenCharacterParser characterParser,
        ILogger<ScanMugenCharactersCommandHandler> logger)
    {
        _characterRepository = characterRepository;
        _characterParser = characterParser;
        _logger = logger;
    }

    /// <summary>
    /// Handles the scan command by finding and parsing MUGEN character files.
    /// </summary>
    /// <param name="request">The scan command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit result.</returns>
    public async Task<Unit> Handle(ScanMugenCharactersCommand request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(request.DirectoryPath))
        {
            throw new DirectoryNotFoundException($"MUGEN directory not found: {request.DirectoryPath}");
        }

        // Find all .def files
        var defFiles = FindCharacterDefinitionFiles(request.DirectoryPath, request.IncludeSubdirectories);

        foreach (var defFile in defFiles)
        {
            await ProcessCharacterFileAsync(defFile, request.OverwriteExisting, cancellationToken);
        }

        return Unit.Value;
    }

    private async Task ProcessCharacterFileAsync(string defFilePath, bool overwriteExisting, CancellationToken ct)
    {
        try
        {
            // Check if it's a valid character definition file
            if (!await _characterParser.IsValidCharacterDefinitionAsync(defFilePath, ct))
            {
                return; // Skip invalid files
            }

            // Extract character directory and name
            var characterDirectory = Path.GetDirectoryName(defFilePath)!;
            var characterName = ExtractCharacterName(defFilePath, characterDirectory);

            // Check if character already exists
            var existingResult = await _characterRepository.GetByNameAsync(characterName, ct);
            if (existingResult.IsSuccess && !overwriteExisting)
            {
                return; // Skip existing characters unless overwrite is enabled
            }

            // Parse character metadata
            var metadata = await _characterParser.ParseCharacterAsync(defFilePath, characterDirectory, ct);

            // Create or update character
            if (existingResult.IsSuccess && existingResult.Value is not null)
            {
                var existingCharacter = existingResult.Value;
                existingCharacter.UpdateMetadata(metadata);
                await _characterRepository.UpdateAsync(existingCharacter, ct);
            }
            else
            {
                var newCharacter = MugenCharacter.Create(characterName, defFilePath, characterDirectory);
                newCharacter.UpdateMetadata(metadata);
                await _characterRepository.AddAsync(newCharacter, ct);
            }
        }
        catch (Exception ex)
        {
            // Log error but continue processing other characters
            _logger.LogError(ex, "Error processing MUGEN character file {DefFilePath}", defFilePath);
        }
    }

    private static IEnumerable<string> FindCharacterDefinitionFiles(string directoryPath, bool includeSubdirectories)
    {
        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(directoryPath, "*.def", searchOption);
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
