using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery;

/// <summary>
/// Implementation of character discovery service for MUGEN.
/// Provides search, recommendations, trending, and community features.
/// </summary>
internal class CharacterDiscoveryServiceOperations : ICharacterDiscoveryService
{
    private readonly ILogger<CharacterDiscoveryService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<Guid, DiscoveredCharacter> _characters = new();
    private readonly ConcurrentDictionary<Guid, CharacterDetail> _characterDetails = new();
    private readonly ConcurrentDictionary<Guid, CharacterCollection> _collections = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _userFavorites = new();
    private readonly ConcurrentDictionary<string, List<Guid>> _recentlyViewed = new();

    public CharacterDiscoveryServiceOperations(ILogger<CharacterDiscoveryService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        SeedSampleData();
    }

    #region Search and Discovery

    /// <inheritdoc />
    public async Task<Result<CharacterSearchResult>> SearchCharactersAsync(
        CharacterSearchQuery query,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Searching characters with term: {SearchTerm}", query.SearchTerm);

            var results = _characters.Values.AsEnumerable();

            // Apply filters
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

            // Apply sorting
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

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetRecommendationsAsync(
        RecommendationCriteria criteria,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting character recommendations of type: {Type}", criteria.Type);

            var recommendations = new List<DiscoveredCharacterRecommendation>();

            switch (criteria.Type)
            {
                case RecommendationType.Trending:
                    var trending = await GetTrendingCharactersAsync(TrendingPeriod.ThisWeek, 10, ct);
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
                    recommendations = _characters.Values
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
                    // Personalized recommendations based on tags
                    var chars = _characters.Values.ToList();
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

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TrendingCharacter>>> GetTrendingCharactersAsync(
        TrendingPeriod period,
        int? limit = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting trending characters for period: {Period}", period);

            var characters = _characters.Values
                .OrderByDescending(c => c.DownloadCount)
                .Take(limit ?? 20)
                .Select((c, index) => new TrendingCharacter(
                    c,
                    index + 1,
                    c.DownloadCount > 1000 ? 1 : 0,
                    (int)(c.DownloadCount * 0.1),
                    (int)((c.Rating - 3) * 10)))
                .ToList();

            return Result<IReadOnlyList<TrendingCharacter>>.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trending characters");
            return Result<IReadOnlyList<TrendingCharacter>>.Failure(
                $"Get trending failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetRecentlyAddedAsync(
        int? limit = null,
        CancellationToken ct = default)
    {
        try
        {
            var characters = _characters.Values
                .OrderByDescending(c => c.AddedDate)
                .Take(limit ?? 20)
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacter>>.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recently added characters");
            return Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Get recently added failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByCategoryAsync(
        string category,
        int? limit = null,
        CancellationToken ct = default)
    {
        try
        {
            var characters = _characters.Values
                .Where(c => c.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(c => c.Rating)
                .Take(limit ?? 20)
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacter>>.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters by category");
            return Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Get by category failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetSimilarCharactersAsync(
        Guid characterId,
        int? limit = null,
        CancellationToken ct = default)
    {
        try
        {
            if (!_characters.TryGetValue(characterId, out var sourceCharacter))
            {
                return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Failure(
                    "Character not found", ErrorType.NotFound);
            }

            var similar = _characters.Values
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

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CharacterCombination>>> GetPopularCombinationsAsync(
        int? limit = null,
        CancellationToken ct = default)
    {
        try
        {
            var combinations = new List<CharacterCombination>
            {
                new(new[] { _characters.Values.First(), _characters.Values.Skip(1).First() },
                    500, 55.0, "Popular rushdown duo"),
                new(new[] { _characters.Values.Skip(2).First(), _characters.Values.Skip(3).First() },
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

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByAuthorAsync(
        string authorName,
        int? limit = null,
        CancellationToken ct = default)
    {
        try
        {
            var characters = _characters.Values
                .Where(c => c.Author.Equals(authorName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.AddedDate)
                .Take(limit ?? 20)
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacter>>.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters by author");
            return Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Get by author failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<FeaturedCharacter>> GetFeaturedCharacterAsync(
        CancellationToken ct = default)
    {
        try
        {
            var featured = _characters.Values
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

    #endregion

    #region Character Details

    /// <inheritdoc />
    public async Task<Result<CharacterDetail>> GetCharacterDetailsAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            if (_characterDetails.TryGetValue(characterId, out var detail))
            {
                // Track view
                if (_characters.TryGetValue(characterId, out var discovered))
                {
                    _recentlyViewed.AddOrUpdate("current_user", new List<Guid> { characterId },
                        (k, v) => { v.Insert(0, characterId); return v.Take(20).ToList(); });
                }

                return Result<CharacterDetail>.Success(detail);
            }

            return Result<CharacterDetail>.Failure("Character not found", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character details");
            return Result<CharacterDetail>.Failure(
                $"Get details failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CharacterReviews>> GetCharacterReviewsAsync(
        Guid characterId,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default)
    {
        try
        {
            var reviews = new List<CharacterReview>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Player1", 5, "Amazing!", "Best character ever", _timeProvider.UtcNow.AddDays(-5), 12, 0),
                new(Guid.NewGuid(), Guid.NewGuid(), "Player2", 4, "Great", "Very well made", _timeProvider.UtcNow.AddDays(-3), 8, 1)
            };

            var distribution = new RatingDistribution(10, 5, 3, 1, 0);
            var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var result = new CharacterReviews(
                reviews,
                reviews.Count,
                avgRating,
                distribution,
                page ?? 1,
                pageSize ?? 10);

            return Result<CharacterReviews>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character reviews");
            return Result<CharacterReviews>.Failure(
                $"Get reviews failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CharacterMatchup>>> GetCharacterMatchupsAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            var matchups = _characters.Values
                .Where(c => c.Id != characterId)
                .Take(10)
                .Select(c => new CharacterMatchup(
                    c.Id,
                    c.Name,
                    new Random().Next(50, 100),
                    new Random().Next(30, 80),
                    new Random().NextDouble() * 100))
                .ToList();

            return Result<IReadOnlyList<CharacterMatchup>>.Success(matchups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character matchups");
            return Result<IReadOnlyList<CharacterMatchup>>.Failure(
                $"Get matchups failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CharacterShowcase>>> GetShowcasesAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            var showcases = new List<CharacterShowcase>
            {
                new("Gameplay Showcase", "https://youtube.com/watch?v=example1", null, "Full combo exhibition", "User1", _timeProvider.UtcNow.AddDays(-10), 5000),
                new("Combo Video", "https://youtube.com/watch?v=example2", null, "Advanced combos", "User2", _timeProvider.UtcNow.AddDays(-5), 3000)
            };

            return Result<IReadOnlyList<CharacterShowcase>>.Success(showcases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get showcases");
            return Result<IReadOnlyList<CharacterShowcase>>.Failure(
                $"Get showcases failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<DownloadHistory>> GetDownloadHistoryAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            var entries = new List<DownloadEntry>
            {
                new(_timeProvider.UtcNow.AddDays(-30), "1.0", 500),
                new(_timeProvider.UtcNow.AddDays(-15), "1.1", 800),
                new(_timeProvider.UtcNow.AddDays(-7), "1.2", 1200)
            };

            var history = new DownloadHistory(characterId, entries.Sum(e => e.DownloadCount), entries);
            return Result<DownloadHistory>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download history");
            return Result<DownloadHistory>.Failure(
                $"Get download history failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region User Interaction

    /// <inheritdoc />
    public async Task<Result> RateCharacterAsync(
        Guid characterId,
        int rating,
        string? review = null,
        CancellationToken ct = default)
    {
        try
        {
            if (rating < 1 || rating > 5)
            {
                return Result.Failure("Rating must be between 1 and 5", ErrorType.Validation);
            }

            _logger.LogInformation("Character {CharacterId} rated {Rating} stars", characterId, rating);

            if (_characters.TryGetValue(characterId, out var character))
            {
                // Update rating (simplified)
                var newRating = (character.Rating * character.ReviewCount + rating) / (character.ReviewCount + 1);
                _characters[characterId] = character with
                {
                    Rating = newRating,
                    ReviewCount = character.ReviewCount + 1
                };
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rate character");
            return Result.Failure($"Rating failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> AddToFavoritesAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            _userFavorites.AddOrUpdate("current_user", new List<Guid> { characterId },
                (k, v) => { if (!v.Contains(characterId)) v.Add(characterId); return v; });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add to favorites");
            return Result.Failure($"Add to favorites failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveFromFavoritesAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            if (_userFavorites.TryGetValue("current_user", out var favorites))
            {
                favorites.Remove(characterId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove from favorites");
            return Result.Failure($"Remove from favorites failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetFavoritesAsync(
        CancellationToken ct = default)
    {
        try
        {
            if (_userFavorites.TryGetValue("current_user", out var favorites))
            {
                var characters = favorites
                    .Select(id => _characters.TryGetValue(id, out var c) ? c : null)
                    .Where(c => c != null)
                    .ToList()!;

                return Result<IReadOnlyList<DiscoveredCharacter>>.Success(characters);
            }

            return Result<IReadOnlyList<DiscoveredCharacter>>.Success(new List<DiscoveredCharacter>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get favorites");
            return Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Get favorites failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ReportCharacterAsync(
        Guid characterId,
        CharacterReportReason reason,
        string? details = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogWarning("Character {CharacterId} reported for reason: {Reason}", characterId, reason);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report character");
            return Result.Failure($"Report failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ShareCharacterAsync(
        Guid characterId,
        ShareOptions options,
        CancellationToken ct = default)
    {
        try
        {
            if (!_characters.TryGetValue(characterId, out var character))
            {
                return Result<string>.Failure("Character not found", ErrorType.NotFound);
            }

            var shareUrl = $"https://savestate.app/characters/{characterId}";
            var message = $"Check out {character.Name} by {character.Author}! {shareUrl}";

            _logger.LogInformation("Character shared via {Platform}", options.Platform);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share character");
            return Result<string>.Failure($"Share failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Collections and Lists

    /// <inheritdoc />
    public async Task<Result<CharacterCollection>> CreateCollectionAsync(
        string name,
        string? description = null,
        bool isPublic = true,
        CancellationToken ct = default)
    {
        try
        {
            var collection = new CharacterCollection(
                Guid.NewGuid(),
                name,
                description,
                "CurrentUser",
                isPublic,
                0,
                new List<string>(),
                0,
                0,
                _timeProvider.UtcNow,
                _timeProvider.UtcNow);

            _collections[collection.Id] = collection;
            return Result<CharacterCollection>.Success(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection");
            return Result<CharacterCollection>.Failure(
                $"Create collection failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> AddToCollectionAsync(
        Guid collectionId,
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_collections.TryGetValue(collectionId, out var collection))
            {
                return Result.Failure("Collection not found", ErrorType.NotFound);
            }

            _collections[collectionId] = collection with
            {
                CharacterCount = collection.CharacterCount + 1,
                LastUpdated = _timeProvider.UtcNow
            };

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add to collection");
            return Result.Failure($"Add to collection failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CharacterCollection>>> GetCollectionsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var userCollections = _collections.Values
                .Where(c => c.CreatorName == "CurrentUser")
                .ToList();

            return Result<IReadOnlyList<CharacterCollection>>.Success(userCollections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections");
            return Result<IReadOnlyList<CharacterCollection>>.Failure(
                $"Get collections failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CharacterCollection>>> GetPublicCollectionsAsync(
        int? limit = null,
        CancellationToken ct = default)
    {
        try
        {
            var publicCollections = _collections.Values
                .Where(c => c.IsPublic)
                .OrderByDescending(c => c.FavoriteCount)
                .Take(limit ?? 20)
                .ToList();

            return Result<IReadOnlyList<CharacterCollection>>.Success(publicCollections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get public collections");
            return Result<IReadOnlyList<CharacterCollection>>.Failure(
                $"Get public collections failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetCollectionCharactersAsync(
        Guid collectionId,
        CancellationToken ct = default)
    {
        try
        {
            // In a real implementation, this would retrieve characters from the collection
            var characters = _characters.Values.Take(5).ToList();
            return Result<IReadOnlyList<DiscoveredCharacter>>.Success(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection characters");
            return Result<IReadOnlyList<DiscoveredCharacter>>.Failure(
                $"Get collection characters failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Comparison Tools

    /// <inheritdoc />
    public async Task<Result<CharacterComparison>> CompareCharactersAsync(
        IReadOnlyList<Guid> characterIds,
        ComparisonOptions options,
        CancellationToken ct = default)
    {
        try
        {
            var characters = characterIds
                .Select(id => _characters.TryGetValue(id, out var c) ? c : null)
                .Where(c => c != null)
                .ToList();

            var compared = characters.Select(c => new ComparedCharacter(c!.Id, c.Name, c.ThumbnailUrl)).ToList();

            var categories = new List<ComparisonCategory>
            {
                new("Rating", characters.Select(c => new ComparisonValue(c!.Id, c.Rating.ToString("F1"), c.Rating >= 4.0)).ToList()),
                new("Downloads", characters.Select(c => new ComparisonValue(c!.Id, c.DownloadCount.ToString(), c.DownloadCount > 1000)).ToList()),
                new("Reviews", characters.Select(c => new ComparisonValue(c!.Id, c.ReviewCount.ToString(), c.ReviewCount > 10)).ToList())
            };

            var comparison = new CharacterComparison(compared, categories);
            return Result<CharacterComparison>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare characters");
            return Result<CharacterComparison>.Failure(
                $"Comparison failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CompatibilityMatrix>> GetCompatibilityMatrixAsync(
        IReadOnlyList<Guid> characterIds,
        CancellationToken ct = default)
    {
        try
        {
            var matrixChars = characterIds
                .Select(id => _characters.TryGetValue(id, out var c) ? new MatrixCharacter(c.Id, c.Name) : null)
                .Where(c => c != null)
                .ToList();

            var scores = new List<IReadOnlyList<CompatibilityScore>>();
            var random = new Random();

            foreach (var char1 in matrixChars)
            {
                var row = new List<CompatibilityScore>();
                foreach (var char2 in matrixChars)
                {
                    var score = random.NextDouble() * 100;
                    var level = score switch
                    {
                        > 80 => CompatibilityLevel.Excellent,
                        > 60 => CompatibilityLevel.Good,
                        > 40 => CompatibilityLevel.Fair,
                        > 20 => CompatibilityLevel.Poor,
                        _ => CompatibilityLevel.Incompatible
                    };
                    row.Add(new CompatibilityScore(score, level));
                }
                scores.Add(row);
            }

            var matrix = new CompatibilityMatrix(matrixChars!, scores);
            return Result<CompatibilityMatrix>.Success(matrix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compatibility matrix");
            return Result<CompatibilityMatrix>.Failure(
                $"Get matrix failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> SuggestRosterCompletionAsync(
        IReadOnlyList<Guid> currentRoster,
        RosterPreferences preferences,
        CancellationToken ct = default)
    {
        try
        {
            var needed = preferences.TargetSize - currentRoster.Count;
            if (needed <= 0)
            {
                return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Success(new List<DiscoveredCharacterRecommendation>());
            }

            var suggestions = _characters.Values
                .Where(c => !currentRoster.Contains(c.Id))
                .Take(needed)
                .Select(c => new DiscoveredCharacterRecommendation(
                    c,
                    85.0,
                    $"Suggested for {preferences.Balance} roster",
                    preferences.RequiredTags.Intersect(c.Tags).ToList()))
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suggest roster completion");
            return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Failure(
                $"Suggestion failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Stats and Analytics

    /// <inheritdoc />
    public async Task<Result<DiscoveryStatistics>> GetStatisticsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var stats = new DiscoveryStatistics(
                _characters.Count,
                _characters.Values.Select(c => c.Author).Distinct().Count(),
                _characters.Values.Sum(c => c.DownloadCount),
                _characters.Values.Sum(c => c.ReviewCount),
                _characters.Values.Average(c => c.Rating),
                _characters.Values.SelectMany(c => c.Categories).GroupBy(c => c)
                    .Select(g => new CategoryStat(g.Key, g.Count())).ToList(),
                _characters.Values.SelectMany(c => c.Tags).GroupBy(t => t)
                    .Select(g => new TagStat(g.Key, g.Count(), 4.0)).Take(20).ToList());

            return Result<DiscoveryStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return Result<DiscoveryStatistics>.Failure(
                $"Get statistics failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserDiscoveryActivity>> GetUserActivityAsync(
        CancellationToken ct = default)
    {
        try
        {
            _recentlyViewed.TryGetValue("current_user", out var viewed);
            var viewedChars = viewed?.Take(5).Select(id => _characters.TryGetValue(id, out var c) ? c : null).Where(c => c != null).ToList() ?? new List<DiscoveredCharacter?>();

            var activity = new UserDiscoveryActivity(
                viewed?.Count ?? 0,
                0,
                0,
                0,
                0,
                viewedChars!,
                new List<DiscoveredCharacter>());

            return Result<UserDiscoveryActivity>.Success(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity");
            return Result<UserDiscoveryActivity>.Failure(
                $"Get activity failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PopularityTrend>>> GetPopularityTrendsAsync(
        TimeSpan period,
        CancellationToken ct = default)
    {
        try
        {
            var trends = _characters.Values.Take(5).Select(c =>
            {
                var dailyStats = new List<DailyStat>();
                for (int i = 0; i < 7; i++)
                {
                    dailyStats.Add(new DailyStat(
                        _timeProvider.UtcNow.AddDays(-i),
                        new Random().Next(10, 100),
                        c.Rating));
                }

                return new PopularityTrend(c.Id, c.Name, dailyStats);
            }).ToList();

            return Result<IReadOnlyList<PopularityTrend>>.Success(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get popularity trends");
            return Result<IReadOnlyList<PopularityTrend>>.Failure(
                $"Get trends failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Private Helpers

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

        // Category facet
        var categories = characters.SelectMany(c => c.Categories)
            .GroupBy(c => c)
            .Select(g => new FacetValue(g.Key, g.Count(), false))
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();
        facets.Add(new SearchFacet("Category", categories));

        // Author facet
        var authors = characters.GroupBy(c => c.Author)
            .Select(g => new FacetValue(g.Key, g.Count(), false))
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToList();
        facets.Add(new SearchFacet("Author", authors));

        return facets;
    }

    private void SeedSampleData()
    {
        // Seed sample characters
        var sampleChars = new[]
        {
            new DiscoveredCharacter(
                Guid.NewGuid(), "Ryu", "Capcom", "The wandering warrior",
                new[] { "shotokan", "balanced" }, new[] { "Street Fighter", "Capcom" },
                4.5, 120, 5000, null, _timeProvider.UtcNow.AddDays(-100), _timeProvider.UtcNow.AddDays(-10),
                new DiscoveredCharacterStats(55, 300, 200)),

            new DiscoveredCharacter(
                Guid.NewGuid(), "Ken", "Capcom", "Ryu's rival",
                new[] { "shotokan", "rushdown" }, new[] { "Street Fighter", "Capcom" },
                4.3, 95, 4500, null, _timeProvider.UtcNow.AddDays(-95), _timeProvider.UtcNow.AddDays(-5),
                new DiscoveredCharacterStats(52, 280, 180)),

            new DiscoveredCharacter(
                Guid.NewGuid(), "Chun-Li", "Capcom", "The strongest woman in the world",
                new[] { "speed", "footsies" }, new[] { "Street Fighter", "Capcom" },
                4.7, 150, 6000, null, _timeProvider.UtcNow.AddDays(-90), _timeProvider.UtcNow.AddDays(-8),
                new DiscoveredCharacterStats(58, 350, 250)),

            new DiscoveredCharacter(
                Guid.NewGuid(), "Goku", "Akira Toriyama", "Super Saiyan warrior",
                new[] { "anime", "shoto", "beam" }, new[] { "Dragon Ball", "Anime" },
                4.2, 200, 8000, null, _timeProvider.UtcNow.AddDays(-80), _timeProvider.UtcNow.AddDays(-15),
                new DiscoveredCharacterStats(50, 400, 300)),

            new DiscoveredCharacter(
                Guid.NewGuid(), "Spider-Man", "Marvel", "Friendly neighborhood hero",
                new[] { "marvel", "agile", "zoning" }, new[] { "Marvel", "Comics" },
                4.4, 180, 5500, null, _timeProvider.UtcNow.AddDays(-70), _timeProvider.UtcNow.AddDays(-12),
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

    #endregion
}
