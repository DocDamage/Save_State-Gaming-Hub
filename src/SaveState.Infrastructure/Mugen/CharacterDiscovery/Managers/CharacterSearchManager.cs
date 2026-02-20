using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery.Managers;

/// <summary>
/// Manages character search, recommendations, trending, and discovery features.
/// </summary>
public sealed class CharacterSearchManager
{
    private readonly ILogger<CharacterSearchManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public CharacterSearchManager(
        ILogger<CharacterSearchManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CharacterSearchResult>> SearchCharactersAsync(
        CharacterSearchQuery query,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Searching characters with term: {SearchTerm}", query.SearchTerm);

            var results = characters.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLowerInvariant();
                results = results.Where(c =>
                    c.Name.ToLowerInvariant().Contains(term) ||
                    c.Author.ToLowerInvariant().Contains(term) ||
                    c.Description?.ToLowerInvariant().Contains(term) == true);
            }

            if (query.Tags?.Any() == true)
            {
                results = results.Where(c => query.Tags.Any(t => c.Tags.Contains(t)));
            }

            if (query.Authors?.Any() == true)
            {
                results = results.Where(c => query.Authors.Contains(c.Author));
            }

            if (query.MinRating.HasValue)
            {
                results = results.Where(c => c.Rating >= query.MinRating.Value);
            }

            if (query.MaxRating.HasValue)
            {
                results = results.Where(c => c.Rating <= query.MaxRating.Value);
            }

            if (query.AddedAfter.HasValue)
            {
                results = results.Where(c => c.AddedDate >= query.AddedAfter.Value);
            }

            if (query.AddedBefore.HasValue)
            {
                results = results.Where(c => c.AddedDate <= query.AddedBefore.Value);
            }

            if (query.MinDownloads.HasValue)
            {
                results = results.Where(c => c.DownloadCount >= query.MinDownloads.Value);
            }

            results = query.SortBy?.ToLowerInvariant() switch
            {
                "name" => query.SortDescending ? results.OrderByDescending(c => c.Name) : results.OrderBy(c => c.Name),
                "rating" => query.SortDescending ? results.OrderByDescending(c => c.Rating) : results.OrderBy(c => c.Rating),
                "downloads" => query.SortDescending ? results.OrderByDescending(c => c.DownloadCount) : results.OrderBy(c => c.DownloadCount),
                "date" => query.SortDescending ? results.OrderByDescending(c => c.AddedDate) : results.OrderBy(c => c.AddedDate),
                _ => results.OrderByDescending(c => c.DownloadCount)
            };

            var totalCount = results.Count();
            var pagedResults = results
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var facets = GenerateSearchFacets(results);

            var searchResult = new CharacterSearchResult(
                pagedResults,
                totalCount,
                query.Page,
                query.PageSize,
                (int)Math.Ceiling((double)totalCount / query.PageSize),
                facets);

            return Result<CharacterSearchResult>.Success(searchResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search characters");
            return Result<CharacterSearchResult>.Failure($"Search failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetRecommendationsAsync(
        RecommendationCriteria criteria,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting character recommendations of type: {Type}", criteria.Type);

            var recommendations = new List<DiscoveredCharacterRecommendation>();

            switch (criteria.Type)
            {
                case RecommendationType.Trending:
                    var trending = await GetTrendingCharactersAsync(TrendingPeriod.ThisWeek, 10, characters, ct);
                    if (trending.IsSuccess && trending.Value != null)
                    {
                        recommendations = trending.Value.Select(t => new DiscoveredCharacterRecommendation(
                            t.Character,
                            t.Rank * 10.0,
                            "Trending this week",
                            new List<string>())).ToList();
                    }
                    break;

                case RecommendationType.StaffPick:
                    recommendations = characters.Values
                        .Where(c => c.Rating >= 4.5)
                        .Take(10)
                        .Select(c => new DiscoveredCharacterRecommendation(
                            c,
                            c.Rating * 20,
                            "Staff Pick",
                            c.Tags.Take(3).ToList()))
                        .ToList();
                    break;

                default:
                    var chars = characters.Values.ToList();
                    if (criteria.PreferredTags?.Any() == true)
                    {
                        chars = chars.Where(c => criteria.PreferredTags.Any(t => c.Tags.Contains(t))).ToList();
                    }

                    recommendations = chars
                        .OrderByDescending(c => c.Rating)
                        .Take(10)
                        .Select(c => new DiscoveredCharacterRecommendation(
                            c,
                            c.Rating * 15 + c.DownloadCount / 100.0,
                            "Recommended for you",
                            c.Tags.Intersect(criteria.PreferredTags ?? new List<string>()).ToList()))
                        .ToList();
                    break;
            }

            return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Success(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations");
            return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Failure(
                $"Recommendations failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<TrendingCharacter>>> GetTrendingCharactersAsync(
        TrendingPeriod period,
        int? limit,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting trending characters for period: {Period}", period);

            var result = characters.Values
                .OrderByDescending(c => c.DownloadCount)
                .Take(limit ?? 20)
                .Select((c, index) => new TrendingCharacter(
                    c,
                    index + 1,
                    c.DownloadCount > 1000 ? 1 : 0,
                    (int)(c.DownloadCount * 0.1),
                    (int)((c.Rating - 3) * 10)))
                .ToList();

            return Result<IReadOnlyList<TrendingCharacter>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trending characters");
            return Result<IReadOnlyList<TrendingCharacter>>.Failure(
                $"Get trending failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetRecentlyAddedAsync(
        int? limit,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var result = characters.Values
                .OrderByDescending(c => c.AddedDate)
                .Take(limit ?? 20)
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacter>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recently added characters");
            return Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Get recently added failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByCategoryAsync(
        string category,
        int? limit,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var result = characters.Values
                .Where(c => c.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(c => c.Rating)
                .Take(limit ?? 20)
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacter>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters by category");
            return Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Get by category failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetSimilarCharactersAsync(
        Guid characterId,
        int? limit,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            if (!characters.TryGetValue(characterId, out var sourceCharacter))
            {
                return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Failure(
                    "Character not found", ErrorType.NotFound);
            }

            var similar = characters.Values
                .Where(c => c.Id != characterId)
                .Select(c => new
                {
                    Character = c,
                    Score = CalculateSimilarityScore(sourceCharacter, c)
                })
                .OrderByDescending(x => x.Score)
                .Take(limit ?? 10)
                .Select(x => new DiscoveredCharacterRecommendation(
                    x.Character,
                    x.Score,
                    "Similar characters",
                    sourceCharacter.Tags.Intersect(x.Character.Tags).ToList()))
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Success(similar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get similar characters");
            return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Failure(
                $"Get similar failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<CharacterCombination>>> GetPopularCombinationsAsync(
        int? limit,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var combinations = new List<CharacterCombination>
            {
                new(new[] { characters.Values.First(), characters.Values.Skip(1).First() },
                    500, 55.0, "Popular rushdown duo"),
                new(new[] { characters.Values.Skip(2).First(), characters.Values.Skip(3).First() },
                    350, 52.0, "Zoning team")
            };

            return Result<IReadOnlyList<CharacterCombination>>.Success(combinations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get popular combinations");
            return Result<IReadOnlyList<CharacterCombination>>.Failure(
                $"Get combinations failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByAuthorAsync(
        string authorName,
        int? limit,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var result = characters.Values
                .Where(c => c.Author.Equals(authorName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.AddedDate)
                .Take(limit ?? 20)
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacter>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters by author");
            return Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Get by author failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<FeaturedCharacter>> GetFeaturedCharacterAsync(
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var featured = characters.Values
                .OrderByDescending(c => c.Rating)
                .FirstOrDefault();

            if (featured == null)
            {
                return Result<FeaturedCharacter>.Failure("No characters available", ErrorType.NotFound);
            }

            var result = new FeaturedCharacter(
                featured,
                FeaturedReason.StaffPick,
                _timeProvider.UtcNow,
                _timeProvider.UtcNow.AddDays(7));

            return Result<FeaturedCharacter>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get featured character");
            return Result<FeaturedCharacter>.Failure(
                $"Get featured failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private double CalculateSimilarityScore(DiscoveredCharacter a, DiscoveredCharacter b)
    {
        var tagScore = a.Tags.Intersect(b.Tags).Count() * 10.0;
        var categoryScore = a.Categories.Intersect(b.Categories).Count() * 5.0;
        var ratingScore = 5 - Math.Abs(a.Rating - b.Rating);

        return tagScore + categoryScore + ratingScore;
    }

    private IReadOnlyList<SearchFacet> GenerateSearchFacets(IEnumerable<DiscoveredCharacter> characters)
    {
        var facets = new List<SearchFacet>();

        var categories = characters.SelectMany(c => c.Categories)
            .GroupBy(c => c)
            .Select(g => new FacetValue(g.Key, g.Count(), false))
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();
        facets.Add(new SearchFacet("Category", categories));

        var authors = characters.GroupBy(c => c.Author)
            .Select(g => new FacetValue(g.Key, g.Count(), false))
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();
        facets.Add(new SearchFacet("Author", authors));

        return facets;
    }
}
