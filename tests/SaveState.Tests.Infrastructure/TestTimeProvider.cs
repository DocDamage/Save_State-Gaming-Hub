using SaveState.Core.Common.Services;

namespace SaveState.Tests.Infrastructure;

/// <summary>
/// Test implementation of ITimeProvider that allows manual control of time.
/// </summary>
public class TestTimeProvider : ITimeProvider
{
    private DateTime _currentTime;
    private long _timestamp;

    public TestTimeProvider(DateTime? initialTime = null)
    {
        _currentTime = initialTime ?? new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _timestamp = 0;
    }

    public DateTime UtcNow => _currentTime.ToUniversalTime();
    public DateTime Now => _currentTime.ToLocalTime();
    public DateTime Today => _currentTime.Date;

    public long GetTimestamp() => _timestamp;

    public ITestableTimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        // In test mode, timers don't auto-fire - manual control
        return new TestTimer(callback, state);
    }

    /// <summary>
    /// Advances the current time by the specified amount.
    /// </summary>
    public void Advance(TimeSpan amount)
    {
        _currentTime = _currentTime.Add(amount);
        _timestamp += amount.Ticks;
    }

    /// <summary>
    /// Sets the current time to the specified value.
    /// </summary>
    public void SetTime(DateTime time)
    {
        _currentTime = time;
    }

    private class TestTimer : ITestableTimer
    {
        private readonly TimerCallback _callback;
        private readonly object? _state;

        public TestTimer(TimerCallback callback, object? state)
        {
            _callback = callback;
            _state = state;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose() { }

        /// <summary>
        /// Manually triggers the timer callback for testing.
        /// </summary>
        public void Trigger()
        {
            _callback?.Invoke(_state);
        }
    }
}
