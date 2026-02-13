using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Constants;
using SaveState.Core.Social;
using SaveState.Core.Social.Entities;
using SaveState.Core.Social.Services;
using SaveState.Core.Sync;

namespace SaveState.Infrastructure.Social;

/// <summary>
/// Service implementation for managing shared collections.
/// </summary>
public class SharedCollectionService : ISharedCollectionService
{
    private readonly ISharedCollectionRepository _repository;
    private readonly ICloudStorageProvider _cloudStorage;
    private readonly ILogger<SharedCollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedCollectionService"/> class.
    /// </summary>
    /// <param name="repository">Repository for accessing shared collections.</param>
    /// <param name="cloudStorage">Cloud storage provider for collection sync.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public SharedCollectionService(
        ISharedCollectionRepository repository,
        ICloudStorageProvider cloudStorage,
        ILogger<SharedCollectionService> logger)
    {
        _repository = repository;
        _cloudStorage = cloudStorage;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new shared collection.
    /// </summary>
    /// <param name="title">The title of the collection.</param>
    /// <param name="description">Optional description of the collection.</param>
    /// <param name="isPublic">Whether the collection is publicly visible.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the created collection or an error.</returns>
    public async Task<Result<SharedCollection>> CreateCollectionAsync(
        string title,
        string? description = null,
        bool isPublic = false,
        CancellationToken ct = default)
    {
        try
        {
            var collection = SharedCollection.Create(title, description, isPublic);
            await _repository.AddAsync(collection, ct);

            _logger.LogInformation("Created shared collection '{Title}' with code {Code}", title, collection.ShareCode);

            return Result.Success<SharedCollection>(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shared collection '{Title}'", title);
            return Result.Failure<SharedCollection>(ErrorMessages.CreateFailed, ErrorType.Internal);
        }
    }

    /// <summary>
    /// Updates an existing shared collection.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection.</param>
    /// <param name="title">Optional new title.</param>
    /// <param name="description">Optional new description.</param>
    /// <param name="isPublic">Optional visibility setting.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the updated collection or an error.</returns>
    public async Task<Result<SharedCollection>> UpdateCollectionAsync(
        Guid collectionId,
        string? title = null,
        string? description = null,
        bool? isPublic = null,
        CancellationToken ct = default)
    {
        try
        {
            var collection = await _repository.GetByIdAsync(collectionId, ct);
            if (collection is null)
            {
                return Result.Failure<SharedCollection>(ErrorMessages.CollectionNotFound, ErrorType.NotFound);
            }

            collection.Update(title, description, isPublic);
            await _repository.UpdateAsync(collection, ct);

            _logger.LogInformation("Updated shared collection {Id}", collectionId);

            return Result.Success<SharedCollection>(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update shared collection {Id}", collectionId);
            return Result.Failure<SharedCollection>(ErrorMessages.UpdateFailed, ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets a collection by its unique identifier.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the collection or an error.</returns>
    public async Task<Result<SharedCollection>> GetCollectionAsync(Guid collectionId, CancellationToken ct = default)
    {
        try
        {
            var collection = await _repository.GetByIdAsync(collectionId, ct);
            if (collection is null)
            {
                return Result.Failure<SharedCollection>(ErrorMessages.CollectionNotFound, ErrorType.NotFound);
            }

            return Result.Success<SharedCollection>(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shared collection {Id}", collectionId);
            return Result.Failure<SharedCollection>(ErrorMessages.OperationFailed, ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets a collection by its share code.
    /// </summary>
    /// <param name="shareCode">The unique share code for the collection.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the collection or an error.</returns>
    public async Task<Result<SharedCollection>> GetCollectionByShareCodeAsync(string shareCode, CancellationToken ct = default)
    {
        try
        {
            var collection = await _repository.GetByShareCodeAsync(shareCode, ct);
            if (collection is null)
            {
                return Result.Failure<SharedCollection>(ErrorMessages.CollectionNotFound, ErrorType.NotFound);
            }

            // Increment download count
            collection.IncrementDownloadCount();
            await _repository.UpdateAsync(collection, ct);

            return Result.Success<SharedCollection>(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shared collection by code {Code}", shareCode);
            return Result.Failure<SharedCollection>("Failed to get collection", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets a paginated list of shared collections with optional filtering.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of collections per page.</param>
    /// <param name="isPublic">Optional filter by visibility.</param>
    /// <param name="searchTerm">Optional search term for filtering.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing a paged list of collections.</returns>
    public async Task<Result<PagedResult<SharedCollection>>> GetCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        bool? isPublic = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _repository.GetCollectionsAsync(pageNumber, pageSize, isPublic, searchTerm, ct);
            return Result.Success<PagedResult<SharedCollection>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shared collections");
            return Result.Failure<PagedResult<SharedCollection>>("Failed to get collections", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets a paginated list of the current user's collections.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of collections per page.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing a paged list of the user's collections.</returns>
    public async Task<Result<PagedResult<SharedCollection>>> GetUserCollectionsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _repository.GetUserCollectionsAsync(pageNumber, pageSize, ct);
            return Result.Success<PagedResult<SharedCollection>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user collections");
            return Result.Failure<PagedResult<SharedCollection>>("Failed to get user collections", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Deletes a shared collection.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection to delete.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> DeleteCollectionAsync(Guid collectionId, CancellationToken ct = default)
    {
        try
        {
            var collection = await _repository.GetByIdAsync(collectionId, ct);
            if (collection is null)
            {
                return Result.Failure("Collection not found", ErrorType.NotFound);
            }

            await _repository.DeleteAsync(collectionId, ct);

            _logger.LogInformation("Deleted shared collection {Id}", collectionId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete shared collection {Id}", collectionId);
            return Result.Failure("Failed to delete collection", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Adds games to an existing collection.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection.</param>
    /// <param name="games">The list of games to add.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> AddGamesToCollectionAsync(
        Guid collectionId,
        IReadOnlyList<CollectionGameRequest> games,
        CancellationToken ct = default)
    {
        try
        {
            var collection = await _repository.GetByIdAsync(collectionId, ct);
            if (collection is null)
            {
                return Result.Failure("Collection not found", ErrorType.NotFound);
            }

            var items = games.Select((game, index) => new SharedCollectionItem
            {
                CollectionId = collectionId,
                GameTitle = game.GameTitle,
                Notes = game.Notes,
                SortOrder = game.SortOrder > 0 ? game.SortOrder : index
            }).ToList();

            await _repository.UpdateItemsAsync(collectionId, items, ct);

            _logger.LogInformation("Added {Count} games to collection {Id}", games.Count, collectionId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add games to collection {Id}", collectionId);
            return Result.Failure("Failed to add games to collection", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Removes games from a collection.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection.</param>
    /// <param name="gameTitles">The titles of the games to remove.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> RemoveGamesFromCollectionAsync(
        Guid collectionId,
        IReadOnlyList<string> gameTitles,
        CancellationToken ct = default)
    {
        try
        {
            foreach (var gameTitle in gameTitles)
            {
                await _repository.RemoveItemAsync(collectionId, gameTitle, ct);
            }

            _logger.LogInformation("Removed {Count} games from collection {Id}", gameTitles.Count, collectionId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove games from collection {Id}", collectionId);
            return Result.Failure("Failed to remove games from collection", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Imports a collection using its share code.
    /// </summary>
    /// <param name="shareCode">The share code of the collection to import.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the imported collection or an error.</returns>
    public async Task<Result<SharedCollection>> ImportCollectionAsync(
        string shareCode,
        CancellationToken ct = default)
    {
        // For now, just return the collection by share code
        // In a real implementation, this might involve downloading from cloud storage
        return await GetCollectionByShareCodeAsync(shareCode, ct);
    }

    /// <summary>
    /// Exports a collection and returns its share code.
    /// </summary>
    /// <param name="collectionId">The unique identifier of the collection to export.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the share code or an error.</returns>
    public async Task<Result<string>> ExportCollectionAsync(
        Guid collectionId,
        CancellationToken ct = default)
    {
        try
        {
            var collection = await _repository.GetByIdAsync(collectionId, ct);
            if (collection is null)
            {
                return Result.Failure<string>("Collection not found", ErrorType.NotFound);
            }

            // For now, just return the share code
            // In a real implementation, this might involve uploading to cloud storage
            return Result.Success<string>(collection.ShareCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export collection {Id}", collectionId);
            return Result.Failure<string>("Failed to export collection", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets statistics about shared collections.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing collection statistics.</returns>
    public async Task<Result<SharedCollectionStatistics>> GetStatisticsAsync(CancellationToken ct = default)
    {
        try
        {
            var statistics = await _repository.GetStatisticsAsync(ct);
            return Result.Success<SharedCollectionStatistics>(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shared collection statistics");
            return Result.Failure<SharedCollectionStatistics>("Failed to get statistics", ErrorType.Internal);
        }
    }
}

