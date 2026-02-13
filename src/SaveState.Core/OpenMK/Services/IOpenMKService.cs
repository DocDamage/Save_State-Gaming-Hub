using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Core.OpenMK.Services;

/// <summary>
/// Main service interface for OpenMK integration with MUGEN/IKEMEN.
/// Provides Mortal Kombat-style gameplay mechanics and character management.
/// </summary>
public interface IOpenMKService
{
    /// <summary>
    /// Gets all available OpenMK characters.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an OpenMK character by ID.
    /// </summary>
    Task<Result<OpenMKCharacter>> GetCharacterAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets characters by realm.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByRealmAsync(OpenMKRealm realm, CancellationToken ct = default);

    /// <summary>
    /// Gets characters by fighting style.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByFightingStyleAsync(OpenMKFightingStyle style, CancellationToken ct = default);

    /// <summary>
    /// Gets characters by alignment.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKCharacter>>> GetCharactersByAlignmentAsync(OpenMKAlignment alignment, CancellationToken ct = default);

    /// <summary>
    /// Gets unlocked characters for a user.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKCharacter>>> GetUnlockedCharactersAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a character is unlocked for a user.
    /// </summary>
    Task<Result<bool>> IsCharacterUnlockedAsync(Guid userId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Unlocks a character for a user.
    /// </summary>
    Task<Result> UnlockCharacterAsync(Guid userId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets available fatalities for a character.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKFatality>>> GetCharacterFatalitiesAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets available special moves for a character.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKSpecialMove>>> GetCharacterSpecialMovesAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Performs a fatality in a match.
    /// </summary>
    Task<Result<OpenMKFatalityResult>> PerformFatalityAsync(
        Guid matchId,
        Guid characterId,
        string fatalityName,
        CancellationToken ct = default);

    /// <summary>
    /// Performs a special move in a match.
    /// </summary>
    Task<Result<OpenMKSpecialMoveResult>> PerformSpecialMoveAsync(
        Guid matchId,
        Guid characterId,
        string specialMoveName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current super bar level for a character in a match.
    /// </summary>
    Task<Result<int>> GetSuperBarLevelAsync(Guid matchId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Increases the super bar level for a character.
    /// </summary>
    Task<Result> IncreaseSuperBarAsync(Guid matchId, Guid characterId, int amount, CancellationToken ct = default);

    /// <summary>
    /// Gets available costumes for a character.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKCostume>>> GetCharacterCostumesAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Sets the active costume for a character in a match.
    /// </summary>
    Task<Result> SetCharacterCostumeAsync(Guid matchId, Guid characterId, string costumeName, CancellationToken ct = default);

    /// <summary>
    /// Gets the character's ending story.
    /// </summary>
    Task<Result<string>> GetCharacterEndingAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Validates if a fatality can be performed in the current match state.
    /// </summary>
    Task<Result<bool>> CanPerformFatalityAsync(Guid matchId, Guid characterId, string fatalityName, CancellationToken ct = default);

    /// <summary>
    /// Validates if a special move can be performed in the current match state.
    /// </summary>
    Task<Result<bool>> CanPerformSpecialMoveAsync(Guid matchId, Guid characterId, string specialMoveName, CancellationToken ct = default);
}

/// <summary>
/// Result of performing a fatality.
/// </summary>
public record OpenMKFatalityResult(
    bool Success,
    string? AnimationPlayed = null,
    string? SoundPlayed = null,
    string? VoiceLinePlayed = null,
    bool MatchEnded = false);

/// <summary>
/// Result of performing a special move.
/// </summary>
public record OpenMKSpecialMoveResult(
    bool Success,
    int DamageDealt,
    string? AnimationPlayed = null,
    string? SoundPlayed = null,
    int SuperBarGained = 0);