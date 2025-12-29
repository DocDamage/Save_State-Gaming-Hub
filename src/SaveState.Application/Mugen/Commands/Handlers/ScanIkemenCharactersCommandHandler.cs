namespace SaveState.Application.Mugen.Commands.Handlers;

using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;

/// <summary>
/// Handles the ScanIkemenCharactersCommand by scanning bundled character directories.
/// </summary>
public class ScanIkemenCharactersCommandHandler : IRequestHandler<ScanIkemenCharactersCommand, Unit>
{
    private readonly IMugenCharacterRepository _characterRepository;
    private readonly IMugenCharacterLoader _characterLoader;
    private readonly ILogger<ScanIkemenCharactersCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the ScanIkemenCharactersCommandHandler.
    /// </summary>
    /// <param name="characterRepository">The character repository.</param>
    /// <param name="characterLoader">The character loader.</param>
    /// <param name="logger">The logger instance.</param>
    public ScanIkemenCharactersCommandHandler(
        IMugenCharacterRepository characterRepository,
        IMugenCharacterLoader characterLoader,
        ILogger<ScanIkemenCharactersCommandHandler> logger)
    {
        _characterRepository = characterRepository;
        _characterLoader = characterLoader;
        _logger = logger;
    }

    /// <summary>
    /// Handles the scan IKEMEN characters command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit result.</returns>
    public async Task<Unit> Handle(ScanIkemenCharactersCommand request, CancellationToken cancellationToken)
    {
        // Scan all IKEMEN character directories
        var characters = await _characterLoader.ScanIkemenCharactersAsync(cancellationToken);

        foreach (var character in characters)
        {
            try
            {
                // Check if character already exists
                var existingCharacter = await _characterRepository.GetByNameAsync(character.Name, cancellationToken);
                if (existingCharacter != null)
                {
                    // Update existing character with new metadata
                    // For now, just mark as rescanned - full metadata update would need more complex logic
                    existingCharacter.UpdateLastScanned();
                    await _characterRepository.UpdateAsync(existingCharacter, cancellationToken);
                }
                else
                {
                    // Add new character
                    await _characterRepository.AddAsync(character, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue processing other characters
                _logger.LogError(ex, "Error processing IKEMEN character {CharacterName}", character.Name);
            }
        }

        return Unit.Value;
    }
}
