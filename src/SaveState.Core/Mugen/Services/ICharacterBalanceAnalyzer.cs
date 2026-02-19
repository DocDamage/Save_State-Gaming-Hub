using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for comprehensive character balance analysis and tier list generation.
/// </summary>
public interface ICharacterBalanceAnalyzer
{
    /// <summary>
    /// Generates a tier list based on match data and win rates.
    /// </summary>
    Task<Result<TierList>> GenerateTierListAsync(
        TierListGenerationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes win rates for a specific character across all matchups.
    /// </summary>
    Task<Result<CharacterWinRates>> AnalyzeCharacterWinRatesAsync(
        string characterName,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets move usage statistics for a character.
    /// </summary>
    Task<Result<MoveUsageStatistics>> GetMoveUsageStatisticsAsync(
        string characterName,
        int? minMatches = null,
        CancellationToken ct = default);

    /// <summary>
    /// Detects potentially overpowered moves or strategies.
    /// </summary>
    Task<Result<OverpoweredDetectionResult>> DetectOverpoweredElementsAsync(
        string? characterName = null,
        double? threshold = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates balance patch suggestions.
    /// </summary>
    Task<Result<BalancePatchSuggestions>> GenerateBalanceSuggestionsAsync(
        BalanceAnalysisRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Compares two characters and provides matchup analysis.
    /// </summary>
    Task<Result<DetailedMatchupAnalysis>> CompareCharactersAsync(
        string character1,
        string character2,
        CancellationToken ct = default);

    /// <summary>
    /// Gets character rankings based on ELO or other metrics.
    /// </summary>
    Task<Result<CharacterRankings>> GetCharacterRankingsAsync(
        RankingCriteria criteria,
        int? topN = null,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes character popularity and usage trends.
    /// </summary>
    Task<Result<PopularityTrends>> AnalyzePopularityTrendsAsync(
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Exports balance analysis to various formats.
    /// </summary>
    Task<Result<string>> ExportAnalysisAsync(
        string analysisId,
        BalanceExportFormat format,
        string outputPath,
        CancellationToken ct = default);
}

/// <summary>
/// Request for tier list generation.
/// </summary>
public record TierListGenerationRequest(
    IReadOnlyList<string>? CharacterNames,
    DateTime? StartDate,
    DateTime? EndDate,
    TierListCriteria Criteria,
    int MinimumMatches,
    bool IncludeMatchupData);

/// <summary>
/// Criteria for tier list generation.
/// </summary>
public enum TierListCriteria
{
    WinRate,
    EloRating,
    TournamentResults,
    MatchupSpread,
    CombinedScore
}

/// <summary>
/// Tier list result.
/// </summary>
public record TierList(
    string Id,
    DateTime GeneratedAt,
    TierListCriteria Criteria,
    int TotalMatchesAnalyzed,
    IReadOnlyList<Tier> Tiers,
    IReadOnlyList<CharacterTierPlacement> Placements);

/// <summary>
/// Individual tier (S, A, B, etc.).
/// </summary>
public record Tier(
    string Name,
    string Description,
    int MinRank,
    int MaxRank,
    IReadOnlyList<string> CharacterNames);

/// <summary>
/// Character placement in tier list.
/// </summary>
public record CharacterTierPlacement(
    string CharacterName,
    string TierName,
    int Rank,
    double Score,
    int MatchesPlayed,
    double WinRate,
    IReadOnlyList<MatchupInfo> KeyMatchups);

/// <summary>
/// Matchup information.
/// </summary>
public record MatchupInfo(
    string OpponentName,
    int Wins,
    int Losses,
    double WinRate);

/// <summary>
/// Character win rates analysis.
/// </summary>
public record CharacterWinRates(
    string CharacterName,
    int TotalMatches,
    int Wins,
    int Losses,
    int Draws,
    double OverallWinRate,
    IReadOnlyList<MatchupWinRate> MatchupWinRates,
    WinRateTrend Trend);

/// <summary>
/// Win rate for a specific matchup.
/// </summary>
public record MatchupWinRate(
    string OpponentName,
    int MatchesPlayed,
    int Wins,
    double WinRate,
    AdvantageLevel Advantage);

/// <summary>
/// Advantage level in matchup.
/// </summary>
public enum AdvantageLevel
{
    HeavilyDisadvantaged, // < 40%
    SlightlyDisadvantaged, // 40-45%
    Even, // 45-55%
    SlightlyAdvantaged, // 55-60%
    HeavilyAdvantaged // > 60%
}

/// <summary>
/// Win rate trend over time.
/// </summary>
public record WinRateTrend(
    double CurrentWinRate,
    double PreviousWeekWinRate,
    double PreviousMonthWinRate,
    TrendDirection Direction);

/// <summary>
/// Move usage statistics.
/// </summary>
public record MoveUsageStatistics(
    string CharacterName,
    int TotalMatchesAnalyzed,
    IReadOnlyList<MoveUsage> MoveUsages,
    IReadOnlyList<string> MostUsedMoves,
    IReadOnlyList<string> LeastUsedMoves,
    IReadOnlyList<string> HighestDamageMoves,
    IReadOnlyList<string> MostEffectiveMoves);

/// <summary>
/// Usage data for a specific move.
/// </summary>
public record MoveUsage(
    string MoveName,
    string Input,
    int TimesUsed,
    double UsageRate,
    int SuccessfulHits,
    double SuccessRate,
    int TotalDamage,
    double AverageDamagePerUse,
    int Knockdowns,
    bool IsOverused,
    bool IsUnderused);

/// <summary>
/// Overpowered elements detection result.
/// </summary>
public record OverpoweredDetectionResult(
    DateTime AnalysisDate,
    double ThresholdUsed,
    IReadOnlyList<OverpoweredCharacter> OverpoweredCharacters,
    IReadOnlyList<OverpoweredMove> OverpoweredMoves,
    IReadOnlyList<OverpoweredStrategy> OverpoweredStrategies);

/// <summary>
/// Overpowered character detection.
/// </summary>
public record OverpoweredCharacter(
    string CharacterName,
    double WinRate,
    double DeviationFromMean,
    string PrimaryIssue,
    IReadOnlyList<string> ProblematicMatchups);

/// <summary>
/// Overpowered move detection.
/// </summary>
public record OverpoweredMove(
    string CharacterName,
    string MoveName,
    double UsageRate,
    double SuccessRate,
    double DamageOutput,
    double SafetyScore,
    string Issue);

/// <summary>
/// Overpowered strategy detection.
/// </summary>
public record OverpoweredStrategy(
    string CharacterName,
    string StrategyName,
    string Description,
    double WinRateWhenUsed,
    double CounterPlayRate,
    string SuggestedCounter);

/// <summary>
/// Request for balance analysis.
/// </summary>
public record BalanceAnalysisRequest(
    IReadOnlyList<string> TargetCharacters,
    double? WinRateThreshold,
    double? UsageThreshold,
    bool SuggestNerfs,
    bool SuggestBuffs,
    BalanceSuggestionPriority Priority);

/// <summary>
/// Balance suggestion priority.
/// </summary>
public enum BalanceSuggestionPriority
{
    Conservative,
    Moderate,
    Aggressive
}

/// <summary>
/// Balance patch suggestions.
/// </summary>
public record BalancePatchSuggestions(
    DateTime GeneratedAt,
    int TotalCharactersAnalyzed,
    IReadOnlyList<CharacterBalanceSuggestion> CharacterSuggestions,
    IReadOnlyList<MoveBalanceSuggestion> MoveSuggestions,
    IReadOnlyList<GeneralBalanceSuggestion> GeneralSuggestions);

/// <summary>
/// Character-level balance suggestion.
/// </summary>
public record CharacterBalanceSuggestion(
    string CharacterName,
    BalanceChangeType ChangeType,
    string Reason,
    double CurrentWinRate,
    double TargetWinRate,
    IReadOnlyList<string> SuggestedChanges);

/// <summary>
/// Balance change type.
/// </summary>
public enum BalanceChangeType
{
    Buff,
    Nerf,
    Rework,
    NoChange
}

/// <summary>
/// Move-level balance suggestion.
/// </summary>
public record MoveBalanceSuggestion(
    string CharacterName,
    string MoveName,
    BalanceChangeType ChangeType,
    string Reason,
    IReadOnlyList<PropertyAdjustment> PropertyAdjustments);

/// <summary>
/// Property adjustment.
/// </summary>
public record PropertyAdjustment(
    string PropertyName,
    double CurrentValue,
    double SuggestedValue,
    string Unit);

/// <summary>
/// General balance suggestion.
/// </summary>
public record GeneralBalanceSuggestion(
    string Category,
    string Description,
    string Rationale,
    IReadOnlyList<string> AffectedCharacters);

/// <summary>
/// Detailed matchup analysis.
/// </summary>
public record DetailedMatchupAnalysis(
    string Character1,
    string Character2,
    int TotalMatches,
    int Character1Wins,
    int Character2Wins,
    double Character1WinRate,
    AdvantageLevel AdvantageLevel,
    IReadOnlyList<MatchupFactor> Factors,
    IReadOnlyList<string> Character1Advantages,
    IReadOnlyList<string> Character2Advantages,
    IReadOnlyList<string> NeutralElements);

/// <summary>
/// Factor in matchup analysis.
/// </summary>
public record MatchupFactor(
    string Name,
    string Description,
    string FavoredCharacter,
    double ImpactScore);

/// <summary>
/// Criteria for character rankings.
/// </summary>
public enum RankingCriteria
{
    EloRating,
    TournamentWins,
    WinRate,
    PickRate,
    BanRate,
    CombinedScore
}

/// <summary>
/// Character rankings.
/// </summary>
public record CharacterRankings(
    RankingCriteria Criteria,
    DateTime GeneratedAt,
    IReadOnlyList<CharacterRanking> Rankings);

/// <summary>
/// Individual character ranking.
/// </summary>
public record CharacterRanking(
    int Rank,
    string CharacterName,
    double Score,
    double PreviousScore,
    int RankChange,
    IReadOnlyList<string> NotableAchievements);

/// <summary>
/// Popularity trends analysis.
/// </summary>
public record PopularityTrends(
    TimeSpan Period,
    DateTime GeneratedAt,
    int TotalMatches,
    IReadOnlyList<CharacterPopularity> Popularities);

/// <summary>
/// Character popularity data.
/// </summary>
public record CharacterPopularity(
    string CharacterName,
    int MatchesPlayed,
    double PickRate,
    double PickRateChange,
    TrendDirection Trend,
    int UniquePlayers);

/// <summary>
/// Export format for balance analysis.
/// </summary>
public enum BalanceExportFormat
{
    Json,
    Csv,
    Markdown,
    Html,
    Pdf
}
