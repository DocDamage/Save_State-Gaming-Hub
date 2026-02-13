namespace SaveState.Application.Mugen.Services.MatchAnalytics;

/// <summary>
/// Calculated statistics for a player across multiple matches.
/// </summary>
public record CalculatedStats(
    Guid PlayerId,
    int TotalMatches,
    int Wins,
    int Losses,
    decimal WinRate,
    decimal AverageDamageDealt,
    decimal AverageDamageReceived,
    decimal AverageComboLength,
    decimal ConsistencyScore,
    IReadOnlyDictionary<StatType, decimal> StatValues);

/// <summary>
/// Aggregated statistics across multiple players or time periods.
/// </summary>
public record AggregateStats(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalMatches,
    IReadOnlyDictionary<string, CharacterStats> CharacterPerformance,
    IReadOnlyDictionary<StatType, decimal> AverageStats,
    IReadOnlyList<TrendPoint> Trends);

/// <summary>
/// Character-specific statistics.
/// </summary>
public record CharacterStats(
    string CharacterName,
    int MatchesPlayed,
    int Wins,
    int Losses,
    decimal WinRate,
    int TotalDamageDealt,
    decimal AverageComboLength,
    IReadOnlyList<string> MostUsedMoves);

/// <summary>
/// Single data point for trend analysis.
/// </summary>
public record TrendPoint(
    DateTime Date,
    decimal Value,
    string Context);

/// <summary>
/// Performance comparison between two players or time periods.
/// </summary>
public record PerformanceComparison(
    string BaselineLabel,
    string ComparisonLabel,
    IReadOnlyDictionary<StatType, StatComparison> Comparisons);

/// <summary>
/// Comparison for a single statistic.
/// </summary>
public record StatComparison(
    decimal BaselineValue,
    decimal ComparisonValue,
    decimal Difference,
    decimal PercentageChange);

/// <summary>
/// Achievement data for player milestones.
/// </summary>
public record AchievementData(
    string Name,
    string Description,
    DateTime UnlockedAt,
    AchievementRarity Rarity);

/// <summary>
/// Achievement rarity levels.
/// </summary>
public enum AchievementRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// Player ranking information.
/// </summary>
public record RankingInfo(
    int GlobalRank,
    int RegionalRank,
    int Rating,
    string Tier,
    IReadOnlyList<RankedStats> RankedStats);

/// <summary>
/// Ranked statistics for a specific game mode.
/// </summary>
public record RankedStats(
    string GameMode,
    int Rank,
    int Rating,
    int Wins,
    int Losses,
    decimal WinRate);

/// <summary>
/// Internal character statistics for calculations.
/// </summary>
internal class CharacterStatistics
{
    public Guid PlayerId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int TotalDamageDealt { get; set; }
    public int TotalDamageReceived { get; set; }
    public int TotalCombos { get; set; }
    public int LongestCombo { get; set; }
}
