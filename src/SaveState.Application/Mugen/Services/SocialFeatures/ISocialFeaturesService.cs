using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Application.Mugen.Models.SocialFeatures;
using SaveState.Core.Common;
using PlayerProfile = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerProfile;

namespace SaveState.Application.Mugen.Services.SocialFeatures;

/// <summary>
/// Social features service interface providing friends management,
/// messaging, reputation system, and community interactions.
/// </summary>
public interface ISocialFeaturesService
{
    // Profile Management
    Task<Result<PlayerProfile>> GetPlayerProfileAsync(string playerId, CancellationToken ct = default);
    Task<Result> UpdatePlayerProfileAsync(string playerId, PlayerProfileUpdate update, CancellationToken ct = default);
    Task<Result> UpdatePlayerStatusAsync(string playerId, PlayerOnlineStatus status, string? activity, CancellationToken ct = default);

    // Friends Management
    Task<Result> SendFriendRequestAsync(string fromPlayerId, string toPlayerId, CancellationToken ct = default);
    Task<Result> AcceptFriendRequestAsync(string playerId, string friendId, CancellationToken ct = default);
    Task<Result> DeclineFriendRequestAsync(string playerId, string friendId, CancellationToken ct = default);
    Task<Result> RemoveFriendAsync(string playerId, string friendId, CancellationToken ct = default);
    Task<Result> BlockPlayerAsync(string playerId, string blockedPlayerId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FriendInfo>>> GetFriendsAsync(string playerId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<FriendRequest>>> GetPendingFriendRequestsAsync(string playerId, CancellationToken ct = default);

    // Messaging
    Task<Result> SendMessageAsync(string fromPlayerId, string toPlayerId, string message, CancellationToken ct = default);
    Task<Result<IReadOnlyList<Models.NetworkFeatures.ChatMessage>>> GetMessageHistoryAsync(string playerId, string otherPlayerId, int limit = 50, CancellationToken ct = default);

    // Reputation & Reporting
    Task<Result<PlayerReputation>> GetPlayerReputationAsync(string playerId, CancellationToken ct = default);
    Task<Result> SubmitPlayerReportAsync(string reporterId, string reportedPlayerId, ReportReason reason, string description, CancellationToken ct = default);

    // Online Presence
    Task<Result<IReadOnlyList<OnlinePlayer>>> GetOnlinePlayersAsync(string? region = null, CancellationToken ct = default);
}
