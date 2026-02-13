using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Core.OpenMK.Services;
using SaveState.Core.OpenMK.ValueObjects;
using SaveState.Infrastructure.OpenMK.Services.OpenMK.Engines;

namespace SaveState.Infrastructure.OpenMK.Services.OpenMK;

/// <summary>
/// Implementation of OpenMK service for Mortal Kombat-style gameplay integration.
/// Acts as a coordinator delegating to specialized engines.
/// </summary>
public class OpenMKService : IOpenMKService
{
    private readonly ILogger<OpenMKService> _logger;
    private readonly CharacterEngine _characterEngine;
    private readonly ProgressionEngine _progressionEngine;
    private readonly FatalityEngine _fatalityEngine;
    private readonly KombatEngine _kombatEngine;

    public OpenMKService(
        IOpenMKCharacterRepository characterRepository,
        IOpenMKProgressRepository progressRepository,
        IOpenMKMatchStateRepository matchStateRepository,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OpenMKService>();
        _characterEngine = new CharacterEngine(characterRepository, loggerFactory.CreateLogger<CharacterEngine>());
        _progressionEngine = new ProgressionEngine(progressRepository, loggerFactory.CreateLogger<ProgressionEngine>());
        _fatalityEngine = new FatalityEngine(characterRepository, matchStateRepository, loggerFactory.CreateLogger<FatalityEngine>());
        _kombatEngine = new KombatEngine(characterRepository, matchStateRepository, loggerFactory.CreateLogger<KombatEngine>());
    }

    #region Character Operations

    public Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersAsync(CancellationToken ct = default)
        => _characterEngine.GetCharactersAsync(ct);

    public Task<Result<OpenMKCharacter>> GetCharacterAsync(Guid characterId, CancellationToken ct = default)
        => _characterEngine.GetCharacterAsync(characterId, ct);

    public Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByRealmAsync(OpenMKRealm realm, CancellationToken ct = default)
        => _characterEngine.GetCharactersByRealmAsync(realm, ct);

    public Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByFightingStyleAsync(OpenMKFightingStyle style, CancellationToken ct = default)
        => _characterEngine.GetCharactersByFightingStyleAsync(style, ct);

    public Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByAlignmentAsync(OpenMKAlignment alignment, CancellationToken ct = default)
        => _characterEngine.GetCharactersByAlignmentAsync(alignment, ct);

    public Task<Result<IReadOnlyList<OpenMKCostume>>> GetCharacterCostumesAsync(Guid characterId, CancellationToken ct = default)
        => _characterEngine.GetCharacterCostumesAsync(characterId, ct);

    public Task<Result<string>> GetCharacterEndingAsync(Guid characterId, CancellationToken ct = default)
        => _characterEngine.GetCharacterEndingAsync(characterId, ct);

    #endregion

    #region Progression Operations

    public Task<Result<IReadOnlyList<OpenMKCharacter>>> GetUnlockedCharactersAsync(Guid userId, CancellationToken ct = default)
        => _progressionEngine.GetUnlockedCharactersAsync(userId, ct);

    public Task<Result<bool>> IsCharacterUnlockedAsync(Guid userId, Guid characterId, CancellationToken ct = default)
        => _progressionEngine.IsCharacterUnlockedAsync(userId, characterId, ct);

    public Task<Result> UnlockCharacterAsync(Guid userId, Guid characterId, CancellationToken ct = default)
        => _progressionEngine.UnlockCharacterAsync(userId, characterId, ct);

    #endregion

    #region Fatality Operations

    public Task<Result<IReadOnlyList<OpenMKFatality>>> GetCharacterFatalitiesAsync(Guid characterId, CancellationToken ct = default)
        => _fatalityEngine.GetCharacterFatalitiesAsync(characterId, ct);

    public Task<Result<OpenMKFatalityResult>> PerformFatalityAsync(Guid matchId, Guid characterId, string fatalityName, CancellationToken ct = default)
        => _fatalityEngine.PerformFatalityAsync(matchId, characterId, fatalityName, ct);

    public Task<Result<bool>> CanPerformFatalityAsync(Guid matchId, Guid characterId, string fatalityName, CancellationToken ct = default)
        => _fatalityEngine.CanPerformFatalityAsync(matchId, characterId, fatalityName, ct);

    #endregion

    #region Kombat Operations

    public Task<Result<IReadOnlyList<OpenMKSpecialMove>>> GetCharacterSpecialMovesAsync(Guid characterId, CancellationToken ct = default)
        => _kombatEngine.GetCharacterSpecialMovesAsync(characterId, ct);

    public Task<Result<OpenMKSpecialMoveResult>> PerformSpecialMoveAsync(Guid matchId, Guid characterId, string specialMoveName, CancellationToken ct = default)
        => _kombatEngine.PerformSpecialMoveAsync(matchId, characterId, specialMoveName, ct);

    public Task<Result<bool>> CanPerformSpecialMoveAsync(Guid matchId, Guid characterId, string specialMoveName, CancellationToken ct = default)
        => _kombatEngine.CanPerformSpecialMoveAsync(matchId, characterId, specialMoveName, ct);

    public Task<Result<int>> GetSuperBarLevelAsync(Guid matchId, Guid characterId, CancellationToken ct = default)
        => _kombatEngine.GetSuperBarLevelAsync(matchId, characterId, ct);

    public Task<Result> IncreaseSuperBarAsync(Guid matchId, Guid characterId, int amount, CancellationToken ct = default)
        => _kombatEngine.IncreaseSuperBarAsync(matchId, characterId, amount, ct);

    public Task<Result> SetCharacterCostumeAsync(Guid matchId, Guid characterId, string costumeName, CancellationToken ct = default)
        => _kombatEngine.SetCharacterCostumeAsync(matchId, characterId, costumeName, ct);

    #endregion
}
