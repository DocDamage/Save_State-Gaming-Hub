namespace SaveState.Application.Mugen.Services.WebPortal.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Engine for managing user profiles in the web portal.
/// </summary>
public class UserManagementEngine
{
    private readonly ILogger<UserManagementEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, WebPortalServiceUserProfile> _profiles;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserManagementEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="profiles">The profiles dictionary storage.</param>
    /// <param name="timeProvider">The time provider instance.</param>
    public UserManagementEngine(
        ILogger<UserManagementEngine> logger,
        Dictionary<string, WebPortalServiceUserProfile> profiles,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _profiles = profiles;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the total number of users.
    /// </summary>
    public int UserCount => _profiles.Count;

    /// <summary>
    /// Gets the number of active users (active within the last 30 days).
    /// </summary>
    public int ActiveUserCount => _profiles.Values.Count(p => p.LastActivity > _timeProvider.UtcNow.AddDays(-30));

    /// <summary>
    /// Gets an existing profile or creates a new one if not found.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user profile.</returns>
    public Task<WebPortalServiceUserProfile> GetOrCreateProfileAsync(string userId, CancellationToken ct = default)
    {
        if (!_profiles.TryGetValue(userId, out var profile))
        {
            profile = new WebPortalServiceUserProfile
            {
                UserId = userId,
                DisplayName = $"User_{userId[..Math.Min(8, userId.Length)]}",
                JoinDate = _timeProvider.UtcNow,
                LastActivity = _timeProvider.UtcNow,
                PreferredCharacters = new List<string>(),
                StreamingLinks = new Dictionary<string, string>(),
                SocialLinks = new Dictionary<string, string>(),
                Stats = new WebPortalServiceUserStats(),
                Achievements = new List<WebPortalServiceUserAchievement>()
            };
            _profiles[userId] = profile;
            _logger.LogDebug("Created new profile for user {UserId}", userId);
        }
        return Task.FromResult(profile);
    }

    /// <summary>
    /// Updates a user profile with the specified request data.
    /// </summary>
    /// <param name="profile">The profile to update.</param>
    /// <param name="request">The update request containing new values.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateProfileAsync(WebPortalServiceUserProfile profile, WebPortalServiceProfileUpdateRequest request)
    {
        if (request.DisplayName != null) profile.DisplayName = request.DisplayName;
        if (request.Bio != null) profile.Bio = request.Bio;
        if (request.AvatarUrl != null) profile.AvatarUrl = request.AvatarUrl;
        if (request.Location != null) profile.Location = request.Location;
        if (request.Website != null) profile.Website = request.Website;
        if (request.PreferredCharacters != null) profile.PreferredCharacters = request.PreferredCharacters;
        if (request.StreamingLinks != null) profile.StreamingLinks = request.StreamingLinks;
        profile.LastUpdated = _timeProvider.UtcNow;
        
        _logger.LogDebug("Updated profile for user {UserId}", profile.UserId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the top contributors for a specified time period.
    /// </summary>
    /// <param name="period">The time period to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of top contributors.</returns>
    public Task<IReadOnlyList<WebPortalServiceTopContributor>> GetTopContributorsAsync(TimeSpan period, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting top contributors for period {Period}", period);
        // Return empty list for now - would calculate actual contributions in full implementation
        return Task.FromResult<IReadOnlyList<WebPortalServiceTopContributor>>(new List<WebPortalServiceTopContributor>());
    }
}
