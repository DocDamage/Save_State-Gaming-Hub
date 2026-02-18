using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Core.OpenMK.Services;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Infrastructure.OpenMK.Services.OpenMK.Engines;

/// <summary>
/// Engine responsible for kombat/match operations.
/// </summary>
public sealed class KombatEngine
{
    private readonly IOpenMKCharacterRepository _characterRepository;
    private readonly IOpenMKMatchStateRepository _matchStateRepository;
    private readonly ILogger<KombatEngine> _logger;

    public KombatEngine(
        IOpenMKCharacterRepository characterRepository,
        IOpenMKMatchStateRepository matchStateRepository,
        ILogger<KombatEngine> logger)
    {
        _characterRepository = characterRepository;
        _matchStateRepository = matchStateRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<OpenMKSpecialMove>>> GetCharacterSpecialMovesAsync(
        Guid characterId, CancellationToken ct = default)
    {
        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character == null)
            {
                return Result.Failure<IReadOnlyList<OpenMKSpecialMove>>("Character not found", ErrorType.NotFound);
            }
            return Result.Success(character.SpecialMoves);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get special moves for character {CharacterId}", characterId);
            return Result.Failure<IReadOnlyList<OpenMKSpecialMove>>("Failed to retrieve special moves", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKSpecialMoveResult>> PerformSpecialMoveAsync(
        Guid matchId, Guid characterId, string specialMoveName, CancellationToken ct = default)
    {
        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character == null)
            {
                return Result.Failure<OpenMKSpecialMoveResult>("Character not found", ErrorType.NotFound);
            }

            var specialMove = character.SpecialMoves.FirstOrDefault(m =>
                m.Name.Equals(specialMoveName, StringComparison.OrdinalIgnoreCase));

            if (specialMove == null)
            {
                return Result.Failure<OpenMKSpecialMoveResult>("Special move not found", ErrorType.NotFound);
            }

            // Check if special move can be performed
            var canPerform = await CanPerformSpecialMoveAsync(matchId, characterId, specialMoveName, ct);
            if (!canPerform.IsSuccess || !canPerform.Value)
            {
                return Result.Failure<OpenMKSpecialMoveResult>("Cannot perform special move in current match state", ErrorType.Validation);
            }

            // Check super bar requirements
            if (specialMove.RequiresSuperBar)
            {
                var superBarResult = await GetSuperBarLevelAsync(matchId, characterId, ct);
                if (superBarResult.IsSuccess && superBarResult.Value < specialMove.SuperBarCost)
                {
                    return Result.Failure<OpenMKSpecialMoveResult>("Insufficient super bar", ErrorType.Validation);
                }
            }

            _logger.LogInformation(
                "Performed special move {SpecialMoveName} for character {CharacterId} in match {MatchId}",
                specialMoveName, characterId, matchId);

            return Result.Success(new OpenMKSpecialMoveResult(
                Success: true,
                DamageDealt: specialMove.Damage,
                AnimationPlayed: specialMove.AnimationName,
                SoundPlayed: specialMove.SoundEffect,
                SuperBarGained: 10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform special move {SpecialMoveName} for character {CharacterId}", specialMoveName, characterId);
            return Result.Failure<OpenMKSpecialMoveResult>("Failed to perform special move", ErrorType.Internal);
        }
    }

    public async Task<Result<bool>> CanPerformSpecialMoveAsync(
        Guid matchId, Guid characterId, string specialMoveName, CancellationToken ct = default)
    {
        try
        {
            var matchState = await _matchStateRepository.GetByMatchIdAsync(matchId, ct);
            if (matchState == null)
            {
                return Result.Success(false);
            }

            // Check if match is in an active state
            var canPerform = matchState.Phase == OpenMKMatchPhase.Fighting ||
                            matchState.Phase == OpenMKMatchPhase.Finisher;

            return Result.Success(canPerform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check special move availability for match {MatchId}", matchId);
            return Result.Failure<bool>("Failed to check special move availability", ErrorType.Internal);
        }
    }

    public async Task<Result<int>> GetSuperBarLevelAsync(
        Guid matchId, Guid characterId, CancellationToken ct = default)
    {
        try
        {
            var matchState = await _matchStateRepository.GetByMatchIdAsync(matchId, ct);
            if (matchState == null)
            {
                return Result.Failure<int>("Match not found", ErrorType.NotFound);
            }

            var isPlayer1 = matchState.Player1CharacterId == characterId;
            var superBar = isPlayer1 ? matchState.Player1SuperBar : matchState.Player2SuperBar;

            return Result.Success(superBar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get super bar level for match {MatchId}", matchId);
            return Result.Failure<int>("Failed to retrieve super bar level", ErrorType.Internal);
        }
    }

    public async Task<Result> IncreaseSuperBarAsync(
        Guid matchId, Guid characterId, int amount, CancellationToken ct = default)
    {
        try
        {
            var matchState = await _matchStateRepository.GetByMatchIdAsync(matchId, ct);
            if (matchState == null)
            {
                return Result.Failure("Match not found", ErrorType.NotFound);
            }

            var isPlayer1 = matchState.Player1CharacterId == characterId;
            var currentBar = isPlayer1 ? matchState.Player1SuperBar : matchState.Player2SuperBar;
            var newValue = Math.Min(currentBar + amount, 100); // Max super bar is 100

            matchState.UpdateSuperBar(characterId, newValue);
            await _matchStateRepository.UpdateAsync(matchState, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increase super bar for match {MatchId}", matchId);
            return Result.Failure("Failed to increase super bar", ErrorType.Internal);
        }
    }

    public async Task<Result> SetCharacterCostumeAsync(
        Guid matchId, Guid characterId, string costumeName, CancellationToken ct = default)
    {
        try
        {
            var matchState = await _matchStateRepository.GetByMatchIdAsync(matchId, ct);
            if (matchState == null)
            {
                return Result.Failure("Match not found", ErrorType.NotFound);
            }

            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character == null)
            {
                return Result.Failure("Character not found", ErrorType.NotFound);
            }

            // Validate costume exists
            var costume = character.Costumes.FirstOrDefault(c =>
                c.Name.Equals(costumeName, StringComparison.OrdinalIgnoreCase));

            if (costume == null)
            {
                return Result.Failure("Costume not found", ErrorType.NotFound);
            }

            matchState.SetCostume(characterId, costumeName);
            await _matchStateRepository.UpdateAsync(matchState, ct);

            _logger.LogInformation(
                "Set costume {CostumeName} for character {CharacterId} in match {MatchId}",
                costumeName, characterId, matchId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set costume for match {MatchId}", matchId);
            return Result.Failure("Failed to set costume", ErrorType.Internal);
        }
    }
}
