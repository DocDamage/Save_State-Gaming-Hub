using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Interface for managing spectator chat.
/// </summary>
public interface IChatEngine
{
    /// <summary>
    /// Gets the message count for a match.
    /// </summary>
    int GetMessageCountForMatch(string matchId);

    /// <summary>
    /// Gets messages for a match with an optional limit.
    /// </summary>
    IReadOnlyList<ChatMessage> GetMessagesForMatch(string matchId, int limit = 50);

    /// <summary>
    /// Creates a new chat message.
    /// </summary>
    Result<ChatMessage> CreateMessage(string sessionId, string matchId, string message);

    /// <summary>
    /// Adds a message to the match chat and invokes a callback.
    /// </summary>
    void AddMessage(ChatMessage message, Action<ChatMessage>? onMessageAdded = null);
}
