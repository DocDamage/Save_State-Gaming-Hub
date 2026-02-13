namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents the results of a character balance analysis.
/// Provides insights into character strengths, weaknesses, and balance recommendations.
/// </summary>
public sealed class CharacterBalanceAnalysis
{
    /// <summary>
    /// Gets the character identifier.
    /// </summary>
    public Guid CharacterId { get; init; }

    /// <summary>
    /// Gets the character name.
    /// </summary>
    public string CharacterName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the overall balance score (0-100, where 50 is perfectly balanced).
    /// </summary>
    public int BalanceScore { get; init; }

    /// <summary>
    /// Gets the tier rating (S, A, B, C, D, F).
    /// </summary>
    public string TierRating { get; init; } = "C";

    /// <summary>
    /// Gets the offensive power rating (0-100).
    /// </summary>
    public int OffensivePower { get; init; }

    /// <summary>
    /// Gets the defensive capability rating (0-100).
    /// </summary>
    public int DefensiveCapability { get; init; }

    /// <summary>
    /// Gets the mobility rating (0-100).
    /// </summary>
    public int Mobility { get; init; }

    /// <summary>
    /// Gets the combo potential rating (0-100).
    /// </summary>
    public int ComboPotential { get; init; }

    /// <summary>
    /// Gets the zoning capability rating (0-100).
    /// </summary>
    public int ZoningCapability { get; init; }

    /// <summary>
    /// Gets the identified strengths.
    /// </summary>
    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the identified weaknesses.
    /// </summary>
    public IReadOnlyList<string> Weaknesses { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the balance recommendations.
    /// </summary>
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the comparison to average character stats.
    /// </summary>
    public string ComparisonToAverage { get; init; } = "Average";

    /// <summary>
    /// Gets the win rate prediction against average opponents (0-100%).
    /// </summary>
    public double PredictedWinRate { get; init; }

    /// <summary>
    /// Gets the analysis timestamp.
    /// </summary>
    public DateTimeOffset AnalyzedAt { get; init; }

    /// <summary>
    /// Gets the detailed move analysis results.
    /// </summary>
    public IReadOnlyList<MoveAnalysis> MoveAnalyses { get; init; } = Array.Empty<MoveAnalysis>();

    /// <summary>
    /// Gets whether the character is considered overpowered.
    /// </summary>
    public bool IsOverpowered => BalanceScore > 70;

    /// <summary>
    /// Gets whether the character is considered underpowered.
    /// </summary>
    public bool IsUnderpowered => BalanceScore < 30;

    /// <summary>
    /// Gets whether the character is well-balanced.
    /// </summary>
    public bool IsBalanced => BalanceScore >= 40 && BalanceScore <= 60;
}

/// <summary>
/// Represents analysis results for a single move.
/// </summary>
public sealed class MoveAnalysis
{
    /// <summary>
    /// Gets the move name.
    /// </summary>
    public string MoveName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the move type (e.g., "Normal", "Special", "Super").
    /// </summary>
    public string MoveType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the effectiveness rating (0-100).
    /// </summary>
    public int Effectiveness { get; init; }

    /// <summary>
    /// Gets the risk/reward ratio.
    /// </summary>
    public double RiskRewardRatio { get; init; }

    /// <summary>
    /// Gets specific issues with this move.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets suggested improvements for this move.
    /// </summary>
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
}
