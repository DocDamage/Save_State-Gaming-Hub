using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Implementation of cloud gaming catalog service with hybrid local/remote data and ETag caching.
/// </summary>
public sealed class CloudCatalogService : ICloudCatalogService
{
    private const string CatalogCacheKey = "CloudCatalog";
    private const string ETagCacheKey = "CloudCatalogETag";
    private const string RemoteCatalogUrl = "https://raw.githubusercontent.com/SaveStateReborn/SaveStateReborn/main/data/cloud_catalog/SaveState_Cloud_Catalog.json";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan RemoteRefreshInterval = TimeSpan.FromHours(6);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CloudCatalogService> _logger;
    private readonly string _localCatalogPath;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset _lastRemoteRefresh = DateTimeOffset.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudCatalogService"/> class.
    /// </summary>
    public CloudCatalogService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<CloudCatalogService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;

        // Resolve local catalog path relative to application base
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        _localCatalogPath = Path.Combine(basePath, "data", "cloud_catalog", "SaveState_Cloud_Catalog.json");

        // Fallback to project root for development
        if (!File.Exists(_localCatalogPath))
        {
            var projectRoot = FindProjectRoot(basePath);
            if (projectRoot != null)
            {
                _localCatalogPath = Path.Combine(projectRoot, "data", "cloud_catalog", "SaveState_Cloud_Catalog.json");
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result<CloudCatalog>> GetCatalogAsync(CancellationToken ct = default)
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(CatalogCacheKey, out CloudCatalog? cachedCatalog) && cachedCatalog != null)
            {
                // Trigger background refresh if needed
                _ = TryBackgroundRefreshAsync(ct);
                return Result.Success(cachedCatalog);
            }

            // Load catalog (local first, then try remote)
            var catalog = await LoadCatalogAsync(ct).ConfigureAwait(false);
            if (catalog == null)
            {
                return Result.Failure<CloudCatalog>("Failed to load cloud gaming catalog");
            }

            // Cache the catalog
            _cache.Set(CatalogCacheKey, catalog, CacheDuration);

            return Result.Success(catalog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cloud gaming catalog");
            return Result.Failure<CloudCatalog>($"Failed to get catalog: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> IsGameAvailableOnProviderAsync(
        string gameTitle,
        CloudGamingProvider provider,
        CancellationToken ct = default)
    {
        var catalogResult = await GetCatalogAsync(ct).ConfigureAwait(false);
        if (!catalogResult.IsSuccess)
        {
            return Result.Failure<bool>(catalogResult.Error!);
        }

        var normalizedTitle = NormalizeTitle(gameTitle);
        var entry = catalogResult.Value.Games.FirstOrDefault(g =>
            NormalizeTitle(g.Title).Equals(normalizedTitle, StringComparison.OrdinalIgnoreCase) ||
            g.Title.Contains(gameTitle, StringComparison.OrdinalIgnoreCase) ||
            gameTitle.Contains(g.Title, StringComparison.OrdinalIgnoreCase));

        var isAvailable = entry?.Providers.Contains(provider) ?? false;

        _logger.LogDebug("Game '{Title}' availability on {Provider}: {Available} (catalog match: {Match})",
            gameTitle, provider, isAvailable, entry?.Title ?? "none");

        return Result.Success(isAvailable);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> IsGameAvailableOnProviderBySteamIdAsync(
        int steamAppId,
        CloudGamingProvider provider,
        CancellationToken ct = default)
    {
        if (steamAppId <= 0)
        {
            return Result.Success(false);
        }

        var catalogResult = await GetCatalogAsync(ct).ConfigureAwait(false);
        if (!catalogResult.IsSuccess)
        {
            return Result.Failure<bool>(catalogResult.Error!);
        }

        var entry = catalogResult.Value.Games.FirstOrDefault(g => g.SteamAppId == steamAppId);
        var isAvailable = entry?.Providers.Contains(provider) ?? false;

        return Result.Success(isAvailable);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CloudGamingProvider>>> GetAvailableProvidersForGameAsync(
        string gameTitle,
        CancellationToken ct = default)
    {
        var catalogResult = await GetCatalogAsync(ct).ConfigureAwait(false);
        if (!catalogResult.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<CloudGamingProvider>>(catalogResult.Error!);
        }

        var normalizedTitle = NormalizeTitle(gameTitle);
        var entry = catalogResult.Value.Games.FirstOrDefault(g =>
            NormalizeTitle(g.Title).Equals(normalizedTitle, StringComparison.OrdinalIgnoreCase) ||
            g.Title.Contains(gameTitle, StringComparison.OrdinalIgnoreCase) ||
            gameTitle.Contains(g.Title, StringComparison.OrdinalIgnoreCase));

        var providers = entry?.Providers ?? Array.Empty<CloudGamingProvider>();
        return Result.Success<IReadOnlyList<CloudGamingProvider>>(providers.ToList());
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CloudGamingProvider>>> GetAvailableProvidersForGameBySteamIdAsync(
        int steamAppId,
        CancellationToken ct = default)
    {
        if (steamAppId <= 0)
        {
            return Result.Success<IReadOnlyList<CloudGamingProvider>>(Array.Empty<CloudGamingProvider>());
        }

        var catalogResult = await GetCatalogAsync(ct).ConfigureAwait(false);
        if (!catalogResult.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<CloudGamingProvider>>(catalogResult.Error!);
        }

        var entry = catalogResult.Value.Games.FirstOrDefault(g => g.SteamAppId == steamAppId);
        var providers = entry?.Providers ?? Array.Empty<CloudGamingProvider>();
        return Result.Success<IReadOnlyList<CloudGamingProvider>>(providers.ToList());
    }

    /// <inheritdoc />
    public async Task<Result<bool>> RefreshCatalogAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var remoteCatalog = await FetchRemoteCatalogAsync(ct).ConfigureAwait(false);
            if (remoteCatalog == null)
            {
                _logger.LogWarning("Failed to fetch remote catalog, keeping cached version");
                return Result.Success(false);
            }

            // Update cache
            _cache.Set(CatalogCacheKey, remoteCatalog, CacheDuration);
            _lastRemoteRefresh = DateTimeOffset.UtcNow;

            _logger.LogInformation("Cloud catalog refreshed successfully - {Count} games, version {Version}",
                remoteCatalog.Games.Count, remoteCatalog.Version);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cloud catalog");
            return Result.Failure<bool>($"Failed to refresh catalog: {ex.Message}");
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<CloudCatalogEntry?>> GetCatalogEntryAsync(
        string gameTitle,
        CancellationToken ct = default)
    {
        var catalogResult = await GetCatalogAsync(ct).ConfigureAwait(false);
        if (!catalogResult.IsSuccess)
        {
            return Result.Failure<CloudCatalogEntry?>(catalogResult.Error!);
        }

        var normalizedTitle = NormalizeTitle(gameTitle);
        var entry = catalogResult.Value.Games.FirstOrDefault(g =>
            NormalizeTitle(g.Title).Equals(normalizedTitle, StringComparison.OrdinalIgnoreCase) ||
            g.Title.Contains(gameTitle, StringComparison.OrdinalIgnoreCase) ||
            gameTitle.Contains(g.Title, StringComparison.OrdinalIgnoreCase));

        return Result.Success(entry);
    }

    private async Task<CloudCatalog?> LoadCatalogAsync(CancellationToken ct)
    {
        // Try local catalog first
        var localCatalog = await LoadLocalCatalogAsync(ct).ConfigureAwait(false);
        if (localCatalog != null)
        {
            _logger.LogInformation("Loaded local cloud catalog - {Count} games, version {Version}",
                localCatalog.Games.Count, localCatalog.Version);
            return localCatalog;
        }

        // Fall back to remote catalog
        var remoteCatalog = await FetchRemoteCatalogAsync(ct).ConfigureAwait(false);
        if (remoteCatalog != null)
        {
            _lastRemoteRefresh = DateTimeOffset.UtcNow;
            return remoteCatalog;
        }

        _logger.LogWarning("No cloud catalog available - local and remote sources failed");
        return null;
    }

    private async Task<CloudCatalog?> LoadLocalCatalogAsync(CancellationToken ct)
    {
        try
        {
            if (!File.Exists(_localCatalogPath))
            {
                _logger.LogDebug("Local catalog not found at {Path}", _localCatalogPath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_localCatalogPath, ct).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<CloudCatalogDto>(json, JsonOptions);

            return dto == null ? null : ConvertToCatalog(dto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load local catalog from {Path}", _localCatalogPath);
            return null;
        }
    }

    private async Task<CloudCatalog?> FetchRemoteCatalogAsync(CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("CloudCatalog");

            // Use ETag for conditional requests
            var request = new HttpRequestMessage(HttpMethod.Get, RemoteCatalogUrl);
            if (_cache.TryGetValue(ETagCacheKey, out string? etag) && !string.IsNullOrEmpty(etag))
            {
                request.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{etag}\""));
            }

            var response = await client.SendAsync(request, ct).ConfigureAwait(false);

            // If not modified, return null to indicate no update needed
            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                _logger.LogDebug("Remote catalog not modified (ETag match)");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<CloudCatalogDto>(json, JsonOptions);

            // Cache the new ETag
            if (response.Headers.ETag != null)
            {
                _cache.Set(ETagCacheKey, response.Headers.ETag.Tag.Trim('"'), CacheDuration);
            }

            _logger.LogInformation("Fetched remote cloud catalog successfully");
            return dto == null ? null : ConvertToCatalog(dto);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch remote catalog from {Url}", RemoteCatalogUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching remote catalog");
            return null;
        }
    }

    private async Task TryBackgroundRefreshAsync(CancellationToken ct)
    {
        // Only refresh if enough time has passed
        if (DateTimeOffset.UtcNow - _lastRemoteRefresh < RemoteRefreshInterval)
        {
            return;
        }

        // Don't wait for the refresh
        _ = Task.Run(async () =>
        {
            try
            {
                await RefreshCatalogAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background catalog refresh failed");
            }
        }, ct);
    }

    private static CloudCatalog ConvertToCatalog(CloudCatalogDto dto)
    {
        var entries = dto.Games.Select(g => new CloudCatalogEntry(
            g.Title,
            g.SteamAppId,
            g.Providers.Select(ParseProvider).OfType<CloudGamingProvider>().ToList()
        )).ToList();

        return new CloudCatalog(dto.Version, dto.LastUpdated, entries);
    }

    private static CloudGamingProvider? ParseProvider(string provider)
    {
        return provider switch
        {
            "GeForceNow" => CloudGamingProvider.GeForceNow,
            "XboxCloud" => CloudGamingProvider.XboxCloud,
            "AmazonLuna" => CloudGamingProvider.AmazonLuna,
            "PlayStationNow" => CloudGamingProvider.PlayStationNow,
            "Boosteroid" => CloudGamingProvider.Boosteroid,
            _ => null
        };
    }

    private static string NormalizeTitle(string title)
    {
        // Remove common punctuation and normalize for fuzzy matching
        return title
            .Replace("'", "")
            .Replace("'", "")
            .Replace(":", "")
            .Replace("-", " ")
            .Replace("  ", " ")
            .Trim()
            .ToLowerInvariant();
    }

    private static string? FindProjectRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SaveState.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        return null;
    }

    // DTO classes for JSON deserialization
    private sealed record CloudCatalogDto(
        string Version,
        DateTimeOffset LastUpdated,
        List<CloudCatalogGameDto> Games);

    private sealed record CloudCatalogGameDto(
        string Title,
        int SteamAppId,
        List<string> Providers);
}
