using System.Collections.Generic;

namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Strategies for automatically balancing moves.
/// </summary>
public enum BalanceStrategy
{
    Conservative,
    Aggressive,
    CharacterSpecific,
    TournamentStandard
}

/// <summary>
/// Parameters supplied to balancing routines.
/// </summary>
public sealed record BalanceParameters
{
    public int CharacterHealth { get; init; } = 1000;
    public int CharacterPower { get; init; } = 3000;
    public DifficultyLevel TargetDifficulty { get; init; } = DifficultyLevel.Medium;
    public BalanceStrategy Strategy { get; init; } = BalanceStrategy.Conservative;
    public IReadOnlyDictionary<string, decimal> CustomMultipliers { get; init; } = new Dictionary<string, decimal>();
}
