using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery;

/// <summary>
/// Thin facade for character discovery operations.
/// </summary>
public class CharacterDiscoveryService : ICharacterDiscoveryService
{
    private readonly CharacterDiscoveryServiceOperations _operations;

    public CharacterDiscoveryService(ILogger<CharacterDiscoveryService> logger)
    {
        _operations = new CharacterDiscoveryServiceOperations(logger);
    }

    public Task<Result<CharacterSearchResult>> SearchCharactersAsync(
        CharacterSearchQuery query,
        CancellationToken ct = default) =>
        _operations.SearchCharactersAsync(query, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetRecommendationsAsync(
        RecommendationCriteria criteria,
        CancellationToken ct = default) =>
        _operations.GetRecommendationsAsync(criteria, ct);

    public Task<Result<IReadOnlyList<TrendingCharacter>>> GetTrendingCharactersAsync(
        TrendingPeriod period,
        int? limit = null,
        CancellationToken ct = default) =>
        _operations.GetTrendingCharactersAsync(period, limit, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetRecentlyAddedAsync(
        int? limit = null,
        CancellationToken ct = default) =>
        _operations.GetRecentlyAddedAsync(limit, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByCategoryAsync(
        string category,
        int? limit = null,
        CancellationToken ct = default) =>
        _operations.GetByCategoryAsync(category, limit, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetSimilarCharactersAsync(
        Guid characterId,
        int? limit = null,
        CancellationToken ct = default) =>
        _operations.GetSimilarCharactersAsync(characterId, limit, ct);

    public Task<Result<IReadOnlyList<CharacterCombination>>> GetPopularCombinationsAsync(
        int? limit = null,
        CancellationToken ct = default) =>
        _operations.GetPopularCombinationsAsync(limit, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByAuthorAsync(
        string authorName,
        int? limit = null,
        CancellationToken ct = default) =>
        _operations.GetByAuthorAsync(authorName, limit, ct);

    public Task<Result<FeaturedCharacter>> GetFeaturedCharacterAsync(
        CancellationToken ct = default) =>
        _operations.GetFeaturedCharacterAsync(ct);

    public Task<Result<CharacterDetail>> GetCharacterDetailsAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _operations.GetCharacterDetailsAsync(characterId, ct);

    public Task<Result<CharacterReviews>> GetCharacterReviewsAsync(
        Guid characterId,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default) =>
        _operations.GetCharacterReviewsAsync(characterId, page, pageSize, ct);

    public Task<Result<IReadOnlyList<CharacterMatchup>>> GetCharacterMatchupsAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _operations.GetCharacterMatchupsAsync(characterId, ct);

    public Task<Result<IReadOnlyList<CharacterShowcase>>> GetShowcasesAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _operations.GetShowcasesAsync(characterId, ct);

    public Task<Result<DownloadHistory>> GetDownloadHistoryAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _operations.GetDownloadHistoryAsync(characterId, ct);

    public Task<Result> RateCharacterAsync(
        Guid characterId,
        int rating,
        string? review = null,
        CancellationToken ct = default) =>
        _operations.RateCharacterAsync(characterId, rating, review, ct);

    public Task<Result> AddToFavoritesAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _operations.AddToFavoritesAsync(characterId, ct);

    public Task<Result> RemoveFromFavoritesAsync(
        Guid characterId,
        CancellationToken ct = default) =>
        _operations.RemoveFromFavoritesAsync(characterId, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetFavoritesAsync(
        CancellationToken ct = default) =>
        _operations.GetFavoritesAsync(ct);

    public Task<Result> ReportCharacterAsync(
        Guid characterId,
        CharacterReportReason reason,
        string? details = null,
        CancellationToken ct = default) =>
        _operations.ReportCharacterAsync(characterId, reason, details, ct);

    public Task<Result<string>> ShareCharacterAsync(
        Guid characterId,
        ShareOptions options,
        CancellationToken ct = default) =>
        _operations.ShareCharacterAsync(characterId, options, ct);

    public Task<Result<CharacterCollection>> CreateCollectionAsync(
        string name,
        string? description = null,
        bool isPublic = true,
        CancellationToken ct = default) =>
        _operations.CreateCollectionAsync(name, description, isPublic, ct);

    public Task<Result> AddToCollectionAsync(
        Guid collectionId,
        Guid characterId,
        CancellationToken ct = default) =>
        _operations.AddToCollectionAsync(collectionId, characterId, ct);

    public Task<Result<IReadOnlyList<CharacterCollection>>> GetCollectionsAsync(
        CancellationToken ct = default) =>
        _operations.GetCollectionsAsync(ct);

    public Task<Result<IReadOnlyList<CharacterCollection>>> GetPublicCollectionsAsync(
        int? limit = null,
        CancellationToken ct = default) =>
        _operations.GetPublicCollectionsAsync(limit, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetCollectionCharactersAsync(
        Guid collectionId,
        CancellationToken ct = default) =>
        _operations.GetCollectionCharactersAsync(collectionId, ct);

    public Task<Result<CharacterComparison>> CompareCharactersAsync(
        IReadOnlyList<Guid> characterIds,
        ComparisonOptions options,
        CancellationToken ct = default) =>
        _operations.CompareCharactersAsync(characterIds, options, ct);

    public Task<Result<CompatibilityMatrix>> GetCompatibilityMatrixAsync(
        IReadOnlyList<Guid> characterIds,
        CancellationToken ct = default) =>
        _operations.GetCompatibilityMatrixAsync(characterIds, ct);

    public Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> SuggestRosterCompletionAsync(
        IReadOnlyList<Guid> currentRoster,
        RosterPreferences preferences,
        CancellationToken ct = default) =>
        _operations.SuggestRosterCompletionAsync(currentRoster, preferences, ct);

    public Task<Result<DiscoveryStatistics>> GetStatisticsAsync(
        CancellationToken ct = default) =>
        _operations.GetStatisticsAsync(ct);

    public Task<Result<UserDiscoveryActivity>> GetUserActivityAsync(
        CancellationToken ct = default) =>
        _operations.GetUserActivityAsync(ct);

    public Task<Result<IReadOnlyList<PopularityTrend>>> GetPopularityTrendsAsync(
        TimeSpan period,
        CancellationToken ct = default) =>
        _operations.GetPopularityTrendsAsync(period, ct);
}
