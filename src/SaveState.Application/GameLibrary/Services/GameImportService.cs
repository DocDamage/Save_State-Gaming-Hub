using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Events;
using SaveState.Core.Common.ValueObjects;
using SaveState.Application.Common.Events;
using SaveState.Application.GameLibrary.DTOs;

namespace SaveState.Application.GameLibrary.Services;

public class GameImportService : IGameImportService
{
    private readonly IEnumerable<IGameProvider> _providers;
    private readonly IMetadataService _metadataService;
    private readonly IGameRepository _gameRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<GameImportService> _logger;

    public GameImportService(
        IEnumerable<IGameProvider> providers,
        IMetadataService metadataService,
        IGameRepository gameRepository,
        IEventPublisher eventPublisher,
        ILogger<GameImportService> logger)
    {
        _providers = providers;
        _metadataService = metadataService;
        _gameRepository = gameRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<ImportResult> ImportAllLibrariesAsync(
        ImportOptions options,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default)
    {
        var result = new ImportResult
        {
            StartedAt = DateTimeOffset.Now
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Step 1: Discover games from all providers
            var (games, providerResults) = await DiscoverGamesFromProvidersAsync(options, progress, ct).ConfigureAwait(false);
            result.ProviderResults = providerResults;

            // Step 2: Deduplicate games
            var uniqueGames = DeduplicateGames(games);
            _logger.LogInformation("Found {UniqueCount} unique games from {TotalCount} discovered games",
                uniqueGames.Count, games.Count);

            // Step 3: Import games with metadata enrichment
            await ImportGamesAsync(uniqueGames, options, progress, result, ct).ConfigureAwait(false);

            result.CompletedAt = DateTimeOffset.Now;
            result.Duration = stopwatch.Elapsed;

            progress?.Report(new ImportProgress
            {
                Stage = ImportStage.Complete,
                Message = $"Import completed: {result.GamesImported} imported, {result.GamesFailed} failed, {result.GamesSkipped} skipped"
            });

            _logger.LogInformation("Game import completed in {Duration}: {Imported} imported, {Failed} failed, {Skipped} skipped",
                result.Duration, result.GamesImported, result.GamesFailed, result.GamesSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game import failed after {Elapsed}", stopwatch.Elapsed);
            result.CompletedAt = DateTimeOffset.Now;
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    private async Task<(List<GameInfo> Games, Dictionary<string, ProviderResult> ProviderResults)> DiscoverGamesFromProvidersAsync(
        ImportOptions options,
        IProgress<ImportProgress>? progress,
        CancellationToken ct)
    {
        var allGames = new List<GameInfo>();
        var providerResults = new Dictionary<string, ProviderResult>();

        var providersToUse = options.ProviderFilter is null || options.ProviderFilter.Count == 0
            ? _providers
            : _providers.Where(p => options.ProviderFilter.Contains(p.Name));

        foreach (var provider in providersToUse)
        {
            if (ct.IsCancellationRequested) break;

            progress?.Report(new ImportProgress
            {
                Stage = ImportStage.Discovery,
                Provider = provider.Name,
                Message = $"Discovering games from {provider.Name}..."
            });

            try
            {
                var games = await provider.GetInstalledGamesAsync(ct).ConfigureAwait(false);
                allGames.AddRange(games);

                providerResults[provider.Name] = new ProviderResult
                {
                    Success = true,
                    GamesFound = games.Count
                };

                _logger.LogInformation("Discovered {Count} games from {Provider}", games.Count, provider.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover games from {Provider}", provider.Name);
                providerResults[provider.Name] = new ProviderResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        return (allGames, providerResults);
    }

    private static IReadOnlyList<GameInfo> DeduplicateGames(List<GameInfo> games)
    {
        return games
            .GroupBy(g => new { g.Source, g.SourceId })
            .Select(g => g.First())
            .ToList();
    }

    private async Task ImportGamesAsync(
        IReadOnlyList<GameInfo> games,
        ImportOptions options,
        IProgress<ImportProgress>? progress,
        ImportResult result,
        CancellationToken ct)
    {
        for (var i = 0; i < games.Count; i++)
        {
            if (ct.IsCancellationRequested) break;

            var gameInfo = games[i];

            progress?.Report(new ImportProgress
            {
                Stage = ImportStage.Import,
                Current = i + 1,
                Total = games.Count,
                Message = $"Importing {gameInfo.Title}..."
            });

            try
            {
                await ImportSingleGameAsync(gameInfo, options, ct).ConfigureAwait(false);
                result.GamesImported++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to import game {Title} from {Source}", gameInfo.Title, gameInfo.Source);
                result.GamesFailed++;
            }
        }
    }

    private async Task ImportSingleGameAsync(GameInfo gameInfo, ImportOptions options, CancellationToken ct)
    {
        // Check for existing game with same source and source ID
        var existingGame = await _gameRepository.GetBySourceAndSourceIdAsync(gameInfo.Source, gameInfo.SourceId, ct).ConfigureAwait(false);

        Game game;
        if (existingGame != null)
        {
            // Update existing game
            _logger.LogDebug("Updating existing game {Title} from {Source}", gameInfo.Title, gameInfo.Source);
            game = existingGame;
        }
        else
        {
            // Create new game
            _logger.LogDebug("Creating new game {Title} from {Source}", gameInfo.Title, gameInfo.Source);
            game = Game.Create(gameInfo.Title, null, null, null, gameInfo.Source, gameInfo.SourceId);
        }

        if (!string.IsNullOrEmpty(gameInfo.InstallPath))
        {
            game.SetInstallPath(gameInfo.InstallPath);
        }

        // Enrich with metadata if requested
        if (!options.SkipMetadata)
        {
            try
            {
                var metadata = await _metadataService.GetGameMetadataAsync(gameInfo.Title, ct).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(metadata.Title) && metadata.Title != gameInfo.Title)
                {
                    // Update title if metadata provides a better one
                    game.Update(metadata.Title);
                }

                // Add metadata
                if (!string.IsNullOrEmpty(metadata.Description))
                {
                    game.Update(description: metadata.Description);
                }

                // Note: The Game entity doesn't currently support all metadata fields
                // This would need to be extended in future versions
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enrich game {Title} with metadata", gameInfo.Title);
                // Continue without metadata
            }
        }

        // Save the game (add new or update existing)
        if (existingGame != null)
        {
            await _gameRepository.UpdateAsync(game, ct).ConfigureAwait(false);
        }
        else
        {
            await _gameRepository.AddAsync(game, ct).ConfigureAwait(false);
        }
        await _eventPublisher.PublishAsync(new GameImportedEvent(game.Id, gameInfo.Source, gameInfo.SourceId), ct).ConfigureAwait(false);

        _logger.LogInformation("Imported game {Title} from {Source}", game.Title, gameInfo.Source);
    }
}
