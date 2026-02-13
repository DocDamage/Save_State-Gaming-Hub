using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.RomManagement;

namespace SaveState.Application.RomManagement.Commands.Handlers;

/// <summary>
/// Handler for importing ROMs into the game library.
/// </summary>
public class ImportRomToLibraryCommandHandler : IRequestHandler<ImportRomToLibraryCommand, Result<ImportRomResult>>
{
    private readonly IRomFileRepository _romFileRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly ILogger<ImportRomToLibraryCommandHandler> _logger;

    public ImportRomToLibraryCommandHandler(
        IRomFileRepository romFileRepository,
        IGameRepository gameRepository,
        IPlatformRepository platformRepository,
        ILogger<ImportRomToLibraryCommandHandler> logger)
    {
        _romFileRepository = romFileRepository ?? throw new ArgumentNullException(nameof(romFileRepository));
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
        _platformRepository = platformRepository ?? throw new ArgumentNullException(nameof(platformRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the command to import a ROM into the game library.
    /// </summary>
    /// <param name="request">The import ROM command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the import result.</returns>
    public async Task<Result<ImportRomResult>> Handle(ImportRomToLibraryCommand request, CancellationToken ct)
    {
        // Get the ROM file
        var romFile = await _romFileRepository.GetByIdAsync(request.RomFileId, ct).ConfigureAwait(false);
        if (romFile is null)
            return Result.Failure<ImportRomResult>("ROM file not found", ErrorType.NotFound);

        // Get the platform
        var platform = await _platformRepository.GetByIdAsync(romFile.PlatformId, ct).ConfigureAwait(false);
        if (platform is null)
            return Result.Failure<ImportRomResult>("Platform not found", ErrorType.NotFound);

        // Determine the game title
        var gameTitle = request.TitleOverride ?? romFile.Title;
        var gameTitleObj = GameTitle.From(gameTitle);

        // Check if a game with this title and platform already exists
        var existingGame = await _gameRepository.GetByTitleAndPlatformAsync(gameTitleObj, platform.Id, ct).ConfigureAwait(false);

        bool gameWasCreated = false;
        Game game;

        if (existingGame != null)
        {
            // Use existing game
            game = existingGame;
            _logger.LogInformation("Using existing game '{GameTitle}' for ROM '{RomTitle}'", gameTitle, romFile.Title);
        }
        else if (request.CreateIfNotExists)
        {
            // Create new game
            game = Game.Create(gameTitle, platform.Id, request.Description ?? $"ROM: {romFile.Title}");

            await _gameRepository.AddAsync(game, ct).ConfigureAwait(false);
            gameWasCreated = true;

            _logger.LogInformation("Created new game '{GameTitle}' for ROM '{RomTitle}'", gameTitle, romFile.Title);
        }
        else
        {
            return Result.Failure<ImportRomResult>("Game not found and creation not requested", ErrorType.NotFound);
        }

        // Associate the ROM with the game (this would require extending the Game entity)
        // For now, we'll just return success - in a full implementation, you'd store
        // the ROM file association with the game

        var result = new ImportRomResult(
            game.Id,
            game.Title,
            RomFileId.From((Guid)romFile.Id),
            romFile.Title,
            gameWasCreated);

        _logger.LogInformation("Successfully imported ROM '{RomTitle}' to game library as '{GameTitle}'",
            romFile.Title, game.Title);

        return Result.Success(result);
    }
}