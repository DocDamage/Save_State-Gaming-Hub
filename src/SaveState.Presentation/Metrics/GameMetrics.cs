using SaveState.Core.Metrics;
using System.Diagnostics;

namespace SaveState.Presentation.Metrics;

/// <summary>
/// Game-specific metrics helper for the presentation layer.
/// Provides convenient methods for tracking game-related metrics in the UI.
/// </summary>
public sealed class GameMetrics : IDisposable
{
    private readonly IMetricsService _metricsService;
    private readonly Dictionary<string, Stopwatch> _operationTimers = new();
    private readonly object _lock = new();

    public GameMetrics(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    #region Game Launch Metrics

    /// <summary>
    /// Records a game launch with platform information.
    /// </summary>
    public void RecordGameLaunched(string gameName, string platform)
    {
        _metricsService.RecordGameLaunched(gameName, platform);
    }

    /// <summary>
    /// Starts timing a game launch operation.
    /// </summary>
    public IDisposable TimeGameLaunch(string gameName)
    {
        return new TimedOperation(_metricsService, gameName, (ms, name) => ms.RecordGameLaunchDuration);
    }

    #endregion

    #region Save State Metrics

    /// <summary>
    /// Records a save state creation.
    /// </summary>
    public void RecordSaveStateCreated(string gameName)
    {
        _metricsService.RecordSaveStateCreated(gameName);
    }

    /// <summary>
    /// Records a save state load.
    /// </summary>
    public void RecordSaveStateLoaded(string gameName)
    {
        _metricsService.RecordSaveStateLoaded(gameName);
    }

    #endregion

    #region Memory Pattern Metrics

    /// <summary>
    /// Records a memory pattern detection.
    /// </summary>
    public void RecordMemoryPatternDetected(string gameName, string patternType)
    {
        _metricsService.RecordMemoryPatternDetected(gameName, patternType);
    }

    /// <summary>
    /// Records an auto-discovery success.
    /// </summary>
    public void RecordAutoDiscoverySuccess(string gameName, int patternsFound)
    {
        _metricsService.RecordAutoDiscoverySuccess(gameName, patternsFound);
    }

    /// <summary>
    /// Records a cheat table import.
    /// </summary>
    public void RecordCheatTableImported(string gameName, int entries)
    {
        _metricsService.RecordCheatTableImported(gameName, entries);
    }

    /// <summary>
    /// Starts timing a memory scan operation.
    /// </summary>
    public IDisposable TimeMemoryScan(string gameName)
    {
        return new TimedOperation(_metricsService, gameName, (ms, name) => ms.RecordMemoryScanDuration);
    }

    /// <summary>
    /// Starts timing a pattern detection operation.
    /// </summary>
    public IDisposable TimePatternDetection(string gameName)
    {
        return new TimedOperation(_metricsService, gameName, (ms, name) => ms.RecordPatternDetectionDuration);
    }

    #endregion

    #region Session Management

    /// <summary>
    /// Increments the active sessions counter.
    /// Call when a user starts a gaming session.
    /// </summary>
    public void StartSession()
    {
        _metricsService.IncrementActiveSessions();
    }

    /// <summary>
    /// Decrements the active sessions counter.
    /// Call when a user ends a gaming session.
    /// </summary>
    public void EndSession()
    {
        _metricsService.DecrementActiveSessions();
    }

    /// <summary>
    /// Increments the attached processes counter.
    /// Call when attaching to a game process.
    /// </summary>
    public void AttachProcess()
    {
        _metricsService.IncrementAttachedProcesses();
    }

    /// <summary>
    /// Decrements the attached processes counter.
    /// Call when detaching from a game process.
    /// </summary>
    public void DetachProcess()
    {
        _metricsService.DecrementAttachedProcesses();
    }

    #endregion

    #region Error Tracking

    /// <summary>
    /// Records an error with type and component information.
    /// </summary>
    public void RecordError(string errorType, string component)
    {
        _metricsService.RecordError(errorType, component);
    }

    /// <summary>
    /// Records a warning with type and component information.
    /// </summary>
    public void RecordWarning(string warningType, string component)
    {
        _metricsService.RecordWarning(warningType, component);
    }

    #endregion

    public void Dispose()
    {
        lock (_lock)
        {
            _operationTimers.Clear();
        }
    }

    /// <summary>
    /// Helper class for timed operations.
    /// </summary>
    private class TimedOperation : IDisposable
    {
        private readonly IMetricsService _metricsService;
        private readonly string _gameName;
        private readonly Func<IMetricsService, string, Action<TimeSpan, string>> _getRecorder;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public TimedOperation(
            IMetricsService metricsService,
            string gameName,
            Func<IMetricsService, string, Action<TimeSpan, string>> getRecorder)
        {
            _metricsService = metricsService;
            _gameName = gameName;
            _getRecorder = getRecorder;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _stopwatch.Stop();
            var recorder = _getRecorder(_metricsService, _gameName);
            recorder(_stopwatch.Elapsed, _gameName);
            _disposed = true;
        }
    }
}

/// <summary>
/// Extension methods for IMetricsService to provide game-specific functionality.
/// </summary>
public static class GameMetricsExtensions
{
    /// <summary>
    /// Creates a GameMetrics wrapper around the metrics service.
    /// </summary>
    public static GameMetrics ForGames(this IMetricsService metricsService)
    {
        return new GameMetrics(metricsService);
    }
}
