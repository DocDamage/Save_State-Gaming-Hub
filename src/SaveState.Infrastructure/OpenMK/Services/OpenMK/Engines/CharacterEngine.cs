using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Infrastructure.OpenMK.Services.OpenMK.Engines;

/// <summary>
/// Engine responsible for character operations.
/// </summary>
public sealed class CharacterEngine
{
    private readonly IOpenMKCharacterRepository _characterRepository;
    private readonly ILogger<CharacterEngine> _logger;

    public CharacterEngine(
        IOpenMKCharacterRepository characterRepository,
        ILogger<CharacterEngine> logger)
    {
        _characterRepository = characterRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersAsync(CancellationToken ct = default)
    {
        try
        {
            var characters = await _characterRepository.GetAllAsync(ct);
            return Result.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters");
            return Result.Failure<IReadOnlyList<OpenMKCharacter>>("Failed to retrieve characters", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKCharacter>> GetCharacterAsync(Guid characterId, CancellationToken ct = default)
    {
        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character == null)
            {
                return Result.Failure<OpenMKCharacter>("Character not found", ErrorType.NotFound);
            }
            return Result.Success(character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character {CharacterId}", characterId);
            return Result.Failure<OpenMKCharacter>("Failed to retrieve character", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByRealmAsync(
        OpenMKRealm realm, CancellationToken ct = default)
    {
        try
        {
            var characters = await _characterRepository.GetByRealmAsync(realm, ct);
            return Result.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters by realm {Realm}", realm);
            return Result.Failure<IReadOnlyList<OpenMKCharacter>>("Failed to retrieve characters", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByFightingStyleAsync(
        OpenMKFightingStyle style, CancellationToken ct = default)
    {
        try
        {
            var characters = await _characterRepository.GetByFightingStyleAsync(style, ct);
            return Result.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters by fighting style {Style}", style);
            return Result.Failure<IReadOnlyList<OpenMKCharacter>>("Failed to retrieve characters", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByAlignmentAsync(
        OpenMKAlignment alignment, CancellationToken ct = default)
    {
        try
        {
            var characters = await _characterRepository.GetByAlignmentAsync(alignment, ct);
            return Result.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters by alignment {Alignment}", alignment);
            return Result.Failure<IReadOnlyList<OpenMKCharacter>>("Failed to retrieve characters", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<OpenMKCostume>>> GetCharacterCostumesAsync(
        Guid characterId, CancellationToken ct = default)
    {
        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character == null)
            {
                return Result.Failure<IReadOnlyList<OpenMKCostume>>("Character not found", ErrorType.NotFound);
            }
            return Result.Success(character.Costumes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get costumes for character {CharacterId}", characterId);
            return Result.Failure<IReadOnlyList<OpenMKCostume>>("Failed to retrieve costumes", ErrorType.Internal);
        }
    }

    public async Task<Result<string>> GetCharacterEndingAsync(Guid characterId, CancellationToken ct = default)
    {
        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character == null)
            {
                return Result.Failure<string>("Character not found", ErrorType.NotFound);
            }
            return Result.Success(character.Ending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ending for character {CharacterId}", characterId);
            return Result.Failure<string>("Failed to retrieve character ending", ErrorType.Internal);
        }
    }
}
