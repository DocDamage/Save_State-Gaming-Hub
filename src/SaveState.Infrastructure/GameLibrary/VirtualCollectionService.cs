using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary;

/// <summary>
/// Service for managing virtual game collections.
/// Provides dynamic collections based on rules, filters, and user preferences.
/// </summary>
public class VirtualCollectionService : IVirtualCollectionService
{
    private readonly IVirtualCollectionRepository _collectionRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<VirtualCollectionService> _logger;

    public VirtualCollectionService(
        IVirtualCollectionRepository collectionRepository,
        IGameRepository gameRepository,
        ILogger<VirtualCollectionService> logger)
    {
        _collectionRepository = collectionRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new manual virtual collection with the specified name.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <param name="icon">Optional icon for the collection.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the created collection or an error.</returns>
    public async Task<Result<VirtualCollection>> CreateManualCollectionAsync(string name, string? icon = null, CancellationToken ct = default)
    {
        try
        {
            var collection = VirtualCollection.CreateManual(name, icon);
            await _collectionRepository.AddAsync(collection, ct);

            _logger.LogInformation("Created manual collection '{Name}' with ID {Id}", name, collection.Id);
            return Result.Success<VirtualCollection>(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create manual collection '{Name}'", name);
            return Result.Failure<VirtualCollection>($"Failed to create collection: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Creates a new smart collection with automatic filtering rules.
    /// </summary>
    /// <param name="name">The name of the collection.</param>
    /// <param name="filter">The filter rules for the smart collection.</param>
    /// <param name="icon">Optional icon for the collection.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the created collection or an error.</returns>
    public async Task<Result<VirtualCollection>> CreateSmartCollectionAsync(string name, CollectionFilter filter, string? icon = null, CancellationToken ct = default)
    {
        try
        {
            var collection = VirtualCollection.CreateSmart(name, filter, icon);
            await _collectionRepository.AddAsync(collection, ct);

            _logger.LogInformation("Created smart collection '{Name}' with filter", name);
            return Result.Success<VirtualCollection>(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create smart collection '{Name}'", name);
            return Result.Failure<VirtualCollection>($"Failed to create smart collection: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Deletes a virtual collection.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection to delete.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> DeleteCollectionAsync(Guid collectionId, CancellationToken ct = default)
    {
        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId, ct);
            if (collection == null)
                return Result.Failure("Collection not found", ErrorType.NotFound);

            if (collection.IsSystemCollection)
                return Result.Failure("Cannot delete system collections", ErrorType.Validation);

            await _collectionRepository.DeleteAsync(collectionId, ct);

            _logger.LogInformation("Deleted collection '{Name}' with ID {Id}", collection.Name, collectionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection {CollectionId}", collectionId);
            return Result.Failure($"Failed to delete collection: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Adds a game to a virtual collection.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection.</param>
    /// <param name="gameId">The unique identifier of the game to add.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> AddGameToCollectionAsync(Guid collectionId, Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId, ct);
            if (collection == null)
                return Result.Failure("Collection not found", ErrorType.NotFound);

            if (collection.Type != CollectionType.Manual)
                return Result.Failure("Can only manually add games to manual collections", ErrorType.Validation);

            await _collectionRepository.AddGameToCollectionAsync(collectionId, gameId, ct: ct);

            _logger.LogInformation("Added game {GameId} to collection {CollectionId}", gameId, collectionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add game {GameId} to collection {CollectionId}", gameId, collectionId);
            return Result.Failure($"Failed to add game to collection: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Removes a game from a virtual collection.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection.</param>
    /// <param name="gameId">The unique identifier of the game to remove.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> RemoveGameFromCollectionAsync(Guid collectionId, Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId, ct);
            if (collection == null)
                return Result.Failure("Collection not found", ErrorType.NotFound);

            if (collection.Type != CollectionType.Manual)
                return Result.Failure("Can only manually remove games from manual collections", ErrorType.Validation);

            await _collectionRepository.RemoveGameFromCollectionAsync(collectionId, gameId, ct);

            _logger.LogInformation("Removed game {GameId} from collection {CollectionId}", gameId, collectionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove game {GameId} from collection {CollectionId}", gameId, collectionId);
            return Result.Failure($"Failed to remove game from collection: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Retrieves all games in a virtual collection.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of games in the collection.</returns>
    public async Task<Result<IReadOnlyList<Game>>> GetGamesInCollectionAsync(Guid collectionId, CancellationToken ct = default)
    {
        try
        {
            var collection = await _collectionRepository.GetByIdAsync(collectionId, ct);
            if (collection == null)
                return Result.Failure<IReadOnlyList<Game>>("Collection not found", ErrorType.NotFound);

            IReadOnlyList<Game> games;

            if (collection.Type == CollectionType.Smart && collection.GetFilter() is { } filter)
            {
                var filterResult = await ExecuteSmartFilterAsync(filter, ct);
                if (!filterResult.IsSuccess)
                {
                    return Result.Failure<IReadOnlyList<Game>>(filterResult.Error!, filterResult.ErrorType);
                }
                games = filterResult.Value!;
            }
            else
            {
                games = await _collectionRepository.GetGamesInCollectionAsync(collectionId, ct);
            }

            return Result.Success<IReadOnlyList<Game>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get games in collection {CollectionId}", collectionId);
            return Result.Failure<IReadOnlyList<Game>>($"Failed to get games: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Retrieves all virtual collections.
    /// </summary>
    /// <param name="includeSystem">Whether to include system collections in the results.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of all collections.</returns>
    public async Task<Result<IReadOnlyList<VirtualCollection>>> GetAllCollectionsAsync(bool includeSystem = true, CancellationToken ct = default)
    {
        try
        {
            var collections = await _collectionRepository.GetAllAsync(includeSystem, ct);
            return Result.Success<IReadOnlyList<VirtualCollection>>(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all collections");
            return Result.Failure<IReadOnlyList<VirtualCollection>>($"Failed to get collections: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Executes a smart filter to find games matching the criteria.
    /// </summary>
    /// <param name="filter">The collection filter criteria.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the filtered list of games.</returns>
    public async Task<Result<IReadOnlyList<Game>>> ExecuteSmartFilterAsync(CollectionFilter filter, CancellationToken ct = default)
    {
        try
        {
            var query = _gameRepository.GetGamesAsync(ct: ct);

            // Apply filters
            var filteredGames = await ApplyCollectionFilterAsync(query, filter, ct);
            return Result.Success<IReadOnlyList<Game>>(filteredGames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute smart filter");
            return Result.Failure<IReadOnlyList<Game>>($"Failed to execute filter: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Creates default system collections for the application.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> CreateSystemCollectionsAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if system collections already exist
            var existingSystemCollections = await _collectionRepository.GetAllAsync(true, ct);
            if (existingSystemCollections.Any(vc => vc.IsSystemCollection))
            {
                _logger.LogInformation("System collections already exist");
                return Result.Success();
            }

            // Create system collections with actual filters
            var systemCollections = new[]
            {
                VirtualCollection.CreateSystemCollection(
                    "Never Played",
                    new CollectionFilter { MaxPlaytime = TimeSpan.Zero }, // Games with zero playtime
                    "🎮"),

                VirtualCollection.CreateSystemCollection(
                    "Recently Added",
                    new CollectionFilter { MaxDaysSinceLastPlayed = 30 }, // Added in last 30 days
                    "🆕"),

                VirtualCollection.CreateSystemCollection(
                    "Short Games",
                    new CollectionFilter { MaxPlaytime = TimeSpan.FromHours(10) }, // Games < 10 hours
                    "⚡"),

                VirtualCollection.CreateSystemCollection(
                    "Retro Classics",
                    new CollectionFilter { PlatformName = "NES|SNES|Genesis|N64|PS1" }, // Classic platforms
                    "🕹️"),

                // VirtualCollection.CreateSystemCollection(
                //    "Unfinished",
                //    new CollectionFilter { Status = GameStatus.InProgress }, // Games marked in progress
                //    "⏸️")
            };

            foreach (var collection in systemCollections)
            {
                await _collectionRepository.AddAsync(collection, ct);
                _logger.LogInformation("Created system collection '{Name}'", collection.Name);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create system collections");
            return Result.Failure($"Failed to create system collections: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<IReadOnlyList<Game>> ApplyCollectionFilterAsync(Task<PagedResult<Game>> gamesTask, CollectionFilter filter, CancellationToken ct)
    {
        var games = await gamesTask;

        var filtered = games.Items.AsEnumerable();

        if (filter.MaxPlaytime.HasValue)
            filtered = filtered.Where(g => g.TotalPlayTime <= filter.MaxPlaytime.Value);

        if (filter.MinPlaytime.HasValue)
            filtered = filtered.Where(g => g.TotalPlayTime >= filter.MinPlaytime.Value);

        if (filter.MaxDaysSinceLastPlayed.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-filter.MaxDaysSinceLastPlayed.Value);
            filtered = filtered.Where(g => g.LastPlayedAt >= cutoffDate || g.LastPlayedAt == null);
        }

        // Platform name filtering - supports pipe-separated platform patterns
        if (!string.IsNullOrEmpty(filter.PlatformName))
        {
            var platforms = filter.PlatformName.Split('|', StringSplitOptions.RemoveEmptyEntries);
            filtered = filtered.Where(g =>
                g.Platform?.Name != null &&
                platforms.Any(p => g.Platform.Name.Value.Contains(p, StringComparison.OrdinalIgnoreCase)));
        }

        // Note: Genre filtering depends on Game entity having genre properties
        // Note: Tag filtering depends on Game entity having tag properties
        // Note: Achievement filtering depends on Game entity having achievement properties

        if (filter.Status.HasValue)
            filtered = filtered.Where(g => g.Status == filter.Status.Value);

        // Note: Rating filtering depends on Game entity having rating properties

        return filtered.ToList();
    }
}

