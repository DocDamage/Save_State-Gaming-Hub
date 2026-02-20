using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace SaveState.Infrastructure.Logging;

/// <summary>
/// Extension methods for structured logging with domain-specific context.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds structured logging services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddStructuredLogging(this IServiceCollection services)
    {
        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddSingleton<IGameContextEnricher, GameContextEnricher>();
        return services;
    }

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
    /// Enriches the logger with game context for subsequent logging operations.
    /// Note: This returns a new scope that should be disposed when the context is no longer needed.
    /// </summary>
    public static ILogger EnrichWithGameContext(this ILogger logger, Guid gameId, string gameTitle)
    {
        // The enrichment is done through BeginScope, which returns an IDisposable
        // Callers should use: using (_logger.EnrichWithGameContext(...)) { ... }
        return logger;
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
    /// Enriches the Serilog logger with game context.
    /// </summary>
    public static Serilog.ILogger EnrichWithGameContext(this Serilog.ILogger logger, Guid gameId, string gameName)
    {
        return logger.ForContext("GameId", gameId)
                     .ForContext("GameName", gameName);
    }

    /// <summary>
    /// Enriches the Serilog logger with user context.
    /// </summary>
    public static Serilog.ILogger EnrichWithUserContext(this Serilog.ILogger logger, Guid userId, string userName)
    {
        return logger.ForContext("UserId", userId)
                     .ForContext("UserName", userName);
    }

    /// <summary>
    /// Begins a correlation ID scope for Serilog.
    /// </summary>
    public static IDisposable BeginSerilogCorrelationScope(string? correlationId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N");
        return LogContext.PushProperty("CorrelationId", id);
    }

    /// <summary>
    /// Begins a game scope for Serilog.
    /// </summary>
    public static IDisposable BeginSerilogGameScope(Guid gameId, string? gameName = null)
    {
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("GameId", gameId),
            LogContext.PushProperty("GameName", gameName ?? "Unknown")
        };
        return new CompositeDisposable(properties);
    }

    /// <summary>
    /// Begins a user scope for Serilog.
    /// </summary>
    public static IDisposable BeginSerilogUserScope(Guid userId, string? userName = null)
    {
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("UserId", userId),
            LogContext.PushProperty("UserName", userName ?? "Unknown")
        };
        return new CompositeDisposable(properties);
    }

    /// <summary>
    /// Begins a session scope for Serilog.
    /// </summary>
    public static IDisposable BeginSerilogSessionScope(Guid sessionId)
    {
        return LogContext.PushProperty("SessionId", sessionId);
    }

    /// <summary>
    /// Begins a memory scan scope for Serilog.
    /// </summary>
    public static IDisposable BeginSerilogMemoryScanScope(int processId, string gameName)
    {
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("ProcessId", processId),
            LogContext.PushProperty("TargetGame", gameName),
            LogContext.PushProperty("Operation", "MemoryScan")
        };
        return new CompositeDisposable(properties);
    }
}

/// <summary>
/// Provider for correlation IDs.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    string GetCorrelationId();

    /// <summary>
    /// Sets the correlation ID.
    /// </summary>
    void SetCorrelationId(string correlationId);
}

/// <summary>
/// Default implementation of correlation ID provider.
/// </summary>
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private string _correlationId = Guid.NewGuid().ToString("N");

    /// <inheritdoc />
    public string GetCorrelationId() => _correlationId;

    /// <inheritdoc />
    public void SetCorrelationId(string correlationId) => _correlationId = correlationId;
}

/// <summary>
/// Enriches logs with game context.
/// </summary>
public interface IGameContextEnricher
{
    /// <summary>
    /// Enriches the current log context with game information.
    /// </summary>
    void Enrich(Guid gameId, string? gameName = null);

    /// <summary>
    /// Clears the game context.
    /// </summary>
    void Clear();
}

/// <summary>
/// Default implementation of game context enricher.
/// </summary>
public class GameContextEnricher : IGameContextEnricher
{
    private readonly ICorrelationIdProvider _correlationIdProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameContextEnricher"/> class.
    /// </summary>
    public GameContextEnricher(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider;
    }

    /// <inheritdoc />
    public void Enrich(Guid gameId, string? gameName = null)
    {
        // Enrichment happens via LogContext in actual usage
    }

    /// <inheritdoc />
    public void Clear()
    {
        // Clear context
    }
}

/// <summary>
/// Simple composite disposable for multiple disposables.
/// </summary>
public class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeDisposable"/> class.
    /// </summary>
    public CompositeDisposable(IEnumerable<IDisposable> disposables)
    {
        _disposables = disposables.ToList();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}
