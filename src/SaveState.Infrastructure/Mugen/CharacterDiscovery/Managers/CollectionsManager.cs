using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery.Managers;

/// <summary>
/// Manager for handling character collections and lists.
/// </summary>
public class CollectionsManager
{
    private readonly ILogger<CollectionsManager> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionsManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeProvider">The time provider instance.</param>
    public CollectionsManager(ILogger<CollectionsManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a new character collection.
    /// </summary>
    /// <param name="name">The collection name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="isPublic">Whether the collection is public.</param>
    /// <param name="userId">The user ID creating the collection.</param>
    /// <param name="collections">The collections dictionary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created collection.</returns>
    public Task<Result<CharacterCollection>> CreateCollectionAsync(
        string name,
        string? description,
        bool isPublic,
        string userId,
        ConcurrentDictionary<Guid, CharacterCollection> collections,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult(Result<CharacterCollection>.Failure(
                    "Collection name cannot be empty", ErrorType.Validation));
            }

            var collection = new CharacterCollection(
                Guid.NewGuid(),
                name,
                description,
                userId,
                isPublic,
                0,
                new List<string>(),
                0,
                0,
                _timeProvider.UtcNow,
                _timeProvider.UtcNow);

            collections[collection.Id] = collection;

            _logger.LogInformation("Created collection {CollectionId} with name {Name} for user {UserId}",
                collection.Id, name, userId);

            return Task.FromResult(Result<CharacterCollection>.Success(collection));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection with name {Name}", name);
            return Task.FromResult(Result<CharacterCollection>.Failure(
                $"Failed to create collection: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Adds a character to a collection.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="characterId">The character ID to add.</param>
    /// <param name="collections">The collections dictionary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    public Task<Result> AddToCollectionAsync(
        Guid collectionId,
        Guid characterId,
        ConcurrentDictionary<Guid, CharacterCollection> collections,
        CancellationToken ct)
    {
        try
        {
            if (!collections.TryGetValue(collectionId, out var collection))
            {
                return Task.FromResult(Result.Failure(
                    $"Collection {collectionId} not found", ErrorType.NotFound));
            }

            var updatedCollection = collection with
            {
                CharacterCount = collection.CharacterCount + 1,
                LastUpdated = _timeProvider.UtcNow
            };

            collections[collectionId] = updatedCollection;

            _logger.LogInformation("Added character {CharacterId} to collection {CollectionId}",
                characterId, collectionId);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add character {CharacterId} to collection {CollectionId}",
                characterId, collectionId);
            return Task.FromResult(Result.Failure(
                $"Failed to add to collection: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Removes a character from a collection.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="characterId">The character ID to remove.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    /// <param name="collections">The collections dictionary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    public Task<Result> RemoveFromCollectionAsync(
        Guid collectionId,
        Guid characterId,
        ITimeProvider timeProvider,
        ConcurrentDictionary<Guid, CharacterCollection> collections,
        CancellationToken ct)
    {
        try
        {
            if (!collections.TryGetValue(collectionId, out var collection))
            {
                return Task.FromResult(Result.Failure(
                    $"Collection {collectionId} not found", ErrorType.NotFound));
            }

            if (collection.CharacterCount <= 0)
            {
                return Task.FromResult(Result.Failure(
                    "Collection is empty", ErrorType.Validation));
            }

            var updatedCollection = collection with
            {
                CharacterCount = collection.CharacterCount - 1,
                LastUpdated = timeProvider.UtcNow
            };

            collections[collectionId] = updatedCollection;

            _logger.LogInformation("Removed character {CharacterId} from collection {CollectionId}",
                characterId, collectionId);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove character {CharacterId} from collection {CollectionId}",
                characterId, collectionId);
            return Task.FromResult(Result.Failure(
                $"Failed to remove from collection: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets collections for a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="collections">The collections dictionary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of user's collections.</returns>
    public Task<Result<IReadOnlyList<CharacterCollection>>> GetCollectionsAsync(
        string userId,
        ConcurrentDictionary<Guid, CharacterCollection> collections,
        CancellationToken ct)
    {
        try
        {
            var userCollections = collections.Values
                .Where(c => c.CreatorName.Equals(userId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.LastUpdated)
                .ToList();

            _logger.LogInformation("Retrieved {Count} collections for user {UserId}",
                userCollections.Count, userId);

            return Task.FromResult(Result<IReadOnlyList<CharacterCollection>>.Success(userCollections));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections for user {UserId}", userId);
            return Task.FromResult(Result<IReadOnlyList<CharacterCollection>>.Failure(
                $"Failed to get collections: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets public collections.
    /// </summary>
    /// <param name="limit">Optional limit on number of results.</param>
    /// <param name="collections">The collections dictionary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of public collections.</returns>
    public Task<Result<IReadOnlyList<CharacterCollection>>> GetPublicCollectionsAsync(
        int? limit,
        ConcurrentDictionary<Guid, CharacterCollection> collections,
        CancellationToken ct)
    {
        try
        {
            var publicCollections = collections.Values
                .Where(c => c.IsPublic)
                .OrderByDescending(c => c.FavoriteCount)
                .ThenByDescending(c => c.ViewCount)
                .Take(limit ?? 20)
                .ToList();

            _logger.LogInformation("Retrieved {Count} public collections", publicCollections.Count);

            return Task.FromResult(Result<IReadOnlyList<CharacterCollection>>.Success(publicCollections));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get public collections");
            return Task.FromResult(Result<IReadOnlyList<CharacterCollection>>.Failure(
                $"Failed to get public collections: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets characters in a collection.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="characters">The characters dictionary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of characters in the collection.</returns>
    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetCollectionCharactersAsync(
        Guid collectionId,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct)
    {
        try
        {
            // In a real implementation, this would retrieve characters from the collection
            // For now, return an empty list as the collection doesn't store character IDs directly
            var collectionCharacters = new List<DiscoveredCharacter>();

            _logger.LogInformation("Retrieved {Count} characters from collection {CollectionId}",
                collectionCharacters.Count, collectionId);

            return Task.FromResult(Result<IReadOnlyList<DiscoveredCharacter>>.Success(collectionCharacters));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters for collection {CollectionId}", collectionId);
            return Task.FromResult(Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Failed to get collection characters: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Deletes a collection.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="userId">The user ID requesting deletion.</param>
    /// <param name="collections">The collections dictionary.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    public Task<Result> DeleteCollectionAsync(
        Guid collectionId,
        string userId,
        ConcurrentDictionary<Guid, CharacterCollection> collections,
        CancellationToken ct)
    {
        try
        {
            if (!collections.TryGetValue(collectionId, out var collection))
            {
                return Task.FromResult(Result.Failure(
                    $"Collection {collectionId} not found", ErrorType.NotFound));
            }

            if (!collection.CreatorName.Equals(userId, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Result.Failure(
                    "Only the creator can delete this collection", ErrorType.Forbidden));
            }

            if (!collections.TryRemove(collectionId, out _))
            {
                return Task.FromResult(Result.Failure(
                    "Failed to remove collection", ErrorType.Internal));
            }

            _logger.LogInformation("Deleted collection {CollectionId} by user {UserId}",
                collectionId, userId);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection {CollectionId}", collectionId);
            return Task.FromResult(Result.Failure(
                $"Failed to delete collection: {ex.Message}", ErrorType.Internal));
        }
    }
}
