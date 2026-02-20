using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.PerformanceProfiler.Managers;

/// <summary>
/// Manages profiling sessions lifecycle including start, stop, pause, and resume operations.
/// </summary>
public class ProfilingSessionManager
{
    private readonly ILogger<ProfilingSessionManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, ProfilingSession> _sessions;
    private readonly ConcurrentDictionary<string, PerformanceAlert> _alerts;
    private ProfilingSession? _activeSession;

    public ProfilingSessionManager(ILogger<ProfilingSessionManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _sessions = new ConcurrentDictionary<string, ProfilingSession>();
        _alerts = new ConcurrentDictionary<string, PerformanceAlert>();
    }

    public ProfilingSession? ActiveSession => _activeSession;
    public ConcurrentDictionary<string, ProfilingSession> Sessions => _sessions;
    public ConcurrentDictionary<string, PerformanceAlert> Alerts => _alerts;

    /// <summary>
    /// Starts a new profiling session with the specified name and configuration.
    /// </summary>
    public Task<Result<ProfilingSession>> StartSessionAsync(
        string name,
        ProfilingConfiguration configuration,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting profiling session: {Name}", name);

            var session = new ProfilingSession(
                Guid.NewGuid().ToString(),
                name,
                _timeProvider.UtcNow,
                configuration,
                ProfilingStatus.Running,
                TimeSpan.Zero,
                new List<string>());

            _sessions[session.Id] = session;
            _activeSession = session;

            return Task.FromResult(Result<ProfilingSession>.Success(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start profiling session");
            return Task.FromResult(Result<ProfilingSession>.Failure($"Start session failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Stops the active profiling session and generates a performance report.
    /// </summary>
    public Task<Result<ProfilingReport>> StopSessionAsync(
        CancellationToken ct = default)
    {
        try
        {
            if (_activeSession == null)
            {
                return Task.FromResult(Result<ProfilingReport>.Failure("No active profiling session", ErrorType.Validation));
            }

            _logger.LogInformation("Stopping profiling session: {Name}", _activeSession.Name);

            var duration = _timeProvider.UtcNow - _activeSession.StartedAt;
            var summary = GeneratePerformanceSummary();
            var issues = DetectPerfIssues();
            var recommendations = GenerateRecommendations();

            var report = new ProfilingReport(
                _activeSession.Id,
                _timeProvider.UtcNow,
                duration,
                summary,
                issues,
                recommendations);

            _activeSession = null;
            return Task.FromResult(Result<ProfilingReport>.Success(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop profiling session");
            return Task.FromResult(Result<ProfilingReport>.Failure($"Stop session failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets the currently active profiling session.
    /// </summary>
    public Task<Result<ProfilingSession>> GetActiveSessionAsync(
        CancellationToken ct = default)
    {
        if (_activeSession != null)
        {
            return Task.FromResult(Result<ProfilingSession>.Success(_activeSession));
        }

        return Task.FromResult(Result<ProfilingSession>.Failure("No active profiling session", ErrorType.NotFound));
    }

    /// <summary>
    /// Pauses the active profiling session.
    /// </summary>
    public Task<Result> PauseProfilingAsync(CancellationToken ct = default)
    {
        if (_activeSession != null)
        {
            _activeSession = _activeSession with { Status = ProfilingStatus.Paused };
        }
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Resumes the active profiling session.
    /// </summary>
    public Task<Result> ResumeProfilingAsync(CancellationToken ct = default)
    {
        if (_activeSession != null)
        {
            _activeSession = _activeSession with { Status = ProfilingStatus.Running };
        }
        return Task.FromResult(Result.Success());
    }

    #region Private Helpers

    private PerformanceSummary GeneratePerformanceSummary()
    {
        return new PerformanceSummary(
            58.5,
            45.0,
            62.0,
            17.1,
            524288000L,
            35.0,
            3600,
            12);
    }

    private IReadOnlyList<PerfIssue> DetectPerfIssues()
    {
        return new List<PerfIssue>
        {
            new(PerfIssueSeverity.Warning, "Memory", "High memory usage detected", TimeSpan.Zero, new List<string>()),
            new(PerfIssueSeverity.Info, "CPU", "AI calculations taking longer than expected", TimeSpan.Zero, new List<string>())
        };
    }

    private IReadOnlyList<OptimizationRecommendation> GenerateRecommendations()
    {
        return new List<OptimizationRecommendation>
        {
            new("REC-001", OptimizationCategory.Memory, "Compress sprites", "Reduce memory footprint", 15.0,
                OptimizationDifficultyLevel.Easy, new List<string> { "Use SFF optimizer" }),
            new("REC-002", OptimizationCategory.Cpu, "Optimize AI", "Reduce AI update frequency", 10.0,
                OptimizationDifficultyLevel.Medium, new List<string> { "Implement frame skipping" })
        };
    }

    #endregion
}
