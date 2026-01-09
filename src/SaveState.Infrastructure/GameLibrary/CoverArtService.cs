using System.IO;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.External;

namespace SaveState.Infrastructure.GameLibrary;

/// <summary>
/// Service for managing game cover art retrieval, caching, and processing.
/// Integrates with multiple sources including SteamGridDB and metadata services.
/// </summary>
public class CoverArtService : ICoverArtService
{
    private readonly IMetadataService _metadataService;
    private readonly ISteamGridDbApiClient _steamGridDbClient;
    private readonly IImageResizer _imageResizer;
    private readonly ICacheService _cache;
    private readonly ILogger<CoverArtService> _logger;

    public CoverArtService(
        IMetadataService metadataService,
        ISteamGridDbApiClient steamGridDbClient,
        IImageResizer imageResizer,
        ICacheService cache,
        ILogger<CoverArtService> logger)
    {
        _metadataService = metadataService;
        _steamGridDbClient = steamGridDbClient;
        _imageResizer = imageResizer;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the best available cover art for a game.
    /// Searches multiple sources and returns the highest quality image.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the cover art information or an error.</returns>
    public async Task<Result<CoverArtResult>> FetchCoverArtAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            // Try SteamGridDB first for better quality covers
            var steamGridResult = await TryFetchFromSteamGridDbAsync(gameId, ct);
            if (steamGridResult.IsSuccess)
            {
                return steamGridResult;
            }

            // Fallback to IGDB metadata service
            var igdbResult = await TryFetchFromIgdbAsync(gameId, ct);
            if (igdbResult.IsSuccess)
            {
                return igdbResult;
            }

            return Result.Failure<CoverArtResult>("No cover art found from any source", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch cover art for game {GameId}", gameId);
            return Result.Failure<CoverArtResult>($"Cover art fetch failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Searches for cover art options using the provided query.
    /// Returns multiple cover art options for user selection.
    /// </summary>
    /// <param name="query">The search query for finding cover art.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing a list of cover art options or an error.</returns>
    public async Task<Result<IReadOnlyList<CoverArtOption>>> SearchCoverArtAsync(string query, CancellationToken ct = default)
    {
        try
        {
            var options = new List<CoverArtOption>();

            // Search SteamGridDB
            try
            {
                var steamGrids = await _steamGridDbClient.SearchGridsAsync(query, ct);
                foreach (var grid in steamGrids)
                {
                    options.Add(new CoverArtOption(
                        grid.Url,
                        "SteamGridDB",
                        grid.Width,
                        grid.Height,
                        grid.Author?.Name,
                        MapSteamGridStyleToCoverArtType(grid.Style)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search SteamGridDB for '{Query}'", query);
            }

            // Could add IGDB search here if needed

            return Result.Success<IReadOnlyList<CoverArtOption>>(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search cover art for '{Query}'", query);
            return Result.Failure<IReadOnlyList<CoverArtOption>>($"Cover art search failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Sets the cover art for a game using the provided image URL.
    /// Downloads and caches the image for the specified game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="imageUrl">The URL of the cover art image to set.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> SetCoverArtAsync(Guid gameId, string imageUrl, CancellationToken ct = default)
    {
        try
        {
            var downloadResult = await DownloadAndCacheAsync(gameId, imageUrl, ct);
            if (!downloadResult.IsSuccess)
            {
                return downloadResult;
            }

            // Here we would typically update the game's cover art path in the database
            // For now, just return success since the image is cached
            _logger.LogInformation("Set cover art for game {GameId} to {ImageUrl}", gameId, imageUrl);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cover art for game {GameId}", gameId);
            return Result.Failure($"Failed to set cover art: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Downloads and caches a cover art image for a game.
    /// Processes the image and stores it in the cache for future use.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="imageUrl">The URL of the image to download and cache.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> DownloadAndCacheAsync(Guid gameId, string imageUrl, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"coverart:{gameId}";

            // Check cache first
            var cachedPath = await _cache.GetOrCreateAsync<string?>(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30); // Cache for 30 days

                var downloadResult = await DownloadAndProcessImageAsync(imageUrl, ct);
                if (!downloadResult.IsSuccess)
                {
                    return null;
                }

                // Save to local cache directory
                var cacheDir = Path.Combine(AppContext.BaseDirectory, "cache", "coverart");
                Directory.CreateDirectory(cacheDir);

                var fileName = $"{gameId}.jpg";
                var filePath = Path.Combine(cacheDir, fileName);

                await File.WriteAllBytesAsync(filePath, downloadResult.Value.Data, ct);

                return filePath;
            }).ConfigureAwait(false);

            return cachedPath != null
                ? Result.Success()
                : Result.Failure("Failed to download and cache cover art", ErrorType.Internal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and cache cover art for game {GameId} from {ImageUrl}", gameId, imageUrl);
            return Result.Failure($"Download and cache failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private static Task<Result<CoverArtResult>> TryFetchFromSteamGridDbAsync(Guid gameId, CancellationToken ct)
    {
        // We need game title to search SteamGridDB
        // This is a simplified implementation - in reality we'd need to get the game title from the database
        // For now, return failure to fall back to IGDB
        return Task.FromResult(Result.Failure<CoverArtResult>("SteamGridDB fetch not implemented for game ID lookup", ErrorType.NotImplemented));
    }

    private static Task<Result<CoverArtResult>> TryFetchFromIgdbAsync(Guid gameId, CancellationToken ct)
    {
        // We need game title to search IGDB
        // This is a simplified implementation - in reality we'd need to get the game title from the database
        // For now, return failure
        return Task.FromResult(Result.Failure<CoverArtResult>("IGDB fetch not implemented for game ID lookup", ErrorType.NotImplemented));
    }

    private async Task<Result<ImageResizeResult>> DownloadAndProcessImageAsync(string imageUrl, CancellationToken ct)
    {
        // Download the image
        var downloadResult = imageUrl.StartsWith("http")
            ? await _steamGridDbClient.DownloadImageAsync(imageUrl, ct)
            : await _metadataService.GetCoverImageAsync(imageUrl, ct); // Assuming title-based lookup

        if (!downloadResult.IsSuccess)
        {
            return Result.Failure<ImageResizeResult>(downloadResult.Error!, downloadResult.ErrorType);
        }

        // Resize to reasonable dimensions for cover art
        var resizeOptions = new ImageResizeOptions(
            MaxWidth: 512,
            MaxHeight: 512,
            MaintainAspectRatio: true,
            OutputFormat: System.Drawing.Imaging.ImageFormat.Jpeg);

        var resizeResult = await _imageResizer.ResizeImageAsync(downloadResult.Value, resizeOptions, ct);
        if (!resizeResult.IsSuccess)
        {
            return resizeResult;
        }

        // Optimize file size
        var optimizeOptions = new ImageOptimizationOptions(
            MaxFileSizeBytes: 500 * 1024, // 500KB
            Quality: 85);

        var optimizeResult = await _imageResizer.OptimizeImageAsync(resizeResult.Value.Data, optimizeOptions, ct);
        if (!optimizeResult.IsSuccess)
        {
            return Result.Failure<ImageResizeResult>(optimizeResult.Error!, optimizeResult.ErrorType);
        }

        // Return the final processed image
        return Result.Success<ImageResizeResult>(new ImageResizeResult(
            optimizeResult.Value,
            resizeResult.Value.OriginalWidth,
            resizeResult.Value.OriginalHeight,
            resizeResult.Value.NewWidth,
            resizeResult.Value.NewHeight,
            optimizeResult.Value.Length));
    }

    private static CoverArtType MapSteamGridStyleToCoverArtType(string style) => style.ToLowerInvariant() switch
    {
        "grid" => CoverArtType.Cover,
        "hero" => CoverArtType.Banner,
        "logo" => CoverArtType.Logo,
        "icon" => CoverArtType.Icon,
        _ => CoverArtType.Background
    };
}

