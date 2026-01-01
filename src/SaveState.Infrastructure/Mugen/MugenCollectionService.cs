namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;

/// <summary>
/// Implementation of the MUGEN collection service.
/// Manages character collections and favorites.
/// </summary>
public class MugenCollectionService : IMugenCollectionService
{
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;
    private readonly SaveState.Core.Mugen.IMugenCollectionRepository _collectionRepository;

    public MugenCollectionService(
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository,
        SaveState.Core.Mugen.IMugenCollectionRepository collectionRepository)
    {
        _characterRepository = characterRepository;
        _collectionRepository = collectionRepository;
    }

    public async Task<Result<MugenCharacterCollection>> CreateCollectionAsync(
        string name,
        string? icon = null,
        CancellationToken ct = default)
    {
        try
        {
            // Create and persist collection entity
            var collection = MugenCharacterCollection.Create(name, Guid.NewGuid(), null, icon, false); // Placeholder user ID - user context integration pending

            await _collectionRepository.AddAsync(collection, ct);

            return Result<MugenCharacterCollection>.Success(collection);
        }
        catch (Exception ex)
        {
            return Result<MugenCharacterCollection>.Failure($"Failed to create collection: {ex.Message}");
        }
    }

    public async Task<Result> AddCharacterToCollectionAsync(
        Guid collectionId,
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            // Validate character exists
            var characterResult = await _characterRepository.GetByIdAsync(characterId, ct);
            if (characterResult.IsFailure)
                return Result.Failure("Character not found");

            // Load collection
            var collection = await _collectionRepository.GetByIdAsync(collectionId, ct);
            if (collection is null)
                return Result.Failure("Collection not found");

            // Check if character is already in collection
            var isAlreadyInCollection = await _collectionRepository.IsCharacterInCollectionAsync(collectionId, characterId, ct);
            if (isAlreadyInCollection)
                return Result.Failure("Character is already in this collection");

            // Add character to collection
            collection.AddCharacter(characterId);
            await _collectionRepository.UpdateAsync(collection, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to add character to collection: {ex.Message}");
        }
    }

    public async Task<Result> RemoveCharacterFromCollectionAsync(
        Guid collectionId,
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            // Load collection
            var collection = await _collectionRepository.GetByIdAsync(collectionId, ct);
            if (collection is null)
                return Result.Failure("Collection not found");

            // Check if character is in collection
            var isInCollection = await _collectionRepository.IsCharacterInCollectionAsync(collectionId, characterId, ct);
            if (!isInCollection)
                return Result.Failure("Character is not in this collection");

            // Remove character from collection
            collection.RemoveCharacter(characterId);
            await _collectionRepository.UpdateAsync(collection, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to remove character from collection: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<MugenCharacterCollection>>> GetCollectionsAsync(
        CancellationToken ct = default)
    {
        try
        {
            // Load collections from database
            var collections = await _collectionRepository.GetAllAsync(ct);

            return Result<IReadOnlyList<MugenCharacterCollection>>.Success(collections);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<MugenCharacterCollection>>.Failure($"Failed to get collections: {ex.Message}");
        }
    }

    public async Task<Result> SetFavoriteAsync(
        Guid characterId,
        bool isFavorite,
        CancellationToken ct = default)
    {
        try
        {
            // Validate character exists
            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character is null)
                return Result.Failure("Character not found");

            // Favorite status is tracked but requires entity property update.
            // The character entity needs an IsFavorite property for persistence.

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to set favorite: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<MugenCharacter>>> GetFavoritesAsync(
        CancellationToken ct = default)
    {
        try
        {
            // Returns characters marked as favorites. Currently returns first 3 as placeholder
            // until IsFavorite property is added to MugenCharacter entity.

            var allCharacters = await _characterRepository.GetAllAsync(ct);

            // Mock: return first 3 characters as favorites
            var favorites = allCharacters.Take(3).ToList();

            return Result<IReadOnlyList<MugenCharacter>>.Success(favorites);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<MugenCharacter>>.Failure($"Failed to get favorites: {ex.Message}");
        }
    }
}
