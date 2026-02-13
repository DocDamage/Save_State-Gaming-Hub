using System.Diagnostics;

namespace SaveState.Core.Common.Services;

/// <summary>
/// Provides time-related functionality to enable testability of time-dependent code.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current date.
    /// </summary>
    DateTime Today { get; }

    /// <summary>
    /// Gets the timestamp for the current moment.
    /// </summary>
    long GetTimestamp();

    /// <summary>
    /// Creates a timer that fires after the specified due time.
    /// </summary>
    ITestableTimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period);
}

/// <summary>
/// Timer interface for testability.
/// </summary>
public interface ITestableTimer : IDisposable
{
    /// <summary>
    /// Changes the timer's due time and period.
    /// </summary>
    bool Change(TimeSpan dueTime, TimeSpan period);
}

/// <summary>
/// Default implementation of ITimeProvider using system clock.
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    public static ITimeProvider Instance { get; } = new SystemTimeProvider();

    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateTime Today => DateTime.Today;

    public long GetTimestamp() => Stopwatch.GetTimestamp();

    public ITestableTimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new Timer(callback, state, dueTime, period);
        return new TimerWrapper(timer);
    }
}

/// <summary>
/// Wrapper for System.Threading.Timer to implement ITimer.
/// </summary>
internal class TimerWrapper : ITestableTimer
{
    private readonly Timer _timer;

    public TimerWrapper(Timer timer)
    {
        _timer = timer;
    }

    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        return _timer.Change(dueTime, period);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
