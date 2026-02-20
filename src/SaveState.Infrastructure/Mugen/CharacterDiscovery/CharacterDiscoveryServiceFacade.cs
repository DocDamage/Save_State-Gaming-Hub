using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.CharacterDiscovery.Managers;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery;

/// <summary>
/// Coordinator service for character discovery operations.
/// Delegates all operations to specialized manager classes.
/// </summary>
public class CharacterDiscoveryService : ICharacterDiscoveryService
{
    private readonly CharacterSearchManager _searchManager;
    private readonly CharacterDetailsManager _detailsManager;
    private readonly UserInteractionManager _interactionManager;
    private readonly CollectionsManager _collectionsManager;
    private readonly CharacterComparisonManager _comparisonManager;
    private readonly DiscoveryAnalyticsManager _analyticsManager;

    // Data stores - shared across managers
    private readonly ConcurrentDictionary<Guid, DiscoveredCharacter> _characters = new();
    private readonly ConcurrentDictionary<Guid, CharacterDetail> _characterDetails = new();
    private readonly ConcurrentDictionary<Guid, CharacterCollection> _collections = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _userFavorites = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _recentlyViewed = new();

    public CharacterDiscoveryService(
        ILogger<CharacterDiscoveryService> logger,
        ILoggerFactory loggerFactory,
        ITimeProvider timeProvider)
    {
        _searchManager = new CharacterSearchManager(
            loggerFactory.CreateLogger<CharacterSearchManager>(),
            timeProvider);
        _detailsManager = new CharacterDetailsManager(
            loggerFactory.CreateLogger<CharacterDetailsManager>(),
            timeProvider);
        _interactionManager = new UserInteractionManager(
            loggerFactory.CreateLogger<UserInteractionManager>());
        _collectionsManager = new CollectionsManager(
            loggerFactory.CreateLogger<CollectionsManager>(),
            timeProvider);
        _comparisonManager = new CharacterComparisonManager(
            loggerFactory.CreateLogger<CharacterComparisonManager>());
        _analyticsManager = new DiscoveryAnalyticsManager(
            loggerFactory.CreateLogger<DiscoveryAnalyticsManager>(),
            timeProvider);

        SeedSampleData();
    }

    #region Search and Discovery

