using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Interface for the web portal service providing comprehensive online community hub
/// with forums, leaderboards, content sharing, and social features.
/// </summary>
public interface IWebPortalService
{
    // User Management
    Task<Result<WebPortalServiceUserProfile>> GetUserProfileAsync(string userId, CancellationToken ct = default);
    Task<Result> UpdateUserProfileAsync(string userId, WebPortalServiceProfileUpdateRequest request, CancellationToken ct = default);

    // Forum
    Task<Result<WebPortalServiceForumThread>> CreateForumThreadAsync(string userId, WebPortalServiceThreadCreationRequest request, CancellationToken ct = default);
    Task<Result<WebPortalServiceForumPost>> CreateForumPostAsync(string userId, WebPortalServicePostCreationRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WebPortalServiceForumThread>>> GetForumThreadsAsync(WebPortalServiceForumQuery query, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WebPortalServiceForumPost>>> GetForumPostsAsync(string threadId, int offset, int limit, CancellationToken ct = default);

    // Leaderboards
    Task<Result<WebPortalServiceLeaderboardData>> GetLeaderboardsAsync(WebPortalServiceLeaderboardQuery query, CancellationToken ct = default);

    // Content
    Task<Result<WebPortalServiceContentSubmission>> SubmitContentAsync(string userId, WebPortalServiceContentSubmissionRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WebPortalServiceContentSubmission>>> GetContentGalleryAsync(WebPortalServiceContentGalleryQuery query, CancellationToken ct = default);

    // Social
    Task<Result<WebPortalServiceSocialFeed>> GetSocialFeedAsync(string userId, WebPortalServiceSocialFeedQuery query, CancellationToken ct = default);
    Task<Result> FollowUserAsync(string followerId, string targetUserId, CancellationToken ct = default);
    Task<Result> UnfollowUserAsync(string followerId, string targetUserId, CancellationToken ct = default);

    // Community Stats
    Task<Result<WebPortalServiceCommunityStats>> GetCommunityStatsAsync(TimeSpan period, CancellationToken ct = default);
}
