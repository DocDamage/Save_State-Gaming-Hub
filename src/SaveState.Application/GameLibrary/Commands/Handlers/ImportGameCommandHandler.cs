using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Application.Common.Events;
using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.ValueObjects;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

/// <summary>
/// Handler for importing games into the library.
/// Validates game data and creates game entities with proper metadata.
/// </summary>
public class ImportGameCommandHandler : IRequestHandler<ImportGameCommand, Result<GameId>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly IGameValidationService _validationService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ImportGameCommandHandler> _logger;

    public ImportGameCommandHandler(
        IGameRepository gameRepository,
        IPlatformRepository platformRepository,
        IGameValidationService validationService,
        IEventPublisher eventPublisher,
        ILogger<ImportGameCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _platformRepository = platformRepository;
        _validationService = validationService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command to import a game into the library.
    /// </summary>
    /// <param name="request">The import game command with game details.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the game ID or an error.</returns>
    public async Task<Result<GameId>> Handle(ImportGameCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Importing game {Title} from {Source}",
                request.Title, request.Source ?? "manual");

            // Get or create platform
            var platformResult = await GetOrCreatePlatformAsync(request.PlatformName, ct).ConfigureAwait(false);
            if (platformResult.IsFailure)
                return Result<GameId>.Failure(platformResult.Error!);

            // Check for existing game
            var existingGame = await _gameRepository.GetByTitleAndPlatformAsync(
                SaveState.Core.Common.ValueObjects.GameTitle.From(request.Title), platformResult.Value.Id, ct).ConfigureAwait(false);

            if (existingGame is not null)
            {
                _logger.LogWarning("Game {Title} already exists for platform {Platform}",
                    request.Title, request.PlatformName);
                return Result<GameId>.Failure($"Game '{request.Title}' already exists");
            }

            // Check for existing game by source and sourceId
            if (!string.IsNullOrEmpty(request.Source) && !string.IsNullOrEmpty(request.SourceId))
            {
                var gameBySource = await _gameRepository.GetBySourceAndSourceIdAsync(
                    request.Source, request.SourceId, ct).ConfigureAwait(false);

                if (gameBySource is not null)
                {
                    _logger.LogWarning("Game from source {Source} with ID {SourceId} already exists",
                        request.Source, request.SourceId);
                    return Result<GameId>.Failure($"Game from source '{request.Source}' with ID '{request.SourceId}' already exists");
                }
            }

            // Create new game
            var game = Game.Create(
                request.Title,
                platformResult.Value.Id,
                request.Description,
                null, // Cover image path determined after download
                request.Source,
                request.SourceId);

            if (!string.IsNullOrEmpty(request.InstallPath))
                game.SetInstallPath(request.InstallPath);

            // Note: Source and tags metadata not yet implemented in Game entity

            // Validate game before saving
            if (!await _validationService.IsValidGameAsync(game, ct).ConfigureAwait(false))
            {
                var errors = await _validationService.GetValidationErrorsAsync(game, ct).ConfigureAwait(false);
                return Result<GameId>.Failure($"Validation failed: {string.Join(", ", errors)}");
            }

            await _gameRepository.AddAsync(game, ct).ConfigureAwait(false);

            _logger.LogInformation("Successfully imported game {GameId}: {Title}",
                game.Id, request.Title);

            return Result<GameId>.Success(GameId.From(game.Id));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed for game import: {Title}", request.Title);
            return Result<GameId>.Failure($"Validation failed: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Domain logic failed for game import: {Title}", request.Title);
            return Result<GameId>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error importing game {Title}", request.Title);
            return Result<GameId>.Failure($"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task<Result<Platform>> GetOrCreatePlatformAsync(string platformName, CancellationToken ct)
    {
        var platform = await _platformRepository.GetByNameAsync(platformName, ct).ConfigureAwait(false);
        if (platform is not null)
            return Result<Platform>.Success(platform);

        // Create new platform - infer type from name
        var platformType = InferPlatformType(platformName);
        platform = new Platform(
            PlatformName.From(platformName),
            PlatformShortName.From(platformName),
            platformType);

        try
        {
            await _platformRepository.AddAsync(platform, ct).ConfigureAwait(false);
            return Result<Platform>.Success(platform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create platform {PlatformName}", platformName);
            return Result<Platform>.Failure($"Failed to create platform '{platformName}': {ex.Message}", ErrorType.Internal);
        }
    }

    private static PlatformType InferPlatformType(string platformName)
    {
        var lowerName = platformName.ToLowerInvariant();

        if (lowerName.Contains("pc") || lowerName.Contains("windows") || lowerName.Contains("linux"))
            return PlatformType.Computer;

        if (lowerName.Contains("playstation") || lowerName.Contains("xbox") ||
            lowerName.Contains("nintendo") || lowerName.Contains("sega"))
            return PlatformType.Console;

        if (lowerName.Contains("game boy") || lowerName.Contains("gba") ||
            lowerName.Contains("nds") || lowerName.Contains("3ds"))
            return PlatformType.Handheld;

        return PlatformType.Console; // Default
    }
}
