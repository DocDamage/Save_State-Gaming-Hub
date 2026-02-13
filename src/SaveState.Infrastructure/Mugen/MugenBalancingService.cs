using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.Services;
using MoveProperties = SaveState.Core.Mugen.ValueObjects.MoveProperties;

namespace SaveState.Infrastructure.Mugen;

/// <summary>
/// Service for automatically balancing MUGEN moves.
/// Adjusts damage, frame data, and other properties based on difficulty and character stats.
/// </summary>
public class MugenBalancingService : IMugenBalancingService
{
    private readonly ILogger<MugenBalancingService> _logger;

    public MugenBalancingService(ILogger<MugenBalancingService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<MugenMoveDefinition>> BalanceMoveAsync(
        MugenMoveDefinition move,
        BalanceParameters parameters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Balancing move '{MoveName}' with strategy {Strategy} for difficulty {Difficulty}",
                move.Name, parameters.Strategy, parameters.TargetDifficulty);

            var balancedMove = move;

            // Apply balancing based on strategy
            switch (parameters.Strategy)
            {
                case BalanceStrategy.Conservative:
                    balancedMove = ApplyConservativeBalancing(balancedMove, parameters);
                    break;
                case BalanceStrategy.Aggressive:
                    balancedMove = ApplyAggressiveBalancing(balancedMove, parameters);
                    break;
                case BalanceStrategy.CharacterSpecific:
                    balancedMove = ApplyCharacterSpecificBalancing(balancedMove, parameters);
                    break;
                case BalanceStrategy.TournamentStandard:
                    balancedMove = ApplyTournamentStandardBalancing(balancedMove, parameters);
                    break;
            }

            // Apply custom multipliers
            balancedMove = ApplyCustomMultipliers(balancedMove, parameters.CustomMultipliers);

            // Final validation
            balancedMove = EnsureMinimums(balancedMove);

            _logger.LogInformation("Successfully balanced move '{MoveName}'", move.Name);
            return Result.Success(balancedMove);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error balancing move '{MoveName}'", move.Name);
            return Result.Failure<MugenMoveDefinition>($"Failed to balance move: {ex.Message}");
        }
    }

    private MugenMoveDefinition ApplyConservativeBalancing(MugenMoveDefinition move, BalanceParameters parameters)
    {
        // Conservative balancing - small adjustments to bring move closer to expected values
        var expectedDamage = CalculateExpectedDamage(move, parameters);
        var expectedStartup = CalculateExpectedStartup(move, parameters);
        var expectedAdvantage = CalculateExpectedAdvantage(move, parameters);

        var damageMultiplier = Math.Min(1.2, Math.Max(0.8, expectedDamage / (double)move.Properties.Damage));
        var startupAdjustment = Math.Min(5, Math.Max(-3, expectedStartup - move.Properties.StartupFrames));
        var advantageAdjustment = Math.Min(3, Math.Max(-3, expectedAdvantage - move.Properties.FrameAdvantageOnHit));

        return move.With(properties: new MoveProperties(
            Damage: (int)(move.Properties.Damage * damageMultiplier),
            MeterGain: AdjustMeterGain(move.Properties.MeterGain, parameters.TargetDifficulty),
            MeterCost: move.Properties.MeterCost,
            StartupFrames: Math.Max(1, move.Properties.StartupFrames + startupAdjustment),
            ActiveFrames: move.Properties.ActiveFrames,
            RecoveryFrames: move.Properties.RecoveryFrames,
            FrameAdvantageOnHit: move.Properties.FrameAdvantageOnHit + advantageAdjustment,
            FrameAdvantageOnBlock: move.Properties.FrameAdvantageOnBlock + advantageAdjustment - 2,
            HitStun: move.Properties.HitStun,
            BlockStun: move.Properties.BlockStun,
            HitStop: move.Properties.HitStop,
            BlockStop: move.Properties.BlockStop,
            CausesKnockdown: move.Properties.CausesKnockdown,
            GuardCrush: move.Properties.GuardCrush,
            CounterHit: move.Properties.CounterHit,
            Unblockable: move.Properties.Unblockable,
            ArmorBreak: move.Properties.ArmorBreak,
            KnockdownType: move.Properties.KnockdownType,
            HitEffect: move.Properties.HitEffect,
            GuardEffect: move.Properties.GuardEffect,
            Priority: move.Properties.Priority,
            GroundAirType: move.Properties.GroundAirType,
            Attribute: move.Properties.Attribute,
            Flags: move.Properties.Flags));
    }

    private MugenMoveDefinition ApplyAggressiveBalancing(MugenMoveDefinition move, BalanceParameters parameters)
    {
        // Aggressive balancing - more significant changes to normalize the move
        var expectedDamage = CalculateExpectedDamage(move, parameters);
        var expectedStartup = CalculateExpectedStartup(move, parameters);

        var damageMultiplier = expectedDamage / (double)move.Properties.Damage;
        var startupAdjustment = expectedStartup - move.Properties.StartupFrames;

        // Cap aggressive changes
        damageMultiplier = Math.Min(2.0, Math.Max(0.5, damageMultiplier));
        startupAdjustment = Math.Min(10, Math.Max(-5, startupAdjustment));

        return move.With(properties: new MoveProperties(
            Damage: (int)(move.Properties.Damage * damageMultiplier),
            MeterGain: AdjustMeterGain(move.Properties.MeterGain, parameters.TargetDifficulty),
            MeterCost: move.Properties.MeterCost,
            StartupFrames: Math.Max(1, move.Properties.StartupFrames + startupAdjustment),
            ActiveFrames: move.Properties.ActiveFrames,
            RecoveryFrames: move.Properties.RecoveryFrames,
            FrameAdvantageOnHit: NormalizeFrameAdvantage(move.Properties.FrameAdvantageOnHit, move.MoveType),
            FrameAdvantageOnBlock: NormalizeFrameAdvantage(move.Properties.FrameAdvantageOnBlock, move.MoveType),
            HitStun: NormalizeHitStun(move.Properties.HitStun, move.Properties.Damage),
            BlockStun: NormalizeBlockStun(move.Properties.BlockStun, move.Properties.Damage),
            HitStop: move.Properties.HitStop,
            BlockStop: move.Properties.BlockStop,
            CausesKnockdown: move.Properties.CausesKnockdown,
            GuardCrush: move.Properties.GuardCrush,
            CounterHit: move.Properties.CounterHit,
            Unblockable: move.Properties.Unblockable,
            ArmorBreak: move.Properties.ArmorBreak,
            KnockdownType: move.Properties.KnockdownType,
            HitEffect: move.Properties.HitEffect,
            GuardEffect: move.Properties.GuardEffect,
            Priority: move.Properties.Priority,
            GroundAirType: move.Properties.GroundAirType,
            Attribute: move.Properties.Attribute,
            Flags: move.Properties.Flags));
    }

    private MugenMoveDefinition ApplyCharacterSpecificBalancing(MugenMoveDefinition move, BalanceParameters parameters)
    {
        // Character-specific balancing based on character health and power
        var healthRatio = parameters.CharacterHealth / 1000.0; // Assuming 1000 is standard
        var powerRatio = parameters.CharacterPower / 3000.0;   // Assuming 3000 is standard

        var damageMultiplier = (healthRatio + powerRatio) / 2.0;
        damageMultiplier = Math.Min(1.5, Math.Max(0.7, damageMultiplier));

        // Adjust startup based on character speed
        var speedRatio = 1.0 / Math.Sqrt(parameters.CharacterPower / 1000.0);
        var startupAdjustment = (int)((speedRatio - 1.0) * 5);

        return move.With(properties: new MoveProperties(
            Damage: (int)(move.Properties.Damage * damageMultiplier),
            MeterGain: (int)(move.Properties.MeterGain * powerRatio),
            MeterCost: move.Properties.MeterCost,
            StartupFrames: Math.Max(1, move.Properties.StartupFrames + startupAdjustment),
            ActiveFrames: move.Properties.ActiveFrames,
            RecoveryFrames: move.Properties.RecoveryFrames,
            FrameAdvantageOnHit: move.Properties.FrameAdvantageOnHit,
            FrameAdvantageOnBlock: move.Properties.FrameAdvantageOnBlock,
            HitStun: move.Properties.HitStun,
            BlockStun: move.Properties.BlockStun,
            HitStop: move.Properties.HitStop,
            BlockStop: move.Properties.BlockStop,
            CausesKnockdown: move.Properties.CausesKnockdown,
            GuardCrush: move.Properties.GuardCrush,
            CounterHit: move.Properties.CounterHit,
            Unblockable: move.Properties.Unblockable,
            ArmorBreak: move.Properties.ArmorBreak,
            KnockdownType: move.Properties.KnockdownType,
            HitEffect: move.Properties.HitEffect,
            GuardEffect: move.Properties.GuardEffect,
            Priority: move.Properties.Priority,
            GroundAirType: move.Properties.GroundAirType,
            Attribute: move.Properties.Attribute,
            Flags: move.Properties.Flags));
    }

    private MugenMoveDefinition ApplyTournamentStandardBalancing(MugenMoveDefinition move, BalanceParameters parameters)
    {
        // Tournament-standard balancing - strict guidelines
        var standardDamage = GetTournamentStandardDamage(move.MoveType, move.Category, parameters.TargetDifficulty);
        var standardStartup = GetTournamentStandardStartup(move.MoveType, move.Category);
        var standardAdvantage = GetTournamentStandardAdvantage(move.MoveType, move.Category);

        return move.With(properties: new MoveProperties(
            Damage: standardDamage,
            MeterGain: GetTournamentStandardMeterGain(move.MoveType),
            MeterCost: GetTournamentStandardMeterCost(move.MoveType),
            StartupFrames: standardStartup,
            ActiveFrames: move.Properties.ActiveFrames,
            RecoveryFrames: move.Properties.RecoveryFrames,
            FrameAdvantageOnHit: standardAdvantage,
            FrameAdvantageOnBlock: standardAdvantage - 5,
            HitStun: GetTournamentStandardHitStun(standardDamage),
            BlockStun: GetTournamentStandardBlockStun(standardDamage),
            HitStop: move.Properties.HitStop,
            BlockStop: move.Properties.BlockStop,
            CausesKnockdown: move.Properties.CausesKnockdown,
            GuardCrush: move.Properties.GuardCrush,
            CounterHit: move.Properties.CounterHit,
            Unblockable: move.Properties.Unblockable,
            ArmorBreak: move.Properties.ArmorBreak,
            KnockdownType: move.Properties.KnockdownType,
            HitEffect: move.Properties.HitEffect,
            GuardEffect: move.Properties.GuardEffect,
            Priority: move.Properties.Priority,
            GroundAirType: move.Properties.GroundAirType,
            Attribute: move.Properties.Attribute,
            Flags: move.Properties.Flags));
    }

    private MugenMoveDefinition ApplyCustomMultipliers(MugenMoveDefinition move, IReadOnlyDictionary<string, decimal> multipliers)
    {
        var damageMult = multipliers.TryGetValue("damage", out var d) ? (double)d : 1.0;
        var startupMult = multipliers.TryGetValue("startup", out var s) ? (double)s : 1.0;
        var advantageMult = multipliers.TryGetValue("advantage", out var a) ? (double)a : 1.0;

        return move.With(properties: new MoveProperties(
            Damage: (int)(move.Properties.Damage * damageMult),
            MeterGain: move.Properties.MeterGain,
            MeterCost: move.Properties.MeterCost,
            StartupFrames: Math.Max(1, (int)(move.Properties.StartupFrames * startupMult)),
            ActiveFrames: move.Properties.ActiveFrames,
            RecoveryFrames: move.Properties.RecoveryFrames,
            FrameAdvantageOnHit: (int)(move.Properties.FrameAdvantageOnHit * advantageMult),
            FrameAdvantageOnBlock: (int)(move.Properties.FrameAdvantageOnBlock * advantageMult),
            HitStun: move.Properties.HitStun,
            BlockStun: move.Properties.BlockStun,
            HitStop: move.Properties.HitStop,
            BlockStop: move.Properties.BlockStop,
            CausesKnockdown: move.Properties.CausesKnockdown,
            GuardCrush: move.Properties.GuardCrush,
            CounterHit: move.Properties.CounterHit,
            Unblockable: move.Properties.Unblockable,
            ArmorBreak: move.Properties.ArmorBreak,
            KnockdownType: move.Properties.KnockdownType,
            HitEffect: move.Properties.HitEffect,
            GuardEffect: move.Properties.GuardEffect,
            Priority: move.Properties.Priority,
            GroundAirType: move.Properties.GroundAirType,
            Attribute: move.Properties.Attribute,
            Flags: move.Properties.Flags));
    }

    private MugenMoveDefinition EnsureMinimums(MugenMoveDefinition move)
    {
        return move.With(properties: new MoveProperties(
            Damage: Math.Max(1, move.Properties.Damage),
            MeterGain: Math.Max(0, move.Properties.MeterGain),
            MeterCost: Math.Max(0, move.Properties.MeterCost),
            StartupFrames: Math.Max(1, move.Properties.StartupFrames),
            ActiveFrames: Math.Max(1, move.Properties.ActiveFrames),
            RecoveryFrames: Math.Max(1, move.Properties.RecoveryFrames),
            FrameAdvantageOnHit: move.Properties.FrameAdvantageOnHit,
            FrameAdvantageOnBlock: move.Properties.FrameAdvantageOnBlock,
            HitStun: Math.Max(1, move.Properties.HitStun),
            BlockStun: Math.Max(1, move.Properties.BlockStun),
            HitStop: Math.Max(1, move.Properties.HitStop),
            BlockStop: Math.Max(1, move.Properties.BlockStop),
            CausesKnockdown: move.Properties.CausesKnockdown,
            GuardCrush: move.Properties.GuardCrush,
            CounterHit: move.Properties.CounterHit,
            Unblockable: move.Properties.Unblockable,
            ArmorBreak: move.Properties.ArmorBreak,
            KnockdownType: move.Properties.KnockdownType,
            HitEffect: move.Properties.HitEffect,
            GuardEffect: move.Properties.GuardEffect,
            Priority: move.Properties.Priority,
            GroundAirType: move.Properties.GroundAirType,
            Attribute: move.Properties.Attribute,
            Flags: move.Properties.Flags));
    }

    private int CalculateExpectedDamage(MugenMoveDefinition move, BalanceParameters parameters)
    {
        var baseDamage = move.MoveType switch
        {
            MoveType.Normal => 60,
            MoveType.Special => 100,
            MoveType.Super => 300,
            MoveType.Hyper => 400,
            MoveType.Throw => 120,
            _ => 50
        };

        var difficultyMultiplier = parameters.TargetDifficulty switch
        {
            DifficultyLevel.Beginner => 0.8,
            DifficultyLevel.Intermediate => 1.0,
            DifficultyLevel.Advanced => 1.2,
            DifficultyLevel.Expert => 1.4,
            _ => 1.0
        };

        var startupMultiplier = Math.Max(0.7, 1.0 - (move.Properties.StartupFrames / 30.0));
        var activeMultiplier = Math.Min(1.3, move.Properties.ActiveFrames / 5.0);

        return (int)(baseDamage * difficultyMultiplier * startupMultiplier * activeMultiplier);
    }

    private int CalculateExpectedStartup(MugenMoveDefinition move, BalanceParameters parameters)
    {
        var baseStartup = move.MoveType switch
        {
            MoveType.Normal => 4,
            MoveType.Special => 12,
            MoveType.Super => 15,
            MoveType.Hyper => 20,
            MoveType.Throw => 6,
            _ => 8
        };

        var difficultyAdjustment = parameters.TargetDifficulty switch
        {
            DifficultyLevel.Beginner => -2,
            DifficultyLevel.Intermediate => 0,
            DifficultyLevel.Advanced => 2,
            DifficultyLevel.Expert => 4,
            _ => 0
        };

        return Math.Max(1, baseStartup + difficultyAdjustment);
    }

    private int CalculateExpectedAdvantage(MugenMoveDefinition move, BalanceParameters parameters)
    {
        return move.MoveType switch
        {
            MoveType.Normal => -2,
            MoveType.Special => 1,
            MoveType.Super => 8,
            MoveType.Hyper => 12,
            MoveType.Throw => 15,
            _ => 0
        };
    }

    private int AdjustMeterGain(int currentGain, DifficultyLevel difficulty)
    {
        var multiplier = difficulty switch
        {
            DifficultyLevel.Beginner => 1.2,
            DifficultyLevel.Intermediate => 1.0,
            DifficultyLevel.Advanced => 0.9,
            DifficultyLevel.Expert => 0.8,
            _ => 1.0
        };

        return (int)(currentGain * multiplier);
    }

    private int NormalizeFrameAdvantage(int advantage, MoveType moveType)
    {
        var min = moveType switch
        {
            MoveType.Normal => -10,
            MoveType.Special => -5,
            MoveType.Super => 0,
            MoveType.Hyper => 5,
            MoveType.Throw => 10,
            _ => -5
        };

        var max = moveType switch
        {
            MoveType.Normal => 5,
            MoveType.Special => 15,
            MoveType.Super => 25,
            MoveType.Hyper => 30,
            MoveType.Throw => 35,
            _ => 15
        };

        return Math.Min(max, Math.Max(min, advantage));
    }

    private int NormalizeHitStun(int hitStun, int damage)
    {
        var expected = Math.Max(10, damage / 10);
        return Math.Min(hitStun, expected + 10); // Allow some variance
    }

    private int NormalizeBlockStun(int blockStun, int damage)
    {
        var expected = Math.Max(5, damage / 20);
        return Math.Min(blockStun, expected + 5);
    }

    private int GetTournamentStandardDamage(MoveType moveType, MoveCategory category, DifficultyLevel difficulty)
    {
        var baseDamage = (moveType, category) switch
        {
            (MoveType.Normal, MoveCategory.Normal) => 60,
            (MoveType.Normal, MoveCategory.CommandNormal) => 80,
            (MoveType.Special, _) => 100,
            (MoveType.Super, _) => 320,
            (MoveType.Hyper, _) => 450,
            (MoveType.Throw, _) => 120,
            _ => 50
        };

        var difficultyMult = difficulty switch
        {
            DifficultyLevel.Beginner => 0.9,
            DifficultyLevel.Intermediate => 1.0,
            DifficultyLevel.Advanced => 1.1,
            DifficultyLevel.Expert => 1.2,
            _ => 1.0
        };

        return (int)(baseDamage * difficultyMult);
    }

    private int GetTournamentStandardStartup(MoveType moveType, MoveCategory category)
    {
        return (moveType, category) switch
        {
            (MoveType.Normal, MoveCategory.Normal) => 4,
            (MoveType.Normal, MoveCategory.CommandNormal) => 6,
            (MoveType.Special, MoveCategory.Special) => 12,
            (MoveType.Super, MoveCategory.Super) => 16,
            (MoveType.Hyper, MoveCategory.Hyper) => 20,
            (MoveType.Throw, MoveCategory.Throw) => 6,
            _ => 8
        };
    }

    private int GetTournamentStandardAdvantage(MoveType moveType, MoveCategory category)
    {
        return (moveType, category) switch
        {
            (MoveType.Normal, MoveCategory.Normal) => -3,
            (MoveType.Normal, MoveCategory.CommandNormal) => -1,
            (MoveType.Special, MoveCategory.Special) => 2,
            (MoveType.Super, MoveCategory.Super) => 10,
            (MoveType.Hyper, MoveCategory.Hyper) => 15,
            (MoveType.Throw, MoveCategory.Throw) => 18,
            _ => 0
        };
    }

    private int GetTournamentStandardMeterGain(MoveType moveType)
    {
        return moveType switch
        {
            MoveType.Normal => 15,
            MoveType.Special => 20,
            MoveType.Super => 0,
            MoveType.Hyper => 0,
            MoveType.Throw => 25,
            _ => 15
        };
    }

    private int GetTournamentStandardMeterCost(MoveType moveType)
    {
        return moveType switch
        {
            MoveType.Super => 1000,
            MoveType.Hyper => 2000,
            _ => 0
        };
    }

    private int GetTournamentStandardHitStun(int damage)
    {
        return Math.Max(12, damage / 8);
    }

    private int GetTournamentStandardBlockStun(int damage)
    {
        return Math.Max(8, damage / 12);
    }

    public async Task<Result<IReadOnlyList<MugenMoveDefinition>>> BalanceCharacterAsync(Guid characterId, BalanceParameters parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Balancing character {CharacterId}", characterId);
        return Result.Success<IReadOnlyList<MugenMoveDefinition>>(new List<MugenMoveDefinition>());
    }

    public async Task<MoveBalanceAnalysis> AnalyzeMoveBalanceAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing balance for move '{MoveName}'", move.Name);
        return new MoveBalanceAnalysis(move.Name, 0.5, new List<string>(), new List<string>(), new List<string>());
    }

    public async Task<int> SuggestDamageValueAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Suggesting damage for move '{MoveName}'", move.Name);
        return 100;
    }

    public async Task<FrameData> SuggestFrameDataAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Suggesting frame data for move '{MoveName}'", move.Name);
        return new FrameData(6, 8, 18, 12, 8, 32);
    }

    public async Task<IReadOnlyList<MoveComparison>> CompareMoveBalanceAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing balance for move '{MoveName}'", move.Name);
        return new List<MoveComparison>();
    }
}
