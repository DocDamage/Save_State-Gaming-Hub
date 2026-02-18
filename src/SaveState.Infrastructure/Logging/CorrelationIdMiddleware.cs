using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SaveState.Infrastructure.Logging;

/// <summary>
/// Middleware for managing correlation IDs across async operations.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private static readonly AsyncLocal<string?> _currentCorrelationId = new();

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
    /// Adds correlation ID to the log scope.
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
    /// Adds correlation ID and context to the log scope.
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
}
