using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.SocialFeatures;

/// <summary>
/// Engine for managing chat messages and history.
/// </summary>
public sealed class MessagingEngine
{
    private readonly ILogger<MessagingEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public MessagingEngine(ILogger<MessagingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    public Models.NetworkFeatures.ChatMessage CreateMessage(
        string fromPlayerId,
        string fromPlayerName,
        string message,
        ChatChannel channel = ChatChannel.Whisper,
        string? targetId = null)
    {
        var chatMessage = new Models.NetworkFeatures.ChatMessage(
            MessageId: Guid.NewGuid().ToString(),
            SenderId: fromPlayerId,
            SenderName: fromPlayerName,
            Message: message,
            Channel: channel,
            Timestamp: _timeProvider.UtcNow,
            TargetId: targetId
        );

        _logger.LogDebug("Created message from {FromPlayer} on channel {Channel}", fromPlayerId, channel);
        return chatMessage;
    }

    /// <summary>
    /// Validates a message.
    /// </summary>
    public (bool IsValid, string? Error) ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, "Message cannot be empty");
        }

        if (message.Length > 500)
        {
            return (false, "Message exceeds maximum length of 500 characters");
        }

        return (true, null);
    }

    /// <summary>
    /// Generates a conversation ID for two players.
    /// </summary>
    public string GetConversationId(string player1, string player2)
    {
        var players = new[] { player1, player2 }.OrderBy(p => p).ToArray();
        return $"{players[0]}_{players[1]}";
    }

    /// <summary>
    /// Gets the conversation ID for a message.
    /// </summary>
    public string GetConversationIdForMessage(string playerId, Models.NetworkFeatures.ChatMessage message)
    {
        if (message.Channel == ChatChannel.Whisper && message.TargetId != null)
        {
            return GetConversationId(playerId, message.TargetId);
        }

        return $"channel_{message.Channel}";
    }

    /// <summary>
    /// Gets recent messages from a conversation.
    /// </summary>
    public IReadOnlyList<Models.NetworkFeatures.ChatMessage> GetRecentMessages(
        IEnumerable<Models.NetworkFeatures.ChatMessage> messages,
        int limit = 50)
    {
        return messages
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .OrderBy(m => m.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Checks if a player can send a message based on friendship status.
    /// </summary>
    public bool CanSendMessage(FriendshipStatus? friendshipStatus)
    {
        // Allow if no friendship exists or not blocked
        return friendshipStatus != FriendshipStatus.Blocked;
    }
}
