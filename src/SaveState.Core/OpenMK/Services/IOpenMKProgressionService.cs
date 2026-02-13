using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Core.OpenMK.Services;

/// <summary>
/// Service for managing OpenMK character progression, unlocks, and rewards.
/// </summary>
public interface IOpenMKProgressionService
{
    /// <summary>
    /// Gets the progression profile for a user.
    /// </summary>
    Task<Result<OpenMKProgressionProfile>> GetProgressionProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Awards experience points to a character.
    /// </summary>
    Task<Result> AwardExperienceAsync(Guid userId, Guid characterId, int experiencePoints, CancellationToken ct = default);

    /// <summary>
    /// Levels up a character if enough experience is available.
    /// </summary>
    Task<Result<OpenMKLevelUpResult>> LevelUpCharacterAsync(Guid userId, Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Awards koins to a user.
    /// </summary>
    Task<Result> AwardKoinsAsync(Guid userId, int amount, OpenMKKoinSource source, CancellationToken ct = default);

    /// <summary>
    /// Spends koins from a user's account.
    /// </summary>
    Task<Result> SpendKoinsAsync(Guid userId, int amount, OpenMKKoinSpendReason reason, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can afford a purchase.
    /// </summary>
    Task<Result<bool>> CanAffordPurchaseAsync(Guid userId, int cost, CancellationToken ct = default);

    /// <summary>
    /// Unlocks content for a user based on progression.
    /// </summary>
    Task<Result<OpenMKUnlockResult>> UnlockContentAsync(Guid userId, OpenMKUnlockType unlockType, CancellationToken ct = default);

    /// <summary>
    /// Gets available unlocks for a user.
    /// </summary>
    Task<Result<IReadOnlyList<OpenMKAvailableUnlock>>> GetAvailableUnlocksAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Processes match rewards (experience, koins, unlocks).
    /// </summary>
    Task<Result<OpenMKMatchRewards>> ProcessMatchRewardsAsync(Guid userId, Guid characterId, OpenMKMatchResult matchResult, CancellationToken ct = default);

    /// <summary>
    /// Gets character statistics and progression.
    /// </summary>
    Task<Result<OpenMKCharacterProgression>> GetCharacterProgressionAsync(Guid userId, Guid characterId, CancellationToken ct = default);
}

/// <summary>
/// User's OpenMK progression profile.
/// </summary>
public record OpenMKProgressionProfile(
    Guid UserId,
    int Level,
    int ExperiencePoints,
    int ExperienceToNextLevel,
    int TotalKoins,
    int TotalMatchesPlayed,
    int TotalWins,
    int TotalLosses,
    decimal WinRate,
    IReadOnlyList<OpenMKCharacterProgression> CharacterProgressions,
    IReadOnlyList<OpenMKUnlock> UnlockedContent);

/// <summary>
/// Character-specific progression data.
/// </summary>
public record OpenMKCharacterProgression(
    Guid CharacterId,
    string CharacterName,
    int Level,
    int ExperiencePoints,
    int ExperienceToNextLevel,
    int MatchesPlayed,
    int Wins,
    int Losses,
    decimal WinRate,
    IReadOnlyList<OpenMKCharacterStat> Stats);

/// <summary>
/// Character statistics.
/// </summary>
public record OpenMKCharacterStat(
    string StatName,
    int Value,
    DateTime LastUpdated);

/// <summary>
/// Result of leveling up a character.
/// </summary>
public record OpenMKLevelUpResult(
    bool LeveledUp,
    int NewLevel,
    int ExperienceRemaining,
    IReadOnlyList<OpenMKLevelUpReward> Rewards);

/// <summary>
/// Rewards granted on level up.
/// </summary>
public record OpenMKLevelUpReward(
    OpenMKLevelUpRewardType Type,
    string Description,
    int? KoinAmount,
    Guid? UnlockedContentId);

/// <summary>
/// Types of level up rewards.
/// </summary>
public enum OpenMKLevelUpRewardType
{
    /// <summary>
    /// Koins reward.
    /// </summary>
    Koins,

    /// <summary>
    /// New move unlocked.
    /// </summary>
    MoveUnlock,

    /// <summary>
    /// New costume unlocked.
    /// </summary>
    CostumeUnlock,

    /// <summary>
    /// Character ability upgrade.
    /// </summary>
    AbilityUpgrade
}

/// <summary>
/// Sources of koin rewards.
/// </summary>
public enum OpenMKKoinSource
{
    /// <summary>
    /// Won a match.
    /// </summary>
    MatchWin,

    /// <summary>
    /// Performed a fatality.
    /// </summary>
    Fatality,

    /// <summary>
    /// Performed a brutality.
    /// </summary>
    Brutality,

    /// <summary>
    /// Achieved a combo.
    /// </summary>
    ComboBonus,

    /// <summary>
    /// Daily login bonus.
    /// </summary>
    DailyLogin,

    /// <summary>
    /// Level up bonus.
    /// </summary>
    LevelUp,

    /// <summary>
    /// Tournament participation.
    /// </summary>
    Tournament,

    /// <summary>
    /// Achievement reward.
    /// </summary>
    Achievement
}

/// <summary>
/// Reasons for spending koins.
/// </summary>
public enum OpenMKKoinSpendReason
{
    /// <summary>
    /// Purchasing a character.
    /// </summary>
    CharacterPurchase,

    /// <summary>
    /// Purchasing a costume.
    /// </summary>
    CostumePurchase,

    /// <summary>
    /// Purchasing a move upgrade.
    /// </summary>
    MoveUpgrade,

    /// <summary>
    /// Entering a tournament.
    /// </summary>
    TournamentEntry,

    /// <summary>
    /// Purchasing cosmetics.
    /// </summary>
    Cosmetics
}

/// <summary>
/// Result of unlocking content.
/// </summary>
public record OpenMKUnlockResult(
    bool Success,
    OpenMKUnlockType UnlockType,
    Guid? UnlockedContentId,
    string? UnlockedContentName,
    int KoinsSpent);

/// <summary>
/// Available unlock for a user.
/// </summary>
public record OpenMKAvailableUnlock(
    Guid ContentId,
    string Name,
    string Description,
    OpenMKUnlockType Type,
    int Cost,
    OpenMKUnlockRequirements Requirements,
    bool CanAfford,
    bool MeetsRequirements);

/// <summary>
/// Content that has been unlocked.
/// </summary>
public record OpenMKUnlock(
    Guid ContentId,
    string Name,
    OpenMKUnlockType Type,
    DateTime UnlockedAt,
    OpenMKKoinSpendReason? PurchaseReason);

/// <summary>
/// Rewards earned from a match.
/// </summary>
public record OpenMKMatchRewards(
    int ExperienceGained,
    int KoinsGained,
    IReadOnlyList<OpenMKLevelUpReward> LevelUpRewards,
    IReadOnlyList<OpenMKUnlock> NewUnlocks,
    IReadOnlyList<string> AchievementsEarned);