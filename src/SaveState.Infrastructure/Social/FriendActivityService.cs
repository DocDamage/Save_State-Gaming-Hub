using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Social;
using SaveState.Core.Social.Entities;
using SaveState.Core.Social.Services;

namespace SaveState.Infrastructure.Social;

/// <summary>
/// Service implementation for friend activity management.
/// </summary>
public class FriendActivityService : IFriendActivityService
{
    private readonly IFriendRepository _friendRepository;
    private readonly IDiscordPresenceService _discordPresenceService;
    private readonly ILogger<FriendActivityService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendActivityService"/> class.
    /// </summary>
    /// <param name="friendRepository">Repository for accessing friend data.</param>
    /// <param name="discordPresenceService">Service for Discord Rich Presence integration.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public FriendActivityService(
        IFriendRepository friendRepository,
        IDiscordPresenceService discordPresenceService,
        ILogger<FriendActivityService> logger)
    {
        _friendRepository = friendRepository;
        _discordPresenceService = discordPresenceService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the activity feed showing recent friend activities.
    /// </summary>
    /// <param name="limit">Maximum number of activities to return.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of friend activities.</returns>
    public async Task<Result<IReadOnlyList<FriendActivity>>> GetActivityFeedAsync(
        int limit = 50,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _friendRepository.GetActivitiesAsync(limit, ct: ct);
            return Result.Success<IReadOnlyList<FriendActivity>>(result.Items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity feed");
            return Result.Failure<IReadOnlyList<FriendActivity>>("Failed to get activity feed", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets the list of all friends.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of friends.</returns>
    public async Task<Result<IReadOnlyList<Friend>>> GetFriendsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _friendRepository.GetFriendsAsync(ct: ct);
            return Result.Success<IReadOnlyList<Friend>>(result.Items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get friends");
            return Result.Failure<IReadOnlyList<Friend>>("Failed to get friends", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets the list of friends who are currently online.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of online friends.</returns>
    public async Task<Result<IReadOnlyList<Friend>>> GetOnlineFriendsAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _friendRepository.GetFriendsAsync(isOnline: true, ct: ct);
            return Result.Success<IReadOnlyList<Friend>>(result.Items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get online friends");
            return Result.Failure<IReadOnlyList<Friend>>("Failed to get online friends", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Synchronizes friend data from Discord.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> SyncDiscordFriendsAsync(CancellationToken ct = default)
    {
        try
        {
            // For now, this is a placeholder implementation
            // In a real implementation, this would:
            // 1. Use Discord API to get the user's friends
            // 2. Check which friends are playing games
            // 3. Update friend records and create activities

            _logger.LogInformation("Discord friends sync requested (placeholder implementation)");

            // Placeholder: Create some sample friends and activities for demonstration
            await CreateSampleFriendsAndActivitiesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Discord friends");
            return Result.Failure("Failed to sync Discord friends", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Synchronizes friend data from Steam.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> SyncSteamFriendsAsync(CancellationToken ct = default)
    {
        try
        {
            // Placeholder for future Steam integration
            _logger.LogInformation("Steam friends sync requested (not yet implemented)");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Steam friends");
            return Task.FromResult(Result.Failure("Failed to sync Steam friends", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Records an activity for a friend.
    /// </summary>
    /// <param name="friendId">The unique identifier of the friend.</param>
    /// <param name="type">The type of activity.</param>
    /// <param name="gameTitle">The title of the game involved.</param>
    /// <param name="details">Optional additional details about the activity.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> RecordActivityAsync(
        Guid friendId,
        ActivityType type,
        string gameTitle,
        string? details = null,
        CancellationToken ct = default)
    {
        try
        {
            var friend = await _friendRepository.GetByPlatformIdAsync(SocialPlatform.Discord, friendId.ToString(), ct);
            if (friend is null)
            {
                return Result.Failure("Friend not found", ErrorType.NotFound);
            }

            var activity = FriendActivity.Create(
                friend.Id,
                type,
                gameTitle,
                SocialPlatform.Discord,
                details);

            await _friendRepository.AddActivityAsync(activity, ct);

            _logger.LogInformation("Recorded activity {Type} for friend {FriendId}: {GameTitle}",
                type, friendId, gameTitle);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record activity for friend {FriendId}", friendId);
            return Result.Failure("Failed to record activity", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Updates the online status of all friends.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> UpdateFriendStatusesAsync(CancellationToken ct = default)
    {
        try
        {
            // For now, this is a placeholder
            // In a real implementation, this would poll Discord/Steam APIs for friend statuses
            _logger.LogInformation("Friend status update requested (placeholder implementation)");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update friend statuses");
            return Task.FromResult(Result.Failure("Failed to update friend statuses", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets statistics about friend activities.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing friend activity statistics.</returns>
    public async Task<Result<FriendActivityStatistics>> GetStatisticsAsync(CancellationToken ct = default)
    {
        try
        {
            var statistics = await _friendRepository.GetStatisticsAsync(ct);
            return Result.Success<FriendActivityStatistics>(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get friend activity statistics");
            return Result.Failure<FriendActivityStatistics>("Failed to get statistics", ErrorType.Internal);
        }
    }

    private async Task CreateSampleFriendsAndActivitiesAsync(CancellationToken ct)
    {
        // Create some sample friends for demonstration
        var friend1 = Friend.Create("Alice", SocialPlatform.Discord, "discord_123", "https://example.com/avatar1.png");
        var friend2 = Friend.Create("Bob", SocialPlatform.Discord, "discord_456", "https://example.com/avatar2.png");
        var friend3 = Friend.Create("Charlie", SocialPlatform.Discord, "discord_789", "https://example.com/avatar3.png");

        friend1.UpdateStatus(true, "The Legend of Zelda");
        friend2.UpdateStatus(true, "Super Mario Bros");
        friend3.UpdateStatus(false, null);

        await _friendRepository.AddOrUpdateFriendAsync(friend1, ct);
        await _friendRepository.AddOrUpdateFriendAsync(friend2, ct);
        await _friendRepository.AddOrUpdateFriendAsync(friend3, ct);

        // Create some sample activities
        var activities = new[]
        {
            FriendActivity.Create(friend1.Id, ActivityType.StartedPlaying, "The Legend of Zelda", SocialPlatform.Discord),
            FriendActivity.Create(friend2.Id, ActivityType.StartedPlaying, "Super Mario Bros", SocialPlatform.Discord),
            FriendActivity.Create(friend1.Id, ActivityType.UnlockedAchievement, "The Legend of Zelda", SocialPlatform.Discord, "Master Sword acquired!"),
            FriendActivity.Create(friend3.Id, ActivityType.CompletedGame, "Final Fantasy VII", SocialPlatform.Discord)
        };

        foreach (var activity in activities)
        {
            await _friendRepository.AddActivityAsync(activity, ct);
        }
    }
}

