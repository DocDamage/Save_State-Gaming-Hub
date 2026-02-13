using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services.WebPortal;
using SaveState.Application.Mugen.Services.WebPortal.Engines;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Web portal service providing comprehensive online community hub
/// with forums, leaderboards, content sharing, and social features.
/// This is a coordinator service that delegates to specialized engines.
/// </summary>
public class WebPortalService : IWebPortalService
{
    private readonly ILogger<WebPortalService> _logger;
    private readonly ICacheService _cache;

    // Data stores (shared with engines)
    private readonly Dictionary<string, WebPortalServiceForumThread> _forumPosts = new();
    private readonly Dictionary<string, WebPortalServiceForumThread> _forumThreads = new();
    private readonly Dictionary<string, WebPortalServiceUserProfile> _userProfiles = new();
    private readonly Dictionary<string, WebPortalServiceContentSubmission> _contentSubmissions = new();

    // Specialized engines
    private readonly UserManagementEngine _userEngine;
    private readonly ForumEngine _forumEngine;
    private readonly ContentManagementEngine _contentEngine;
    private readonly CommunityEngine _communityEngine;
    private readonly SocialFeaturesEngine _socialEngine;
    private readonly AuthenticationEngine _authEngine;
    private readonly AnalyticsEngine _analyticsEngine;
    private readonly ApiEngine _apiEngine;

    public WebPortalService(
        ILogger<WebPortalService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;

        // Initialize engines with shared data stores
        _userEngine = new UserManagementEngine(
            loggerFactory.CreateLogger<UserManagementEngine>(),
            _userProfiles);

        _forumEngine = new ForumEngine(
            loggerFactory.CreateLogger<ForumEngine>(),
            _forumThreads,
            new Dictionary<string, WebPortalServiceForumPost>());

        _contentEngine = new ContentManagementEngine(
            loggerFactory.CreateLogger<ContentManagementEngine>(),
            _contentSubmissions);

        _communityEngine = new CommunityEngine(
            loggerFactory.CreateLogger<CommunityEngine>());

        _socialEngine = new SocialFeaturesEngine(
            loggerFactory.CreateLogger<SocialFeaturesEngine>());

        _authEngine = new AuthenticationEngine(
            loggerFactory.CreateLogger<AuthenticationEngine>());

        _analyticsEngine = new AnalyticsEngine(
            loggerFactory.CreateLogger<AnalyticsEngine>());

        _apiEngine = new ApiEngine(
            loggerFactory.CreateLogger<ApiEngine>());

        _logger.LogInformation("WebPortalService initialized with {EngineCount} engines", 8);
    }

    #region User Management

