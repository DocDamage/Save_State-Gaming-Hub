using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Monitoring;
using SaveState.Core.Common.Services;
using SaveState.Core.Performance.Services;

namespace SaveState.Infrastructure.Monitoring;

/// <summary>
/// Implementation of IApplicationMetrics using .NET's built-in metrics system.
/// Provides comprehensive application performance monitoring.
/// </summary>
public class ApplicationMetricsService : IApplicationMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly ILogger<ApplicationMetricsService> _logger;

    // Performance Counters
    private readonly Counter<long> _responseTimeCounter;
    private readonly Counter<long> _throughputCounter;
    private readonly ObservableGauge<long> _memoryUsageGauge;
    private readonly ObservableGauge<double> _cpuUsageGauge;

    // Database Metrics
    private readonly Histogram<double> _databaseQueryDuration;
    private readonly ObservableGauge<int> _databaseConnectionGauge;
    private readonly Counter<long> _databaseErrorCounter;

    // Cache Metrics
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    private readonly Counter<long> _cacheEvictionCounter;

    // API Client Metrics
    private readonly Histogram<double> _apiCallDuration;
    private readonly Counter<long> _apiSuccessCounter;
    private readonly Counter<long> _apiErrorCounter;
    private readonly Counter<long> _apiRateLimitCounter;

    // AI Service Metrics
    private readonly Histogram<double> _aiRequestDuration;
    private readonly Counter<long> _aiSuccessCounter;
    private readonly Counter<long> _aiErrorCounter;
    private readonly Counter<long> _aiTokenUsageCounter;

    // Error Tracking
    private readonly Counter<long> _exceptionCounter;
    private readonly Counter<long> _unhandledExceptionCounter;

    // Custom Metrics
    private readonly Counter<long> _customCounter;
    private readonly ObservableGauge<double> _customGauge;

    // In-memory storage for snapshot calculations
    private readonly MetricsStorage _storage;

    public ApplicationMetricsService(ILogger<ApplicationMetricsService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _meter = new Meter("SaveState.Application", "1.0.0");
        _storage = new MetricsStorage(timeProvider);

        // Initialize performance counters
        _responseTimeCounter = _meter.CreateCounter<long>("response_time_ms", "ms", "Response time in milliseconds");
        _throughputCounter = _meter.CreateCounter<long>("throughput", "requests", "Number of requests processed");
        _memoryUsageGauge = _meter.CreateObservableGauge<long>("memory_usage_bytes", () => GetCurrentMemoryUsage(), "bytes", "Current memory usage");
        _cpuUsageGauge = _meter.CreateObservableGauge<double>("cpu_usage_percent", () => GetCurrentCpuUsage(), "%", "Current CPU usage percentage");

        // Initialize database metrics
        _databaseQueryDuration = _meter.CreateHistogram<double>("database_query_duration_ms", "ms", "Database query duration");
        _databaseConnectionGauge = _meter.CreateObservableGauge<int>("database_connections", () => GetCurrentDatabaseConnections(), "connections", "Current database connections");
        _databaseErrorCounter = _meter.CreateCounter<long>("database_errors", "errors", "Number of database errors");

        // Initialize cache metrics
        _cacheHitCounter = _meter.CreateCounter<long>("cache_hits", "hits", "Number of cache hits");
        _cacheMissCounter = _meter.CreateCounter<long>("cache_misses", "misses", "Number of cache misses");
        _cacheEvictionCounter = _meter.CreateCounter<long>("cache_evictions", "evictions", "Number of cache evictions");

        // Initialize API metrics
        _apiCallDuration = _meter.CreateHistogram<double>("api_call_duration_ms", "ms", "API call duration");
        _apiSuccessCounter = _meter.CreateCounter<long>("api_calls_success", "calls", "Number of successful API calls");
        _apiErrorCounter = _meter.CreateCounter<long>("api_calls_error", "calls", "Number of failed API calls");
        _apiRateLimitCounter = _meter.CreateCounter<long>("api_rate_limits", "limits", "Number of API rate limit hits");

        // Initialize AI metrics
        _aiRequestDuration = _meter.CreateHistogram<double>("ai_request_duration_ms", "ms", "AI request duration");
        _aiSuccessCounter = _meter.CreateCounter<long>("ai_requests_success", "requests", "Number of successful AI requests");
        _aiErrorCounter = _meter.CreateCounter<long>("ai_requests_error", "requests", "Number of failed AI requests");
        _aiTokenUsageCounter = _meter.CreateCounter<long>("ai_tokens_used", "tokens", "Number of AI tokens used");

        // Initialize error tracking
        _exceptionCounter = _meter.CreateCounter<long>("exceptions", "exceptions", "Number of exceptions");
        _unhandledExceptionCounter = _meter.CreateCounter<long>("unhandled_exceptions", "exceptions", "Number of unhandled exceptions");

        // Initialize custom metrics
        _customCounter = _meter.CreateCounter<long>("custom_counter", "count", "Custom counter metric");
        _customGauge = _meter.CreateObservableGauge<double>("custom_gauge", () => GetCustomMetrics(), "value", "Custom gauge metric");

        _logger.LogInformation("Application metrics service initialized");
    }

    // Performance Counters Implementation
    public void RecordResponseTime(string operation, TimeSpan duration)
    {
        var durationMs = duration.TotalMilliseconds;
        _responseTimeCounter.Add((long)durationMs, new KeyValuePair<string, object?>("operation", operation));
        _storage.RecordResponseTime(operation, duration);
        // Each response time recording counts as one request
        RecordThroughput(operation, 1);
    }

    public void RecordThroughput(string operation, int count = 1)
    {
        _throughputCounter.Add(count, new KeyValuePair<string, object?>("operation", operation));
        _storage.RecordThroughput(operation, count);
    }

    public void RecordMemoryUsage(long bytes)
    {
        _storage.RecordMemoryUsage(bytes);
    }

    public void RecordCpuUsage(double percentage)
    {
        _storage.RecordCpuUsage(percentage);
    }

    // Database Metrics Implementation
    public void RecordDatabaseQuery(string operation, TimeSpan duration)
    {
        var durationMs = duration.TotalMilliseconds;
        _databaseQueryDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
        _storage.RecordDatabaseQuery(operation, duration);
    }

    public void RecordDatabaseConnectionCount(int count)
    {
        _storage.RecordDatabaseConnections(count);
    }

    public void RecordDatabaseError(string operation, string errorType)
    {
        _databaseErrorCounter.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("error_type", errorType));
        _storage.RecordDatabaseError(operation, errorType);
    }

    // PERFORMANCE OPTIMIZATION: Advanced performance monitoring
    public void RecordSlowQuery(string operation, TimeSpan duration, int recordCount)
    {
        // Record as a performance warning with additional context
        _logger.LogWarning("Slow query detected: {Operation} took {Duration}ms for {RecordCount} records",
            operation, duration.TotalMilliseconds, recordCount);

        // Could add a specific metric for slow queries if needed
        _customCounter.Add(1,
            new KeyValuePair<string, object?>("metric", "slow_query"),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("duration_ms", duration.TotalMilliseconds),
            new KeyValuePair<string, object?>("record_count", recordCount));
    }

    public void RecordBatchOperation(string operation, int itemCount, TimeSpan duration)
    {
        _logger.LogInformation("Batch operation completed: {Operation} processed {ItemCount} items in {Duration}ms",
            operation, itemCount, duration.TotalMilliseconds);

        _customCounter.Add(1,
            new KeyValuePair<string, object?>("metric", "batch_operation"),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("item_count", itemCount),
            new KeyValuePair<string, object?>("duration_ms", duration.TotalMilliseconds));
    }

    public void RecordPerformanceWarning(string operation, string message)
    {
        _logger.LogWarning("Performance warning in {Operation}: {Message}", operation, message);

        _customCounter.Add(1,
            new KeyValuePair<string, object?>("metric", "performance_warning"),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("message", message));
    }

    // Cache Metrics Implementation
    public void RecordCacheHit(string cacheName)
    {
        _cacheHitCounter.Add(1, new KeyValuePair<string, object?>("cache", cacheName));
        _storage.RecordCacheHit(cacheName);
    }

    public void RecordCacheMiss(string cacheName)
    {
        _cacheMissCounter.Add(1, new KeyValuePair<string, object?>("cache", cacheName));
        _storage.RecordCacheMiss(cacheName);
    }

    public void RecordCacheEviction(string cacheName)
    {
        _cacheEvictionCounter.Add(1, new KeyValuePair<string, object?>("cache", cacheName));
        _storage.RecordCacheEviction(cacheName);
    }

    // API Client Metrics Implementation
    public void RecordApiCall(string service, string endpoint, TimeSpan duration, bool success)
    {
        var durationMs = duration.TotalMilliseconds;
        var tags = new TagList
        {
            { "service", service },
            { "endpoint", endpoint }
        };

        _apiCallDuration.Record(durationMs, tags);

        if (success)
        {
            _apiSuccessCounter.Add(1, tags);
        }
        else
        {
            _apiErrorCounter.Add(1, tags);
        }

        _storage.RecordApiCall(service, endpoint, duration, success);
    }

    public void RecordApiRateLimit(string service, TimeSpan retryAfter)
    {
        var retryAfterMs = retryAfter.TotalMilliseconds;
        _apiRateLimitCounter.Add(1, new KeyValuePair<string, object?>("service", service), new KeyValuePair<string, object?>("retry_after_ms", retryAfterMs));
        _storage.RecordApiRateLimit(service, retryAfter);
    }

    public void RecordApiError(string service, string errorType)
    {
        _apiErrorCounter.Add(1, new KeyValuePair<string, object?>("service", service), new KeyValuePair<string, object?>("error_type", errorType));
        _storage.RecordApiError(service, errorType);
    }

    // AI Service Metrics Implementation
    public void RecordAiRequest(string provider, string operation, TimeSpan duration, bool success)
    {
        var durationMs = duration.TotalMilliseconds;
        var tags = new TagList
        {
            { "provider", provider },
            { "operation", operation }
        };

        _aiRequestDuration.Record(durationMs, tags);

        if (success)
        {
            _aiSuccessCounter.Add(1, tags);
        }
        else
        {
            _aiErrorCounter.Add(1, tags);
        }

        _storage.RecordAiRequest(provider, operation, duration, success);
    }

    public void RecordAiTokenUsage(string provider, int inputTokens, int outputTokens)
    {
        var totalTokens = inputTokens + outputTokens;
        _aiTokenUsageCounter.Add(totalTokens, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("direction", "input"));
        _aiTokenUsageCounter.Add(outputTokens, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("direction", "output"));
        _storage.RecordAiTokenUsage(provider, inputTokens, outputTokens);
    }

    // Error Tracking Implementation
    public void RecordException(string source, string exceptionType, string message)
    {
        _exceptionCounter.Add(1, new KeyValuePair<string, object?>("source", source), new KeyValuePair<string, object?>("type", exceptionType));
        _storage.RecordException(source, exceptionType, message);
    }

    public void RecordUnhandledException(string source, Exception exception)
    {
        _unhandledExceptionCounter.Add(1, new KeyValuePair<string, object?>("source", source), new KeyValuePair<string, object?>("type", exception.GetType().Name));
        _storage.RecordUnhandledException(source, exception);
        _logger.LogError(exception, "Unhandled exception recorded in metrics: {Source}", source);
    }

    // Custom Metrics Implementation
    public void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null)
    {
        _storage.RecordCustomMetric(name, value, tags);
    }

    public void IncrementCounter(string name, Dictionary<string, string>? tags = null)
    {
        _customCounter.Add(1, tags?.Select(t => new KeyValuePair<string, object?>(t.Key, t.Value)).ToArray() ?? Array.Empty<KeyValuePair<string, object?>>());
        _storage.IncrementCounter(name, tags);
    }

    // Performance Monitoring Implementation
    public void RecordPerformanceSnapshot(PerformanceSnapshot snapshot)
    {
        // Record individual metrics from the snapshot
        RecordMemoryUsage(snapshot.RamUsageMb * 1024 * 1024); // Convert MB to bytes
        RecordCpuUsage(snapshot.CpuUsagePercent);

        // Record frame time if available
        if (snapshot.Fps > 0)
        {
            var frameTimeMs = 1000.0 / snapshot.Fps;
            RecordCustomMetric("performance.fps", snapshot.Fps);
            RecordCustomMetric("performance.frame_time_ms", frameTimeMs);
        }

        // Record GPU usage if available
        if (snapshot.GpuUsagePercent.HasValue)
        {
            RecordCustomMetric("performance.gpu_usage_percent", snapshot.GpuUsagePercent.Value);
        }
    }

    // Metrics Snapshot Implementation
    public async Task<Result<MetricsSnapshot>> GetMetricsSnapshotAsync(CancellationToken ct = default)
    {
        try
        {
            var snapshot = await Task.Run(() => _storage.GetSnapshot(), ct);
            return Result.Success(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics snapshot");
            return Result.Failure<MetricsSnapshot>(
                $"Failed to get metrics snapshot: {ex.Message}",
                ErrorType.Internal);
        }
    }

    // Helper methods for observable gauges
    private IEnumerable<Measurement<long>> GetCurrentMemoryUsage()
    {
        var memoryUsage = _storage.GetCurrentMemoryUsage();
        return new[] { new Measurement<long>(memoryUsage, new KeyValuePair<string, object?>("process", "savestate")) };
    }

    private IEnumerable<Measurement<double>> GetCurrentCpuUsage()
    {
        var cpuUsage = _storage.GetCurrentCpuUsage();
        return new[] { new Measurement<double>(cpuUsage, new KeyValuePair<string, object?>("process", "savestate")) };
    }

    private IEnumerable<Measurement<double>> GetCustomMetrics()
    {
        return _storage.GetCustomMetrics().Select(kvp =>
            new Measurement<double>(kvp.Value, new KeyValuePair<string, object?>("name", kvp.Key)));
    }

    private int GetCurrentDatabaseConnections()
    {
        return _storage.GetCurrentDatabaseConnections();
    }

    public void Dispose()
    {
        _meter.Dispose();
        _logger.LogInformation("Application metrics service disposed");
    }
}
