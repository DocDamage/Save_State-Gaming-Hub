using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;

namespace SaveState.Infrastructure.OpenMK.Services.OpenMK.Engines;

/// <summary>
/// Engine responsible for character progression and unlocks.
/// </summary>
public sealed class ProgressionEngine
{
    private readonly IOpenMKProgressRepository _progressRepository;
    private readonly ILogger<ProgressionEngine> _logger;

    public ProgressionEngine(
        IOpenMKProgressRepository progressRepository,
        ILogger<ProgressionEngine> logger)
    {
        _progressRepository = progressRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<OpenMKCharacter>>> GetUnlockedCharactersAsync(
        Guid userId, CancellationToken ct = default)
    {
        try
        {
            var characters = await _progressRepository.GetUnlockedCharactersAsync(userId, ct);
            return Result.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unlocked characters for user {UserId}", userId);
            return Result.Failure<IReadOnlyList<OpenMKCharacter>>("Failed to retrieve unlocked characters", ErrorType.Internal);
        }
    }

    public async Task<Result<bool>> IsCharacterUnlockedAsync(
        Guid userId, Guid characterId, CancellationToken ct = default)
    {
        try
        {
            var isUnlocked = await _progressRepository.IsCharacterUnlockedAsync(userId, characterId, ct);
            return Result.Success(isUnlocked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check unlock status for user {UserId} and character {CharacterId}", userId, characterId);
            return Result.Failure<bool>("Failed to check unlock status", ErrorType.Internal);
        }
    }

    public async Task<Result> UnlockCharacterAsync(
        Guid userId, Guid characterId, CancellationToken ct = default)
    {
        try
        {
            await _progressRepository.UnlockCharacterAsync(userId, characterId, ct);
            _logger.LogInformation("Unlocked character {CharacterId} for user {UserId}", characterId, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock character {CharacterId} for user {UserId}", characterId, userId);
            return Result.Failure("Failed to unlock character", ErrorType.Internal);
        }
    }
}
