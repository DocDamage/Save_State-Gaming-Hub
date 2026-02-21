using System.Collections.Concurrent;
using SaveState.Core.Common.Services;
using SaveState.Core.Monitoring;

namespace SaveState.Infrastructure.Monitoring;

/// <summary>
/// Thread-safe storage for metrics data used to calculate snapshots.
/// Maintains rolling buffers and counters for performance metrics.
/// </summary>
internal class MetricsStorage
{
    private readonly ITimeProvider _timeProvider;

    // Thread-safe collections for concurrent access
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TimeSpan>> _responseTimes = new();
    private readonly ConcurrentDictionary<string, long> _throughputCounters = new();
    private readonly ConcurrentQueue<long> _memoryUsageBuffer = new();
    private readonly ConcurrentQueue<double> _cpuUsageBuffer = new();

    // Database metrics
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TimeSpan>> _databaseQueryTimes = new();
    private readonly ConcurrentDictionary<string, long> _databaseErrorCounters = new();
    private long _currentDatabaseConnections;

    // Cache metrics
    private readonly ConcurrentDictionary<string, long> _cacheHitCounters = new();
    private readonly ConcurrentDictionary<string, long> _cacheMissCounters = new();
    private readonly ConcurrentDictionary<string, long> _cacheEvictionCounters = new();

    // API metrics
    private readonly ConcurrentDictionary<string, ConcurrentQueue<(string Endpoint, TimeSpan Duration, bool Success)>> _apiCalls = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TimeSpan>> _apiRateLimits = new();
    private readonly ConcurrentDictionary<string, long> _apiErrorCounters = new();

    // AI metrics
    private readonly ConcurrentDictionary<string, ConcurrentQueue<(string Operation, TimeSpan Duration, bool Success)>> _aiRequests = new();
    private readonly ConcurrentDictionary<string, (long InputTokens, long OutputTokens)> _aiTokenUsage = new();

    // Error tracking
    private readonly ConcurrentDictionary<string, long> _exceptionCounters = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Exception>> _unhandledExceptions = new();

    // Custom metrics
    private readonly ConcurrentDictionary<string, double> _customMetrics = new();
    private readonly ConcurrentDictionary<string, long> _customCounters = new();

    // Rolling buffer size to prevent unbounded memory growth
    private const int MaxBufferSize = 1000;

