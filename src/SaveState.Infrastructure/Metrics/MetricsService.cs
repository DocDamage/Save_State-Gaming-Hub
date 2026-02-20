using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.Extensions.Logging;
using SaveState.Core.Metrics;

namespace SaveState.Infrastructure.Metrics;

/// <summary>
/// Implementation of IMetricsService using System.Diagnostics.Metrics.
/// Provides comprehensive metrics collection with Prometheus export capability.
/// </summary>
public sealed class MetricsService : IMetricsService, IMetricsReporter, IDisposable
{
    private readonly ILogger<MetricsService> _logger;
    private readonly Meter _meter;
    private readonly Dictionary<string, Counter<long>> _counters = new();
    private readonly Dictionary<string, Histogram<double>> _histograms = new();
    private readonly Dictionary<string, long> _counterValues = new();
    private readonly Dictionary<string, List<double>> _histogramValues = new();

    // Active session tracking
    private long _activeSessions = 0;
    private long _attachedProcesses = 0;
    private double _cpuUsage = 0;
    private long _memoryUsage = 0;
    private long _diskUsage = 0;

    private readonly object _lock = new();

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
        _meter = new Meter("SaveStateReborn.Metrics", "2.5.1");

        InitializeMetrics();
        InitializeSystemGauges();

        _logger.LogInformation("Metrics service initialized with version {Version}", "2.5.1");
    }

    private void InitializeMetrics()
    {
        // Business Counters
        _counters["games.launched"] = _meter.CreateCounter<long>(
            "games.launched",
            description: "Total number of games launched");

        _counters["saves.created"] = _meter.CreateCounter<long>(
            "saves.created",
            description: "Total number of save states created");

        _counters["saves.loaded"] = _meter.CreateCounter<long>(
            "saves.loaded",
            description: "Total number of save states loaded");

        _counters["memory.patterns.detected"] = _meter.CreateCounter<long>(
            "memory.patterns.detected",
            description: "Total number of memory patterns detected");

        _counters["autodiscovery.success"] = _meter.CreateCounter<long>(
            "autodiscovery.success",
            description: "Number of successful auto-discovery operations");

        _counters["cheattables.imported"] = _meter.CreateCounter<long>(
            "cheattables.imported",
            description: "Number of cheat tables imported");

        _counters["errors.total"] = _meter.CreateCounter<long>(
            "errors.total",
            description: "Total number of errors");

        _counters["warnings.total"] = _meter.CreateCounter<long>(
            "warnings.total",
            description: "Total number of warnings");

        // Performance Histograms
        _histograms["memory.scan.duration"] = _meter.CreateHistogram<double>(
            "memory.scan.duration",
            unit: "ms",
            description: "Memory scan duration in milliseconds");

        _histograms["pattern.detection.duration"] = _meter.CreateHistogram<double>(
            "pattern.detection.duration",
            unit: "ms",
            description: "Pattern detection duration in milliseconds");

        _histograms["game.launch.duration"] = _meter.CreateHistogram<double>(
            "game.launch.duration",
            unit: "ms",
            description: "Game launch duration in milliseconds");

        // Initialize counter value tracking
        foreach (var key in _counters.Keys)
        {
            _counterValues[key] = 0;
        }

        // Initialize histogram value tracking
        foreach (var key in _histograms.Keys)
        {
            _histogramValues[key] = new List<double>();
        }
    }

    private void InitializeSystemGauges()
    {
        _meter.CreateObservableGauge(
            "system.cpu.usage",
            () => Interlocked.Exchange(ref _cpuUsage, _cpuUsage),
            unit: "%",
            description: "CPU usage percentage");

        _meter.CreateObservableGauge(
            "system.memory.usage",
            () => Interlocked.Read(ref _memoryUsage),
            unit: "bytes",
            description: "Memory usage in bytes");

        _meter.CreateObservableGauge(
            "system.disk.usage",
            () => Interlocked.Read(ref _diskUsage),
            unit: "bytes",
            description: "Disk usage in bytes");

        _meter.CreateObservableGauge(
            "sessions.active",
            () => Interlocked.Read(ref _activeSessions),
            description: "Number of active user sessions");

        _meter.CreateObservableGauge(
            "processes.attached",
            () => Interlocked.Read(ref _attachedProcesses),
            description: "Number of attached game processes");
    }

    #region Business Metrics Implementation

    public void RecordGameLaunched(string gameName, string platform)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName),
            new KeyValuePair<string, object?>("platform", platform)
        };

        _counters["games.launched"].Add(1, tags);
        IncrementCounterValue("games.launched");

        _logger.LogDebug("Metric: Game launched - {GameName} ({Platform})", gameName, platform);
    }

    public void RecordSaveStateCreated(string gameName)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName)
        };

        _counters["saves.created"].Add(1, tags);
        IncrementCounterValue("saves.created");

        _logger.LogDebug("Metric: Save state created for {GameName}", gameName);
    }

    public void RecordSaveStateLoaded(string gameName)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName)
        };

        _counters["saves.loaded"].Add(1, tags);
        IncrementCounterValue("saves.loaded");

        _logger.LogDebug("Metric: Save state loaded for {GameName}", gameName);
    }

    public void RecordMemoryPatternDetected(string gameName, string patternType)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName),
            new KeyValuePair<string, object?>("type", patternType)
        };

        _counters["memory.patterns.detected"].Add(1, tags);
        IncrementCounterValue("memory.patterns.detected");

        _logger.LogDebug("Metric: Memory pattern detected - {PatternType} in {GameName}", patternType, gameName);
    }

    public void RecordAutoDiscoverySuccess(string gameName, int patternsFound)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName),
            new KeyValuePair<string, object?>("patterns_found", patternsFound)
        };

        _counters["autodiscovery.success"].Add(1, tags);
        IncrementCounterValue("autodiscovery.success");

        _logger.LogDebug("Metric: Auto-discovery success - {GameName} with {PatternsFound} patterns", gameName, patternsFound);
    }

    public void RecordCheatTableImported(string gameName, int entries)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName),
            new KeyValuePair<string, object?>("entries", entries)
        };

        _counters["cheattables.imported"].Add(1, tags);
        IncrementCounterValue("cheattables.imported");

        _logger.LogDebug("Metric: Cheat table imported - {GameName} with {Entries} entries", gameName, entries);
    }

    #endregion

    #region Performance Metrics Implementation

    public void RecordMemoryScanDuration(TimeSpan duration, string gameName)
    {
        var durationMs = duration.TotalMilliseconds;
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName)
        };

        _histograms["memory.scan.duration"].Record(durationMs, tags);
        RecordHistogramValue("memory.scan.duration", durationMs);

        _logger.LogDebug("Metric: Memory scan duration - {DurationMs}ms for {GameName}", durationMs, gameName);
    }

    public void RecordPatternDetectionDuration(TimeSpan duration, string gameName)
    {
        var durationMs = duration.TotalMilliseconds;
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName)
        };

        _histograms["pattern.detection.duration"].Record(durationMs, tags);
        RecordHistogramValue("pattern.detection.duration", durationMs);

        _logger.LogDebug("Metric: Pattern detection duration - {DurationMs}ms for {GameName}", durationMs, gameName);
    }

    public void RecordGameLaunchDuration(TimeSpan duration, string gameName)
    {
        var durationMs = duration.TotalMilliseconds;
        var tags = new[]
        {
            new KeyValuePair<string, object?>("game", gameName)
        };

        _histograms["game.launch.duration"].Record(durationMs, tags);
        RecordHistogramValue("game.launch.duration", durationMs);

        _logger.LogDebug("Metric: Game launch duration - {DurationMs}ms for {GameName}", durationMs, gameName);
    }

    #endregion

    #region System Metrics Implementation

    public void RecordCpuUsage(double percentage)
    {
        Interlocked.Exchange(ref _cpuUsage, percentage);
        _logger.LogDebug("Metric: CPU usage - {Percentage}%", percentage);
    }

    public void RecordMemoryUsage(long bytes)
    {
        Interlocked.Exchange(ref _memoryUsage, bytes);
        _logger.LogDebug("Metric: Memory usage - {Bytes} bytes", bytes);
    }

    public void RecordDiskUsage(long bytes)
    {
        Interlocked.Exchange(ref _diskUsage, bytes);
        _logger.LogDebug("Metric: Disk usage - {Bytes} bytes", bytes);
    }

    #endregion

    #region Error Metrics Implementation

    public void RecordError(string errorType, string component)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("type", errorType),
            new KeyValuePair<string, object?>("component", component)
        };

        _counters["errors.total"].Add(1, tags);
        IncrementCounterValue("errors.total");

        _logger.LogWarning("Metric: Error recorded - {ErrorType} in {Component}", errorType, component);
    }

    public void RecordWarning(string warningType, string component)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("type", warningType),
            new KeyValuePair<string, object?>("component", component)
        };

        _counters["warnings.total"].Add(1, tags);
        IncrementCounterValue("warnings.total");

        _logger.LogWarning("Metric: Warning recorded - {WarningType} in {Component}", warningType, component);
    }

    #endregion

    #region Session Tracking Implementation

    public void IncrementActiveSessions()
    {
        var newValue = Interlocked.Increment(ref _activeSessions);
        _logger.LogDebug("Metric: Active sessions incremented to {Count}", newValue);
    }

    public void DecrementActiveSessions()
    {
        var newValue = Interlocked.Decrement(ref _activeSessions);
        _logger.LogDebug("Metric: Active sessions decremented to {Count}", newValue);
    }

    public void IncrementAttachedProcesses()
    {
        var newValue = Interlocked.Increment(ref _attachedProcesses);
        _logger.LogDebug("Metric: Attached processes incremented to {Count}", newValue);
    }

    public void DecrementAttachedProcesses()
    {
        var newValue = Interlocked.Decrement(ref _attachedProcesses);
        _logger.LogDebug("Metric: Attached processes decremented to {Count}", newValue);
    }

    #endregion

    #region IMetricsReporter Implementation

    public string ExportPrometheusFormat()
    {
        var sb = new StringBuilder();

        // Export counters
        foreach (var counter in _counters)
        {
            var metricName = counter.Key.Replace('.', '_');
            sb.AppendLine($"# HELP {metricName} {counter.Value.Description}");
            sb.AppendLine($"# TYPE {metricName} counter");

            lock (_lock)
            {
                var value = _counterValues.GetValueOrDefault(counter.Key, 0);
                sb.AppendLine($"{metricName} {value}");
            }
        }

        sb.AppendLine();

        // Export gauges
        sb.AppendLine("# HELP sessions_active Number of active user sessions");
        sb.AppendLine("# TYPE sessions_active gauge");
        sb.AppendLine($"sessions_active {Interlocked.Read(ref _activeSessions)}");

        sb.AppendLine();

        sb.AppendLine("# HELP processes_attached Number of attached game processes");
        sb.AppendLine("# TYPE processes_attached gauge");
        sb.AppendLine($"processes_attached {Interlocked.Read(ref _attachedProcesses)}");

        sb.AppendLine();

        sb.AppendLine("# HELP system_cpu_usage CPU usage percentage");
        sb.AppendLine("# TYPE system_cpu_usage gauge");
        sb.AppendLine($"system_cpu_usage {Interlocked.CompareExchange(ref _cpuUsage, 0, 0)}");

        sb.AppendLine();

        sb.AppendLine("# HELP system_memory_usage Memory usage in bytes");
        sb.AppendLine("# TYPE system_memory_usage gauge");
        sb.AppendLine($"system_memory_usage {Interlocked.Read(ref _memoryUsage)}");

        sb.AppendLine();

        // Export histograms
        foreach (var histogram in _histograms)
        {
            var metricName = histogram.Key.Replace('.', '_');
            sb.AppendLine($"# HELP {metricName} {histogram.Value.Description}");
            sb.AppendLine($"# TYPE {metricName} histogram");

            lock (_lock)
            {
                var values = _histogramValues.GetValueOrDefault(histogram.Key, new List<double>());
                if (values.Any())
                {
                    sb.AppendLine($"{metricName}_count {values.Count}");
                    sb.AppendLine($"{metricName}_sum {values.Sum()}");
                }
                else
                {
                    sb.AppendLine($"{metricName}_count 0");
                    sb.AppendLine($"{metricName}_sum 0");
                }
            }
        }

        return sb.ToString();
    }

    public ServiceMetricsSnapshot GetSnapshot()
    {
        var snapshot = new ServiceMetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            Counters = new Dictionary<string, long>(),
            Gauges = new Dictionary<string, double>(),
            Histograms = new Dictionary<string, MetricHistogram>()
        };

        lock (_lock)
        {
            foreach (var kvp in _counterValues)
            {
                snapshot.Counters[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in _histogramValues)
            {
                var values = kvp.Value;
                snapshot.Histograms[kvp.Key] = new MetricHistogram
                {
                    Count = values.Count,
                    Sum = values.Sum(),
                    Min = values.Any() ? values.Min() : 0,
                    Max = values.Any() ? values.Max() : 0
                };
            }
        }

        snapshot.Gauges["sessions.active"] = Interlocked.Read(ref _activeSessions);
        snapshot.Gauges["processes.attached"] = Interlocked.Read(ref _attachedProcesses);
        snapshot.Gauges["system.cpu.usage"] = Interlocked.CompareExchange(ref _cpuUsage, 0, 0);
        snapshot.Gauges["system.memory.usage"] = Interlocked.Read(ref _memoryUsage);
        snapshot.Gauges["system.disk.usage"] = Interlocked.Read(ref _diskUsage);

        return snapshot;
    }

    #endregion

    #region Helper Methods

    private void IncrementCounterValue(string key)
    {
        lock (_lock)
        {
            if (!_counterValues.ContainsKey(key))
            {
                _counterValues[key] = 0;
            }
            _counterValues[key]++;
        }
    }

    private void RecordHistogramValue(string key, double value)
    {
        lock (_lock)
        {
            if (!_histogramValues.ContainsKey(key))
            {
                _histogramValues[key] = new List<double>();
            }
            _histogramValues[key].Add(value);

            // Limit stored values to prevent memory growth
            if (_histogramValues[key].Count > 10000)
            {
                _histogramValues[key].RemoveAt(0);
            }
        }
    }

    #endregion

    public void Dispose()
    {
        _meter.Dispose();
        _logger.LogInformation("Metrics service disposed");
    }
}
