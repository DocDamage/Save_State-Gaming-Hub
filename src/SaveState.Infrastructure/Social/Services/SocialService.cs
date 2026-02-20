using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Constants;
using SaveState.Core.Common.Services;
using SaveState.Core.Social.Services;
using SaveState.Core.Social.Entities;

namespace SaveState.Infrastructure.Social.Services;

/// <summary>
/// Implementation of social features including friends and leaderboards.
/// </summary>
public class SocialService : ISocialService
{
    private readonly ILogger<SocialService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<Guid, Friend> _friends = new();
    private readonly Dictionary<LeaderboardType, List<LeaderboardEntry>> _leaderboards = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SocialService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="timeProvider">Time provider for testable time operations.</param>
    public SocialService(ILogger<SocialService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        InitializeSampleData();
    }

    /// <summary>
    /// Gets the list of all friends.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of friends.</returns>
    public Task<Result<IReadOnlyList<Friend>>> GetFriendsAsync(CancellationToken ct = default)
    {
        try
        {
            var friends = _friends.Values.ToList();
            _logger.LogInformation("Retrieved {Count} friends", friends.Count);
            return Task.FromResult(Result.Success<IReadOnlyList<Friend>>(friends));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving friends");
            return Task.FromResult(Result.Failure<IReadOnlyList<Friend>>(ErrorMessages.OperationFailed, ErrorType.Internal));
        }
    }

    /// <summary>
    /// Sends a friend request to a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to add.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> SendFriendRequestAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var friend = Friend.Create(
                name: $"user_{userId.ToString().Substring(0, 8)}",
                platform: SocialPlatform.Discord,
                platformUserId: userId.ToString());

            _friends[userId] = friend;
            _logger.LogInformation("Friend added: {UserId}", userId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding friend {UserId}", userId);
            return Task.FromResult(Result.Failure(ErrorMessages.OperationFailed, ErrorType.Internal));
        }
    }

    /// <summary>
    /// Removes a friend from the user's friend list.
    /// </summary>
    /// <param name="friendId">The unique identifier of the friend to remove.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> RemoveFriendAsync(Guid friendId, CancellationToken ct = default)
    {
        try
        {
            if (_friends.Remove(friendId))
            {
                _logger.LogInformation("Friend {FriendId} removed", friendId);
                return Task.FromResult(Result.Success());
            }
            return Task.FromResult(Result.Failure(ErrorMessages.FriendNotFound, ErrorType.NotFound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing friend {FriendId}", friendId);
            return Task.FromResult(Result.Failure(ErrorMessages.OperationFailed, ErrorType.Internal));
        }
    }

    /// <summary>
    /// Shares an achievement to social platforms.
    /// </summary>
    /// <param name="achievementName">The name of the achievement.</param>
    /// <param name="description">A description of the achievement.</param>
    /// <param name="rarity">The rarity level of the achievement.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<Result> ShareAchievementAsync(string achievementName, string description, string rarity, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Achievement shared: {AchievementName} ({Rarity})", achievementName, rarity);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing achievement");
            return Task.FromResult(Result.Failure(ErrorMessages.OperationFailed, ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets leaderboard entries for the specified type.
    /// </summary>
    /// <param name="type">The type of leaderboard to retrieve.</param>
    /// <param name="gameId">Optional game ID to filter results.</param>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the leaderboard entries.</returns>
    public Task<Result<IReadOnlyList<LeaderboardEntry>>> GetLeaderboardAsync(LeaderboardType type, string gameId = "", int limit = 50, CancellationToken ct = default)
    {
        try
        {
            if (!_leaderboards.TryGetValue(type, out var leaderboard))
            {
                leaderboard = GenerateSampleLeaderboard(type, gameId, limit);
                _leaderboards[type] = leaderboard;
            }

            var entries = leaderboard.Take(limit).ToList();
            _logger.LogInformation("Retrieved {Count} leaderboard entries for type {Type}", entries.Count, type);
            return Task.FromResult(Result.Success<IReadOnlyList<LeaderboardEntry>>(entries));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leaderboard for type {Type}", type);
            return Task.FromResult(Result.Failure<IReadOnlyList<LeaderboardEntry>>(ErrorMessages.OperationFailed, ErrorType.Internal));
        }
    }

    private void InitializeSampleData()
    {
        // Add some sample friends
        for (int i = 0; i < 5; i++)
        {
            var friendId = Guid.NewGuid();
            _friends[friendId] = Friend.Create(
                name: $"Friend {i + 1}",
                platform: SocialPlatform.Discord,
                platformUserId: $"discord_{i + 1}");
        }
    }

    private List<LeaderboardEntry> GenerateSampleLeaderboard(LeaderboardType type, string? gameId, int count)
    {
        var entries = new List<LeaderboardEntry>();
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            entries.Add(new LeaderboardEntry(
                Rank: i + 1,
                UserId: Guid.NewGuid(),
                Username: $"player_{i + 1}",
                Score: random.Next(1000, 50000),
                ScoreType: "points",
                Metadata: new Dictionary<string, object>
                {
                    ["games_played"] = random.Next(10, 200),
                    ["win_rate"] = random.Next(30, 95)
                },
                LastUpdated: _timeProvider.UtcNow.AddMinutes(-random.Next(0, 1440))));
        }

        return entries.OrderByDescending(e => e.Score).ToList();
    }
}

