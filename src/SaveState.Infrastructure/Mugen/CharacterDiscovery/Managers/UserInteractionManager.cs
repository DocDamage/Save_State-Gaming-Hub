using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery.Managers;

/// <summary>
/// Manages user interactions with discovered characters including ratings, favorites, reports, and sharing.
/// </summary>
public sealed class UserInteractionManager
{
    private readonly ILogger<UserInteractionManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInteractionManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public UserInteractionManager(ILogger<UserInteractionManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Rates a character with the specified rating and optional review.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="rating">The rating value (1-5).</param>
    /// <param name="review">Optional review text.</param>
    /// <param name="characters">The character storage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> RateCharacterAsync(
        Guid characterId,
        int rating,
        string? review,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromResult(Result.Failure("Operation was cancelled", ErrorType.Cancelled));
            }

            if (rating < 1 || rating > 5)
            {
                _logger.LogWarning("Invalid rating {Rating} for character {CharacterId}. Rating must be between 1 and 5.", rating, characterId);
                return Task.FromResult(Result.Failure("Rating must be between 1 and 5", ErrorType.Validation));
            }

            if (!characters.TryGetValue(characterId, out var character))
            {
                _logger.LogWarning("Character {CharacterId} not found for rating", characterId);
                return Task.FromResult(Result.Failure($"Character {characterId} not found", ErrorType.NotFound));
            }

            // Calculate new rating using weighted average formula: (currentRating * reviewCount + newRating) / (reviewCount + 1)
            var currentRating = character.Rating;
            var reviewCount = character.ReviewCount;
            var newRating = (currentRating * reviewCount + rating) / (reviewCount + 1);

            var updatedCharacter = character with
            {
                Rating = newRating,
                ReviewCount = reviewCount + 1
            };

            characters[characterId] = updatedCharacter;

            _logger.LogInformation("Character {CharacterId} rated {Rating}. New average: {NewRating}", characterId, rating, newRating);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating character {CharacterId}", characterId);
            return Task.FromResult(Result.Failure($"Error rating character: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Adds a character to the user's favorites.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="userFavorites">The user favorites storage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> AddToFavoritesAsync(
        Guid characterId,
        string userId,
        ConcurrentDictionary<string, List<Guid>> userFavorites,
        CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromResult(Result.Failure("Operation was cancelled", ErrorType.Cancelled));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(Result.Failure("User ID cannot be empty", ErrorType.Validation));
            }

            userFavorites.AddOrUpdate(
                userId,
                [characterId],
                (key, existingList) =>
                {
                    if (!existingList.Contains(characterId))
                    {
                        existingList.Add(characterId);
                    }
                    return existingList;
                });

            _logger.LogInformation("Character {CharacterId} added to favorites for user {UserId}", characterId, userId);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding character {CharacterId} to favorites for user {UserId}", characterId, userId);
            return Task.FromResult(Result.Failure($"Error adding to favorites: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Removes a character from the user's favorites.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="userFavorites">The user favorites storage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> RemoveFromFavoritesAsync(
        Guid characterId,
        string userId,
        ConcurrentDictionary<string, List<Guid>> userFavorites,
        CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromResult(Result.Failure("Operation was cancelled", ErrorType.Cancelled));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(Result.Failure("User ID cannot be empty", ErrorType.Validation));
            }

            if (userFavorites.TryGetValue(userId, out var favorites))
            {
                favorites.Remove(characterId);
                _logger.LogInformation("Character {CharacterId} removed from favorites for user {UserId}", characterId, userId);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing character {CharacterId} from favorites for user {UserId}", characterId, userId);
            return Task.FromResult(Result.Failure($"Error removing from favorites: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets the user's favorite characters.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="userFavorites">The user favorites storage.</param>
    /// <param name="characters">The character storage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of favorite characters.</returns>
    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetFavoritesAsync(
        string userId,
        ConcurrentDictionary<string, List<Guid>> userFavorites,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromResult(Result<IReadOnlyList<DiscoveredCharacter>>.Failure("Operation was cancelled", ErrorType.Cancelled));
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult(Result<IReadOnlyList<DiscoveredCharacter>>.Failure("User ID cannot be empty", ErrorType.Validation));
            }

            if (!userFavorites.TryGetValue(userId, out var favoriteIds))
            {
                return Task.FromResult(Result<IReadOnlyList<DiscoveredCharacter>>.Success(Array.Empty<DiscoveredCharacter>()));
            }

            var favorites = new List<DiscoveredCharacter>();
            foreach (var characterId in favoriteIds)
            {
                if (characters.TryGetValue(characterId, out var character))
                {
                    favorites.Add(character);
                }
            }

            _logger.LogInformation("Retrieved {Count} favorites for user {UserId}", favorites.Count, userId);

            return Task.FromResult(Result<IReadOnlyList<DiscoveredCharacter>>.Success(favorites));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorites for user {UserId}", userId);
            return Task.FromResult(Result<IReadOnlyList<DiscoveredCharacter>>.Failure($"Error retrieving favorites: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Reports a character for a specific reason.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="reason">The report reason.</param>
    /// <param name="details">Optional details about the report.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> ReportCharacterAsync(
        Guid characterId,
        CharacterReportReason reason,
        string? details,
        CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromResult(Result.Failure("Operation was cancelled", ErrorType.Cancelled));
            }

            _logger.LogWarning(
                "Character {CharacterId} reported for reason: {Reason}. Details: {Details}",
                characterId,
                reason,
                details ?? "None provided");

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting character {CharacterId}", characterId);
            return Task.FromResult(Result.Failure($"Error reporting character: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Shares a character and returns a shareable URL.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <param name="options">The share options.</param>
    /// <param name="characters">The character storage.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the share URL.</returns>
    public Task<Result<string>> ShareCharacterAsync(
        Guid characterId,
        ShareOptions options,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested)
            {
                return Task.FromResult(Result<string>.Failure("Operation was cancelled", ErrorType.Cancelled));
            }

            if (!characters.TryGetValue(characterId, out var character))
            {
                _logger.LogWarning("Character {CharacterId} not found for sharing", characterId);
                return Task.FromResult(Result<string>.Failure($"Character {characterId} not found", ErrorType.NotFound));
            }

            var shareUrl = $"https://savestate.app/characters/{characterId}";

            _logger.LogInformation(
                "Character {CharacterName} ({CharacterId}) shared via {Platform}. IncludeStats: {IncludeStats}",
                character.Name,
                characterId,
                options.Platform,
                options.IncludeStats);

            if (!string.IsNullOrEmpty(options.Message))
            {
                _logger.LogDebug("Share message: {Message}", options.Message);
            }

            return Task.FromResult(Result<string>.Success(shareUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing character {CharacterId}", characterId);
            return Task.FromResult(Result<string>.Failure($"Error sharing character: {ex.Message}", ErrorType.Internal));
        }
    }
}
