namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Forum thread data.
/// </summary>
public class WebPortalServiceForumThread
{
    public string ThreadId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public WebPortalServiceForumCategory Category { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public bool IsSticky { get; set; } = default!;
    public bool IsLocked { get; set; } = default!;
    public int ViewCount { get; set; } = default!;
    public int ReplyCount { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastActivity { get; set; } = default!;
    public IReadOnlyList<string> Participants { get; set; } = default!;
}

/// <summary>
/// Forum post data.
/// </summary>
public class WebPortalServiceForumPost
{
    public string PostId { get; set; } = default!;
    public string ThreadId { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public string Content { get; set; } = default!;
    public WebPortalServicePostType WebPortalServicePostType { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime? EditedAt { get; set; } = default!;
    public int Likes { get; set; } = default!;
    public int Dislikes { get; set; } = default!;
    public string? ParentPostId { get; set; } = default!;
}

/// <summary>
/// Thread creation request.
/// </summary>
public class WebPortalServiceThreadCreationRequest
{
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public WebPortalServiceForumCategory Category { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
}

/// <summary>
/// Post creation request.
/// </summary>
public class WebPortalServicePostCreationRequest
{
    public string ThreadId { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string? ParentPostId { get; set; } = default!;
}

/// <summary>
/// Forum query parameters.
/// </summary>
public class WebPortalServiceForumQuery
{
    public WebPortalServiceForumCategory? Category { get; set; } = default!;
    public string? AuthorId { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public WebPortalServiceForumSort SortBy { get; set; } = default!;
    public int Offset { get; set; } = default!;
    public int Limit { get; set; } = default!;
}
