using SaveState.Core.Common;

namespace SaveState.Core.Intelligence.GamingDna.Services;

/// <summary>
/// Analyzes user gaming behavior to create a unique "Gaming DNA" profile.
/// Classifies users into archetypes and tracks genre evolution over time.
/// </summary>
public interface IGamingDnaAnalyzer
{
    /// <summary>
    /// Analyzes user gaming history and generates a complete DNA profile.
    /// </summary>
    /// <param name="userId">The user ID to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The complete gaming DNA profile.</returns>
    Task<Result<GamingDnaProfile>> AnalyzeProfileAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the primary gaming archetypes for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of gaming archetypes with confidence scores.</returns>
    Task<Result<IReadOnlyList<GamingArchetypeScore>>> GetArchetypesAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Tracks how genre preferences have evolved over time.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="timeRange">Time range to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Genre evolution timeline.</returns>
    Task<Result<GenreEvolutionTimeline>> GetGenreEvolutionAsync(
        Guid userId,
        TimeRange timeRange,
        CancellationToken ct = default);

    /// <summary>
    /// Gets visualization data for the DNA profile.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Visualization data structures.</returns>
    Task<Result<DnaVisualizationData>> GetVisualizationDataAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Compares two users' gaming DNA profiles.
    /// </summary>
    /// <param name="userId1">First user ID.</param>
    /// <param name="userId2">Second user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>DNA comparison result.</returns>
    Task<Result<DnaComparisonResult>> CompareProfilesAsync(
        Guid userId1,
        Guid userId2,
        CancellationToken ct = default);

    /// <summary>
    /// Refreshes the DNA analysis with latest data.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the refresh operation.</returns>
    Task<Result> RefreshAnalysisAsync(
        Guid userId,
        CancellationToken ct = default);
}

/// <summary>
/// Complete gaming DNA profile for a user.
/// </summary>
public sealed record GamingDnaProfile(
    Guid UserId,
    DateTime GeneratedAt,
    IReadOnlyList<GamingArchetypeScore> Archetypes,
    GenrePreferences GenrePreferences,
    PlayStyleMetrics PlayStyleMetrics,
    EngagementPatterns EngagementPatterns,
    SocialGamingProfile SocialProfile,
    AchievementProfile AchievementProfile,
    DnaSignature Signature);

/// <summary>
/// Gaming archetype with confidence score.
/// </summary>
public sealed record GamingArchetypeScore(
    GamingArchetype Archetype,
    float ConfidenceScore,
    IReadOnlyList<string> Indicators);

/// <summary>
/// Gaming archetype classifications.
/// </summary>
public enum GamingArchetype
{
    Completionist,
    Explorer,
    Competitor,
    StorySeeker,
    Strategist,
    Speedrunner,
    Socialite,
    Collector,
    Casual,
    Hardcore,
    Creative,
    Achiever
}

/// <summary>
/// Genre preferences with weights.
/// </summary>
public sealed record GenrePreferences(
    IReadOnlyList<WeightedGenre> TopGenres,
    IReadOnlyList<WeightedGenre> EmergingGenres,
    IReadOnlyList<WeightedGenre> DecliningGenres);

/// <summary>
/// Genre with weight and metadata.
/// </summary>
public sealed record WeightedGenre(
    string Genre,
    float Weight,
    int GameCount,
    TimeSpan TotalPlayTime,
    DateTime LastPlayed);

/// <summary>
/// Time of day periods.
/// </summary>
public enum TimeOfDay
{
    Morning,    // 6:00 - 12:00
    Afternoon,  // 12:00 - 18:00
    Evening,    // 18:00 - 22:00
    Night       // 22:00 - 6:00
}

/// <summary>
/// Play style metrics.
/// </summary>
public sealed record PlayStyleMetrics(
    float AverageSessionLengthMinutes,
    float PreferredSessionLengthMinutes,
    Core.Intelligence.Recommendations.Services.TimeOfDay PeakPlayTime,
    DayOfWeek MostActiveDay,
    float WeekendVsWeekdayRatio,
    float SinglePlayerVsMultiplayerRatio,
    float StoryVsGameplayFocus,
    DifficultyPreference PreferredDifficulty);

/// <summary>
/// Difficulty preference.
/// </summary>
public enum DifficultyPreference
{
    VeryEasy,
    Easy,
    Normal,
    Hard,
    VeryHard,
    Variable
}

/// <summary>
/// Engagement patterns.
/// </summary>
public sealed record EngagementPatterns(
    float CompletionRate,
    float AbandonmentRate,
    float ReplayRate,
    float EarlyAccessInterest,
    float IndieAffinity,
    float AaaAffinity,
    float NewReleaseInterest,
    float ClassicGameInterest);

