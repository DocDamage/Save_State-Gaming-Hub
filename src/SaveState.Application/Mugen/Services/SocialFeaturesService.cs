using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Application.Mugen.Models.SocialFeatures;
using SaveState.Application.Mugen.Services.SocialFeatures;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Social features service providing friends management, messaging, reputation system,
/// and community interactions for MUGEN. Acts as a coordinator delegating to specialized engines.
/// </summary>
public sealed class SocialFeaturesService : ISocialFeaturesService
{
    private readonly ILogger<SocialFeaturesService> _logger;
    private readonly ICacheService _cache;
    private readonly ProfileEngine _profileEngine;
    private readonly FriendshipEngine _friendshipEngine;
    private readonly MessagingEngine _messagingEngine;
    private readonly ReputationEngine _reputationEngine;
    private readonly StateHelperEngine _stateHelper;

    private readonly Dictionary<string, PlayerProfile> _playerProfiles = new();
    private readonly Dictionary<string, List<Friendship>> _friendships = new();
    private readonly Dictionary<string, List<Models.NetworkFeatures.ChatMessage>> _chatHistory = new();
    private readonly Dictionary<string, PlayerReputation> _reputations = new();
    private readonly List<PlayerReport> _reports = new();

    public SocialFeaturesService(
        ILogger<SocialFeaturesService> logger,
        ICacheService cache,
        ProfileEngine profileEngine,
        FriendshipEngine friendshipEngine,
        MessagingEngine messagingEngine,
        ReputationEngine reputationEngine,
        StateHelperEngine stateHelper)
    {
        _logger = logger;
        _cache = cache;
        _profileEngine = profileEngine;
        _friendshipEngine = friendshipEngine;
        _messagingEngine = messagingEngine;
        _reputationEngine = reputationEngine;
        _stateHelper = stateHelper;
        InitializeSampleData();
    }

