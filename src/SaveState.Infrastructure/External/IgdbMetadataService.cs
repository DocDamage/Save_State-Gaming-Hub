using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public class IgdbMetadataService : IMetadataService
{
    private readonly IIgdbApiClient _apiClient;
    private readonly ICacheService _cache;
    private readonly ILogger<IgdbMetadataService> _logger;

    public IgdbMetadataService(
        IIgdbApiClient apiClient,
        ICacheService cache,
        ILogger<IgdbMetadataService> logger)
    {
        _apiClient = apiClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GameMetadata> GetGameMetadataAsync(string title, CancellationToken ct = default)
    {
        var cacheKey = $"igdb:metadata:{title.ToLowerInvariant()}";

        var cached = await _cache.GetOrCreateAsync<GameMetadata?>(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

            var result = await FetchMetadataFromApiAsync(title, ct).ConfigureAwait(false);
            return result.IsSuccess ? result.Value : null;
        }).ConfigureAwait(false);

        return cached ?? GameMetadata.Empty;
    }

    public async Task<Result<byte[]>> GetCoverImageAsync(string title, CancellationToken ct = default)
    {
        var metadata = await GetGameMetadataAsync(title, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(metadata.CoverImageUrl))
            return Result<byte[]>.Failure("No cover image URL available", ErrorType.NotFound);

        var imageResult = await _apiClient.DownloadImageAsync(metadata.CoverImageUrl, ct).ConfigureAwait(false);
        return imageResult.IsSuccess
            ? Result<byte[]>.Success(imageResult.Value)
            : Result<byte[]>.Failure(imageResult.Error!, imageResult.ErrorType);
    }

    private async Task<Result<GameMetadata?>> FetchMetadataFromApiAsync(string title, CancellationToken ct)
    {
        try
        {
            var games = await _apiClient.SearchGamesAsync(title, ct).ConfigureAwait(false);
            var bestMatch = games
                .OrderByDescending(g => CalculateSimilarity(title, g.Name))
                .FirstOrDefault(g => CalculateSimilarity(title, g.Name) > 0.3); // Minimum similarity threshold

            if (bestMatch is null)
            {
                _logger.LogInformation("No suitable match found for game title '{Title}'", title);
                return Result<GameMetadata?>.Success(null);
            }

            var metadata = new GameMetadata
            {
                Title = bestMatch.Name,
                Description = bestMatch.Summary,
                ReleaseDate = bestMatch.FirstReleaseDate,
                Genres = bestMatch.Genres.Select(g => g.Name).ToArray(),
                CoverImageUrl = bestMatch.Cover?.Url,
                Developer = null, // IGDB doesn't provide this in search results
                Publisher = null  // IGDB doesn't provide this in search results
            };

            return Result<GameMetadata?>.Success(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch metadata for game '{Title}' from IGDB", title);
            return Result<GameMetadata?>.Failure($"Failed to fetch metadata: {ex.Message}", ErrorType.Internal);
        }
    }

    private static double CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0;

        // Simple Jaccard similarity for game title matching
        var setA = a.ToLowerInvariant()
            .Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
        var setB = b.ToLowerInvariant()
            .Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        var intersection = setA.Intersect(setB).Count();
        var union = setA.Union(setB).Count();

        return union > 0 ? (double)intersection / union : 0;
    }
}
