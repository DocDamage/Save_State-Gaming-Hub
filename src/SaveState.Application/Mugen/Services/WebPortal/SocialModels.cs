namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Alias for social activity (for compatibility).
/// </summary>
public class WebPortalServiceSocialActivity : WebPortalServiceWebSocialActivity
{
}

/// <summary>
/// Social feed data.
/// </summary>
public class WebPortalServiceSocialFeed
{
    public string UserId { get; set; } = default!;
    public IReadOnlyList<WebPortalServiceWebSocialActivity> Activities { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public bool HasMore { get; set; } = default!;
    public string? NextCursor { get; set; } = default!;
}

/// <summary>
/// Social activity data.
/// </summary>
public class WebPortalServiceWebSocialActivity
{
    public string ActivityId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public WebPortalServiceSocialActivityType Type { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public int Likes { get; set; } = default!;
    public int Comments { get; set; } = default!;
    public int Shares { get; set; } = default!;
}

/// <summary>
/// Social feed query parameters.
/// </summary>
public class WebPortalServiceSocialFeedQuery
{
    public int Limit { get; set; } = default!;
    public string? Cursor { get; set; } = default!;
    public IReadOnlyList<WebPortalServiceSocialActivityType> ActivityTypes { get; set; } = default!;
}

/// <summary>
/// Follow relationship data.
/// </summary>
public class FollowRelationship
{
    public string FollowerId { get; set; } = default!;
    public string FollowingId { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public bool IsMutual { get; set; }
}

/// <summary>
/// Notification data.
/// </summary>
public class Notification
{
    public string NotificationId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
}
