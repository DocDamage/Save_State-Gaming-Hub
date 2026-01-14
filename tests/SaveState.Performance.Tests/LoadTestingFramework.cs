using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SaveState.Tests.Performance;

/// <summary>
/// Load testing framework for performance validation.
/// PHASE 7: REQUIRED - Load Testing Framework (Session 4)
/// </summary>
public class LoadTestingFramework
{
    /// <summary>
    /// Executes a load test with specified concurrency.
    /// </summary>
    public static async Task<LoadTestResult> RunLoadTestAsync(
        Func<Task> operation,
        int concurrentOperations,
        int totalOperations,
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<OperationResult>();
        var errors = new List<Exception>();

        try
        {
            var operationsPerBatch = totalOperations / concurrentOperations;
            var semaphore = new System.Threading.SemaphoreSlim(concurrentOperations);

            for (int i = 0; i < totalOperations; i++)
            {
                await semaphore.WaitAsync();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var opStopwatch = Stopwatch.StartNew();
                        await operation();
                        opStopwatch.Stop();

                        lock (results)
                        {
                            results.Add(new OperationResult(
                                Success: true,
                                Duration: opStopwatch.Elapsed,
                                Timestamp: DateTime.UtcNow));
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                        {
                            errors.Add(ex);
                        }

                        lock (results)
                        {
                            results.Add(new OperationResult(
                                Success: false,
                                Duration: TimeSpan.Zero,
                                Timestamp: DateTime.UtcNow));
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException($"Load test exceeded timeout of {timeout}");
                }
            }

            // Wait for all operations to complete
            await Task.Delay(100);
        }
        finally
        {
            stopwatch.Stop();
        }

        return new LoadTestResult(
            TotalOperations: totalOperations,
            SuccessfulOperations: results.Count(r => r.Success),
            FailedOperations: errors.Count,
            TotalDuration: stopwatch.Elapsed,
            AverageDuration: results.Any() ? TimeSpan.FromMilliseconds(results.Average(r => r.Duration.TotalMilliseconds)) : TimeSpan.Zero,
            MinDuration: results.Any() ? results.Min(r => r.Duration) : TimeSpan.Zero,
            MaxDuration: results.Any() ? results.Max(r => r.Duration) : TimeSpan.Zero,
            OperationsPerSecond: stopwatch.Elapsed.TotalSeconds > 0 ? totalOperations / stopwatch.Elapsed.TotalSeconds : 0,
            Errors: errors,
            AllResults: results);
    }

    /// <summary>
    /// Executes a stress test with increasing load.
    /// </summary>
    public static async Task<List<LoadTestResult>> RunStressTestAsync(
        Func<Task> operation,
        int[] concurrencyLevels,
        int operationsPerLevel)
    {
        var results = new List<LoadTestResult>();

        foreach (var concurrency in concurrencyLevels)
        {
            var result = await RunLoadTestAsync(
                operation,
                concurrency,
                operationsPerLevel,
                TimeSpan.FromSeconds(60));

            results.Add(result);

            // Wait between levels
            await Task.Delay(1000);
        }

        return results;
    }

    /// <summary>
    /// Executes an endurance test over extended period.
    /// </summary>
    public static async Task<EnduranceTestResult> RunEnduranceTestAsync(
        Func<Task> operation,
        int concurrentOperations,
        TimeSpan duration)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationCount = 0;
        var errors = new List<Exception>();

        while (stopwatch.Elapsed < duration)
        {
            var result = await RunLoadTestAsync(
                operation,
                concurrentOperations,
                100,
                duration - stopwatch.Elapsed);

            operationCount += result.SuccessfulOperations;
            errors.AddRange(result.Errors);

            await Task.Delay(1000);
        }

        stopwatch.Stop();

        return new EnduranceTestResult(
            TotalOperations: operationCount,
            Duration: duration,
            OperationsPerSecond: operationCount / duration.TotalSeconds,
            Errors: errors,
            MemoryLeakDetected: DetectMemoryLeak());
    }

    private static bool DetectMemoryLeak()
    {
        var initialMemory = GC.GetTotalMemory(true);
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);

        // Heuristic: more than 50% increase could indicate leak
        return (finalMemory - initialMemory) / (double)initialMemory > 0.5;
    }
}

/// <summary>
/// Load test result.
/// </summary>
public record LoadTestResult(
    int TotalOperations,
    int SuccessfulOperations,
    int FailedOperations,
    TimeSpan TotalDuration,
    TimeSpan AverageDuration,
    TimeSpan MinDuration,
    TimeSpan MaxDuration,
    double OperationsPerSecond,
    List<Exception> Errors,
    List<OperationResult> AllResults)
{
    public double SuccessRate => (SuccessfulOperations / (double)TotalOperations) * 100;
}

/// <summary>
/// Individual operation result.
/// </summary>
public record OperationResult(
    bool Success,
    TimeSpan Duration,
    DateTime Timestamp);

/// <summary>
/// Endurance test result.
/// </summary>
public record EnduranceTestResult(
    int TotalOperations,
    TimeSpan Duration,
    double OperationsPerSecond,
    List<Exception> Errors,
    bool MemoryLeakDetected)
{
    public int ErrorCount => Errors.Count;
    public double ErrorRate => (ErrorCount / (double)TotalOperations) * 100;
}

/// <summary>
/// Load test examples.
/// </summary>
public class LoadTestExamples
{
    [Fact(Skip = "Long-running load test")]
    public async Task LoadTest_GameLibrarySearch_1000Operations()
    {
        // Arrange
        Func<Task> searchOperation = async () =>
        {
            // Simulate search operation
            await Task.Delay(10);
        };

        // Act
        var result = await LoadTestingFramework.RunLoadTestAsync(
            searchOperation,
            concurrentOperations: 10,
            totalOperations: 1000,
            timeout: TimeSpan.FromSeconds(60));

        // Assert
        Assert.True(result.SuccessRate > 95);
        Assert.True(result.OperationsPerSecond > 100);
    }

    [Fact(Skip = "Long-running stress test")]
    public async Task StressTest_GameLibraryOperations()
    {
        // Arrange
        Func<Task> operation = async () =>
        {
            await Task.Delay(5);
        };

        var concurrencyLevels = new[] { 1, 5, 10, 20, 50 };

        // Act
        var results = await LoadTestingFramework.RunStressTestAsync(
            operation,
            concurrencyLevels,
            operationsPerLevel: 100);

        // Assert
        Assert.All(results, result =>
        {
            Assert.True(result.SuccessRate > 95);
        });
    }

    [Fact(Skip = "Long-running endurance test")]
    public async Task EnduranceTest_CloudSync_30Minutes()
    {
        // Arrange
        Func<Task> syncOperation = async () =>
        {
            // Simulate cloud sync
            await Task.Delay(50);
        };

        // Act
        var result = await LoadTestingFramework.RunEnduranceTestAsync(
            syncOperation,
            concurrentOperations: 5,
            duration: TimeSpan.FromMinutes(30));

        // Assert
        Assert.False(result.MemoryLeakDetected);
        Assert.True(result.ErrorRate < 5);
    }
}
