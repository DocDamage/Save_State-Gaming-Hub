using SaveState.Core.Ai.Services;

namespace SaveState.Core.Ai.Context;

/// <summary>
/// Represents a conversation session with history.
/// </summary>
public sealed class ConversationContext
{
    public string SessionId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastActivityAt { get; private set; }
    private readonly List<ChatMessage> _messages = new();

    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    public ConversationContext(string sessionId)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        CreatedAt = DateTimeOffset.UtcNow;
        LastActivityAt = CreatedAt;
    }

    public void AddMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages.Add(message);
        LastActivityAt = DateTimeOffset.UtcNow;
    }

    public void Clear()
    {
        _messages.Clear();
        LastActivityAt = DateTimeOffset.UtcNow;
    }
}