    public async Task<Result<PlayerProfile>> GetPlayerProfileAsync(string playerId, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"profile_{playerId}";
            if (_cache.TryGetValue<PlayerProfile>(cacheKey, out var cached))
                return Result.Success(cached!);
            if (_playerProfiles.TryGetValue(playerId, out var profile))
            {
                _cache.Set(cacheKey, profile, TimeSpan.FromMinutes(30));
                return Result.Success(profile);
            }
            return Result.Failure<PlayerProfile>("Player profile not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile for {PlayerId}", playerId);
            return Result.Failure<PlayerProfile>($"Failed to get profile: {ex.Message}");
        }
    }

    public async Task<Result> UpdatePlayerProfileAsync(string playerId, PlayerProfileUpdate update, CancellationToken ct = default)
    {
        try
        {
            if (!_playerProfiles.TryGetValue(playerId, out var profile))
                return Result.Failure("Player profile not found");
            _playerProfiles[playerId] = _profileEngine.ApplyProfileUpdate(profile, update);
            _cache.Remove($"profile_{playerId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for {PlayerId}", playerId);
            return Result.Failure($"Failed to update profile: {ex.Message}");
        }
    }

    public async Task<Result> UpdatePlayerStatusAsync(string playerId, PlayerOnlineStatus status, string? activity, CancellationToken ct = default)
    {
        try
        {
            if (!_playerProfiles.TryGetValue(playerId, out var profile))
                return Result.Failure("Player profile not found");
            _playerProfiles[playerId] = _profileEngine.UpdateStatus(profile, status, activity);
            _cache.Remove($"profile_{playerId}");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for {PlayerId}", playerId);
            return Result.Failure($"Failed to update status: {ex.Message}");
        }
    }

    public async Task<Result> SendFriendRequestAsync(string fromPlayerId, string toPlayerId, CancellationToken ct = default)
    {
        try
        {
            if (!_playerProfiles.ContainsKey(fromPlayerId) || !_playerProfiles.ContainsKey(toPlayerId))
                return Result.Failure("One or both players not found");
            if (GetFriendship(fromPlayerId, toPlayerId) != null)
                return Result.Failure("Friendship already exists or request pending");
            var friendship = _friendshipEngine.CreateFriendRequest(fromPlayerId, toPlayerId);
            _stateHelper.AddFriendshipToList(_friendships, fromPlayerId, friendship);
            _stateHelper.AddFriendshipToList(_friendships, toPlayerId, friendship);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request from {FromPlayer} to {ToPlayer}", fromPlayerId, toPlayerId);
            return Result.Failure($"Failed to send friend request: {ex.Message}");
        }
    }

    public async Task<Result> AcceptFriendRequestAsync(string playerId, string friendId, CancellationToken ct = default)
    {
        try
        {
            var friendship = GetFriendship(playerId, friendId);
            if (friendship == null)
                return Result.Failure("No pending friend request found");
            return _friendshipEngine.AcceptFriendRequest(friendship, playerId) 
                ? Result.Success() : Result.Failure("Failed to accept friend request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting friend request for {PlayerId}", playerId);
            return Result.Failure($"Failed to accept friend request: {ex.Message}");
        }
    }

    public async Task<Result> DeclineFriendRequestAsync(string playerId, string friendId, CancellationToken ct = default)
    {
        try
        {
            var friendship = GetFriendship(playerId, friendId);
            if (friendship == null)
                return Result.Failure("No pending friend request found");
            return _friendshipEngine.DeclineFriendRequest(friendship) 
                ? Result.Success() : Result.Failure("Failed to decline friend request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining friend request for {PlayerId}", playerId);
            return Result.Failure($"Failed to decline friend request: {ex.Message}");
        }
    }

    public async Task<Result> RemoveFriendAsync(string playerId, string friendId, CancellationToken ct = default)
    {
        try
        {
            var friendship = GetFriendship(playerId, friendId);
            if (friendship == null)
                return Result.Failure("No active friendship found");
            return _friendshipEngine.RemoveFriend(friendship) 
                ? Result.Success() : Result.Failure("Failed to remove friend");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing friendship for {PlayerId}", playerId);
            return Result.Failure($"Failed to remove friend: {ex.Message}");
        }
    }

    public async Task<Result> BlockPlayerAsync(string playerId, string blockedPlayerId, CancellationToken ct = default)
    {
        try
        {
            var friendship = GetFriendship(playerId, blockedPlayerId);
            if (friendship != null)
                _friendshipEngine.BlockFriendship(friendship, playerId);
            else
            {
                var blockFriendship = _friendshipEngine.CreateBlock(playerId, blockedPlayerId);
                _stateHelper.AddFriendshipToList(_friendships, playerId, blockFriendship);
                _stateHelper.AddFriendshipToList(_friendships, blockedPlayerId, blockFriendship);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking player for {PlayerId}", playerId);
            return Result.Failure($"Failed to block player: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<FriendInfo>>> GetFriendsAsync(string playerId, CancellationToken ct = default)
    {
        try
        {
            if (!_friendships.TryGetValue(playerId, out var friendships))
                return Result.Success<IReadOnlyList<FriendInfo>>(Array.Empty<FriendInfo>());
            var friends = _friendshipEngine.GetAcceptedFriends(friendships)
                .Select(f => _friendshipEngine.CreateFriendInfo(playerId, f, _playerProfiles[
                    f.Player1Id == playerId ? f.Player2Id : f.Player1Id])).ToList();
            return Result.Success<IReadOnlyList<FriendInfo>>(friends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friends for {PlayerId}", playerId);
            return Result.Failure<IReadOnlyList<FriendInfo>>($"Failed to get friends: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<FriendRequest>>> GetPendingFriendRequestsAsync(string playerId, CancellationToken ct = default)
    {
        try
        {
            if (!_friendships.TryGetValue(playerId, out var friendships))
                return Result.Success<IReadOnlyList<FriendRequest>>(Array.Empty<FriendRequest>());
            var pendingRequests = _friendshipEngine.GetPendingRequestsForPlayer(friendships, playerId)
                .Select(f => _friendshipEngine.CreateFriendRequestDto(f, _playerProfiles[f.RequestedBy])).ToList();
            return Result.Success<IReadOnlyList<FriendRequest>>(pendingRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending requests for {PlayerId}", playerId);
            return Result.Failure<IReadOnlyList<FriendRequest>>($"Failed to get friend requests: {ex.Message}");
        }
    }

    public async Task<Result> SendMessageAsync(string fromPlayerId, string toPlayerId, string message, CancellationToken ct = default)
    {
        try
        {
            if (!_playerProfiles.ContainsKey(fromPlayerId) || !_playerProfiles.ContainsKey(toPlayerId))
                return Result.Failure("One or both players not found");
            var friendship = GetFriendship(fromPlayerId, toPlayerId);
            if (!_messagingEngine.CanSendMessage(friendship?.Status))
                return Result.Failure("Cannot send message to blocked player");
            var (isValid, error) = _messagingEngine.ValidateMessage(message);
            if (!isValid)
                return Result.Failure(error!);
            var chatMessage = _messagingEngine.CreateMessage(
                fromPlayerId, _playerProfiles[fromPlayerId].PlayerName, message, ChatChannel.Whisper, toPlayerId);
            AddMessageToHistory(fromPlayerId, chatMessage);
            AddMessageToHistory(toPlayerId, chatMessage);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message from {FromPlayer} to {ToPlayer}", fromPlayerId, toPlayerId);
            return Result.Failure($"Failed to send message: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<Models.NetworkFeatures.ChatMessage>>> GetMessageHistoryAsync(string playerId, string otherPlayerId, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var conversationId = _messagingEngine.GetConversationId(playerId, otherPlayerId);
            if (!_chatHistory.TryGetValue(conversationId, out var messages))
                return Result.Success<IReadOnlyList<Models.NetworkFeatures.ChatMessage>>(Array.Empty<Models.NetworkFeatures.ChatMessage>());
            return Result.Success<IReadOnlyList<Models.NetworkFeatures.ChatMessage>>(_messagingEngine.GetRecentMessages(messages, limit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting message history for {PlayerId}", playerId);
            return Result.Failure<IReadOnlyList<Models.NetworkFeatures.ChatMessage>>($"Failed to get message history: {ex.Message}");
        }
    }

    public async Task<Result<PlayerReputation>> GetPlayerReputationAsync(string playerId, CancellationToken ct = default)
    {
        try
        {
            if (_reputations.TryGetValue(playerId, out var reputation))
                return Result.Success(reputation);
            var defaultReputation = _reputationEngine.CreateDefaultReputation(playerId);
            _reputations[playerId] = defaultReputation;
            return Result.Success(defaultReputation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation for {PlayerId}", playerId);
            return Result.Failure<PlayerReputation>($"Failed to get reputation: {ex.Message}");
        }
    }

    public async Task<Result> SubmitPlayerReportAsync(string reporterId, string reportedPlayerId, ReportReason reason, string description, CancellationToken ct = default)
    {
        try
        {
            var report = _reputationEngine.CreateReport(reporterId, reportedPlayerId, reason, description);
            _reports.Add(report);
            var reputation = await GetPlayerReputationAsync(reportedPlayerId, ct);
            if (reputation.IsSuccess)
            {
                _reputationEngine.UpdateReputationForReport(reputation.Value, reason);
                _reputations[reportedPlayerId] = reputation.Value;
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting report for {ReportedPlayer}", reportedPlayerId);
            return Result.Failure($"Failed to submit report: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<OnlinePlayer>>> GetOnlinePlayersAsync(string? region = null, CancellationToken ct = default)
    {
        try
        {
            return Result.Success(_profileEngine.GetOnlinePlayers(_playerProfiles.Values, region));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online players");
            return Result.Failure<IReadOnlyList<OnlinePlayer>>($"Failed to get online players: {ex.Message}");
        }
    }

    private void InitializeSampleData()
    {
        _stateHelper.InitializeSamplePlayers(_playerProfiles, _friendships, _profileEngine);
        CreateSampleFriendship("player1", "player2");
        CreateSampleFriendship("player1", "player3");
        CreateSampleFriendship("player2", "player4");
    }

    private void CreateSampleFriendship(string player1, string player2)
    {
        var friendship = _friendshipEngine.CreateSampleFriendship(player1, player2);
        _stateHelper.AddFriendshipToList(_friendships, player1, friendship);
        _stateHelper.AddFriendshipToList(_friendships, player2, friendship);
    }

    private Friendship? GetFriendship(string player1Id, string player2Id)
    {
        if (!_friendships.TryGetValue(player1Id, out var friendships))
            return null;
        return _friendshipEngine.FindFriendship(friendships, player1Id, player2Id);
    }

    private void AddMessageToHistory(string playerId, Models.NetworkFeatures.ChatMessage message)
    {
        var conversationId = _messagingEngine.GetConversationIdForMessage(playerId, message);
        _stateHelper.AddMessageToHistory(_chatHistory, conversationId, message);
    }
}
