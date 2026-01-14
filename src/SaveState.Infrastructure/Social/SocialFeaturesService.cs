using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using System.Collections.Generic;

namespace SaveState.Infrastructure.Social;

/// <summary>
/// Social features including achievements sharing and social media integration.
/// PHASE 7: REQUIRED - Social Features (Session 5)
/// </summary>
public class SocialFeaturesService
{
    private readonly ILogger<SocialFeaturesService> _logger;
    private readonly Dictionary<string, SocialPost> _posts = new();
    private readonly Dictionary<string, UserProfile> _profiles = new();

    public SocialFeaturesService(ILogger<SocialFeaturesService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a user social profile.
    /// </summary>
    public async Task<Result<UserProfile>> CreateProfileAsync(
        string userId,
        string username,
        string? bio = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating social profile for user: {UserId}", userId);

            var profile = new UserProfile(
                UserId: userId,
                Username: username,
                Bio: bio,
                CreatedAt: DateTime.UtcNow,
                Followers: new List<string>(),
                Following: new List<string>(),
                Posts: new List<string>());

            _profiles[userId] = profile;

            _logger.LogInformation("Social profile created for user: {UserId}", userId);
            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create profile for user: {UserId}", userId);
            return Result.Failure<UserProfile>(
                $"Profile creation failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Shares an achievement to social media.
    /// </summary>
    public async Task<Result> ShareAchievementAsync(
        string userId,
        string achievementId,
        string achievementName,
        string platform,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Sharing achievement {AchievementId} to {Platform}",
                achievementId,
                platform);

            var post = new SocialPost(
                id: Guid.NewGuid().ToString(),
                userId: userId,
                type: SocialPostType.Achievement,
                content: $"I just unlocked the '{achievementName}' achievement!",
                platform: platform,
                createdAt: DateTime.UtcNow,
                likes: 0,
                comments: new List<string>());

            _posts[post.Id] = post;

            _logger.LogInformation("Achievement shared to {Platform}", platform);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share achievement");
            return Result.Failure($"Share failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Shares a gameplay clip.
    /// </summary>
    public async Task<Result> ShareGameplayClipAsync(
        string userId,
        string clipId,
        string clipName,
        string clipUrl,
        string platform,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Sharing gameplay clip {ClipId} to {Platform}", clipId, platform);

            var post = new SocialPost(
                id: Guid.NewGuid().ToString(),
                userId: userId,
                type: SocialPostType.GameplayClip,
                content: $"Check out my gameplay clip: {clipName}",
                platform: platform,
                createdAt: DateTime.UtcNow,
                likes: 0,
                comments: new List<string>(),
                mediaUrl: clipUrl);

            _posts[post.Id] = post;

            _logger.LogInformation("Gameplay clip shared to {Platform}", platform);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share gameplay clip");
            return Result.Failure($"Share failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Creates a social post.
    /// </summary>
    public async Task<Result<SocialPost>> CreatePostAsync(
        string userId,
        string content,
        string? mediaUrl = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating social post for user: {UserId}", userId);

            var post = new SocialPost(
                id: Guid.NewGuid().ToString(),
                userId: userId,
                type: SocialPostType.Status,
                content: content,
                platform: "Local",
                createdAt: DateTime.UtcNow,
                likes: 0,
                comments: new List<string>(),
                mediaUrl: mediaUrl);

            _posts[post.Id] = post;

            if (_profiles.TryGetValue(userId, out var profile))
            {
                profile.Posts.Add(post.Id);
            }

            _logger.LogInformation("Social post created: {PostId}", post.Id);
            return Result.Success(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create post");
            return Result.Failure<SocialPost>(
                $"Post creation failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Follows a user.
    /// </summary>
    public async Task<Result> FollowUserAsync(
        string followerId,
        string followeeId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_profiles.TryGetValue(followerId, out var followerProfile))
            {
                return Result.Failure("Follower profile not found", ErrorType.Validation);
            }

            if (!_profiles.TryGetValue(followeeId, out var followeeProfile))
            {
                return Result.Failure("Followee profile not found", ErrorType.Validation);
            }

            followerProfile.Following.Add(followeeId);
            followeeProfile.Followers.Add(followerId);

            _logger.LogInformation(
                "User {FollowerId} followed user {FolloweeId}",
                followerId,
                followeeId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to follow user");
            return Result.Failure($"Follow failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Likes a post.
    /// </summary>
    public async Task<Result> LikePostAsync(
        string postId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_posts.TryGetValue(postId, out var post))
            {
                return Result.Failure("Post not found", ErrorType.Validation);
            }

            post.Likes++;

            _logger.LogInformation("Post {PostId} liked", postId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to like post");
            return Result.Failure($"Like failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets user feed.
    /// </summary>
    public async Task<Result<List<SocialPost>>> GetUserFeedAsync(
        string userId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_profiles.TryGetValue(userId, out var profile))
            {
                return Result.Failure<List<SocialPost>>(
                    "Profile not found",
                    ErrorType.Validation);
            }

            var feed = _posts.Values
                .Where(p => profile.Following.Contains(p.UserId) || p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return Result.Success(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user feed");
            return Result.Failure<List<SocialPost>>(
                $"Feed fetch failed: {ex.Message}",
                ErrorType.Internal);
        }
    }
}

/// <summary>
/// User social profile.
/// </summary>
public record UserProfile(
    string UserId,
    string Username,
    string? Bio,
    DateTime CreatedAt,
    List<string> Followers,
    List<string> Following,
    List<string> Posts);

/// <summary>
/// Social post.
/// </summary>
public class SocialPost
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public SocialPostType Type { get; set; }
    public string Content { get; set; }
    public string Platform { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Likes { get; set; }
    public List<string> Comments { get; set; }
    public string? MediaUrl { get; set; }

    public SocialPost(
        string id,
        string userId,
        SocialPostType type,
        string content,
        string platform,
        DateTime createdAt,
        int likes,
        List<string> comments,
        string? mediaUrl = null)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Content = content;
        Platform = platform;
        CreatedAt = createdAt;
        Likes = likes;
        Comments = comments;
        MediaUrl = mediaUrl;
    }
}

/// <summary>
/// Social post type.
/// </summary>
public enum SocialPostType
{
    Status,
    Achievement,
    GameplayClip,
    Screenshot,
    Review
}
