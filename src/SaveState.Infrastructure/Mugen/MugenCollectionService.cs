namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.UserManagement.Services;

/// <summary>
/// Implementation of the MUGEN collection service.
/// Manages character collections and favorites.
/// </summary>
public class MugenCollectionService : IMugenCollectionService
{
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;
    private readonly SaveState.Core.Mugen.IMugenCollectionRepository _collectionRepository;
    private readonly IUserContextService _userContextService;

    public MugenCollectionService(
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository,
        SaveState.Core.Mugen.IMugenCollectionRepository collectionRepository,
        IUserContextService userContextService)
    {
        _characterRepository = characterRepository;
        _collectionRepository = collectionRepository;
        _userContextService = userContextService;
    }

    public async Task<Result<MugenCharacterCollection>> CreateCollectionAsync(
        string name,
        string? icon = null,
        CancellationToken ct = default)
    {
        try
        {
            // Get current user ID
            var userId = _userContextService.GetCurrentUserIdRequired();

            // Create and persist collection entity
            var collection = MugenCharacterCollection.Create(name, userId, null, icon, false);

            await _collectionRepository.AddAsync(collection, ct);

            return Result.Success<MugenCharacterCollection>(collection);
        }
        catch (Exception ex)
        {
            return Result.Failure<MugenCharacterCollection>($"Failed to create collection: {ex.Message}");
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

            return Result.Success<IReadOnlyList<MugenCharacterCollection>>(collections);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MugenCharacterCollection>>($"Failed to get collections: {ex.Message}");
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
            var characterResult = await _characterRepository.GetByIdAsync(characterId, ct);
            if (characterResult.IsFailure || characterResult.Value is null)
                return Result.Failure("Character not found");

            var character = characterResult.Value;

            // Update favorite status
            character.SetFavorite(isFavorite);

            // Save changes
            await _characterRepository.UpdateAsync(character, ct);

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
            // Get all characters and filter by favorite status
            var allCharacters = await _characterRepository.GetAllAsync(ct);
            var favorites = allCharacters.Where(c => c.IsFavorite).ToList();

            return Result.Success<IReadOnlyList<MugenCharacter>>(favorites);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MugenCharacter>>($"Failed to get favorites: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<MugenCharacter>>> GetRosterAsync(CancellationToken ct = default)
    {
        try
        {
            var allCharacters = await _characterRepository.GetAllAsync(ct);
            return Result.Success<IReadOnlyList<MugenCharacter>>(allCharacters);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MugenCharacter>>($"Failed to get roster: {ex.Message}");
        }
    }
}