    public MetricsStorage(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    // Performance Counters Implementation
    public void RecordResponseTime(string operation, TimeSpan duration)
    {
        var queue = _responseTimes.GetOrAdd(operation, _ => new ConcurrentQueue<TimeSpan>());
        queue.Enqueue(duration);

        // Maintain rolling buffer
        if (queue.Count > MaxBufferSize)
        {
            queue.TryDequeue(out _);
        }
    }

    public void RecordThroughput(string operation, int count)
    {
        _throughputCounters.AddOrUpdate(operation, count, (_, existing) => existing + count);
    }

    public void RecordMemoryUsage(long bytes)
    {
        _memoryUsageBuffer.Enqueue(bytes);

        // Maintain rolling buffer
        if (_memoryUsageBuffer.Count > MaxBufferSize)
        {
            _memoryUsageBuffer.TryDequeue(out _);
        }
    }

    public void RecordCpuUsage(double percentage)
    {
        _cpuUsageBuffer.Enqueue(percentage);

        // Maintain rolling buffer
        if (_cpuUsageBuffer.Count > MaxBufferSize)
        {
            _cpuUsageBuffer.TryDequeue(out _);
        }
    }

    // Database Metrics Implementation
    public void RecordDatabaseQuery(string operation, TimeSpan duration)
    {
        var queue = _databaseQueryTimes.GetOrAdd(operation, _ => new ConcurrentQueue<TimeSpan>());
        queue.Enqueue(duration);

        // Maintain rolling buffer
        if (queue.Count > MaxBufferSize)
        {
            queue.TryDequeue(out _);
        }
    }

    public void RecordDatabaseConnections(int count)
    {
        Interlocked.Exchange(ref _currentDatabaseConnections, count);
    }

    public void RecordDatabaseError(string operation, string errorType)
    {
        var key = $"{operation}:{errorType}";
        _databaseErrorCounters.AddOrUpdate(key, 1, (_, existing) => existing + 1);
    }

    // Cache Metrics Implementation
    public void RecordCacheHit(string cacheName)
    {
        _cacheHitCounters.AddOrUpdate(cacheName, 1, (_, existing) => existing + 1);
    }

    public void RecordCacheMiss(string cacheName)
    {
        _cacheMissCounters.AddOrUpdate(cacheName, 1, (_, existing) => existing + 1);
    }

    public void RecordCacheEviction(string cacheName)
    {
        _cacheEvictionCounters.AddOrUpdate(cacheName, 1, (_, existing) => existing + 1);
    }

    // API Metrics Implementation
    public void RecordApiCall(string service, string endpoint, TimeSpan duration, bool success)
    {
        var queue = _apiCalls.GetOrAdd(service, _ => new ConcurrentQueue<(string, TimeSpan, bool)>());
        queue.Enqueue((endpoint, duration, success));

        // Maintain rolling buffer
        if (queue.Count > MaxBufferSize)
        {
            queue.TryDequeue(out _);
        }
    }

    public void RecordApiRateLimit(string service, TimeSpan retryAfter)
    {
        var queue = _apiRateLimits.GetOrAdd(service, _ => new ConcurrentQueue<TimeSpan>());
        queue.Enqueue(retryAfter);

        // Maintain rolling buffer
        if (queue.Count > MaxBufferSize)
        {
            queue.TryDequeue(out _);
        }
    }

    public void RecordApiError(string service, string errorType)
    {
        var key = $"{service}:{errorType}";
        _apiErrorCounters.AddOrUpdate(key, 1, (_, existing) => existing + 1);
    }

    // AI Metrics Implementation
    public void RecordAiRequest(string provider, string operation, TimeSpan duration, bool success)
    {
        var queue = _aiRequests.GetOrAdd(provider, _ => new ConcurrentQueue<(string, TimeSpan, bool)>());
        queue.Enqueue((operation, duration, success));

        // Maintain rolling buffer
        if (queue.Count > MaxBufferSize)
        {
            queue.TryDequeue(out _);
        }
    }

    public void RecordAiTokenUsage(string provider, int inputTokens, int outputTokens)
    {
        _aiTokenUsage.AddOrUpdate(provider,
            (inputTokens, outputTokens),
            (_, existing) => (existing.InputTokens + inputTokens, existing.OutputTokens + outputTokens));
    }

    // Error Tracking Implementation
    public void RecordException(string source, string exceptionType, string message)
    {
        var key = $"{source}:{exceptionType}";
        _exceptionCounters.AddOrUpdate(key, 1, (_, existing) => existing + 1);
    }

    public void RecordUnhandledException(string source, Exception exception)
    {
        var queue = _unhandledExceptions.GetOrAdd(source, _ => new ConcurrentQueue<Exception>());
        queue.Enqueue(exception);

        // Maintain rolling buffer
        if (queue.Count > MaxBufferSize)
        {
            queue.TryDequeue(out _);
        }
    }

    // Custom Metrics Implementation
    public void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = tags != null ? $"{name}:{string.Join(",", tags.OrderBy(t => t.Key).Select(t => $"{t.Key}={t.Value}"))}" : name;
        _customMetrics[key] = value;
    }

    public void IncrementCounter(string name, Dictionary<string, string>? tags = null)
    {
        var key = tags != null ? $"{name}:{string.Join(",", tags.OrderBy(t => t.Key).Select(t => $"{t.Key}={t.Value}"))}" : name;
        _customCounters.AddOrUpdate(key, 1, (_, existing) => existing + 1);
    }

