using SaveState.Core.Ai.Services;
using SaveState.Core.Common;

namespace SaveState.Core.Ai.Context;

/// <summary>
/// Manages AI conversation contexts and session history.
/// </summary>
public interface IConversationContextService
{
    /// <summary>
    /// Gets or creates a conversation context for the given session.
    /// </summary>
    Task<Result<ConversationContext>> GetOrCreateContextAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Stores a message in the conversation history.
    /// </summary>
    Task<Result> AddMessageAsync(string sessionId, ChatMessage message, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all messages for a session.
    /// </summary>
    Task<Result<IReadOnlyList<ChatMessage>>> GetHistoryAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Clears the conversation history for a session.
    /// </summary>
    Task<Result> ClearSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of active sessions.
    /// </summary>
    int GetActiveSessionCount();
}
