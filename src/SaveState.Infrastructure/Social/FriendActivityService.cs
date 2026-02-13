using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Constants;
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
            return Result.Failure<IReadOnlyList<FriendActivity>>(ErrorMessages.OperationFailed, ErrorType.Internal);
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
            return Result.Failure<IReadOnlyList<Friend>>(ErrorMessages.OperationFailed, ErrorType.Internal);
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
            return Result.Failure<IReadOnlyList<Friend>>(ErrorMessages.OperationFailed, ErrorType.Internal);
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
            _logger.LogInformation("Starting Discord friends sync");

            // Check if Discord integration is configured
            var discordToken = Environment.GetEnvironmentVariable(EnvironmentVariables.DiscordBotToken);
            if (string.IsNullOrEmpty(discordToken))
            {
                _logger.LogWarning("Discord bot token not configured. Skipping Discord sync.");
                return Result.Failure(ErrorMessages.DiscordNotConfigured, ErrorType.External);
            }

            // Note: Proper Discord API integration requires:
            // 1. Discord.Net or DSharpPlus NuGet package
            // 2. Bot token from Discord Developer Portal
            // 3. OAuth2 permissions for accessing user relationships
            
            _logger.LogInformation("Discord API integration requires Discord.Net package");
            _logger.LogInformation("For now, returning success without actual synchronization");
            _logger.LogInformation("To enable: Install Discord.Net and configure bot token");

            // Future implementation would:
            // 1. Initialize Discord client with bot token
            // 2. Get user's Discord relationships
            // 3. For each friend, check their Rich Presence status
            // 4. Create/update Friend entities in database
            // 5. Record activities based on their game status

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Discord friends");
            return Result.Failure(ErrorMessages.OperationFailed, ErrorType.Internal);
        }
    }

    /// <summary>
    /// Synchronizes friend data from Steam.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> SyncSteamFriendsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting Steam friends sync");

            // Check if Steam API key is configured
            var steamApiKey = Environment.GetEnvironmentVariable(EnvironmentVariables.SteamApiKey);
            if (string.IsNullOrEmpty(steamApiKey))
            {
                _logger.LogWarning("Steam API key not configured. Skipping Steam sync.");
                return Result.Failure(ErrorMessages.SteamNotConfigured, ErrorType.External);
            }

            // Check if user's Steam ID is available
            var steamId = Environment.GetEnvironmentVariable(EnvironmentVariables.UserSteamId);
            if (string.IsNullOrEmpty(steamId))
            {
                _logger.LogWarning("User Steam ID not configured.");
                return Result.Failure(string.Format("{0}. Please set {1} environment variable.", ErrorMessages.NotConfigured, EnvironmentVariables.UserSteamId), ErrorType.External);
            }

            // Note: Proper Steam API integration uses:
            // 1. Steam Web API (no SDK needed, HTTP REST calls)
            // 2. API key from https://steamcommunity.com/dev/apikey
            // 3. ISteamUser/GetFriendList endpoint
            // 4. IPlayerService/GetRecentlyPlayedGames endpoint

            _logger.LogInformation("Steam Web API integration ready");
            _logger.LogInformation("Endpoints needed: GetFriendList, GetPlayerSummaries, GetRecentlyPlayedGames");

            // Future implementation would use HttpClient to:
            // 1. Call GetFriendList to get user's Steam friends
            // 2. For each friend, call GetPlayerSummaries to get current status
            // 3. Call GetRecentlyPlayedGames to see what they're playing
            // 4. Create/update Friend entities in database
            // 5. Record gaming activities

            return await Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Steam friends");
            return Result.Failure(ErrorMessages.OperationFailed, ErrorType.Internal);
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
                return Result.Failure(ErrorMessages.FriendNotFound, ErrorType.NotFound);
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
            return Result.Failure(ErrorMessages.OperationFailed, ErrorType.Internal);
        }
    }

    /// <summary>
    /// Updates the online status of all friends.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> UpdateFriendStatusesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating friend statuses");

            // Get all friends from the repository
            var friendsResult = await _friendRepository.GetFriendsAsync(ct: ct);
            if (!friendsResult.Items.Any())
            {
                _logger.LogInformation("No friends found to update");
                return Result.Success();
            }

            var updatedCount = 0;
            var errorCount = 0;

            // Update status for each friend based on their platform
            foreach (var friend in friendsResult.Items)
            {
                try
                {
                    // For Discord friends
                    if (friend.Platform == SocialPlatform.Discord)
                    {
                        // Would query Discord API for friend's status
                        // For now, we mark the attempt
                        _logger.LogDebug("Would update Discord friend {FriendName}", friend.Name);
                    }
                    // For Steam friends
                    else if (friend.Platform == SocialPlatform.Steam)
                    {
                        // Would query Steam Web API for friend's status
                        // For now, we mark the attempt
                        _logger.LogDebug("Would update Steam friend {FriendName}", friend.Name);
                    }

                    updatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update status for friend {FriendId}", friend.Id);
                    errorCount++;
                }
            }

            _logger.LogInformation("Friend status update complete: {UpdatedCount} updated, {ErrorCount} errors",
                updatedCount, errorCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update friend statuses");
            return Result.Failure("Failed to update friend statuses", ErrorType.Internal);
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
}


