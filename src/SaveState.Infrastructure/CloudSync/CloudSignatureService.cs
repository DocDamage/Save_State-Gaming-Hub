using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SaveState.Core.CloudSync.Services;
using SaveState.Core.Common;
using SaveState.Infrastructure.CloudSync.Models;

namespace SaveState.Infrastructure.CloudSync;

/// <summary>
/// Implementation of the cloud signature database service.
/// Provides HTTP client-based access to the cloud signature API.
/// </summary>
public class CloudSignatureService : ICloudSignatureDatabase
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CloudSignatureService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudSignatureService"/> class.
    /// </summary>
    public CloudSignatureService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<CloudSignatureService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _baseUrl = configuration["CloudSignatureDatabase:BaseUrl"] ?? "https://api.savestatereborn.com/signatures";
        _apiKey = configuration["CloudSignatureDatabase:ApiKey"] ?? "";
        
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    /// <inheritdoc />
    public async Task<Result<CloudSignatureSearchResult>> SearchSignaturesAsync(
        CloudSignatureSearchRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"search_{request.GameTitle}_{request.PatternType}_{request.SortBy}_{request.Skip}_{request.Take}";
            
            if (_cache.TryGetValue(cacheKey, out CloudSignatureSearchResult? cachedResult))
            {
                _logger.LogDebug("Cache hit for signature search: {CacheKey}", cacheKey);
                return Result<CloudSignatureSearchResult>.Success(cachedResult!);
            }

            _logger.LogInformation(
                "Searching cloud signatures for game: {GameTitle}", 
                request.GameTitle);

            var query = new Dictionary<string, string>
            {
                ["skip"] = request.Skip.ToString(),
                ["take"] = request.Take.ToString(),
                ["sortBy"] = request.SortBy.ToString()
            };
            
            if (!string.IsNullOrEmpty(request.GameTitle))
                query["gameTitle"] = request.GameTitle;
            if (!string.IsNullOrEmpty(request.PatternType))
                query["patternType"] = request.PatternType;
            if (!string.IsNullOrEmpty(request.Platform))
                query["platform"] = request.Platform;
            if (!string.IsNullOrEmpty(request.GameVersion))
                query["gameVersion"] = request.GameVersion;

            var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var response = await _httpClient.GetAsync($"{_baseUrl}/search?{queryString}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Signature search failed: {StatusCode} - {Error}", response.StatusCode, error);
                return Result<CloudSignatureSearchResult>.Failure("Failed to search signatures", ErrorType.External);
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<SignatureSearchResponse>(ct);
            var result = new CloudSignatureSearchResult
            {
                Signatures = apiResponse?.Items.Select(MapToCloudSignature).ToList() ?? new List<CloudSignature>(),
                TotalCount = apiResponse?.TotalCount ?? 0,
                HasMore = apiResponse?.HasMore ?? false
            };
            
            // Cache for 5 minutes
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            
            return Result<CloudSignatureSearchResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cloud signatures");
            return Result<CloudSignatureSearchResult>.Failure("Search failed", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CloudSignature>> GetSignatureAsync(string signatureId, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"sig_{signatureId}";
            
            if (_cache.TryGetValue(cacheKey, out CloudSignature? cached))
            {
                return Result<CloudSignature>.Success(cached!);
            }

            var response = await _httpClient.GetAsync($"{_baseUrl}/{signatureId}", ct);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result<CloudSignature>.Failure("Signature not found", ErrorType.NotFound);
            }
            
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<SignatureItemResponse>(ct);
            var signature = MapToCloudSignature(apiResponse!);
            
            _cache.Set(cacheKey, signature, TimeSpan.FromMinutes(10));
            
            return Result<CloudSignature>.Success(signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signature {SignatureId}", signatureId);
            return Result<CloudSignature>.Failure("Failed to get signature", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<CloudSignature>>> GetSignaturesForGameAsync(
        string gameTitle, 
        string? gameVersion = null,
        CancellationToken ct = default)
    {
        var request = new CloudSignatureSearchRequest
        {
            GameTitle = gameTitle,
            GameVersion = gameVersion,
            Take = 100
        };
        
        var result = await SearchSignaturesAsync(request, ct);
        
        if (result.IsSuccess)
        {
            return Result<List<CloudSignature>>.Success(result.Value.Signatures);
        }
        
        return Result<List<CloudSignature>>.Failure(result.Error!, result.ErrorType);
    }

    /// <inheritdoc />
    public async Task<Result<CloudSignatureUploadResult>> UploadSignatureAsync(
        CloudSignatureUploadRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Uploading signature for game: {GameTitle}, pattern: {PatternName}",
                request.GameTitle,
                request.Signature.Name);

            var payload = new SignatureUploadPayload
            {
                GameTitle = request.GameTitle,
                GameVersion = request.GameVersion,
                Platform = request.Platform,
                Name = request.Signature.Name,
                Category = request.Signature.Tags.FirstOrDefault() ?? "general",
                Pattern = request.Signature.Pattern,
                Offset = request.Signature.Offset,
                ValueType = request.Signature.ValueType,
                Description = request.Signature.Description,
                Author = request.Author,
                Notes = request.Notes
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/upload", payload, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return Result<CloudSignatureUploadResult>.Failure(
                    $"Upload failed: {error}", 
                    ErrorType.External);
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<SignatureUploadResponse>(ct);
            
            var result = new CloudSignatureUploadResult
            {
                SignatureId = apiResponse!.Id,
                Status = apiResponse.Status,
                ReviewUrl = apiResponse.ReviewUrl
            };
            
            _logger.LogInformation(
                "Signature uploaded successfully. ID: {SignatureId}, Status: {Status}",
                result.SignatureId,
                result.Status);
            
            return Result<CloudSignatureUploadResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading signature");
            return Result<CloudSignatureUploadResult>.Failure("Upload failed", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result> VoteSignatureAsync(string signatureId, bool isUpvote, CancellationToken ct = default)
    {
        try
        {
            var payload = new VoteRequest { IsUpvote = isUpvote };
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/{signatureId}/vote", 
                payload, 
                ct);
            
            response.EnsureSuccessStatusCode();
            
            // Invalidate cache
            _cache.Remove($"sig_{signatureId}");
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voting on signature");
            return Result.Failure("Vote failed", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SignatureSyncResult>> GetChangesSinceAsync(DateTime since, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting signature changes since {Since}", since);
            
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/sync/changes?since={since:O}", 
                ct);
            
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<SyncChangesResponse>(ct);
            
            var result = new SignatureSyncResult
            {
                NewSignatures = apiResponse?.New.Select(MapToCloudSignature).ToList() ?? new List<CloudSignature>(),
                UpdatedSignatures = apiResponse?.Updated.Select(MapToCloudSignature).ToList() ?? new List<CloudSignature>(),
                DeprecatedSignatures = apiResponse?.Deprecated ?? new List<string>(),
                SyncTimestamp = apiResponse?.Timestamp ?? DateTime.UtcNow
            };
            
            _logger.LogInformation(
                "Sync result: {NewCount} new, {UpdatedCount} updated, {DeprecatedCount} deprecated",
                result.NewSignatures.Count,
                result.UpdatedSignatures.Count,
                result.DeprecatedSignatures.Count);
            
            return Result<SignatureSyncResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signature changes");
            return Result<SignatureSyncResult>.Failure("Sync failed", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SignatureSyncManifest>> GetSyncManifestAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/sync/manifest", ct);
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<SyncManifestResponse>(ct);
            
            var manifest = new SignatureSyncManifest
            {
                LastUpdated = apiResponse!.LastUpdated,
                TotalSignatures = apiResponse.TotalSignatures,
                SupportedGames = apiResponse.SupportedGames,
                ETag = apiResponse.ETag
            };
            
            return Result<SignatureSyncManifest>.Success(manifest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync manifest");
            return Result<SignatureSyncManifest>.Failure("Failed to get manifest", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<GameVersionInfo>>> GetSupportedGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/games", ct);
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<List<GameVersionInfoResponse>>(ct);
            
            var result = apiResponse?.Select(g => new GameVersionInfo
            {
                GameTitle = g.GameTitle,
                Versions = g.Versions,
                Platforms = g.Platforms
            }).ToList() ?? new List<GameVersionInfo>();
            
            return Result<List<GameVersionInfo>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported games");
            return Result<List<GameVersionInfo>>.Success(new List<GameVersionInfo>());
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateSignatureAsync(string signatureId, CloudSignatureUpdateRequest request, CancellationToken ct = default)
    {
        try
        {
            var payload = new UpdateRequest
            {
                Description = request.Description,
                Notes = request.Notes
            };
            
            var response = await _httpClient.PatchAsJsonAsync($"{_baseUrl}/{signatureId}", payload, ct);
            response.EnsureSuccessStatusCode();
            
            // Invalidate cache
            _cache.Remove($"sig_{signatureId}");
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating signature {SignatureId}", signatureId);
            return Result.Failure("Update failed", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteSignatureAsync(string signatureId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/{signatureId}", ct);
            response.EnsureSuccessStatusCode();
            
            // Invalidate cache
            _cache.Remove($"sig_{signatureId}");
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting signature {SignatureId}", signatureId);
            return Result.Failure("Delete failed", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ReportSignatureAsync(string signatureId, SignatureReportRequest request, CancellationToken ct = default)
    {
        try
        {
            var payload = new ReportRequest
            {
                Reason = request.Reason,
                Details = request.Details
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/{signatureId}/report", payload, ct);
            response.EnsureSuccessStatusCode();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting signature {SignatureId}", signatureId);
            return Result.Failure("Report failed", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SignatureStats>> GetSignatureStatsAsync(string signatureId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/{signatureId}/stats", ct);
            response.EnsureSuccessStatusCode();
            
            var apiResponse = await response.Content.ReadFromJsonAsync<SignatureStatsResponse>(ct);
            
            var stats = new SignatureStats
            {
                TotalDownloads = apiResponse!.TotalDownloads,
                SuccessCount = apiResponse.SuccessCount,
                FailureCount = apiResponse.FailureCount,
                Upvotes = apiResponse.Upvotes,
                Downvotes = apiResponse.Downvotes
            };
            
            return Result<SignatureStats>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signature stats {SignatureId}", signatureId);
            return Result<SignatureStats>.Success(new SignatureStats());
        }
    }

    private static CloudSignature MapToCloudSignature(SignatureItemResponse item)
    {
        return new CloudSignature
        {
            Id = item.Id,
            GameTitle = item.GameTitle,
            GameVersion = item.GameVersion,
            Platform = item.Platform,
            Name = item.Name,
            Category = item.Category,
            Pattern = item.Pattern,
            Offset = item.Offset,
            ValueType = item.ValueType,
            Description = item.Description,
            Author = item.Author,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            DownloadCount = item.DownloadCount,
            Upvotes = item.Upvotes,
            Downvotes = item.Downvotes,
            Status = Enum.Parse<SignatureStatus>(item.Status, ignoreCase: true),
            VerificationHash = item.VerificationHash
        };
    }
}
