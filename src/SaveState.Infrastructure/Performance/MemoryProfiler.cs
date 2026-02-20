using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using System.Diagnostics;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Real-time memory and CPU profiler for monitoring application performance.
/// PHASE 7: REQUIRED - Performance Profiling
/// </summary>
public class MemoryProfiler
{
    private readonly ILogger<MemoryProfiler> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, OperationMetrics> _metrics = new();

    public MemoryProfiler(ILogger<MemoryProfiler> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Profiles an operation and records memory/CPU usage.
    /// </summary>
    public async Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var startMemory = GC.GetTotalMemory(true);
        var startCpu = Process.GetCurrentProcess().TotalProcessorTime;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await operation();
        }
        finally
        {
            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var endCpu = Process.GetCurrentProcess().TotalProcessorTime;

            var memoryUsed = endMemory - startMemory;
            var cpuUsed = endCpu - startCpu;

            var metrics = new OperationMetrics(
                OperationName: operationName,
                Duration: stopwatch.Elapsed,
                MemoryUsedBytes: memoryUsed,
                CpuTimeMilliseconds: (long)cpuUsed.TotalMilliseconds,
                Timestamp: _timeProvider.UtcNow);

            RecordMetrics(operationName, metrics);
            LogMetrics(metrics);
        }
    }

    /// <summary>
    /// Profiles a synchronous operation.
    /// </summary>
    public T Profile<T>(string operationName, Func<T> operation)
    {
        var startMemory = GC.GetTotalMemory(true);
        var startCpu = Process.GetCurrentProcess().TotalProcessorTime;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return operation();
        }
        finally
        {
            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var endCpu = Process.GetCurrentProcess().TotalProcessorTime;

            var memoryUsed = endMemory - startMemory;
            var cpuUsed = endCpu - startCpu;

            var metrics = new OperationMetrics(
                OperationName: operationName,
                Duration: stopwatch.Elapsed,
                MemoryUsedBytes: memoryUsed,
                CpuTimeMilliseconds: (long)cpuUsed.TotalMilliseconds,
                Timestamp: _timeProvider.UtcNow);

            RecordMetrics(operationName, metrics);
            LogMetrics(metrics);
        }
    }

    /// <summary>
    /// Gets current memory usage statistics.
    /// </summary>
    public MemoryStatistics GetMemoryStatistics()
    {
        var process = Process.GetCurrentProcess();
        var totalMemory = GC.GetTotalMemory(false);

        return new MemoryStatistics(
            WorkingSetMB: process.WorkingSet64 / 1024 / 1024,
            PrivateMemoryMB: process.PrivateMemorySize64 / 1024 / 1024,
            ManagedMemoryMB: totalMemory / 1024 / 1024,
            PeakMemoryMB: process.PeakWorkingSet64 / 1024 / 1024,
            Timestamp: _timeProvider.UtcNow);
    }

    /// <summary>
    /// Gets current CPU usage.
    /// </summary>
    public CpuStatistics GetCpuStatistics()
    {
        var process = Process.GetCurrentProcess();
        var cpuTime = process.TotalProcessorTime;

        return new CpuStatistics(
            TotalProcessorTimeMs: (long)cpuTime.TotalMilliseconds,
            UserProcessorTimeMs: (long)process.UserProcessorTime.TotalMilliseconds,
            PrivilegedProcessorTimeMs: (long)process.PrivilegedProcessorTime.TotalMilliseconds,
            ThreadCount: process.Threads.Count,
            Timestamp: _timeProvider.UtcNow);
    }

    /// <summary>
    /// Detects potential memory leaks by monitoring GC patterns.
    /// </summary>
    public MemoryLeakAnalysis AnalyzeMemoryLeaks()
    {
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);

        var totalMemory = GC.GetTotalMemory(false);
        var issueSuspected = gen2Collections > 100 && totalMemory > 500 * 1024 * 1024;

        return new MemoryLeakAnalysis(
            Generation0Collections: gen0Collections,
            Generation1Collections: gen1Collections,
            Generation2Collections: gen2Collections,
            CurrentMemoryMB: totalMemory / 1024 / 1024,
            LeakSuspected: issueSuspected,
            Timestamp: _timeProvider.UtcNow);
    }

    /// <summary>
    /// Gets recorded metrics for an operation.
    /// </summary>
    public IReadOnlyList<OperationMetrics> GetOperationMetrics(string operationName)
    {
        if (_metrics.TryGetValue(operationName, out var metric))
        {
            return new List<OperationMetrics> { metric };
        }
        return new List<OperationMetrics>();
    }

    /// <summary>
    /// Gets all recorded metrics.
    /// </summary>
    public IReadOnlyDictionary<string, OperationMetrics> GetAllMetrics() => _metrics.AsReadOnly();

    /// <summary>
    /// Clears all recorded metrics.
    /// </summary>
    public void ClearMetrics() => _metrics.Clear();

    private void RecordMetrics(string operationName, OperationMetrics metrics)
    {
        _metrics[operationName] = metrics;
    }

    private void LogMetrics(OperationMetrics metrics)
    {
        var level = metrics.Duration.TotalMilliseconds > 1000 ? LogLevel.Warning : LogLevel.Debug;

        _logger.Log(
            level,
            "Operation: {Operation} | Duration: {Duration}ms | Memory: {Memory}MB | CPU: {Cpu}ms",
            metrics.OperationName,
            (long)metrics.Duration.TotalMilliseconds,
            metrics.MemoryUsedBytes / 1024 / 1024,
            metrics.CpuTimeMilliseconds);
    }
}

/// <summary>
/// Metrics for a profiled operation.
/// </summary>
public record OperationMetrics(
    string OperationName,
    TimeSpan Duration,
    long MemoryUsedBytes,
    long CpuTimeMilliseconds,
    DateTime Timestamp);

/// <summary>
/// Memory statistics snapshot.
/// </summary>
public record MemoryStatistics(
    long WorkingSetMB,
    long PrivateMemoryMB,
    long ManagedMemoryMB,
    long PeakMemoryMB,
    DateTime Timestamp);

/// <summary>
/// CPU statistics snapshot.
/// </summary>
public record CpuStatistics(
    long TotalProcessorTimeMs,
    long UserProcessorTimeMs,
    long PrivilegedProcessorTimeMs,
    int ThreadCount,
    DateTime Timestamp);

/// <summary>
/// Memory leak analysis results.
/// </summary>
public record MemoryLeakAnalysis(
    int Generation0Collections,
    int Generation1Collections,
    int Generation2Collections,
    long CurrentMemoryMB,
    bool LeakSuspected,
    DateTime Timestamp);
