using Microsoft.Extensions.Logging;
using SaveState.Core.CloudSync.Services;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Presentation.Services;

/// <summary>
/// Manager for community signature operations in the UI layer.
/// Provides methods for searching, contributing, voting on, and reporting signatures.
/// </summary>
public class CommunitySignatureManager
{
    private readonly ICloudSignatureDatabase _cloudDb;
    private readonly ILogger<CommunitySignatureManager> _logger;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommunitySignatureManager"/> class.
    /// </summary>
    public CommunitySignatureManager(
        ICloudSignatureDatabase cloudDb,
        ILogger<CommunitySignatureManager> logger,
        INotificationService notificationService)
    {
        _cloudDb = cloudDb;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Searches the community database for signatures matching a game title.
    /// </summary>
    /// <param name="gameTitle">The game title to search for.</param>
    /// <returns>Result containing the list of matching signatures.</returns>
    public async Task<Result<List<CloudSignature>>> SearchForGameAsync(string gameTitle)
    {
        _logger.LogInformation("Searching community signatures for {GameTitle}", gameTitle);
        
        var request = new CloudSignatureSearchRequest
        {
            GameTitle = gameTitle,
            SortBy = SignatureSortBy.MostPopular,
            Take = 100
        };
        
        var result = await _cloudDb.SearchSignaturesAsync(request);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Found {Count} community signatures for {GameTitle}",
                result.Value.TotalCount,
                gameTitle);
        }
        else
        {
            _logger.LogWarning(
                "Failed to search community signatures for {GameTitle}: {Error}",
                gameTitle,
                result.Error);
        }
        
