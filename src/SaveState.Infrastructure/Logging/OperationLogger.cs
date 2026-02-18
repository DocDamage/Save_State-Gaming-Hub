using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SaveState.Infrastructure.Logging;

/// <summary>
/// Helper for logging operations with timing and context.
/// </summary>
public class OperationLogger
{
    private readonly ILogger _logger;

    public OperationLogger(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes and logs an operation with timing.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        Dictionary<string, object>? context = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var scopeData = new Dictionary<string, object>
        {
            ["Operation"] = operationName
        };
        
        if (context != null)
        {
            foreach (var kvp in context)
            {
                scopeData[kvp.Key] = kvp.Value;
            }
        }

        using (_logger.BeginScope(scopeData))
        {
            _logger.LogInformation("Starting operation {Operation}", operationName);
            
            try
            {
                var result = await operation();
                stopwatch.Stop();
                
                _logger.LogInformation(
                    "Completed operation {Operation} in {ElapsedMs}ms",
                    operationName,
                    stopwatch.ElapsedMilliseconds);
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(
                    ex,
                    "Operation {Operation} failed after {ElapsedMs}ms",
                    operationName,
                    stopwatch.ElapsedMilliseconds);
                
                throw;
            }
        }
    }

    /// <summary>
    /// Executes and logs an operation with timing.
    /// </summary>
    public async Task ExecuteAsync(
        string operationName,
        Func<Task> operation,
        Dictionary<string, object>? context = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var scopeData = new Dictionary<string, object>
        {
            ["Operation"] = operationName
        };
        
        if (context != null)
        {
            foreach (var kvp in context)
            {
                scopeData[kvp.Key] = kvp.Value;
            }
        }

        using (_logger.BeginScope(scopeData))
        {
            _logger.LogInformation("Starting operation {Operation}", operationName);
            
            try
            {
                await operation();
                stopwatch.Stop();
                
                _logger.LogInformation(
                    "Completed operation {Operation} in {ElapsedMs}ms",
                    operationName,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(
                    ex,
                    "Operation {Operation} failed after {ElapsedMs}ms",
                    operationName,
                    stopwatch.ElapsedMilliseconds);
                
                throw;
            }
        }
    }
}

/// <summary>
/// Extension methods for operation logging.
/// </summary>
public static class OperationLoggerExtensions
{
    /// <summary>
    /// Creates an operation logger from the logger.
    /// </summary>
    public static OperationLogger ToOperationLogger(this ILogger logger)
    {
        return new OperationLogger(logger);
    }
}
