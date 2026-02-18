using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Engine for managing spectator chat.
/// </summary>
public class ChatEngine : IChatEngine
{
    private readonly ILogger<ChatEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, List<ChatMessage>> _matchMessages = new();

    public ChatEngine(ILogger<ChatEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the message count for a match.
    /// </summary>
    public int GetMessageCountForMatch(string matchId)
    {
        if (_matchMessages.TryGetValue(matchId, out var messages))
        {
            return messages.Count;
        }
        return 0;
    }

    /// <summary>
    /// Gets messages for a match with an optional limit.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetMessagesForMatch(string matchId, int limit = 50)
    {
        if (_matchMessages.TryGetValue(matchId, out var messages))
        {
            return messages
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .ToList();
        }
        return new List<ChatMessage>();
    }

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    public Result<ChatMessage> CreateMessage(string sessionId, string matchId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Result.Failure<ChatMessage>("Message cannot be empty");
        }

        if (message.Length > 500)
        {
            return Result.Failure<ChatMessage>("Message too long (max 500 characters)");
        }

        var chatMessage = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString("N"),
            MatchId = matchId,
            SessionId = sessionId,
            SenderName = $"Spectator_{sessionId[..8]}",
            Message = message.Trim(),
            Timestamp = _timeProvider.UtcNow,
            MessageType = SpectatorMessageType.Chat
        };

        return Result.Success(chatMessage);
    }

    /// <summary>
    /// Adds a message to the match chat and invokes a callback.
    /// </summary>
    public void AddMessage(ChatMessage message, Action<ChatMessage>? onMessageAdded = null)
    {
        if (!_matchMessages.TryGetValue(message.MatchId, out var messages))
        {
            messages = new List<ChatMessage>();
            _matchMessages[message.MatchId] = messages;
        }

        messages.Add(message);

        // Keep only last 1000 messages per match to prevent memory issues
        if (messages.Count > 1000)
        {
            messages.RemoveAt(0);
        }

        onMessageAdded?.Invoke(message);

        _logger.LogDebug(
            "Added chat message {MessageId} to match {MatchId}",
            message.MessageId,
            message.MatchId);
    }
}
