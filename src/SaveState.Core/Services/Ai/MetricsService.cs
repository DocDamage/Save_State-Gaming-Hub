using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Serilog;

namespace SaveState.Core.Services.Ai;

/// <summary>
/// Tracks and provides metrics for AI operations.
/// Includes request counts, latency tracking, success rates, and observability data.
/// </summary>
public class MetricsService
{
    private readonly ILogger _logger = Log.ForContext<MetricsService>();
    private readonly ConcurrentQueue<ObservabilityData> _recentEvents = new();
    private readonly List<ObservabilityHandler> _observers = new();

    // Core metrics
    private int _totalRequests;
    private int _successfulRequests;
    private int _failedRequests;
    private int _cacheHits;
    private int _cacheMisses;
    private int _fallbacksUsed;
    private long _totalLatencyMs;
    private readonly ConcurrentDictionary<string, StageMetrics> _stageMetrics = new();
    private readonly ConcurrentQueue<long> _latencyHistory = new();

    private const int MaxHistorySize = 1000;
    private const int MaxEventsToKeep = 100;

    /// <summary>
    /// Records the start of a request.
    /// </summary>
    public void RecordRequestStart()
    {
        Interlocked.Increment(ref _totalRequests);
    }

    /// <summary>
    /// Records a successful request completion.
    /// </summary>
    public void RecordRequestSuccess(long latencyMs)
    {
        Interlocked.Increment(ref _successfulRequests);
        Interlocked.Add(ref _totalLatencyMs, latencyMs);
        AddToLatencyHistory(latencyMs);
    }

    /// <summary>
    /// Records a failed request.
    /// </summary>
    public void RecordRequestFailure()
    {
        Interlocked.Increment(ref _failedRequests);
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    public void RecordCacheHit()
    {
        Interlocked.Increment(ref _cacheHits);
    }

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    public void RecordCacheMiss()
    {
        Interlocked.Increment(ref _cacheMisses);
    }

    /// <summary>
    /// Records fallback usage.
    /// </summary>
    public void RecordFallbackUsed()
    {
        Interlocked.Increment(ref _fallbacksUsed);
    }

    /// <summary>
    /// Records stage-specific metrics.
    /// </summary>
    public void RecordStageExecution(string stageName, bool success, long latencyMs)
    {
        var metrics = _stageMetrics.GetOrAdd(stageName, _ => new StageMetrics { StageName = stageName });

        Interlocked.Increment(ref metrics.Executions);

        if (success)
        {
            Interlocked.Increment(ref metrics.Successes);
        }
        else
        {
            Interlocked.Increment(ref metrics.Failures);
        }

        // Update average latency (simple moving average)
        var newAverage = ((metrics.AverageLatency * (metrics.Executions - 1)) + latencyMs) / metrics.Executions;
        metrics.AverageLatency = newAverage;
    }

    /// <summary>
    /// Adds an observability event.
    /// </summary>
    public void AddObservabilityEvent(ObservabilityData data)
    {
        _recentEvents.Enqueue(data);

        // Keep only recent events
        while (_recentEvents.Count > MaxEventsToKeep)
        {
            _recentEvents.TryDequeue(out _);
        }

        // Notify observers
        foreach (var observer in _observers)
        {
            try
            {
                observer(data);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Observer failed to handle event");
            }
        }
    }

    /// <summary>
    /// Registers an observability handler.
    /// </summary>
    public void AddObserver(ObservabilityHandler handler)
    {
        _observers.Add(handler);
        _logger.Debug("Added observability observer");
    }

    /// <summary>
    /// Gets comprehensive metrics.
    /// </summary>
    public OrchestratorMetrics GetMetrics()
    {
        var latencyHistory = _latencyHistory.ToArray();
        var avgLatency = latencyHistory.Length > 0 ? latencyHistory.Average() : 0;

        var stageMetrics = _stageMetrics.Values.ToList();

        return new OrchestratorMetrics
        {
            TotalRequests = _totalRequests,
            SuccessfulRequests = _successfulRequests,
            FailedRequests = _failedRequests,
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            FallbacksUsed = _fallbacksUsed,
            AverageLatencyMs = avgLatency,
            TotalLatencyMs = _totalLatencyMs,
            CacheHitRate = _totalRequests > 0 ? (double)_cacheHits / (_cacheHits + _cacheMisses) : 0,
            SuccessRate = _totalRequests > 0 ? (double)_successfulRequests / _totalRequests : 0,
            StageMetrics = stageMetrics
        };
    }

    /// <summary>
    /// Gets recent observability events.
    /// </summary>
    public List<ObservabilityData> GetRecentEvents(int count = 50)
    {
        return _recentEvents.TakeLast(Math.Min(count, _recentEvents.Count)).ToList();
    }

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    public void ResetMetrics()
    {
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _successfulRequests, 0);
        Interlocked.Exchange(ref _failedRequests, 0);
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
        Interlocked.Exchange(ref _fallbacksUsed, 0);
        Interlocked.Exchange(ref _totalLatencyMs, 0);

        _stageMetrics.Clear();
        _latencyHistory.Clear();

        _logger.Information("Metrics reset");
    }

    private void AddToLatencyHistory(long latencyMs)
    {
        _latencyHistory.Enqueue(latencyMs);

        // Keep only recent history
        while (_latencyHistory.Count > MaxHistorySize)
        {
            _latencyHistory.TryDequeue(out _);
        }
    }
}
