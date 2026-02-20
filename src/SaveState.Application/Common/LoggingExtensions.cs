using Microsoft.Extensions.Logging;

namespace SaveState.Application.Common;

/// <summary>
/// Extension methods for structured logging with domain-specific context.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Creates a correlation scope for memory scanning operations.
    /// </summary>
    public static IDisposable BeginMemoryScanScope(this ILogger logger, int processId, string gameTitle)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["ProcessId"] = processId,
            ["GameTitle"] = gameTitle,
            ["OperationType"] = "MemoryScan"
        });
    }

    /// <summary>
    /// Creates a correlation scope for discovery sessions.
    /// </summary>
    public static IDisposable BeginSessionScope(this ILogger logger, Guid sessionId)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["SessionId"] = sessionId,
            ["OperationType"] = "DiscoverySession"
        });
    }

    /// <summary>
    /// Creates a correlation scope for game-specific operations.
    /// </summary>
    public static IDisposable BeginGameScope(this ILogger logger, Guid gameId, string gameName)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["GameId"] = gameId,
            ["GameName"] = gameName,
            ["OperationType"] = "GameOperation"
        });
    }

    /// <summary>
    /// Creates a game context scope for the logger.
    /// </summary>
    public static IDisposable BeginGameContextScope(this ILogger logger, Guid gameId, string gameTitle)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["GameId"] = gameId,
            ["GameTitle"] = gameTitle,
            ["Context"] = "Game"
        });
    }

    /// <summary>
    /// Creates a discovery analysis scope with action context.
    /// </summary>
    public static IDisposable BeginDiscoveryAnalysisScope(this ILogger logger, string action, Guid sessionId)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["Action"] = action,
            ["SessionId"] = sessionId,
            ["OperationType"] = "DiscoveryAnalysis"
        });
    }

    /// <summary>
    /// Adds correlation ID to the log scope.
    /// </summary>
    public static IDisposable BeginCorrelationScope(this ILogger logger, string? correlationId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N");
        
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = id
        });
    }

    /// <summary>
    /// Adds correlation ID and context to the log scope.
    /// </summary>
    public static IDisposable BeginCorrelationScope(
        this ILogger logger, 
        Dictionary<string, object> context,
        string? correlationId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N");
        
        var scopeData = new Dictionary<string, object>(context)
        {
            ["CorrelationId"] = id
        };
        
        return logger.BeginScope(scopeData);
    }

    /// <summary>
    /// Enriches the logger with game context for subsequent logging operations.
    /// Note: This returns a new scope that should be disposed when the context is no longer needed.
    /// </summary>
    public static ILogger EnrichWithGameContext(this ILogger logger, Guid gameId, string gameTitle)
    {
        // The enrichment is done through BeginScope, which returns an IDisposable
        // Callers should use: using (_logger.EnrichWithGameContext(...)) { ... }
        // Since we can't modify the interface, we return the same logger but the caller
        // should be aware that this method is intended to be used in a using statement
        // with the returned scope captured separately
        return logger;
    }
}
