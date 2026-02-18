using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Core.OpenMK.Services;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Infrastructure.OpenMK.Services.OpenMK.Engines;

/// <summary>
/// Engine responsible for fatality operations.
/// </summary>
public sealed class FatalityEngine
{
    private readonly IOpenMKCharacterRepository _characterRepository;
    private readonly IOpenMKMatchStateRepository _matchStateRepository;
    private readonly ILogger<FatalityEngine> _logger;

    public FatalityEngine(
        IOpenMKCharacterRepository characterRepository,
        IOpenMKMatchStateRepository matchStateRepository,
        ILogger<FatalityEngine> logger)
    {
        _characterRepository = characterRepository;
        _matchStateRepository = matchStateRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<OpenMKFatality>>> GetCharacterFatalitiesAsync(
        Guid characterId, CancellationToken ct = default)
    {
        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character == null)
            {
                return Result.Failure<IReadOnlyList<OpenMKFatality>>("Character not found", ErrorType.NotFound);
            }
            return Result.Success(character.Fatalities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fatalities for character {CharacterId}", characterId);
            return Result.Failure<IReadOnlyList<OpenMKFatality>>("Failed to retrieve fatalities", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKFatalityResult>> PerformFatalityAsync(
        Guid matchId, Guid characterId, string fatalityName, CancellationToken ct = default)
    {
        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, ct);
            if (character == null)
            {
                return Result.Failure<OpenMKFatalityResult>("Character not found", ErrorType.NotFound);
            }

            var fatality = character.Fatalities.FirstOrDefault(f =>
                f.Name.Equals(fatalityName, StringComparison.OrdinalIgnoreCase));

            if (fatality == null)
            {
                return Result.Failure<OpenMKFatalityResult>("Fatality not found", ErrorType.NotFound);
            }

            // Check if fatality can be performed
            var canPerform = await CanPerformFatalityAsync(matchId, characterId, fatalityName, ct);
            if (!canPerform.IsSuccess || !canPerform.Value)
            {
                return Result.Failure<OpenMKFatalityResult>("Cannot perform fatality in current match state", ErrorType.Validation);
            }

            _logger.LogInformation(
                "Performed fatality {FatalityName} for character {CharacterId} in match {MatchId}",
                fatalityName, characterId, matchId);

            return Result.Success(new OpenMKFatalityResult(
                Success: true,
                AnimationPlayed: fatality.AnimationSequence,
                SoundPlayed: fatality.SoundEffect,
                VoiceLinePlayed: fatality.VoiceLine,
                MatchEnded: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform fatality {FatalityName} for character {CharacterId}", fatalityName, characterId);
            return Result.Failure<OpenMKFatalityResult>("Failed to perform fatality", ErrorType.Internal);
        }
    }

    public async Task<Result<bool>> CanPerformFatalityAsync(
        Guid matchId, Guid characterId, string fatalityName, CancellationToken ct = default)
    {
        try
        {
            var matchState = await _matchStateRepository.GetByMatchIdAsync(matchId, ct);
            if (matchState == null)
            {
                return Result.Success(false);
            }

            // Check if match is in a state where fatalities can be performed
            // (e.g., opponent health is 0, round is in finish him/her phase)
            var isPlayer1 = matchState.Player1CharacterId == characterId;
            var opponentHealth = isPlayer1 ? matchState.Player2Health : matchState.Player1Health;

            // Fatality can be performed if opponent health is 0 and match is in appropriate phase
            var canPerform = opponentHealth <= 0 && matchState.Phase == OpenMKMatchPhase.Finisher;

            return Result.Success(canPerform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check fatality availability for match {MatchId}", matchId);
            return Result.Failure<bool>("Failed to check fatality availability", ErrorType.Internal);
        }
    }
}
