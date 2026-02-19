using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for discovering and recommending MUGEN characters.
/// Provides search, recommendations, trending characters, and community features.
/// </summary>
public interface ICharacterDiscoveryService
{
    #region Search and Discovery

    /// <summary>
    /// Searches for characters by various criteria.
    /// </summary>
    Task<Result<CharacterSearchResult>> SearchCharactersAsync(
        CharacterSearchQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets character recommendations based on user preferences.
    /// </summary>
    Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetRecommendationsAsync(
        RecommendationCriteria criteria,
        CancellationToken ct = default);

    /// <summary>
    /// Gets trending characters.
    /// </summary>
    Task<Result<IReadOnlyList<TrendingCharacter>>> GetTrendingCharactersAsync(
        TrendingPeriod period,
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets recently added characters.
    /// </summary>
    Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetRecentlyAddedAsync(
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets characters by category/tag.
    /// </summary>
    Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByCategoryAsync(
        string category,
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets similar characters to a given character.
    /// </summary>
    Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetSimilarCharactersAsync(
        Guid characterId,
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets popular character combinations.
    /// </summary>
    Task<Result<IReadOnlyList<CharacterCombination>>> GetPopularCombinationsAsync(
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets characters by author.
    /// </summary>
    Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByAuthorAsync(
        string authorName,
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets featured character of the day/week.
    /// </summary>
    Task<Result<FeaturedCharacter>> GetFeaturedCharacterAsync(
        CancellationToken ct = default);

    #endregion

    #region Character Details

    /// <summary>
    /// Gets detailed information about a character.
    /// </summary>
    Task<Result<CharacterDetail>> GetCharacterDetailsAsync(
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets character reviews and ratings.
    /// </summary>
    Task<Result<CharacterReviews>> GetCharacterReviewsAsync(
        Guid characterId,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets character matchups and compatibility.
    /// </summary>
    Task<Result<IReadOnlyList<CharacterMatchup>>> GetCharacterMatchupsAsync(
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets character showcase videos.
    /// </summary>
    Task<Result<IReadOnlyList<CharacterShowcase>>> GetShowcasesAsync(
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets download history for a character.
    /// </summary>
    Task<Result<DownloadHistory>> GetDownloadHistoryAsync(
        Guid characterId,
        CancellationToken ct = default);

    #endregion

    #region User Interaction

    /// <summary>
    /// Rates a character.
    /// </summary>
    Task<Result> RateCharacterAsync(
        Guid characterId,
        int rating,
        string? review = null,
        CancellationToken ct = default);

    /// <summary>
    /// Adds character to favorites.
    /// </summary>
    Task<Result> AddToFavoritesAsync(
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Removes character from favorites.
    /// </summary>
    Task<Result> RemoveFromFavoritesAsync(
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets user's favorite characters.
    /// </summary>
    Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetFavoritesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Reports a character (inappropriate content, etc.).
    /// </summary>
    Task<Result> ReportCharacterAsync(
        Guid characterId,
        CharacterReportReason reason,
        string? details = null,
        CancellationToken ct = default);

    /// <summary>
    /// Shares character with others.
    /// </summary>
    Task<Result<string>> ShareCharacterAsync(
        Guid characterId,
        ShareOptions options,
        CancellationToken ct = default);

    #endregion

    #region Collections and Lists

    /// <summary>
    /// Creates a character collection.
    /// </summary>
    Task<Result<CharacterCollection>> CreateCollectionAsync(
        string name,
        string? description = null,
        bool isPublic = true,
        CancellationToken ct = default);

    /// <summary>
    /// Adds character to collection.
    /// </summary>
    Task<Result> AddToCollectionAsync(
        Guid collectionId,
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets user's collections.
    /// </summary>
    Task<Result<IReadOnlyList<CharacterCollection>>> GetCollectionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets public collections.
    /// </summary>
    Task<Result<IReadOnlyList<CharacterCollection>>> GetPublicCollectionsAsync(
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets characters in a collection.
    /// </summary>
    Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetCollectionCharactersAsync(
        Guid collectionId,
        CancellationToken ct = default);

    #endregion

    #region Comparison Tools

    /// <summary>
    /// Compares multiple characters.
    /// </summary>
    Task<Result<CharacterComparison>> CompareCharactersAsync(
        IReadOnlyList<Guid> characterIds,
        ComparisonOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets character compatibility matrix.
    /// </summary>
    Task<Result<CompatibilityMatrix>> GetCompatibilityMatrixAsync(
        IReadOnlyList<Guid> characterIds,
        CancellationToken ct = default);

    /// <summary>
    /// Suggests characters to complete a roster.
    /// </summary>
    Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> SuggestRosterCompletionAsync(
        IReadOnlyList<Guid> currentRoster,
        RosterPreferences preferences,
        CancellationToken ct = default);

    #endregion

    #region Stats and Analytics

    /// <summary>
    /// Gets global discovery statistics.
    /// </summary>
    Task<Result<DiscoveryStatistics>> GetStatisticsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets user's discovery activity.
    /// </summary>
    Task<Result<UserDiscoveryActivity>> GetUserActivityAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets character popularity trends.
    /// </summary>
    Task<Result<IReadOnlyList<PopularityTrend>>> GetPopularityTrendsAsync(
        TimeSpan period,
        CancellationToken ct = default);

    #endregion
}

#region Request/Response Models

/// <summary>
/// Character search query.
/// </summary>
public record CharacterSearchQuery(
    string? SearchTerm = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? Authors = null,
    int? MinRating = null,
    int? MaxRating = null,
    DateTime? AddedAfter = null,
    DateTime? AddedBefore = null,
    int? MinDownloads = null,
    int? MaxDownloads = null,
    string? SortBy = null,
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20);

/// <summary>
/// Character search result.
/// </summary>
public record CharacterSearchResult(
    IReadOnlyList<DiscoveredCharacter> Characters,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<SearchFacet> Facets);

/// <summary>
/// Search facet for filtering.
/// </summary>
public record SearchFacet(
    string Name,
    IReadOnlyList<FacetValue> Values);

/// <summary>
/// Facet value.
/// </summary>
public record FacetValue(
    string Value,
    int Count,
    bool IsSelected);

/// <summary>
/// Discovered character.
/// </summary>
public record DiscoveredCharacter(
    Guid Id,
    string Name,
    string Author,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Categories,
    double Rating,
    int ReviewCount,
    int DownloadCount,
    string? ThumbnailUrl,
    DateTime AddedDate,
    DateTime LastUpdated,
    DiscoveredCharacterStats Stats);

/// <summary>
/// Character stats.
/// </summary>
public record DiscoveredCharacterStats(
    int WinRate,
    int UsageCount,
    int FavoriteCount);

/// <summary>
/// Character recommendation.
/// </summary>
public record DiscoveredCharacterRecommendation(
    DiscoveredCharacter Character,
    double MatchScore,
    string Reason,
    IReadOnlyList<string> MatchedTags);

/// <summary>
/// Recommendation criteria.
/// </summary>
public record RecommendationCriteria(
    IReadOnlyList<string>? PreferredTags = null,
    IReadOnlyList<string>? PreferredAuthors = null,
    IReadOnlyList<string>? AvoidTags = null,
    int? MinRating = null,
    RecommendationType Type = RecommendationType.Personalized);

/// <summary>
/// Recommendation type.
/// </summary>
public enum RecommendationType
{
    Personalized,
    Trending,
    Similar,
    Random,
    StaffPick
}

/// <summary>
/// Trending character.
/// </summary>
public record TrendingCharacter(
    DiscoveredCharacter Character,
    int Rank,
    int TrendDirection,
    int DownloadChange,
    int RatingChange);

/// <summary>
/// Trending period.
/// </summary>
public enum TrendingPeriod
{
    Today,
    ThisWeek,
    ThisMonth,
    ThisYear,
    AllTime
}

/// <summary>
/// Character combination.
/// </summary>
public record CharacterCombination(
    IReadOnlyList<DiscoveredCharacter> Characters,
    int UsageCount,
    double WinRate,
    string? Description);

/// <summary>
/// Featured character.
/// </summary>
public record FeaturedCharacter(
    DiscoveredCharacter Character,
    FeaturedReason Reason,
    DateTime FeaturedDate,
    DateTime? FeaturedUntil);

/// <summary>
/// Featured reason.
/// </summary>
public enum FeaturedReason
{
    StaffPick,
    CommunityChoice,
    NewRelease,
    Updated,
    Anniversary
}

/// <summary>
/// Character detail.
/// </summary>
public record CharacterDetail(
    Guid Id,
    string Name,
    string Author,
    string? Description,
    string? Story,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> GameplayStyles,
    double Rating,
    int ReviewCount,
    int DownloadCount,
    string? ThumbnailUrl,
    IReadOnlyList<string> ScreenshotUrls,
    string? VideoUrl,
    IReadOnlyList<string> DownloadUrls,
    DateTime AddedDate,
    DateTime LastUpdated,
    DiscoveredCharacterStats Stats,
    CharacterMoveList Moves,
    CharacterPaletteInfo Palettes,
    IReadOnlyList<CharacterCompatibility> Compatibility);

/// <summary>
/// Character move list.
/// </summary>
public record CharacterMoveList(
    int TotalMoves,
    IReadOnlyList<string> SpecialMoves,
    IReadOnlyList<string> HyperMoves);

/// <summary>
/// Character palette info.
/// </summary>
public record CharacterPaletteInfo(
    int PaletteCount,
    IReadOnlyList<string> DefaultPalettes,
    bool HasCustomPalettes);

/// <summary>
/// Character compatibility.
/// </summary>
public record CharacterCompatibility(
    string GameVersion,
    CompatibilityStatus Status,
    string? Notes);

/// <summary>
/// Compatibility status.
/// </summary>
public enum CompatibilityStatus
{
    FullyCompatible,
    PartiallyCompatible,
    Incompatible,
    Unknown
}

/// <summary>
/// Character reviews.
/// </summary>
public record CharacterReviews(
    IReadOnlyList<CharacterReview> Reviews,
    int TotalCount,
    double AverageRating,
    RatingDistribution Distribution,
    int Page,
    int PageSize);

/// <summary>
/// Character review.
/// </summary>
public record CharacterReview(
    Guid Id,
    Guid UserId,
    string UserName,
    int Rating,
    string? Title,
    string? Content,
    DateTime PostedDate,
    int HelpfulCount,
    int NotHelpfulCount);

/// <summary>
/// Rating distribution.
/// </summary>
public record RatingDistribution(
    int FiveStars,
    int FourStars,
    int ThreeStars,
    int TwoStars,
    int OneStar);

/// <summary>
/// Character matchup.
/// </summary>
public record CharacterMatchup(
    Guid OpponentId,
    string OpponentName,
    int Wins,
    int Losses,
    double WinRate);

/// <summary>
/// Character showcase.
/// </summary>
public record CharacterShowcase(
    string Title,
    string VideoUrl,
    string? ThumbnailUrl,
    string? Description,
    string UploaderName,
    DateTime UploadedDate,
    int ViewCount);

/// <summary>
/// Download history.
/// </summary>
public record DownloadHistory(
    Guid CharacterId,
    int TotalDownloads,
    IReadOnlyList<DownloadEntry> RecentDownloads);

/// <summary>
/// Download entry.
/// </summary>
public record DownloadEntry(
    DateTime Date,
    string Version,
    int DownloadCount);

/// <summary>
/// Character report reason.
/// </summary>
public enum CharacterReportReason
{
    InappropriateContent,
    CopyrightViolation,
    Malware,
    MisleadingDescription,
    BrokenDownload,
    Other
}

/// <summary>
/// Share options.
/// </summary>
public record ShareOptions(
    SharePlatform Platform,
    string? Message = null,
    bool IncludeStats = true);

/// <summary>
/// Share platform.
/// </summary>
public enum SharePlatform
{
    Clipboard,
    Twitter,
    Discord,
    Reddit,
    Email
}

/// <summary>
/// Character collection.
/// </summary>
public record CharacterCollection(
    Guid Id,
    string Name,
    string? Description,
    string CreatorName,
    bool IsPublic,
    int CharacterCount,
    IReadOnlyList<string> Tags,
    int ViewCount,
    int FavoriteCount,
    DateTime CreatedDate,
    DateTime LastUpdated);

/// <summary>
/// Character comparison.
/// </summary>
public record CharacterComparison(
    IReadOnlyList<ComparedCharacter> Characters,
    IReadOnlyList<ComparisonCategory> Categories);

/// <summary>
/// Compared character.
/// </summary>
public record ComparedCharacter(
    Guid Id,
    string Name,
    string? ThumbnailUrl);

/// <summary>
/// Comparison category.
/// </summary>
public record ComparisonCategory(
    string Name,
    IReadOnlyList<ComparisonValue> Values);

/// <summary>
/// Comparison value.
/// </summary>
public record ComparisonValue(
    Guid CharacterId,
    string Value,
    bool IsHighlighted);

/// <summary>
/// Comparison options.
/// </summary>
public record ComparisonOptions(
    IReadOnlyList<string> Categories,
    bool HighlightDifferences,
    bool ShowPercentages);

/// <summary>
/// Compatibility matrix.
/// </summary>
public record CompatibilityMatrix(
    IReadOnlyList<MatrixCharacter> Characters,
    IReadOnlyList<IReadOnlyList<CompatibilityScore>> Scores);

/// <summary>
/// Matrix character.
/// </summary>
public record MatrixCharacter(
    Guid Id,
    string Name);

/// <summary>
/// Compatibility score.
/// </summary>
public record CompatibilityScore(
    double Score,
    CompatibilityLevel Level);

/// <summary>
/// Compatibility level.
/// </summary>
public enum CompatibilityLevel
{
    Excellent,
    Good,
    Fair,
    Poor,
    Incompatible
}

/// <summary>
/// Roster preferences.
/// </summary>
public record RosterPreferences(
    int TargetSize,
    BalanceType Balance,
    IReadOnlyList<string> RequiredTags,
    IReadOnlyList<string> AvoidTags);

/// <summary>
/// Balance type.
/// </summary>
public enum BalanceType
{
    Balanced,
    OffenseHeavy,
    DefenseHeavy,
    ZoningHeavy,
    GrapplerHeavy,
    Random
}

/// <summary>
/// Discovery statistics.
/// </summary>
public record DiscoveryStatistics(
    int TotalCharacters,
    int TotalAuthors,
    int TotalDownloads,
    int TotalReviews,
    double AverageRating,
    IReadOnlyList<CategoryStat> CategoryStats,
    IReadOnlyList<TagStat> TagStats);

/// <summary>
/// Category stat.
/// </summary>
public record CategoryStat(
    string Category,
    int CharacterCount);

/// <summary>
/// Tag stat.
/// </summary>
public record TagStat(
    string Tag,
    int CharacterCount,
    double AverageRating);

/// <summary>
/// User discovery activity.
/// </summary>
public record UserDiscoveryActivity(
    int CharactersViewed,
    int CharactersDownloaded,
    int CharactersRated,
    int ReviewsWritten,
    int CollectionsCreated,
    IReadOnlyList<DiscoveredCharacter> RecentlyViewed,
    IReadOnlyList<DiscoveredCharacter> RecentlyDownloaded);

/// <summary>
/// Popularity trend.
/// </summary>
public record PopularityTrend(
    Guid CharacterId,
    string CharacterName,
    IReadOnlyList<DailyStat> DailyStats);

/// <summary>
/// Daily stat.
/// </summary>
public record DailyStat(
    DateTime Date,
    int Downloads,
    double Rating);

#endregion
