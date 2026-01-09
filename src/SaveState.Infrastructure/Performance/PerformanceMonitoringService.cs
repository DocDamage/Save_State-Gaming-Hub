using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Service for monitoring and optimizing application performance.
/// </summary>
public class PerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly Stopwatch _startupTimer;
    private readonly Dictionary<string, Stopwatch> _operationTimers = new();

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger;
        _startupTimer = Stopwatch.StartNew();
    }

    /// <summary>
    /// Marks the completion of application startup.
    /// </summary>
    public void MarkStartupComplete()
    {
        _startupTimer.Stop();
        _logger.LogInformation("Application startup completed in {ElapsedMs}ms", _startupTimer.ElapsedMilliseconds);
    }

    /// <summary>
    /// Starts timing an operation.
    /// </summary>
    public void StartOperation(string operationName)
    {
        if (_operationTimers.ContainsKey(operationName))
        {
            _operationTimers[operationName].Restart();
        }
        else
        {
            _operationTimers[operationName] = Stopwatch.StartNew();
        }
    }

    /// <summary>
    /// Stops timing an operation and logs the result.
    /// </summary>
    public void StopOperation(string operationName)
    {
        if (_operationTimers.TryGetValue(operationName, out var timer))
        {
            timer.Stop();
            _logger.LogDebug("{OperationName} completed in {ElapsedMs}ms", operationName, timer.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Gets current memory usage in MB.
    /// </summary>
    public long GetMemoryUsageMB()
    {
        var process = Process.GetCurrentProcess();
        return process.WorkingSet64 / 1024 / 1024;
    }

    /// <summary>
    /// Logs current memory usage.
    /// </summary>
    public void LogMemoryUsage(string context)
    {
        var memoryMB = GetMemoryUsageMB();
        _logger.LogInformation("Memory usage at {Context}: {MemoryMB}MB", context, memoryMB);
    }

    /// <summary>
    /// Forces garbage collection (use sparingly).
    /// </summary>
    public void ForceGarbageCollection()
    {
        var beforeMB = GetMemoryUsageMB();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterMB = GetMemoryUsageMB();
        _logger.LogInformation("Garbage collection freed {FreedMB}MB (before: {BeforeMB}MB, after: {AfterMB}MB)",
            beforeMB - afterMB, beforeMB, afterMB);
    }
}
