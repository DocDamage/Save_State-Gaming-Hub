using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Ai.Context;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.Ai.Context;

/// <summary>
/// In-memory implementation of conversation context management with sliding expiration.
/// </summary>
public sealed class InMemoryConversationContextService : IConversationContextService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryConversationContextService> _logger;
    private readonly TimeSpan _sessionTimeout;
    private readonly ConcurrentDictionary<string, byte> _activeSessions = new();

    private const string CacheKeyPrefix = "conv_ctx_";

    public InMemoryConversationContextService(
        IMemoryCache cache,
        IOptions<AiOptions> options,
        ILogger<InMemoryConversationContextService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionTimeout = TimeSpan.FromMinutes(options.Value.SessionTimeoutMinutes > 0
            ? options.Value.SessionTimeoutMinutes
            : 30);
    }

    public Task<Result<ConversationContext>> GetOrCreateContextAsync(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Task.FromResult(Result.Failure<ConversationContext>("Session ID cannot be empty"));

        var cacheKey = $"{CacheKeyPrefix}{sessionId}";

        var context = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = _sessionTimeout;
            entry.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _activeSessions.TryRemove(sessionId, out _);
                _logger.LogDebug("Session {SessionId} evicted: {Reason}", sessionId, reason);
            });

            _activeSessions.TryAdd(sessionId, 0);
            _logger.LogDebug("Created new conversation context for session {SessionId}", sessionId);
            return new ConversationContext(sessionId);
        });

        return Task.FromResult(Result.Success<ConversationContext>(context!));
    }

    public async Task<Result> AddMessageAsync(string sessionId, ChatMessage message, CancellationToken ct = default)
    {
        var contextResult = await GetOrCreateContextAsync(sessionId, ct);
        if (contextResult.IsFailure)
            return Result.Failure(contextResult.Error!);

        contextResult.Value!.AddMessage(message);
        _logger.LogDebug("Added {Role} message to session {SessionId}", message.Role, sessionId);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<ChatMessage>>> GetHistoryAsync(string sessionId, CancellationToken ct = default)
    {
        var contextResult = await GetOrCreateContextAsync(sessionId, ct);
        if (contextResult.IsFailure)
            return Result.Failure<IReadOnlyList<ChatMessage>>(contextResult.Error!);

        return Result.Success<IReadOnlyList<ChatMessage>>(contextResult.Value!.Messages);
    }

    public Task<Result> ClearSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{sessionId}";
        _cache.Remove(cacheKey);
        _activeSessions.TryRemove(sessionId, out _);
        _logger.LogInformation("Cleared session {SessionId}", sessionId);
        return Task.FromResult(Result.Success());
    }

    public int GetActiveSessionCount() => _activeSessions.Count;
}