    public Task<Result<CharacterSearchResult>> SearchCharactersAsync(
        CharacterSearchQuery query,
        CancellationToken ct = default) =>
        _searchManager.SearchCharactersAsync(query, _characters, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetRecommendationsAsync(
        RecommendationCriteria criteria,
        CancellationToken ct = default) =>
        _searchManager.GetRecommendationsAsync(criteria, _characters, ct);

    public Task<Result<IReadOnlyList<TrendingCharacter>>> GetTrendingCharactersAsync(
        TrendingPeriod period,
        int? limit = null,
        CancellationToken ct = default) =>
        _searchManager.GetTrendingCharactersAsync(period, limit, _characters, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetRecentlyAddedAsync(
        int? limit = null,
        CancellationToken ct = default) =>
        _searchManager.GetRecentlyAddedAsync(limit, _characters, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByCategoryAsync(
        string category,
        int? limit = null,
        CancellationToken ct = default) =>
        _searchManager.GetByCategoryAsync(category, limit, _characters, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetSimilarCharactersAsync(
        Guid characterId,
        int? limit = null,
        CancellationToken ct = default) =>
        _searchManager.GetSimilarCharactersAsync(characterId, limit, _characters, ct);

    public Task<Result<IReadOnlyList<CharacterCombination>>> GetPopularCombinationsAsync(
        int? limit = null,
        CancellationToken ct = default) =>
        _searchManager.GetPopularCombinationsAsync(limit, _characters, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByAuthorAsync(
        string authorName,
        int? limit = null,
        CancellationToken ct = default) =>
        _searchManager.GetByAuthorAsync(authorName, limit, _characters, ct);

    public Task<Result<FeaturedCharacter>> GetFeaturedCharacterAsync(
        CancellationToken ct = default) =>
        _searchManager.GetFeaturedCharacterAsync(_characters, ct);

    #endregion

    #region Character Details

    public Task<Result<CharacterDetail>> GetCharacterDetailsAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _detailsManager.GetCharacterDetailsAsync(characterId, _characterDetails, _characters, _recentlyViewed, ct);

    public Task<Result<CharacterReviews>> GetCharacterReviewsAsync(
        Guid characterId,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default) =>
        _detailsManager.GetCharacterReviewsAsync(characterId, page, pageSize, ct);

    public Task<Result<IReadOnlyList<CharacterMatchup>>> GetCharacterMatchupsAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _detailsManager.GetCharacterMatchupsAsync(characterId, _characters, ct);

    public Task<Result<IReadOnlyList<CharacterShowcase>>> GetShowcasesAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _detailsManager.GetShowcasesAsync(characterId, ct);

    public Task<Result<DownloadHistory>> GetDownloadHistoryAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _detailsManager.GetDownloadHistoryAsync(characterId, ct);

    #endregion

    #region User Interaction

    public Task<Result> RateCharacterAsync(
        Guid characterId,
        int rating,
        string? review = null,
        CancellationToken ct = default) =>
        _interactionManager.RateCharacterAsync(characterId, rating, review, _characters, ct);

    public Task<Result> AddToFavoritesAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _interactionManager.AddToFavoritesAsync(characterId, "current_user", _userFavorites, ct);

    public Task<Result> RemoveFromFavoritesAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _interactionManager.RemoveFromFavoritesAsync(characterId, "current_user", _userFavorites, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetFavoritesAsync(
        CancellationToken ct = default) =>
        _interactionManager.GetFavoritesAsync("current_user", _userFavorites, _characters, ct);

    public Task<Result> ReportCharacterAsync(
        Guid characterId,
        CharacterReportReason reason,
        string? details = null,
        CancellationToken ct = default) =>
        _interactionManager.ReportCharacterAsync(characterId, reason, details, ct);

    public Task<Result<string>> ShareCharacterAsync(
        Guid characterId,
        ShareOptions options,
        CancellationToken ct = default) =>
        _interactionManager.ShareCharacterAsync(characterId, options, _characters, ct);

    #endregion

    #region Collections and Lists

    public Task<Result<CharacterCollection>> CreateCollectionAsync(
        string name,
        string? description = null,
        bool isPublic = true,
        CancellationToken ct = default) =>
        _collectionsManager.CreateCollectionAsync(name, description, isPublic, "CurrentUser", _collections, ct);

    public Task<Result> AddToCollectionAsync(
        Guid collectionId,
        Guid characterId,
        CancellationToken ct = default) =>
        _collectionsManager.AddToCollectionAsync(collectionId, characterId, _collections, ct);

    public Task<Result<IReadOnlyList<CharacterCollection>>> GetCollectionsAsync(
        CancellationToken ct = default) =>
        _collectionsManager.GetCollectionsAsync("CurrentUser", _collections, ct);

    public Task<Result<IReadOnlyList<CharacterCollection>>> GetPublicCollectionsAsync(
        int? limit = null,
        CancellationToken ct = default) =>
        _collectionsManager.GetPublicCollectionsAsync(limit, _collections, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetCollectionCharactersAsync(
        Guid collectionId,
        CancellationToken ct = default) =>
        _collectionsManager.GetCollectionCharactersAsync(collectionId, _characters, ct);

    #endregion

    #region Comparison Tools

    public Task<Result<CharacterComparison>> CompareCharactersAsync(
        IReadOnlyList<Guid> characterIds,
        ComparisonOptions options,
        CancellationToken ct = default) =>
        _comparisonManager.CompareCharactersAsync(characterIds, options, _characters, ct);

    public Task<Result<CompatibilityMatrix>> GetCompatibilityMatrixAsync(
        IReadOnlyList<Guid> characterIds,
        CancellationToken ct = default) =>
        _comparisonManager.GetCompatibilityMatrixAsync(characterIds, _characters, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> SuggestRosterCompletionAsync(
        IReadOnlyList<Guid> currentRoster,
        RosterPreferences preferences,
        CancellationToken ct = default) =>
        _comparisonManager.SuggestRosterCompletionAsync(currentRoster, preferences, _characters, ct);

    #endregion

    #region Stats and Analytics

    public Task<Result<DiscoveryStatistics>> GetStatisticsAsync(
        CancellationToken ct = default) =>
        _analyticsManager.GetStatisticsAsync(_characters, ct);

    public Task<Result<UserDiscoveryActivity>> GetUserActivityAsync(
        CancellationToken ct = default) =>
        _analyticsManager.GetUserActivityAsync("current_user", _recentlyViewed, _characters, ct);

    public Task<Result<IReadOnlyList<PopularityTrend>>> GetPopularityTrendsAsync(
        TimeSpan period,
        CancellationToken ct = default) =>
        _analyticsManager.GetPopularityTrendsAsync(period, _characters, ct);

    #endregion

    private void SeedSampleData()
    {
        var timeProvider = new SystemTimeProvider();
        var sampleChars = new[]
        {
            new DiscoveredCharacter(
                Guid.NewGuid(), "Ryu", "Capcom", "The wandering warrior",
                new[] { "shotokan", "balanced" }, new[] { "Street Fighter", "Capcom" },
                4.5, 120, 5000, null, timeProvider.UtcNow.AddDays(-100), timeProvider.UtcNow.AddDays(-10),
                new DiscoveredCharacterStats(55, 300, 200)),
            new DiscoveredCharacter(
                Guid.NewGuid(), "Ken", "Capcom", "Ryu's rival",
                new[] { "shotokan", "rushdown" }, new[] { "Street Fighter", "Capcom" },
                4.3, 95, 4500, null, timeProvider.UtcNow.AddDays(-95), timeProvider.UtcNow.AddDays(-5),
                new DiscoveredCharacterStats(52, 280, 180)),
            new DiscoveredCharacter(
                Guid.NewGuid(), "Chun-Li", "Capcom", "The strongest woman in the world",
                new[] { "speed", "footsies" }, new[] { "Street Fighter", "Capcom" },
                4.7, 150, 6000, null, timeProvider.UtcNow.AddDays(-90), timeProvider.UtcNow.AddDays(-8),
                new DiscoveredCharacterStats(58, 350, 250)),
            new DiscoveredCharacter(
                Guid.NewGuid(), "Goku", "Akira Toriyama", "Super Saiyan warrior",
                new[] { "anime", "shoto", "beam" }, new[] { "Dragon Ball", "Anime" },
                4.2, 200, 8000, null, timeProvider.UtcNow.AddDays(-80), timeProvider.UtcNow.AddDays(-15),
                new DiscoveredCharacterStats(50, 400, 300)),
            new DiscoveredCharacter(
                Guid.NewGuid(), "Spider-Man", "Marvel", "Friendly neighborhood hero",
                new[] { "marvel", "agile", "zoning" }, new[] { "Marvel", "Comics" },
                4.4, 180, 5500, null, timeProvider.UtcNow.AddDays(-70), timeProvider.UtcNow.AddDays(-12),
                new DiscoveredCharacterStats(53, 320, 220))
        };

        foreach (var c in sampleChars)
        {
            _characters[c.Id] = c;

            var detail = new CharacterDetail(
                c.Id, c.Name, c.Author, c.Description,
                $"{c.Name} is a powerful fighter with unique abilities.",
                c.Tags, c.Categories, new[] { "Balanced", "Footsies" },
                c.Rating, c.ReviewCount, c.DownloadCount, c.ThumbnailUrl,
                new List<string>(), null, new[] { $"https://example.com/{c.Name}.zip" },
                c.AddedDate, c.LastUpdated, c.Stats,
                new CharacterMoveList(10, new[] { "Hadouken", "Shoryuken" }, new[] { "Super Art" }),
                new CharacterPaletteInfo(12, new[] { "Default", "Alternate" }, true),
                new[] { new CharacterCompatibility("1.0", CompatibilityStatus.FullyCompatible, null) });

            _characterDetails[c.Id] = detail;
        }
    }
}
