using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Monitoring;

namespace SaveState.Infrastructure.Monitoring;

/// <summary>
/// Service for tracking and monitoring application errors and exceptions.
/// Provides error rate analysis, pattern detection, and alerting capabilities.
/// </summary>
public class ErrorTrackingService : IDisposable
{
    private readonly IApplicationMetrics _metrics;
    private readonly ILogger<ErrorTrackingService> _logger;
    private readonly Timer _errorAnalysisTimer;
    private bool _disposed;

    // Error tracking data
    private readonly ConcurrentDictionary<string, ErrorStats> _errorStatistics = new();
    private readonly CircularBuffer<ErrorEvent> _recentErrors;

    public ErrorTrackingService(
        IApplicationMetrics metrics,
        ILogger<ErrorTrackingService> logger)
    {
        _metrics = metrics;
        _logger = logger;
        _recentErrors = new CircularBuffer<ErrorEvent>(1000); // Keep last 1000 errors

        // Analyze error patterns every 10 minutes
        _errorAnalysisTimer = new Timer(AnalyzeErrorPatterns, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));

        _logger.LogInformation("Error tracking service initialized");
    }

    /// <summary>
    /// Records an exception with context information.
    /// </summary>
    public void RecordException(string source, string exceptionType, string message, Exception? exception = null)
    {
        _metrics.RecordException(source, exceptionType, message);

        var errorEvent = new ErrorEvent
        {
            Timestamp = DateTime.UtcNow,
            Source = source,
            ExceptionType = exceptionType,
            Message = message,
            Exception = exception
        };

        _recentErrors.Add(errorEvent);
        UpdateErrorStats(errorEvent);

        // Log based on exception type
        if (exception is OperationCanceledException)
        {
            _logger.LogDebug(exception, "Operation cancelled: {Source}", source);
        }
        else if (exception is TimeoutException)
        {
            _logger.LogWarning(exception, "Timeout occurred: {Source}", source);
        }
        else if (exception is InvalidOperationException)
        {
            _logger.LogWarning(exception, "Invalid operation: {Source} - {Message}", source, message);
        }
        else
        {
            _logger.LogError(exception, "Exception occurred: {Source} - {ExceptionType}", source, exceptionType);
        }
    }

    /// <summary>
    /// Records an unhandled exception (critical).
    /// </summary>
    public void RecordUnhandledException(string source, Exception exception)
    {
        _metrics.RecordUnhandledException(source, exception);

        var errorEvent = new ErrorEvent
        {
            Timestamp = DateTime.UtcNow,
            Source = source,
            ExceptionType = exception.GetType().Name,
            Message = exception.Message,
            Exception = exception,
            IsUnhandled = true
        };

        _recentErrors.Add(errorEvent);
        UpdateErrorStats(errorEvent);

        _logger.LogCritical(exception, "Unhandled exception occurred: {Source}", source);
    }

    private void UpdateErrorStats(ErrorEvent errorEvent)
    {
        var key = $"{errorEvent.Source}:{errorEvent.ExceptionType}";
        var stats = _errorStatistics.GetOrAdd(key, _ => new ErrorStats());

        // Use thread-safe operations for updating stats
        lock (stats)
        {
            stats.Count++;
            stats.LastOccurrence = errorEvent.Timestamp;

            if (errorEvent.IsUnhandled)
            {
                stats.UnhandledCount++;
            }
        }

        // Keep only recent statistics (last hour)
        if (DateTime.UtcNow - stats.FirstOccurrence > TimeSpan.FromHours(1))
        {
            stats.Reset();
            stats.FirstOccurrence = errorEvent.Timestamp;
        }
    }

    private void AnalyzeErrorPatterns(object? state)
    {
        try
        {
            if (_disposed)
                return;

            var now = DateTime.UtcNow;
            var recentErrors = _recentErrors.ToList();

            // Analyze error rates over different time windows
            var last5Minutes = recentErrors.Where(e => now - e.Timestamp <= TimeSpan.FromMinutes(5)).ToList();
            var last15Minutes = recentErrors.Where(e => now - e.Timestamp <= TimeSpan.FromMinutes(15)).ToList();
            var lastHour = recentErrors.Where(e => now - e.Timestamp <= TimeSpan.FromHours(1)).ToList();

            // Check for error rate spikes
            CheckErrorRateSpike("5 minutes", last5Minutes, 10); // More than 10 errors in 5 minutes
            CheckErrorRateSpike("15 minutes", last15Minutes, 25); // More than 25 errors in 15 minutes
            CheckErrorRateSpike("1 hour", lastHour, 50); // More than 50 errors in 1 hour

            // Check for repeated error patterns
            CheckRepeatedErrors(lastHour);

            // Check for unhandled exceptions
            var unhandledErrors = recentErrors.Where(e => e.IsUnhandled).ToList();
            if (unhandledErrors.Count > 0)
            {
                _logger.LogWarning(
                    "Detected {Count} unhandled exceptions in the last hour: {Sources}",
                    unhandledErrors.Count,
                    string.Join(", ", unhandledErrors.Select(e => e.Source).Distinct()));
            }

            _logger.LogDebug("Error pattern analysis completed: {TotalErrors} total, {RecentErrors} in last hour",
                recentErrors.Count, lastHour.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze error patterns");
        }
    }

    private void CheckErrorRateSpike(string timeWindow, List<ErrorEvent> errors, int threshold)
    {
        if (errors.Count > threshold)
        {
            var errorGroups = errors.GroupBy(e => e.ExceptionType)
                .OrderByDescending(g => g.Count())
                .Take(3);

            _logger.LogWarning(
                "High error rate detected in last {TimeWindow}: {TotalErrors} errors (threshold: {Threshold}). " +
                "Top error types: {ErrorTypes}",
                timeWindow,
                errors.Count,
                threshold,
                string.Join(", ", errorGroups.Select(g => $"{g.Key}({g.Count()})")));
        }
    }

    private void CheckRepeatedErrors(List<ErrorEvent> errors)
    {
        var errorGroups = errors.GroupBy(e => $"{e.Source}:{e.ExceptionType}")
            .Where(g => g.Count() >= 5) // Same error 5+ times
            .OrderByDescending(g => g.Count())
            .Take(3);

        foreach (var group in errorGroups)
        {
            var firstError = group.First();
            _logger.LogWarning(
                "Repeated error pattern detected: {Source}:{ExceptionType} occurred {Count} times. " +
                "First occurrence: {FirstTime}, Message: {Message}",
                firstError.Source,
                firstError.ExceptionType,
                group.Count(),
                firstError.Timestamp,
                firstError.Message);
        }
    }

    /// <summary>
    /// Gets error statistics for monitoring and alerting.
    /// </summary>
    public Result<IReadOnlyDictionary<string, ErrorStats>> GetErrorStatistics()
    {
        try
        {
            return Result.Success<IReadOnlyDictionary<string, ErrorStats>>(new Dictionary<string, ErrorStats>(_errorStatistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve error statistics");
            return Result.Failure<IReadOnlyDictionary<string, ErrorStats>>(
                $"Failed to retrieve error statistics: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets recent errors for analysis.
    /// </summary>
    public Result<IReadOnlyList<ErrorEvent>> GetRecentErrors(int count = 100)
    {
        try
        {
            return Result.Success<IReadOnlyList<ErrorEvent>>(_recentErrors.ToList().TakeLast(count).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent errors");
            return Result.Failure<IReadOnlyList<ErrorEvent>>(
                $"Failed to retrieve recent errors: {ex.Message}",
                ErrorType.Internal);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _errorAnalysisTimer?.Dispose();

        _logger.LogInformation("Error tracking service disposed");
    }
}

/// <summary>
/// Statistics for a specific error type.
/// </summary>
public class ErrorStats
{
    public long Count { get; set; }
    public long UnhandledCount { get; set; }
    public DateTime FirstOccurrence { get; set; } = DateTime.UtcNow;
    public DateTime LastOccurrence { get; set; } = DateTime.UtcNow;

    public double ErrorRatePerHour => Count > 0 ?
        Count / Math.Max(1, (DateTime.UtcNow - FirstOccurrence).TotalHours) : 0;

    public void Reset()
    {
        Count = 0;
        UnhandledCount = 0;
        FirstOccurrence = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a single error event.
/// </summary>
public class ErrorEvent
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public bool IsUnhandled { get; set; }
}

/// <summary>
/// Simple circular buffer implementation.
/// </summary>
internal class CircularBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;
    private int _count;

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    public int Count => _count;
    public int Capacity => _buffer.Length;

    public void Add(T item)
    {
        _buffer[_tail] = item;
        _tail = (_tail + 1) % Capacity;

        if (_count < Capacity)
        {
            _count++;
        }
        else
        {
            _head = (_head + 1) % Capacity;
        }
    }

    public List<T> ToList()
    {
        var result = new List<T>(_count);
        var index = _head;

        for (int i = 0; i < _count; i++)
        {
            result.Add(_buffer[index]);
            index = (index + 1) % Capacity;
        }

        return result;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ToList().GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Extension methods for error tracking.
/// </summary>
public static class ErrorTrackingExtensions
{
    public static void TrackError(
        this ErrorTrackingService tracker,
        string source,
        Exception exception)
    {
        tracker.RecordException(source, exception.GetType().Name, exception.Message, exception);
    }

    public static void TrackUnhandledError(
        this ErrorTrackingService tracker,
        string source,
        Exception exception)
    {
        tracker.RecordUnhandledException(source, exception);
    }
}