        return result.IsSuccess 
            ? Result<List<CloudSignature>>.Success(result.Value.Signatures)
            : Result<List<CloudSignature>>.Failure(result.Error!, result.ErrorType);
    }

    /// <summary>
    /// Searches the community database with advanced filtering options.
    /// </summary>
    /// <param name="request">The search request with filters.</param>
    /// <returns>Result containing the search result with signatures and pagination info.</returns>
    public async Task<Result<CloudSignatureSearchResult>> SearchAsync(CloudSignatureSearchRequest request)
    {
        _logger.LogInformation(
            "Searching community signatures with filters: Game={GameTitle}, Platform={Platform}, Sort={SortBy}",
            request.GameTitle,
            request.Platform,
            request.SortBy);
        
        return await _cloudDb.SearchSignaturesAsync(request);
    }

    /// <summary>
    /// Contributes a local signature to the community database.
    /// </summary>
    /// <param name="gameTitle">The game title.</param>
    /// <param name="localSignature">The signature to contribute.</param>
    /// <param name="notes">Optional notes about the signature.</param>
    /// <returns>Result of the upload operation.</returns>
    public async Task<Result<CloudSignatureUploadResult>> ContributeSignatureAsync(
        string gameTitle, 
        GameMemorySignature localSignature,
        string? notes = null)
    {
        _logger.LogInformation(
            "Contributing signature {SignatureName} for {GameTitle}",
            localSignature.Name,
            gameTitle);

        var request = new CloudSignatureUploadRequest
        {
            GameTitle = gameTitle,
            GameVersion = localSignature.GameVersion ?? "1.0",
            Platform = "PC",
            Signature = localSignature,
            Notes = notes
        };
        
        var result = await _cloudDb.UploadSignatureAsync(request);
        
        if (result.IsSuccess)
        {
            _notificationService.ShowSuccess(
                $"Your signature has been uploaded and is pending review. ID: {result.Value.SignatureId}",
                "Signature Contributed");
        }
        else
        {
            _notificationService.ShowError(
                $"Failed to upload signature: {result.Error}",
                "Upload Failed");
        }
        
        return result;
    }

    /// <summary>
    /// Imports a cloud signature into the local database.
    /// </summary>
    /// <param name="cloudSignature">The cloud signature to import.</param>
    /// <returns>The imported local signature.</returns>
    public GameMemorySignature ImportSignature(CloudSignature cloudSignature)
    {
        _logger.LogInformation(
            "Importing cloud signature {SignatureId} for {GameTitle}/{Name}",
            cloudSignature.Id,
            cloudSignature.GameTitle,
            cloudSignature.Name);

        var localSig = new GameMemorySignature
        {
            GameTitle = cloudSignature.GameTitle,
            Name = cloudSignature.Name,
            Pattern = cloudSignature.Pattern,
            Offset = cloudSignature.Offset,
            ValueType = cloudSignature.ValueType,
            Description = cloudSignature.Description,
            CloudId = cloudSignature.Id,
            CloudVersion = cloudSignature.UpdatedAt,
            IsFromCloud = true,
            IsEnabled = true,
            GameVersion = cloudSignature.GameVersion,
            Tags = new List<string> { cloudSignature.Category, "cloud", "imported" }
        };

        return localSig;
    }

    /// <summary>
    /// Casts a vote on a signature.
    /// </summary>
    /// <param name="signatureId">The signature ID.</param>
    /// <param name="isUpvote">True for upvote, false for downvote.</param>
    public async Task VoteAsync(string signatureId, bool isUpvote)
    {
        var result = await _cloudDb.VoteSignatureAsync(signatureId, isUpvote);
        
        if (result.IsSuccess)
        {
            _notificationService.ShowSuccess(
                "Thank you for your feedback!",
                "Vote Recorded");
        }
        else
        {
            _notificationService.ShowError(
                $"Failed to record vote: {result.Error}",
                "Vote Failed");
        }
    }

    /// <summary>
    /// Reports a signature for review.
    /// </summary>
    /// <param name="signatureId">The signature ID to report.</param>
    /// <param name="reason">The reason for the report.</param>
    public async Task ReportAsync(string signatureId, string reason)
    {
        var request = new SignatureReportRequest
        {
            Reason = reason
        };
        
        var result = await _cloudDb.ReportSignatureAsync(signatureId, request);
        
        if (result.IsSuccess)
        {
            _notificationService.ShowSuccess(
                "Thank you for helping keep the community safe!",
                "Report Submitted");
        }
        else
        {
            _notificationService.ShowError(
                $"Failed to submit report: {result.Error}",
                "Report Failed");
        }
    }

    /// <summary>
    /// Gets detailed information about a signature.
    /// </summary>
    /// <param name="signatureId">The signature ID.</param>
    /// <returns>Result containing the signature details.</returns>
    public async Task<Result<CloudSignature>> GetSignatureDetailsAsync(string signatureId)
    {
        return await _cloudDb.GetSignatureAsync(signatureId);
    }

    /// <summary>
    /// Gets statistics for a signature.
    /// </summary>
    /// <param name="signatureId">The signature ID.</param>
    /// <returns>Result containing the signature statistics.</returns>
    public async Task<Result<SignatureStats>> GetSignatureStatsAsync(string signatureId)
    {
        return await _cloudDb.GetSignatureStatsAsync(signatureId);
    }

    /// <summary>
    /// Gets all supported games from the community database.
    /// </summary>
    /// <returns>Result containing the list of supported games.</returns>
    public async Task<Result<List<GameVersionInfo>>> GetSupportedGamesAsync()
    {
        return await _cloudDb.GetSupportedGamesAsync();
    }

    /// <summary>
    /// Deletes a signature from the community database (requires ownership/permissions).
    /// </summary>
    /// <param name="signatureId">The signature ID to delete.</param>
    public async Task DeleteSignatureAsync(string signatureId)
    {
        var result = await _cloudDb.DeleteSignatureAsync(signatureId);
        
        if (result.IsSuccess)
        {
            _notificationService.ShowSuccess(
                "The signature has been deleted successfully.",
                "Signature Deleted");
        }
        else
        {
            _notificationService.ShowError(
                $"Failed to delete signature: {result.Error}",
                "Delete Failed");
        }
    }
}
