using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Application.Mugen.Models.SocialFeatures;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.SocialFeatures;

/// <summary>
/// Engine for managing friendships and friend requests.
/// </summary>
public sealed class FriendshipEngine
{
    private readonly ILogger<FriendshipEngine> _logger;

    public FriendshipEngine(ILogger<FriendshipEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new friend request.
    /// </summary>
    public Friendship CreateFriendRequest(string fromPlayerId, string toPlayerId)
    {
        var friendship = new Friendship
        {
            Id = Guid.NewGuid().ToString(),
            Player1Id = fromPlayerId,
            Player2Id = toPlayerId,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = fromPlayerId
        };

        _logger.LogInformation("Created friend request from {FromPlayer} to {ToPlayer}", fromPlayerId, toPlayerId);
        return friendship;
    }

    /// <summary>
    /// Accepts a pending friend request.
    /// </summary>
    public bool AcceptFriendRequest(Friendship friendship, string acceptingPlayerId)
    {
        if (friendship.Status != FriendshipStatus.Pending)
        {
            _logger.LogWarning("Cannot accept friendship: status is {Status}", friendship.Status);
            return false;
        }

        if (friendship.RequestedBy == acceptingPlayerId)
        {
            _logger.LogWarning("Player cannot accept their own friend request");
            return false;
        }

        friendship.Status = FriendshipStatus.Accepted;
        friendship.AcceptedAt = DateTime.UtcNow;

        _logger.LogInformation("Friend request accepted between {Player1} and {Player2}",
            friendship.Player1Id, friendship.Player2Id);
        return true;
    }

    /// <summary>
    /// Declines a pending friend request.
    /// </summary>
    public bool DeclineFriendRequest(Friendship friendship)
    {
        if (friendship.Status != FriendshipStatus.Pending)
        {
            _logger.LogWarning("Cannot decline friendship: status is {Status}", friendship.Status);
            return false;
        }

        friendship.Status = FriendshipStatus.Blocked;
        friendship.DeclinedAt = DateTime.UtcNow;

        _logger.LogInformation("Friend request declined");
        return true;
    }

    /// <summary>
    /// Removes an active friendship.
    /// </summary>
    public bool RemoveFriend(Friendship friendship)
    {
        if (friendship.Status != FriendshipStatus.Accepted)
        {
            _logger.LogWarning("Cannot remove friendship: status is {Status}", friendship.Status);
            return false;
        }

        friendship.Status = FriendshipStatus.Blocked;
        friendship.RemovedAt = DateTime.UtcNow;

        _logger.LogInformation("Friendship removed between {Player1} and {Player2}",
            friendship.Player1Id, friendship.Player2Id);
        return true;
    }

    /// <summary>
    /// Creates a block relationship.
    /// </summary>
    public Friendship CreateBlock(string blockerId, string blockedId)
    {
        var friendship = new Friendship
        {
            Id = Guid.NewGuid().ToString(),
            Player1Id = blockerId,
            Player2Id = blockedId,
            Status = FriendshipStatus.Blocked,
            BlockedAt = DateTime.UtcNow,
            BlockedBy = blockerId,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = blockerId
        };

        _logger.LogInformation("Player {Blocked} blocked by {Blocker}", blockedId, blockerId);
        return friendship;
    }

    /// <summary>
    /// Blocks an existing friendship.
    /// </summary>
    public void BlockFriendship(Friendship friendship, string blockerId)
    {
        friendship.Status = FriendshipStatus.Blocked;
        friendship.BlockedAt = DateTime.UtcNow;
        friendship.BlockedBy = blockerId;

        _logger.LogInformation("Player {Blocker} blocked friendship", blockerId);
    }

    /// <summary>
    /// Creates friend info from a friendship.
    /// </summary>
    public FriendInfo CreateFriendInfo(string playerId, Friendship friendship, PlayerProfile friendProfile)
    {
        var friendId = friendship.Player1Id == playerId ? friendship.Player2Id : friendship.Player1Id;

        return new FriendInfo(
            FriendId: friendId,
            FriendName: friendProfile.PlayerName,
            Status: friendship.Status,
            FriendsSince: friendship.AcceptedAt ?? friendship.RequestedAt,
            IsOnline: friendProfile.Status == PlayerOnlineStatus.Online,
            CurrentActivity: friendProfile.CurrentActivity
        );
    }

    /// <summary>
    /// Creates a friend request DTO from a friendship.
    /// </summary>
    public FriendRequest CreateFriendRequestDto(Friendship friendship, PlayerProfile fromProfile)
    {
        return new FriendRequest
        {
            RequestId = friendship.Id,
            FromPlayerId = friendship.RequestedBy,
            FromPlayerName = fromProfile.PlayerName,
            RequestedAt = friendship.RequestedAt,
            Message = friendship.Message
        };
    }

    /// <summary>
    /// Finds a friendship between two players.
    /// </summary>
    public Friendship? FindFriendship(IEnumerable<Friendship> friendships, string player1Id, string player2Id)
    {
        return friendships.FirstOrDefault(f =>
            (f.Player1Id == player1Id && f.Player2Id == player2Id) ||
            (f.Player1Id == player2Id && f.Player2Id == player1Id));
    }

    /// <summary>
    /// Gets all accepted friends from a player's friendship list.
    /// </summary>
    public IEnumerable<Friendship> GetAcceptedFriends(IEnumerable<Friendship> friendships)
    {
        return friendships.Where(f => f.Status == FriendshipStatus.Accepted);
    }

    /// <summary>
    /// Gets pending friend requests where the specified player is the recipient.
    /// </summary>
    public IEnumerable<Friendship> GetPendingRequestsForPlayer(IEnumerable<Friendship> friendships, string playerId)
    {
        return friendships.Where(f =>
            f.Status == FriendshipStatus.Pending &&
            f.RequestedBy != playerId);
    }

    /// <summary>
    /// Creates sample friendships for testing.
    /// </summary>
    public Friendship CreateSampleFriendship(string player1, string player2)
    {
        return new Friendship
        {
            Id = Guid.NewGuid().ToString(),
            Player1Id = player1,
            Player2Id = player2,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow.AddDays(-30),
            AcceptedAt = DateTime.UtcNow.AddDays(-29),
            RequestedBy = player1
        };
    }
}
