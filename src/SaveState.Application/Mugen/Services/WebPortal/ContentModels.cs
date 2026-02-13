namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Portal content data.
/// </summary>
public class PortalContent
{
    public string ContentId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public WebPortalServiceContentType ContentType { get; set; }
    public ContentStatus Status { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public int ViewCount { get; set; }
    public int DownloadCount { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
}

/// <summary>
/// Content revision tracking.
/// </summary>
public class ContentRevision
{
    public string RevisionId { get; set; } = default!;
    public string ContentId { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public int VersionNumber { get; set; }
    public string ChangeSummary { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? PreviousVersionId { get; set; }
}

/// <summary>
/// Content submission data.
/// </summary>
public class WebPortalServiceContentSubmission
{
    public string SubmissionId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public WebPortalServiceContentType WebPortalServiceContentType { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public IReadOnlyList<WebPortalServiceContentFile> Files { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public IReadOnlyList<string> Screenshots { get; set; } = default!;
    public WebPortalServiceSubmissionStatus Status { get; set; } = default!;
    public DateTime SubmittedAt { get; set; } = default!;
    public string? ReviewNotes { get; set; } = default!;
    public DateTime? ApprovedAt { get; set; } = default!;
    public DateTime? PublishedAt { get; set; } = default!;
    public int DownloadCount { get; set; } = default!;
    public double Rating { get; set; } = default!;
    public int RatingCount { get; set; } = default!;
}

/// <summary>
/// Content submission request.
/// </summary>
public class WebPortalServiceContentSubmissionRequest
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public WebPortalServiceContentType WebPortalServiceContentType { get; set; } = default!;
    public IReadOnlyList<WebPortalServiceContentFile> Files { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public IReadOnlyList<string> Screenshots { get; set; } = default!;
}

/// <summary>
/// Content file data.
/// </summary>
public class WebPortalServiceContentFile
{
    public string FileName { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public long FileSize { get; set; } = default!;
    public string WebPortalServiceContentType { get; set; } = default!;
}

/// <summary>
/// Content gallery query.
/// </summary>
public class WebPortalServiceContentGalleryQuery
{
    public WebPortalServiceContentType? WebPortalServiceContentType { get; set; } = default!;
    public string? AuthorId { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public WebPortalServiceContentSort SortBy { get; set; } = default!;
    public int Offset { get; set; } = default!;
    public int Limit { get; set; } = default!;
}

/// <summary>
/// Popular tag data.
/// </summary>
public class WebPortalServicePopularTag
{
    public string Tag { get; set; } = default!;
    public int UsageCount { get; set; } = default!;
}
