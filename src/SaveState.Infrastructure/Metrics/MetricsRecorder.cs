using System.Diagnostics;

namespace SaveState.Infrastructure.Metrics;

/// <summary>
/// Helper for recording timed operations as metrics.
/// </summary>
public static class MetricsRecorder
{
    /// <summary>
    /// Records a timed operation.
    /// </summary>
    public static async Task<T> RecordAsync<T>(
        Func<Task<T>> operation,
        Action<double> recordDuration,
        Action? onSuccess = null,
        Action? onFailure = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            recordDuration(stopwatch.Elapsed.TotalSeconds);
            onSuccess?.Invoke();
            
            return result;
        }
        catch
        {
            stopwatch.Stop();
            recordDuration(stopwatch.Elapsed.TotalSeconds);
            onFailure?.Invoke();
            throw;
        }
    }

    /// <summary>
    /// Records a timed operation.
    /// </summary>
    public static async Task RecordAsync(
        Func<Task> operation,
        Action<double> recordDuration,
        Action? onSuccess = null,
        Action? onFailure = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await operation();
            stopwatch.Stop();
            
            recordDuration(stopwatch.Elapsed.TotalSeconds);
            onSuccess?.Invoke();
        }
        catch
        {
            stopwatch.Stop();
            recordDuration(stopwatch.Elapsed.TotalSeconds);
            onFailure?.Invoke();
            throw;
        }
    }
}

/// <summary>
/// Disposable timer for recording operation duration.
/// </summary>
public class MetricsTimer : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly Action<double> _onComplete;
    private bool _completed;

    public MetricsTimer(Action<double> onComplete)
    {
        _onComplete = onComplete;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Completes the timer and records the duration.
    /// </summary>
    public void Complete()
    {
        if (_completed) return;
        
        _stopwatch.Stop();
        _onComplete(_stopwatch.Elapsed.TotalSeconds);
        _completed = true;
    }

    public void Dispose()
    {
        Complete();
    }
}
