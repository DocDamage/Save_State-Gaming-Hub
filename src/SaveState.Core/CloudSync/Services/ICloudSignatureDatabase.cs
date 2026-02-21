using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.CloudSync.Services;

/// <summary>
/// Interface for accessing the cloud-based signature database.
/// Enables community-driven sharing of game memory signatures.
/// </summary>
public interface ICloudSignatureDatabase
{
    /// <summary>
    /// Searches for signatures in the cloud database.
    /// </summary>
    Task<Result<CloudSignatureSearchResult>> SearchSignaturesAsync(
        CloudSignatureSearchRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a specific signature by ID.
    /// </summary>
    Task<Result<CloudSignature>> GetSignatureAsync(
        string signatureId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all signatures for a specific game.
    /// </summary>
    Task<Result<List<CloudSignature>>> GetSignaturesForGameAsync(
        string gameTitle, 
        string? gameVersion = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a list of all supported games with their versions.
    /// </summary>
    Task<Result<List<GameVersionInfo>>> GetSupportedGamesAsync(
        CancellationToken ct = default);
    
    /// <summary>
    /// Uploads a new signature to the cloud database.
    /// </summary>
    Task<Result<CloudSignatureUploadResult>> UploadSignatureAsync(
        CloudSignatureUploadRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Updates an existing signature.
    /// </summary>
    Task<Result> UpdateSignatureAsync(
        string signatureId,
        CloudSignatureUpdateRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a signature from the cloud database.
    /// </summary>
    Task<Result> DeleteSignatureAsync(
        string signatureId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Reports a signature for review.
    /// </summary>
    Task<Result> ReportSignatureAsync(
        string signatureId, 
        SignatureReportRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Casts a vote on a signature (upvote or downvote).
    /// </summary>
    Task<Result> VoteSignatureAsync(
        string signatureId, 
        bool isUpvote,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets statistics for a signature.
    /// </summary>
    Task<Result<SignatureStats>> GetSignatureStatsAsync(
        string signatureId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all changes since a specific timestamp.
    /// Used for incremental sync operations.
    /// </summary>
    Task<Result<SignatureSyncResult>> GetChangesSinceAsync(
        DateTime since,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets the sync manifest with metadata about the database state.
    /// </summary>
    Task<Result<SignatureSyncManifest>> GetSyncManifestAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Request object for searching cloud signatures.
/// </summary>
public class CloudSignatureSearchRequest
{
    /// <summary>
    /// Filter by game title (partial match supported).
    /// </summary>
    public string? GameTitle { get; set; }
    
    /// <summary>
    /// Filter by pattern type (e.g., "health", "score").
    /// </summary>
    public string? PatternType { get; set; }
    
    /// <summary>
    /// Filter by platform (e.g., "PC", "PS5").
    /// </summary>
    public string? Platform { get; set; }
    
    /// <summary>
    /// Filter by game version.
    /// </summary>
    public string? GameVersion { get; set; }
    
    /// <summary>
    /// Sort order for results.
    /// </summary>
    public SignatureSortBy SortBy { get; set; } = SignatureSortBy.MostPopular;
    
    /// <summary>
    /// Number of results to skip (for pagination).
    /// </summary>
    public int Skip { get; set; } = 0;
    
    /// <summary>
    /// Number of results to return (for pagination).
    /// </summary>
    public int Take { get; set; } = 50;
}

/// <summary>
/// Sort options for signature search results.
/// </summary>
public enum SignatureSortBy
{
    /// <summary>Sort by popularity score (default).</summary>
    MostPopular,
    
    /// <summary>Sort by most recently added or updated.</summary>
    MostRecent,
    
    /// <summary>Sort by highest user rating.</summary>
    HighestRated,
    
    /// <summary>Sort by download count.</summary>
    MostDownloaded
}

/// <summary>
/// Result of a signature search operation.
/// </summary>
public class CloudSignatureSearchResult
{
    /// <summary>
    /// List of signatures matching the search criteria.
    /// </summary>
    public List<CloudSignature> Signatures { get; set; } = new();
    
    /// <summary>
    /// Total number of matching signatures (for pagination).
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Whether more results are available.
    /// </summary>
    public bool HasMore { get; set; }
}

/// <summary>
/// Represents a cloud-stored memory signature.
/// </summary>
public class CloudSignature
{
    /// <summary>Unique identifier for the signature.</summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>The game title this signature applies to.</summary>
    public string GameTitle { get; set; } = string.Empty;
    
    /// <summary>The game version this signature is for.</summary>
    public string GameVersion { get; set; } = string.Empty;
    
    /// <summary>The platform this signature works on.</summary>
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>The name of the signature (e.g., "Health", "Score").</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>The category of the signature.</summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>The hex pattern to search for.</summary>
    public string Pattern { get; set; } = string.Empty;
    
    /// <summary>Offset from pattern match to actual value.</summary>
    public int Offset { get; set; }
    
    /// <summary>Data type of the value (e.g., "int32", "float").</summary>
    public string ValueType { get; set; } = string.Empty;
    
    /// <summary>Optional description of the signature.</summary>
    public string? Description { get; set; }
    
    /// <summary>Author of the signature (optional).</summary>
    public string? Author { get; set; }
    
    /// <summary>When the signature was created.</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>When the signature was last updated.</summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>Number of times this signature has been downloaded.</summary>
    public int DownloadCount { get; set; }
    
    /// <summary>Number of upvotes.</summary>
    public int Upvotes { get; set; }
    
    /// <summary>Number of downvotes.</summary>
    public int Downvotes { get; set; }
    
    /// <summary>Current status of the signature.</summary>
    public SignatureStatus Status { get; set; }
    
    /// <summary>Hash for verifying signature integrity.</summary>
    public string? VerificationHash { get; set; }
}

/// <summary>
/// Status of a cloud signature.
/// </summary>
public enum SignatureStatus
{
    /// <summary>Signature is pending review.</summary>
    Pending,
    
    /// <summary>Signature has been verified by moderators.</summary>
    Verified,
    
    /// <summary>Signature has high community confidence.</summary>
    CommunityVerified,
    
    /// <summary>Signature is deprecated and should not be used.</summary>
    Deprecated,
    
    /// <summary>Signature has been reported and is under review.</summary>
    Reported
}

/// <summary>
/// Request to upload a new signature.
/// </summary>
public class CloudSignatureUploadRequest
{
    /// <summary>The game title.</summary>
    public string GameTitle { get; set; } = string.Empty;
    
    /// <summary>The game version.</summary>
    public string GameVersion { get; set; } = string.Empty;
    
    /// <summary>The platform.</summary>
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>The signature to upload.</summary>
    public required GameMemorySignature Signature { get; init; }
    
    /// <summary>Optional author name.</summary>
    public string? Author { get; set; }
    
    /// <summary>Optional notes about the signature.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Result of a signature upload operation.
/// </summary>
public class CloudSignatureUploadResult
{
    /// <summary>The assigned signature ID.</summary>
    public string SignatureId { get; set; } = string.Empty;
    
    /// <summary>The current status of the uploaded signature.</summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>Optional URL to review the submission.</summary>
    public string? ReviewUrl { get; set; }
}

/// <summary>
/// Statistics for a signature.
/// </summary>
public class SignatureStats
{
    /// <summary>Total number of downloads.</summary>
    public int TotalDownloads { get; set; }
    
    /// <summary>Number of successful uses.</summary>
    public int SuccessCount { get; set; }
    
    /// <summary>Number of failed uses.</summary>
    public int FailureCount { get; set; }
    
    /// <summary>Success rate as a percentage (0-1).</summary>
    public double SuccessRate => TotalDownloads > 0 ? (double)SuccessCount / TotalDownloads : 0;
    
    /// <summary>Number of upvotes.</summary>
    public int Upvotes { get; set; }
    
    /// <summary>Number of downvotes.</summary>
    public int Downvotes { get; set; }
    
    /// <summary>Rating as a percentage (0-1).</summary>
    public double Rating => Upvotes + Downvotes > 0 ? (double)Upvotes / (Upvotes + Downvotes) : 0;
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public class SignatureSyncResult
{
    /// <summary>New signatures since last sync.</summary>
    public List<CloudSignature> NewSignatures { get; set; } = new();
    
    /// <summary>Updated signatures since last sync.</summary>
    public List<CloudSignature> UpdatedSignatures { get; set; } = new();
    
    /// <summary>IDs of deprecated signatures.</summary>
    public List<string> DeprecatedSignatures { get; set; } = new();
    
    /// <summary>Timestamp of this sync operation.</summary>
    public DateTime SyncTimestamp { get; set; }
}

/// <summary>
/// Manifest for sync operations.
/// </summary>
public class SignatureSyncManifest
{
    /// <summary>When the database was last updated.</summary>
    public DateTime LastUpdated { get; set; }
    
    /// <summary>Total number of signatures in the database.</summary>
    public int TotalSignatures { get; set; }
    
    /// <summary>Number of games with signatures.</summary>
    public int SupportedGames { get; set; }
    
    /// <summary>ETag for caching.</summary>
    public string? ETag { get; set; }
}

/// <summary>
/// Information about a game version.
/// </summary>
public class GameVersionInfo
{
    /// <summary>The game title.</summary>
    public string GameTitle { get; set; } = string.Empty;
    
    /// <summary>List of supported versions.</summary>
    public List<string> Versions { get; set; } = new();
    
    /// <summary>List of supported platforms.</summary>
    public List<string> Platforms { get; set; } = new();
}

/// <summary>
/// Request to update an existing signature.
/// </summary>
public class CloudSignatureUpdateRequest
{
    /// <summary>New description (optional).</summary>
    public string? Description { get; set; }
    
    /// <summary>New notes (optional).</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request to report a signature.
/// </summary>
public class SignatureReportRequest
{
    /// <summary>Reason for the report.</summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>Additional details (optional).</summary>
    public string? Details { get; set; }
}