/// <summary>
/// Social gaming profile.
/// </summary>
public sealed record SocialGamingProfile(
    float SocialGamingScore,
    int PreferredPartySize,
    float CoopVsCompetitiveRatio,
    float VoiceChatPreference,
    float CommunityEngagement,
    IReadOnlyList<string> PreferredSocialFeatures);

/// <summary>
/// Achievement profile.
/// </summary>
public sealed record AchievementProfile(
    float AchievementHunterScore,
    float CompletionistScore,
    float ChallengeSeekerScore,
    int TotalAchievementsUnlocked,
    int RareAchievementsUnlocked,
    float AverageAchievementCompletionRate);

/// <summary>
/// Unique DNA signature for comparison.
/// </summary>
public sealed record DnaSignature(
    string Hash,
    IReadOnlyList<float> Vector,
    DateTime GeneratedAt);

/// <summary>
/// Time range for analysis.
/// </summary>
public sealed record TimeRange(
    DateTime Start,
    DateTime End)
{
    public static TimeRange LastMonth => new(
        DateTime.UtcNow.AddMonths(-1),
        DateTime.UtcNow);

    public static TimeRange LastQuarter => new(
        DateTime.UtcNow.AddMonths(-3),
        DateTime.UtcNow);

    public static TimeRange LastYear => new(
        DateTime.UtcNow.AddYears(-1),
        DateTime.UtcNow);

    public static TimeRange AllTime => new(
        DateTime.MinValue,
        DateTime.UtcNow);
}

/// <summary>
/// Genre evolution timeline.
/// </summary>
public sealed record GenreEvolutionTimeline(
    Guid UserId,
    TimeRange TimeRange,
    IReadOnlyList<GenreEvolutionPoint> DataPoints);

/// <summary>
/// Single point in genre evolution.
/// </summary>
public sealed record GenreEvolutionPoint(
    DateTime Date,
    IReadOnlyList<WeightedGenre> GenreWeights,
    float DiversityScore,
    GamingArchetype? DominantArchetype);

/// <summary>
/// DNA visualization data.
/// </summary>
public sealed record DnaVisualizationData(
    Guid UserId,
    RadarChartData RadarChart,
    TimelineChartData TimelineChart,
    HeatmapData Heatmap,
    ArchetypeVisualization ArchetypeViz);

/// <summary>
/// Radar chart data for DNA visualization.
/// </summary>
public sealed record RadarChartData(
    IReadOnlyList<RadarDimension> Dimensions);

/// <summary>
/// Single dimension for radar chart.
/// </summary>
public sealed record RadarDimension(
    string Name,
    float Value,
    float MaxValue = 100);

/// <summary>
/// Timeline chart data.
/// </summary>
public sealed record TimelineChartData(
    IReadOnlyList<string> Labels,
    IReadOnlyList<TimelineDataset> Datasets);

/// <summary>
/// Timeline dataset.
/// </summary>
public sealed record TimelineDataset(
    string Label,
    IReadOnlyList<float> Data,
    string Color);

/// <summary>
/// Heatmap data.
/// </summary>
public sealed record HeatmapData(
    IReadOnlyList<HeatmapCell> Cells,
    int Rows,
    int Columns);

/// <summary>
/// Single heatmap cell.
/// </summary>
public sealed record HeatmapCell(
    int Row,
    int Column,
    float Value,
    string? Label = null);

/// <summary>
/// Archetype visualization data.
/// </summary>
public sealed record ArchetypeVisualization(
    IReadOnlyList<ArchetypeNode> Nodes,
    IReadOnlyList<ArchetypeEdge> Edges);

/// <summary>
/// Archetype node for network graph.
/// </summary>
public sealed record ArchetypeNode(
    string Id,
    string Label,
    float Size,
    string Color,
    float X,
    float Y);

/// <summary>
/// Archetype edge for network graph.
/// </summary>
public sealed record ArchetypeEdge(
    string Source,
    string Target,
    float Weight);

/// <summary>
/// DNA comparison result between two users.
/// </summary>
public sealed record DnaComparisonResult(
    Guid UserId1,
    Guid UserId2,
    float SimilarityScore,
    IReadOnlyList<string> SharedPreferences,
    IReadOnlyList<string> ComplementaryTraits,
    IReadOnlyList<DnaGameRecommendation> RecommendedGamesToPlayTogether);

/// <summary>
/// Game recommendation for DNA comparison.
/// </summary>
public sealed record DnaGameRecommendation(
    Guid GameId,
    string Title,
    string Reason,
    float MatchScore);