    // Snapshot calculation methods
    public MetricsSnapshot GetSnapshot()
    {
        var snapshot = new MetricsSnapshot
        {
            Timestamp = _timeProvider.UtcNow,

            // Performance metrics
            AverageResponseTime = CalculateAverageResponseTime(),
            TotalRequests = (int)_throughputCounters.Values.Sum(),
            CurrentMemoryUsage = GetCurrentMemoryUsage(),
            CurrentCpuUsage = GetCurrentCpuUsage(),

            // Database metrics
            AverageDatabaseQueryTime = CalculateAverageDatabaseQueryTime(),
            TotalDatabaseQueries = (int)_databaseQueryTimes.Values.Sum(q => q.Count),
            CurrentDatabaseConnections = (int)_currentDatabaseConnections,
            DatabaseErrors = (int)_databaseErrorCounters.Values.Sum(),

            // Cache metrics
            CacheHitRatio = CalculateCacheHitRatio(),
            TotalCacheRequests = CalculateTotalCacheRequests(),
            CacheEvictions = (int)_cacheEvictionCounters.Values.Sum(),

            // API metrics
            TotalApiCalls = (int)_apiCalls.Values.Sum(q => q.Count),
            SuccessfulApiCalls = (int)_apiCalls.Values.Sum(q => q.Count(item => item.Success)),
            FailedApiCalls = (int)_apiCalls.Values.Sum(q => q.Count(item => !item.Success)),

            // AI metrics
            TotalAiRequests = _aiRequests.Values.Sum(q => q.Count),
            SuccessfulAiRequests = _aiRequests.Values.Sum(q => q.Count(item => item.Success)),
            TotalTokensUsed = _aiTokenUsage.Values.Sum(t => t.InputTokens + t.OutputTokens),

            // Error metrics
            TotalExceptions = (int)_exceptionCounters.Values.Sum(),
            UnhandledExceptions = (int)_unhandledExceptions.Values.Sum(q => q.Count),
            ExceptionsByType = GetExceptionsByType(),

            // Custom metrics
            CustomMetrics = new Dictionary<string, double>(_customMetrics),
            Counters = new Dictionary<string, long>(_customCounters)
        };

        return snapshot;
    }

    private TimeSpan CalculateAverageResponseTime()
    {
        var allTimes = _responseTimes.Values.SelectMany(q => q).ToList();
        return allTimes.Count > 0 ? TimeSpan.FromTicks((long)allTimes.Average(t => t.Ticks)) : TimeSpan.Zero;
    }

    private TimeSpan CalculateAverageDatabaseQueryTime()
    {
        var allTimes = _databaseQueryTimes.Values.SelectMany(q => q).ToList();
        return allTimes.Count > 0 ? TimeSpan.FromTicks((long)allTimes.Average(t => t.Ticks)) : TimeSpan.Zero;
    }

    private double CalculateCacheHitRatio()
    {
        var totalHits = _cacheHitCounters.Values.Sum();
        var totalMisses = _cacheMissCounters.Values.Sum();
        var totalRequests = totalHits + totalMisses;

        return totalRequests > 0 ? (double)totalHits / totalRequests : 0.0;
    }

    private int CalculateTotalCacheRequests()
    {
        return (int)(_cacheHitCounters.Values.Sum() + _cacheMissCounters.Values.Sum());
    }

    public long GetCurrentMemoryUsage()
    {
        return _memoryUsageBuffer.TryPeek(out var latest) ? latest : 0;
    }

    public double GetCurrentCpuUsage()
    {
        return _cpuUsageBuffer.TryPeek(out var latest) ? latest : 0.0;
    }

    public int GetCurrentDatabaseConnections()
    {
        return (int)_currentDatabaseConnections;
    }

    private Dictionary<string, int> GetExceptionsByType()
    {
        return _exceptionCounters.ToDictionary(kvp => kvp.Key, kvp => (int)kvp.Value);
    }

    public Dictionary<string, double> GetCustomMetrics()
    {
        return new Dictionary<string, double>(_customMetrics);
    }
}