    public async Task<Result<WebPortalServiceUserProfile>> GetUserProfileAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            var profile = await _userEngine.GetOrCreateProfileAsync(userId, ct);
            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile for {UserId}", userId);
            return Result.Failure<WebPortalServiceUserProfile>($"Profile retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result> UpdateUserProfileAsync(string userId, WebPortalServiceProfileUpdateRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_userProfiles.TryGetValue(userId, out var profile))
            {
                return Result.Failure("User profile not found");
            }

            _logger.LogInformation("Updating user profile for {UserId}", userId);

            await _userEngine.UpdateProfileAsync(profile, request);

            // Cache updated profile
            var cacheKey = $"user_profile_{userId}";
            _cache.Set(cacheKey, profile, TimeSpan.FromHours(1));

            _logger.LogInformation("User profile updated successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for {UserId}", userId);
            return Result.Failure($"Profile update failed: {ex.Message}");
        }
    }

    #endregion

    #region Forum

    public async Task<Result<WebPortalServiceForumThread>> CreateForumThreadAsync(
        string userId,
        WebPortalServiceThreadCreationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating forum thread by {UserId}: {Title}", userId, request.Title);

            var thread = await _forumEngine.CreateThreadAsync(userId, request, ct);

            _logger.LogInformation("Forum thread created: {ThreadId}", thread.ThreadId);
            return Result.Success(thread);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating forum thread by {UserId}", userId);
            return Result.Failure<WebPortalServiceForumThread>($"Thread creation failed: {ex.Message}");
        }
    }

    public async Task<Result<WebPortalServiceForumPost>> CreateForumPostAsync(
        string userId,
        WebPortalServicePostCreationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating forum post by {UserId} in thread {ThreadId}", userId, request.ThreadId);

            var (post, success, error) = await _forumEngine.CreatePostAsync(userId, request, ct);

            if (!success)
            {
                return Result.Failure<WebPortalServiceForumPost>(error);
            }

            _logger.LogInformation("Forum post created: {PostId}", post.PostId);
            return Result.Success(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating forum post by {UserId}", userId);
            return Result.Failure<WebPortalServiceForumPost>($"Post creation failed: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<WebPortalServiceForumThread>>> GetForumThreadsAsync(
        WebPortalServiceForumQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var threads = await _forumEngine.QueryThreadsAsync(query, ct);
            return Result.Success(threads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying forum threads");
            return Result.Failure<IReadOnlyList<WebPortalServiceForumThread>>($"Forum query failed: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<WebPortalServiceForumPost>>> GetForumPostsAsync(
        string threadId,
        int offset,
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            var posts = await _forumEngine.GetThreadPostsAsync(threadId, offset, limit, ct);
            return Result.Success(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forum posts for thread {ThreadId}", threadId);
            return Result.Failure<IReadOnlyList<WebPortalServiceForumPost>>($"Post retrieval failed: {ex.Message}");
        }
    }

    #endregion

    #region Leaderboards

    public async Task<Result<WebPortalServiceLeaderboardData>> GetLeaderboardsAsync(
        WebPortalServiceLeaderboardQuery query,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating leaderboard for {Type}", query.WebPortalServiceLeaderboardType);

            var leaderboard = await _communityEngine.GenerateLeaderboardAsync(query, ct);

            _logger.LogInformation("Leaderboard generated with {Count} entries", leaderboard.Entries.Count);
            return Result.Success(leaderboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating leaderboard");
            return Result.Failure<WebPortalServiceLeaderboardData>($"Leaderboard generation failed: {ex.Message}");
        }
    }

    #endregion

    #region Content

    public async Task<Result<WebPortalServiceContentSubmission>> SubmitContentAsync(
        string userId,
        WebPortalServiceContentSubmissionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Submitting content by {UserId}: {Title}", userId, request.Title);

            var submission = await _contentEngine.SubmitContentAsync(userId, request, ct);

            _logger.LogInformation("Content submitted: {SubmissionId}", submission.SubmissionId);
            return Result.Success(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting content by {UserId}", userId);
            return Result.Failure<WebPortalServiceContentSubmission>($"Content submission failed: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<WebPortalServiceContentSubmission>>> GetContentGalleryAsync(
        WebPortalServiceContentGalleryQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var submissions = await _contentEngine.QueryGalleryAsync(query, ct);
            return Result.Success(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying content gallery");
            return Result.Failure<IReadOnlyList<WebPortalServiceContentSubmission>>($"Gallery query failed: {ex.Message}");
        }
    }

    #endregion

    #region Social

    public async Task<Result<WebPortalServiceSocialFeed>> GetSocialFeedAsync(
        string userId,
        WebPortalServiceSocialFeedQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var feed = await _socialEngine.GenerateSocialFeedAsync(userId, query, ct);
            return Result.Success(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating social feed for {UserId}", userId);
            return Result.Failure<WebPortalServiceSocialFeed>($"Social feed generation failed: {ex.Message}");
        }
    }

    public async Task<Result> FollowUserAsync(string followerId, string targetUserId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("User {FollowerId} following {TargetUserId}", followerId, targetUserId);

            await _socialEngine.FollowUserAsync(followerId, targetUserId, ct);

            _logger.LogInformation("Follow relationship created successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating follow relationship");
            return Result.Failure($"Follow operation failed: {ex.Message}");
        }
    }

    public async Task<Result> UnfollowUserAsync(string followerId, string targetUserId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("User {FollowerId} unfollowing {TargetUserId}", followerId, targetUserId);

            await _socialEngine.UnfollowUserAsync(followerId, targetUserId, ct);

            _logger.LogInformation("Follow relationship removed successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing follow relationship");
            return Result.Failure($"Unfollow operation failed: {ex.Message}");
        }
    }

    #endregion

    #region Community Stats

    public async Task<Result<WebPortalServiceCommunityStats>> GetCommunityStatsAsync(
        TimeSpan period,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating community stats for period {Period}", period);

            var stats = new WebPortalServiceCommunityStats
            {
                TotalUsers = _userEngine.UserCount,
                ActiveUsers = _userEngine.ActiveUserCount,
                TotalForumPosts = _forumEngine.TotalPosts,
                TotalForumThreads = _forumEngine.TotalThreads,
                TotalContentSubmissions = _contentEngine.TotalSubmissions,
                ApprovedContent = _contentEngine.ApprovedContentCount,
                PeriodStart = DateTime.UtcNow.Subtract(period),
                PeriodEnd = DateTime.UtcNow,
                TopContributors = await _userEngine.GetTopContributorsAsync(period, ct),
                PopularTags = await _contentEngine.GetPopularTagsAsync(period, ct),
                WebPortalServiceEngagementMetrics = await _analyticsEngine.CalculateEngagementMetricsAsync(period, ct)
            };

            _logger.LogInformation("Community stats generated successfully");
            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating community stats");
            return Result.Failure<WebPortalServiceCommunityStats>($"Stats generation failed: {ex.Message}");
        }
    }

    #endregion

    #region Engine Access (for advanced scenarios)

    /// <summary>
    /// Gets the user management engine.
    /// </summary>
    internal UserManagementEngine UserEngine => _userEngine;

    /// <summary>
    /// Gets the forum engine.
    /// </summary>
    internal ForumEngine ForumEngine => _forumEngine;

    /// <summary>
    /// Gets the content management engine.
    /// </summary>
    internal ContentManagementEngine ContentEngine => _contentEngine;

    /// <summary>
    /// Gets the social features engine.
    /// </summary>
    internal SocialFeaturesEngine SocialEngine => _socialEngine;

    /// <summary>
    /// Gets the authentication engine.
    /// </summary>
    internal AuthenticationEngine AuthEngine => _authEngine;

    /// <summary>
    /// Gets the analytics engine.
    /// </summary>
    internal AnalyticsEngine AnalyticsEngine => _analyticsEngine;

    /// <summary>
    /// Gets the API engine.
    /// </summary>
    internal ApiEngine ApiEngine => _apiEngine;

    #endregion
}
