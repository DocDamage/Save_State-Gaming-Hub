using System.Text.Json.Serialization;

namespace SaveState.Infrastructure.CloudSync.Models;

/// <summary>
/// API request payload for uploading a signature.
/// </summary>
internal class SignatureUploadPayload
{
    [JsonPropertyName("gameTitle")]
    public string GameTitle { get; set; } = string.Empty;
    
    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;
    
    [JsonPropertyName("offset")]
    public int Offset { get; set; }
    
    [JsonPropertyName("valueType")]
    public string ValueType { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

/// <summary>
/// API response for signature upload.
/// </summary>
internal class SignatureUploadResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("reviewUrl")]
    public string? ReviewUrl { get; set; }
}

/// <summary>
/// API response for signature search.
/// </summary>
internal class SignatureSearchResponse
{
    [JsonPropertyName("items")]
    public List<SignatureItemResponse> Items { get; set; } = new();
    
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}

/// <summary>
/// Individual signature item in API responses.
/// </summary>
internal class SignatureItemResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("gameTitle")]
    public string GameTitle { get; set; } = string.Empty;
    
    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;
    
    [JsonPropertyName("offset")]
    public int Offset { get; set; }
    
    [JsonPropertyName("valueType")]
    public string ValueType { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
    
    [JsonPropertyName("downloadCount")]
    public int DownloadCount { get; set; }
    
    [JsonPropertyName("upvotes")]
    public int Upvotes { get; set; }
    
    [JsonPropertyName("downvotes")]
    public int Downvotes { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("verificationHash")]
    public string? VerificationHash { get; set; }
}

/// <summary>
/// API response for sync changes.
/// </summary>
internal class SyncChangesResponse
{
    [JsonPropertyName("new")]
    public List<SignatureItemResponse> New { get; set; } = new();
    
    [JsonPropertyName("updated")]
    public List<SignatureItemResponse> Updated { get; set; } = new();
    
    [JsonPropertyName("deprecated")]
    public List<string> Deprecated { get; set; } = new();
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// API response for sync manifest.
/// </summary>
internal class SyncManifestResponse
{
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
    
    [JsonPropertyName("totalSignatures")]
    public int TotalSignatures { get; set; }
    
    [JsonPropertyName("supportedGames")]
    public int SupportedGames { get; set; }
    
    [JsonPropertyName("etag")]
    public string? ETag { get; set; }
}

/// <summary>
/// API response for game version info.
/// </summary>
internal class GameVersionInfoResponse
{
    [JsonPropertyName("gameTitle")]
    public string GameTitle { get; set; } = string.Empty;
    
    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();
    
    [JsonPropertyName("platforms")]
    public List<string> Platforms { get; set; } = new();
}

/// <summary>
/// API response for signature statistics.
/// </summary>
internal class SignatureStatsResponse
{
    [JsonPropertyName("totalDownloads")]
    public int TotalDownloads { get; set; }
    
    [JsonPropertyName("successCount")]
    public int SuccessCount { get; set; }
    
    [JsonPropertyName("failureCount")]
    public int FailureCount { get; set; }
    
    [JsonPropertyName("upvotes")]
    public int Upvotes { get; set; }
    
    [JsonPropertyName("downvotes")]
    public int Downvotes { get; set; }
}

/// <summary>
/// Vote request payload.
/// </summary>
internal class VoteRequest
{
    [JsonPropertyName("isUpvote")]
    public bool IsUpvote { get; set; }
}

/// <summary>
/// Report request payload.
/// </summary>
internal class ReportRequest
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
    
    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

/// <summary>
/// Update request payload.
/// </summary>
internal class UpdateRequest
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
