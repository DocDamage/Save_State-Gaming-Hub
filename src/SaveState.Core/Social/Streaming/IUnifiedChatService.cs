using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Social.Streaming;

/// <summary>
/// Service for aggregating chat from multiple streaming platforms into a unified interface.
/// </summary>
public interface IUnifiedChatService
{
    /// <summary>
    /// Connects to chat for a stream session.
    /// </summary>
    Task<Result> ConnectAsync(string sessionId, IReadOnlyList<StreamingPlatformType> platforms, CancellationToken ct = default);

    /// <summary>
    /// Disconnects from all chats.
    /// </summary>
    Task<Result> DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a message to all connected platforms.
    /// </summary>
    Task<Result> SendMessageAsync(string message, CancellationToken ct = default);

    /// <summary>
    /// Sends a message to a specific platform.
    /// </summary>
    Task<Result> SendMessageToPlatformAsync(StreamingPlatformType platform, string message, CancellationToken ct = default);

    /// <summary>
    /// Deletes a message from all platforms where possible.
    /// </summary>
    Task<Result> DeleteMessageAsync(string messageId, CancellationToken ct = default);

    /// <summary>
    /// Timeouts a user on all platforms.
    /// </summary>
    Task<Result> TimeoutUserAsync(string username, TimeSpan duration, CancellationToken ct = default);

    /// <summary>
    /// Bans a user from all platforms.
    /// </summary>
    Task<Result> BanUserAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Gets recent chat messages.
    /// </summary>
    Task<Result<IReadOnlyList<ChatMessage>>> GetRecentMessagesAsync(int count = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets chat statistics.
    /// </summary>
    Task<Result<ChatStatistics>> GetStatisticsAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a chat command.
    /// </summary>
    Task<Result> AddCommandAsync(ChatCommand command, CancellationToken ct = default);

    /// <summary>
    /// Removes a chat command.
    /// </summary>
    Task<Result> RemoveCommandAsync(string commandName, CancellationToken ct = default);

    /// <summary>
    /// Gets all active chat commands.
    /// </summary>
    Task<Result<IReadOnlyList<ChatCommand>>> GetCommandsAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when a chat message is received.
    /// </summary>
    event EventHandler<ChatMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Event raised when a chat command is triggered.
    /// </summary>
    event EventHandler<ChatCommandTriggeredEventArgs>? CommandTriggered;

    /// <summary>
    /// Event raised when a user joins chat.
    /// </summary>
    event EventHandler<UserJoinedEventArgs>? UserJoined;

    /// <summary>
    /// Event raised when a user leaves chat.
    /// </summary>
    event EventHandler<UserLeftEventArgs>? UserLeft;
}

/// <summary>
/// Chat message from any platform.
/// </summary>
public sealed record ChatMessage(
    string Id,
    string Content,
    ChatUser User,
    StreamingPlatformType Platform,
    DateTime Timestamp,
    IReadOnlyList<ChatBadge> Badges,
    IReadOnlyList<ChatEmote> Emotes,
    bool IsReply,
    string? ReplyToMessageId = null,
    string? ReplyToUsername = null);

/// <summary>
/// Chat user information.
/// </summary>
public sealed record ChatUser(
    string Id,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string? Color,
    bool IsModerator,
    bool IsSubscriber,
    bool IsBroadcaster,
    bool IsVerified,
    UserRole Role);

/// <summary>
/// Chat badge.
/// </summary>
public sealed record ChatBadge(
    string Name,
    string Url,
    int Version = 1);

/// <summary>
/// Chat emote.
/// </summary>
public sealed record ChatEmote(
    string Name,
    string Url,
    bool IsAnimated = false);

/// <summary>
/// Chat statistics.
/// </summary>
public sealed record ChatStatistics(
    int TotalMessages,
    int UniqueChatters,
    int MessagesPerMinute,
    IReadOnlyDictionary<StreamingPlatformType, int> MessagesByPlatform,
    IReadOnlyList<string> TopEmotes,
    IReadOnlyList<string> TopChatters);

/// <summary>
/// Chat command definition.
/// </summary>
public sealed record ChatCommand(
    string Name,
    string Description,
    string Response,
    UserRole MinRole,
    int CooldownSeconds,
    bool IsEnabled = true,
    IReadOnlyList<string>? Aliases = null);

/// <summary>
/// User roles for chat commands.
/// </summary>
public enum UserRole
{
    Everyone,
    Subscriber,
    Moderator,
    Broadcaster
}

/// <summary>
/// Event args for chat message received events.
/// </summary>
public sealed class ChatMessageReceivedEventArgs : EventArgs
{
    public ChatMessage Message { get; }
    public DateTime ReceivedAt { get; }

    public ChatMessageReceivedEventArgs(ChatMessage message, ITimeProvider? timeProvider = null)
    {
        Message = message;
        ReceivedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for chat command triggered events.
/// </summary>
public sealed class ChatCommandTriggeredEventArgs : EventArgs
{
    public string CommandName { get; }
    public ChatMessage Message { get; }
    public string[] Arguments { get; }
    public DateTime TriggeredAt { get; }

    public ChatCommandTriggeredEventArgs(string commandName, ChatMessage message, string[] arguments, ITimeProvider? timeProvider = null)
    {
        CommandName = commandName;
        Message = message;
        Arguments = arguments;
        TriggeredAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for user joined events.
/// </summary>
public sealed class UserJoinedEventArgs : EventArgs
{
    public string Username { get; }
    public StreamingPlatformType Platform { get; }
    public DateTime JoinedAt { get; }

    public UserJoinedEventArgs(string username, StreamingPlatformType platform, ITimeProvider? timeProvider = null)
    {
        Username = username;
        Platform = platform;
        JoinedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for user left events.
/// </summary>
public sealed class UserLeftEventArgs : EventArgs
{
    public string Username { get; }
    public StreamingPlatformType Platform { get; }
    public DateTime LeftAt { get; }

    public UserLeftEventArgs(string username, StreamingPlatformType platform, ITimeProvider? timeProvider = null)
    {
        Username = username;
        Platform = platform;
        LeftAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}
