namespace SaveState.Application.Mugen.Services.WebPortal.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Engine for managing content submissions in the web portal.
/// </summary>
public class ContentManagementEngine
{
    private readonly ILogger<ContentManagementEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, WebPortalServiceContentSubmission> _submissions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentManagementEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="submissions">The submissions dictionary storage.</param>
    /// <param name="timeProvider">The time provider instance.</param>
    public ContentManagementEngine(
        ILogger<ContentManagementEngine> logger,
        Dictionary<string, WebPortalServiceContentSubmission> submissions,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _submissions = submissions;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the total number of submissions.
    /// </summary>
    public int TotalSubmissions => _submissions.Count;

    /// <summary>
    /// Gets the number of approved content submissions.
    /// </summary>
    public int ApprovedContentCount => _submissions.Values.Count(s => s.Status == WebPortalServiceSubmissionStatus.Approved);

    /// <summary>
    /// Submits new content for review.
    /// </summary>
    /// <param name="userId">The author user identifier.</param>
    /// <param name="request">The content submission request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created content submission.</returns>
    public Task<WebPortalServiceContentSubmission> SubmitContentAsync(
        string userId,
        WebPortalServiceContentSubmissionRequest request,
        CancellationToken ct = default)
    {
        var submission = new WebPortalServiceContentSubmission
        {
            SubmissionId = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            AuthorId = userId,
            WebPortalServiceContentType = request.WebPortalServiceContentType,
            Files = request.Files ?? new List<WebPortalServiceContentFile>(),
            Tags = request.Tags ?? new List<string>(),
            Screenshots = request.Screenshots ?? new List<string>(),
            Status = WebPortalServiceSubmissionStatus.PendingReview,
            SubmittedAt = _timeProvider.UtcNow,
            DownloadCount = 0,
            Rating = 0,
            RatingCount = 0
        };
        _submissions[submission.SubmissionId] = submission;
        
        _logger.LogDebug("Content submission {SubmissionId} created by user {UserId}", submission.SubmissionId, userId);
        return Task.FromResult(submission);
    }

    /// <summary>
    /// Queries the content gallery based on specified criteria.
    /// </summary>
    /// <param name="query">The gallery query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of content submissions matching the query.</returns>
    public Task<IReadOnlyList<WebPortalServiceContentSubmission>> QueryGalleryAsync(
        WebPortalServiceContentGalleryQuery query,
        CancellationToken ct = default)
    {
        var results = _submissions.Values.AsEnumerable();
        
        if (query.WebPortalServiceContentType.HasValue)
            results = results.Where(s => s.WebPortalServiceContentType == query.WebPortalServiceContentType.Value);
        
        if (!string.IsNullOrEmpty(query.AuthorId))
            results = results.Where(s => s.AuthorId == query.AuthorId);
        
        if (query.Tags?.Any() == true)
            results = results.Where(s => query.Tags.All(tag => s.Tags.Contains(tag)));

        results = query.SortBy switch
        {
            WebPortalServiceContentSort.DownloadCount => results.OrderByDescending(s => s.DownloadCount),
            WebPortalServiceContentSort.Rating => results.OrderByDescending(s => s.Rating),
            WebPortalServiceContentSort.Alphabetical => results.OrderBy(s => s.Title),
            _ => results.OrderByDescending(s => s.SubmittedAt)
        };

        var list = results.Skip(query.Offset).Take(query.Limit).ToList();
        return Task.FromResult<IReadOnlyList<WebPortalServiceContentSubmission>>(list);
    }

    /// <summary>
    /// Gets the most popular tags for a specified time period.
    /// </summary>
    /// <param name="period">The time period to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of popular tags.</returns>
    public Task<IReadOnlyList<WebPortalServicePopularTag>> GetPopularTagsAsync(TimeSpan period, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting popular tags for period {Period}", period);
        // Return empty list for now - would calculate actual tag popularity in full implementation
        return Task.FromResult<IReadOnlyList<WebPortalServicePopularTag>>(new List<WebPortalServicePopularTag>());
    }
}
