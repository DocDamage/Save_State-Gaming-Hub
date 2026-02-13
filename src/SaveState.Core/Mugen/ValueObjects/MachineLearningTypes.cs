namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents the confidence level of a prediction or recommendation.
/// </summary>
public enum PredictionConfidence
{
    Low,
    Medium,
    High,
    VeryHigh
}



/// <summary>
/// Represents basic character attributes for ML analysis.
/// </summary>
public record MLCharacterAttributes(
    double Health,
    double Attack,
    double Defense
);

/// <summary>
/// Represents character attributes for game balance and analysis.
/// </summary>
public class CharacterAttributes
{
    public double Health { get; }
    public double Attack { get; }
    public double Defense { get; }

    public CharacterAttributes(double health, double attack, double defense)
    {
        Health = health;
        Attack = attack;
        Defense = defense;
    }
}

/// <summary>
/// Represents a match result used for machine learning.
/// </summary>
public record MLMatchResult(
    string WinnerId,
    string LoserId,
    string WinnerCharacter,
    string LoserCharacter,
    TimeSpan Duration,
    int RoundsWonByWinner,
    int RoundsWonByLoser,
    SaveState.Core.Mugen.Entities.MatchResult Outcome
);
