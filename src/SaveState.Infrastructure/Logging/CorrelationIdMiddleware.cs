using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace SaveState.Infrastructure.Logging;

/// <summary>
/// Middleware for managing correlation IDs across async operations.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private static readonly AsyncLocal<string?> _currentCorrelationId = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    public CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the current correlation ID for this async context.
    /// </summary>
    public static string? CurrentCorrelationId => _currentCorrelationId.Value;

    /// <summary>
    /// Executes an action with a new correlation ID.
    /// </summary>
    public async Task<T> ExecuteWithCorrelationIdAsync<T>(Func<Task<T>> action, string? correlationId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N");
        var previousId = _currentCorrelationId.Value;
        
        try
        {
            _currentCorrelationId.Value = id;
            
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = id,
                ["Operation"] = action.Method.Name
            }))
            using (LogContext.PushProperty("CorrelationId", id))
            {
                _logger.LogDebug("Starting operation with correlation ID {CorrelationId}", id);
                
                var stopwatch = Stopwatch.StartNew();
                var result = await action();
                stopwatch.Stop();
                
                _logger.LogDebug(
                    "Completed operation with correlation ID {CorrelationId} in {ElapsedMs}ms",
                    id,
                    stopwatch.ElapsedMilliseconds);
                
                return result;
            }
        }
        finally
        {
            _currentCorrelationId.Value = previousId;
        }
    }

    /// <summary>
    /// Executes an action with a new correlation ID.
    /// </summary>
    public async Task ExecuteWithCorrelationIdAsync(Func<Task> action, string? correlationId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N");
        var previousId = _currentCorrelationId.Value;
        
        try
        {
            _currentCorrelationId.Value = id;
            
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = id,
                ["Operation"] = action.Method.Name
            }))
            using (LogContext.PushProperty("CorrelationId", id))
            {
                _logger.LogDebug("Starting operation with correlation ID {CorrelationId}", id);
                
                var stopwatch = Stopwatch.StartNew();
                await action();
                stopwatch.Stop();
                
                _logger.LogDebug(
                    "Completed operation with correlation ID {CorrelationId} in {ElapsedMs}ms",
                    id,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        finally
        {
            _currentCorrelationId.Value = previousId;
        }
    }
}

/// <summary>
/// Extension methods for correlation ID logging.
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Adds correlation ID to the log scope using Microsoft.Extensions.Logging.
    /// </summary>
    public static IDisposable BeginCorrelationScope(this ILogger logger, string? correlationId = null)
    {
        var id = correlationId ?? CorrelationIdMiddleware.CurrentCorrelationId ?? Guid.NewGuid().ToString("N");
        
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = id
        });
    }

    /// <summary>
    /// Adds correlation ID and context to the log scope using Microsoft.Extensions.Logging.
    /// </summary>
    public static IDisposable BeginCorrelationScope(
        this ILogger logger, 
        Dictionary<string, object> context,
        string? correlationId = null)
    {
        var id = correlationId ?? CorrelationIdMiddleware.CurrentCorrelationId ?? Guid.NewGuid().ToString("N");
        
        var scopeData = new Dictionary<string, object>(context)
        {
            ["CorrelationId"] = id
        };
        
        return logger.BeginScope(scopeData);
    }

    /// <summary>
    /// Begins a correlation ID scope for Serilog.
    /// </summary>
    public static IDisposable BeginSerilogCorrelationScope(this ILogger logger, string? correlationId = null)
    {
        var id = correlationId ?? CorrelationIdMiddleware.CurrentCorrelationId ?? Guid.NewGuid().ToString("N");
        return LogContext.PushProperty("CorrelationId", id);
    }

    /// <summary>
    /// Begins a game scope for logging.
    /// </summary>
    public static IDisposable BeginGameScope(this ILogger logger, Guid gameId, string? gameName = null)
    {
        var correlationId = CorrelationIdMiddleware.CurrentCorrelationId ?? Guid.NewGuid().ToString("N");
        
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("CorrelationId", correlationId),
            LogContext.PushProperty("GameId", gameId),
            LogContext.PushProperty("GameName", gameName ?? "Unknown")
        };
        
        return new SerilogCompositeDisposable(properties);
    }

    /// <summary>
    /// Begins a user scope for logging.
    /// </summary>
    public static IDisposable BeginUserScope(this ILogger logger, Guid userId, string? userName = null)
    {
        var correlationId = CorrelationIdMiddleware.CurrentCorrelationId ?? Guid.NewGuid().ToString("N");
        
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("CorrelationId", correlationId),
            LogContext.PushProperty("UserId", userId),
            LogContext.PushProperty("UserName", userName ?? "Unknown")
        };
        
        return new SerilogCompositeDisposable(properties);
    }

    /// <summary>
    /// Begins a session scope for logging.
    /// </summary>
    public static IDisposable BeginSessionScope(this ILogger logger, Guid sessionId)
    {
        var correlationId = CorrelationIdMiddleware.CurrentCorrelationId ?? Guid.NewGuid().ToString("N");
        
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("CorrelationId", correlationId),
            LogContext.PushProperty("SessionId", sessionId)
        };
        
        return new SerilogCompositeDisposable(properties);
    }

    /// <summary>
    /// Begins a memory scan scope for logging.
    /// </summary>
    public static IDisposable BeginMemoryScanScope(this ILogger logger, int processId, string gameName)
    {
        var correlationId = CorrelationIdMiddleware.CurrentCorrelationId ?? Guid.NewGuid().ToString("N");
        
        var properties = new List<IDisposable>
        {
            LogContext.PushProperty("CorrelationId", correlationId),
            LogContext.PushProperty("ProcessId", processId),
            LogContext.PushProperty("TargetGame", gameName),
            LogContext.PushProperty("Operation", "MemoryScan")
        };
        
        return new SerilogCompositeDisposable(properties);
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
}

/// <summary>
/// Composite disposable for Serilog properties.
/// </summary>
public class SerilogCompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogCompositeDisposable"/> class.
    /// </summary>
    public SerilogCompositeDisposable(IEnumerable<IDisposable> disposables)
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
