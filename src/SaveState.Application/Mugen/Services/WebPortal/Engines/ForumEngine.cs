namespace SaveState.Application.Mugen.Services.WebPortal.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Engine for managing forum functionality in the web portal.
/// </summary>
public class ForumEngine
{
    private readonly ILogger<ForumEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, WebPortalServiceForumThread> _threads;
    private readonly Dictionary<string, WebPortalServiceForumPost> _posts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForumEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="threads">The threads dictionary storage.</param>
    /// <param name="posts">The posts dictionary storage.</param>
    /// <param name="timeProvider">The time provider instance.</param>
    public ForumEngine(
        ILogger<ForumEngine> logger,
        Dictionary<string, WebPortalServiceForumThread> threads,
        Dictionary<string, WebPortalServiceForumPost> posts,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _threads = threads;
        _posts = posts;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the total number of posts.
    /// </summary>
    public int TotalPosts => _posts.Count;

    /// <summary>
    /// Gets the total number of threads.
    /// </summary>
    public int TotalThreads => _threads.Count;

    /// <summary>
    /// Creates a new forum thread.
    /// </summary>
    /// <param name="userId">The author user identifier.</param>
    /// <param name="request">The thread creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created forum thread.</returns>
    public Task<WebPortalServiceForumThread> CreateThreadAsync(
        string userId, 
        WebPortalServiceThreadCreationRequest request, 
        CancellationToken ct = default)
    {
        var thread = new WebPortalServiceForumThread
        {
            ThreadId = Guid.NewGuid().ToString(),
            Title = request.Title,
            Content = request.Content,
            AuthorId = userId,
            Category = request.Category,
            Tags = request.Tags ?? new List<string>(),
            CreatedAt = _timeProvider.UtcNow,
            LastActivity = _timeProvider.UtcNow,
            Participants = new List<string> { userId },
            ReplyCount = 0,
            ViewCount = 0,
            IsSticky = false,
            IsLocked = false
        };
        _threads[thread.ThreadId] = thread;
        
        _logger.LogDebug("Created thread {ThreadId} by user {UserId}", thread.ThreadId, userId);
        return Task.FromResult(thread);
    }

    /// <summary>
    /// Creates a new post in a thread.
    /// </summary>
    /// <param name="userId">The author user identifier.</param>
    /// <param name="request">The post creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the post (if successful), success flag, and error message.</returns>
    public Task<(WebPortalServiceForumPost? post, bool success, string error)> CreatePostAsync(
        string userId,
        WebPortalServicePostCreationRequest request,
        CancellationToken ct = default)
    {
        if (!_threads.ContainsKey(request.ThreadId))
        {
            _logger.LogWarning("Attempted to post to non-existent thread {ThreadId}", request.ThreadId);
            return Task.FromResult<(WebPortalServiceForumPost?, bool, string)>((null, false, "Thread not found"));
        }

        var post = new WebPortalServiceForumPost
        {
            PostId = Guid.NewGuid().ToString(),
            ThreadId = request.ThreadId,
            AuthorId = userId,
            Content = request.Content,
            CreatedAt = _timeProvider.UtcNow,
            ParentPostId = request.ParentPostId,
            WebPortalServicePostType = WebPortalServicePostType.Reply,
            Likes = 0,
            Dislikes = 0
        };
        _posts[post.PostId] = post;
        
        var thread = _threads[request.ThreadId];
        thread.ReplyCount++;
        thread.LastActivity = _timeProvider.UtcNow;
        
        if (!thread.Participants.Contains(userId))
        {
            thread.Participants = thread.Participants.Append(userId).ToList();
        }
        
        _logger.LogDebug("Created post {PostId} in thread {ThreadId} by user {UserId}", post.PostId, request.ThreadId, userId);
        return Task.FromResult<(WebPortalServiceForumPost?, bool, string)>((post, true, string.Empty));
    }

    /// <summary>
    /// Queries forum threads based on specified criteria.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of forum threads matching the query.</returns>
    public Task<IReadOnlyList<WebPortalServiceForumThread>> QueryThreadsAsync(
        WebPortalServiceForumQuery query, 
        CancellationToken ct = default)
    {
        var results = _threads.Values.AsEnumerable();
        
        if (query.Category.HasValue)
            results = results.Where(t => t.Category == query.Category.Value);
        
        if (!string.IsNullOrEmpty(query.AuthorId))
            results = results.Where(t => t.AuthorId == query.AuthorId);
        
        if (query.Tags?.Any() == true)
            results = results.Where(t => query.Tags.All(tag => t.Tags.Contains(tag)));

        results = query.SortBy switch
        {
            WebPortalServiceForumSort.LastActivity => results.OrderByDescending(t => t.LastActivity),
            WebPortalServiceForumSort.ViewCount => results.OrderByDescending(t => t.ViewCount),
            WebPortalServiceForumSort.ReplyCount => results.OrderByDescending(t => t.ReplyCount),
            _ => results.OrderByDescending(t => t.CreatedAt)
        };

        var list = results.Skip(query.Offset).Take(query.Limit).ToList();
        return Task.FromResult<IReadOnlyList<WebPortalServiceForumThread>>(list);
    }

    /// <summary>
    /// Gets posts for a specific thread.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="offset">The offset for pagination.</param>
    /// <param name="limit">The maximum number of posts to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of posts in the thread.</returns>
    public Task<IReadOnlyList<WebPortalServiceForumPost>> GetThreadPostsAsync(
        string threadId,
        int offset,
        int limit,
        CancellationToken ct = default)
    {
        var posts = _posts.Values
            .Where(p => p.ThreadId == threadId)
            .OrderBy(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<WebPortalServiceForumPost>>(posts);
    }
}
