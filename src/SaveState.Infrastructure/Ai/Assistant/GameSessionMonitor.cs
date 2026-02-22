using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Assistant;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SuggestedDifficulty = SaveState.Core.Assistant.Services.SuggestedDifficulty;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.Ai.Assistant;

/// <summary>
/// Background service that monitors game sessions in real-time.
/// Emits notifications for difficulty suggestions, break reminders, and coaching tips.
/// </summary>
public sealed class GameSessionMonitor : BackgroundService, IGameSessionMonitor
{
    private readonly ILogger<GameSessionMonitor> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IDifficultyAnalyzer _difficultyAnalyzer;
    private readonly IEyeTrackingMonitor? _eyeTrackingMonitor;
    
    private readonly ConcurrentDictionary<Guid, MonitoredSession> _activeSessions = new();
    private readonly ConcurrentDictionary<Guid, SessionMetrics> _sessionMetrics = new();
    private readonly ConcurrentDictionary<Guid, List<GameplayEvent>> _sessionEvents = new();
    
    // Configuration
    private readonly TimeSpan _analysisInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _breakReminderInterval = TimeSpan.FromMinutes(60);
    private readonly int _difficultySuggestionCooldownMinutes = 10;
    
    // Events
    public event EventHandler<DifficultySuggestionEventArgs>? DifficultySuggestionReceived;
    public event EventHandler<BreakReminderEventArgs>? BreakReminderTriggered;
    public event EventHandler<SessionMetricsUpdatedEventArgs>? SessionMetricsUpdated;

    public GameSessionMonitor(
        ILogger<GameSessionMonitor> logger,
        ITimeProvider timeProvider,
        IDifficultyAnalyzer difficultyAnalyzer,
        IEyeTrackingMonitor? eyeTrackingMonitor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _difficultyAnalyzer = difficultyAnalyzer ?? throw new ArgumentNullException(nameof(difficultyAnalyzer));
        _eyeTrackingMonitor = eyeTrackingMonitor;
    }

    #region IGameSessionMonitor Implementation

    /// <inheritdoc />
    public Task<Result> StartSessionAsync(Guid sessionId, Guid gameId, CancellationToken ct = default)
    {
        if (_activeSessions.ContainsKey(sessionId))
        {
            return Task.FromResult(Result.Failure(
                $"Session {sessionId} is already being monitored.",
                ErrorType.Validation));
        }

        var nowUtc = _timeProvider.UtcNow;
        var session = new MonitoredSession
        {
            SessionId = sessionId,
            GameId = gameId,
            StartTimeUtc = nowUtc,
            IsActive = true,
            LastAnalysisAtUtc = nowUtc,
            LastBreakReminderAtUtc = nowUtc
        };

        _activeSessions[sessionId] = session;
        _sessionMetrics[sessionId] = new SessionMetrics(
            CurrentActionsPerMinute: 0,
            AverageErrorRate: 0,
            HasRecentRapidBursts: false,
            HasRecentIdleSpikes: false,
            TimeSinceLastDeath: TimeSpan.Zero,
            PauseCount: 0,
            TotalPausedTime: TimeSpan.Zero);
        _sessionEvents[sessionId] = new List<GameplayEvent>();

        _logger.LogInformation(
            "Started monitoring session {SessionId} for game {GameId}",
            sessionId,
            gameId);

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(Result.Failure(
                $"Session {sessionId} not found.",
                ErrorType.NotFound));
        }

        session.IsActive = false;
        session.EndTimeUtc = _timeProvider.UtcNow;

        // Cleanup after a delay to allow final analysis
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            _activeSessions.TryRemove(sessionId, out _);
            _sessionMetrics.TryRemove(sessionId, out _);
            _sessionEvents.TryRemove(sessionId, out _);
        }, ct);

        _logger.LogInformation(
            "Ended monitoring session {SessionId}. Duration: {Duration}",
            sessionId,
            session.EndTimeUtc - session.StartTimeUtc);

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public async Task<Result> RecordEventAsync(Guid sessionId, GameplayEvent gameEvent, CancellationToken ct = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return Result.Failure(
                $"Session {sessionId} not found.",
                ErrorType.NotFound);
        }

        if (!session.IsActive)
        {
            return Result.Failure(
                $"Session {sessionId} is not active.",
                ErrorType.Validation);
        }

        // Store the event
        if (_sessionEvents.TryGetValue(sessionId, out var events))
        {
            lock (events)
            {
                events.Add(gameEvent);
            }
        }

        // Update session state based on event type
        UpdateSessionFromEvent(session, gameEvent);

        // Update metrics
        UpdateMetrics(sessionId);

        // Run immediate checks so high-signal events can trigger suggestions without waiting
        // for the background poll interval.
        var nowUtc = _timeProvider.UtcNow;
        await CheckDifficultyAdjustmentAsync(session, nowUtc, ct).ConfigureAwait(false);
        await CheckBreakReminderAsync(session, nowUtc, ct).ConfigureAwait(false);
        EmitMetricsUpdate(sessionId, nowUtc);

        return Result.Success();
    }

    /// <inheritdoc />
    public Task<Result<SessionState>> GetSessionStateAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(Result.Failure<SessionState>(
                $"Session {sessionId} not found.",
                ErrorType.NotFound));
        }

        var metrics = _sessionMetrics.TryGetValue(sessionId, out var m) ? m : new SessionMetrics(
            0, 0, false, false, TimeSpan.Zero, 0, TimeSpan.Zero);

        var state = new SessionState(
            session.SessionId,
            session.GameId,
            session.StartTimeUtc,
            session.EndTimeUtc,
            session.DeathCount,
            session.RetryCount,
            GetTotalPlayTime(session),
            session.TimeInCurrentSection,
            metrics,
            session.IsActive);

        return Task.FromResult(Result.Success(state));
    }

    #endregion

    /// <summary>
    /// Main monitoring loop executed as a background service.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Game session monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorActiveSessionsAsync(stoppingToken);
                await Task.Delay(_analysisInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in session monitoring loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Game session monitor stopped");
    }

    /// <summary>
    /// Monitors all active sessions and triggers analyses.
    /// </summary>
    private async Task MonitorActiveSessionsAsync(CancellationToken ct)
    {
        var nowUtc = _timeProvider.UtcNow;
        
        foreach (var (sessionId, session) in _activeSessions)
        {
            if (!session.IsActive)
            {
                continue;
            }

            try
            {
                // Update session metrics
                UpdateMetrics(sessionId);

                // Check for difficulty adjustment
                await CheckDifficultyAdjustmentAsync(session, nowUtc, ct);

                // Check for break reminder
                await CheckBreakReminderAsync(session, nowUtc, ct);

                // Emit metrics update event
                EmitMetricsUpdate(sessionId, nowUtc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring session {SessionId}", sessionId);
            }
        }
    }

    /// <summary>
    /// Checks if difficulty adjustment should be suggested.
    /// </summary>
    private async Task CheckDifficultyAdjustmentAsync(MonitoredSession session, DateTime nowUtc, CancellationToken ct)
    {
        // Minimum session duration before suggesting
        var sessionDuration = nowUtc - session.StartTimeUtc;
        if (sessionDuration < TimeSpan.FromMinutes(5))
        {
            return;
        }

        // Build player behavior metrics
        var metrics = BuildPlayerBehaviorMetrics(session, nowUtc);
        
        // Analyze
        var analysisResult = await _difficultyAnalyzer.AnalyzeAsync(metrics, ct);
        if (analysisResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to analyze difficulty for session {SessionId}: {Error}",
                session.SessionId,
                analysisResult.Error);
            return;
        }

        var analysis = analysisResult.Value;

        // Only suggest if confidence is high enough and not "Maintain"
        if (analysis.SuggestedDifficulty == SuggestedDifficulty.Maintain ||
            analysis.Confidence < 0.65f)
        {
            return;
        }

        // Keep evaluating continuously, but suppress duplicate suggestion events
        // during the cooldown period.
        if (session.LastDifficultySuggestionAtUtc.HasValue &&
            nowUtc - session.LastDifficultySuggestionAtUtc.Value < TimeSpan.FromMinutes(_difficultySuggestionCooldownMinutes))
        {
            return;
        }

        // Emit suggestion event
        session.LastDifficultySuggestionAtUtc = nowUtc;
        
        var eventArgs = new DifficultySuggestionEventArgs
        {
            SessionId = session.SessionId,
            GameId = session.GameId,
            SuggestedDifficulty = analysis.SuggestedDifficulty,
            Confidence = analysis.Confidence,
            Reasoning = analysis.Reasoning,
            ContributingFactors = analysis.ContributingFactors,
            TimestampUtc = nowUtc
        };

        try
        {
            DifficultySuggestionReceived?.Invoke(this, eventArgs);
            _logger.LogInformation(
                "Difficulty suggestion for session {SessionId}: {Suggestion} (confidence: {Confidence:P0})",
                session.SessionId,
                analysis.SuggestedDifficulty,
                analysis.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emitting difficulty suggestion event");
        }
    }

    /// <summary>
    /// Checks if break reminder should be triggered.
    /// </summary>
    private Task CheckBreakReminderAsync(MonitoredSession session, DateTime nowUtc, CancellationToken ct)
    {
        var sessionDuration = nowUtc - session.LastBreakReminderAtUtc;
        
        if (sessionDuration < _breakReminderInterval)
        {
            return Task.CompletedTask;
        }

        session.LastBreakReminderAtUtc = nowUtc;
        session.BreaksTaken++;

        var eventArgs = new BreakReminderEventArgs
        {
            SessionId = session.SessionId,
            GameId = session.GameId,
            SessionDuration = nowUtc - session.StartTimeUtc,
            BreaksTaken = session.BreaksTaken,
            Message = $"You've been playing for {(nowUtc - session.StartTimeUtc).TotalMinutes:F0} minutes. Consider taking a break!",
            TimestampUtc = nowUtc
        };

        try
        {
            BreakReminderTriggered?.Invoke(this, eventArgs);
            _logger.LogInformation(
                "Break reminder for session {SessionId} after {Duration:hh\\:mm}",
                session.SessionId,
                eventArgs.SessionDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emitting break reminder event");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates session state based on gameplay event.
    /// </summary>
    private void UpdateSessionFromEvent(MonitoredSession session, GameplayEvent gameEvent)
    {
        switch (gameEvent)
        {
            case DeathEvent death:
                session.DeathCount++;
                session.LastDeathAtUtc = death.TimestampUtc;
                session.TimeInCurrentSection = TimeSpan.Zero; // Reset section timer
                _logger.LogDebug(
                    "Session {SessionId}: Death recorded (total: {Total})",
                    session.SessionId,
                    session.DeathCount);
                break;

            case RetryEvent retry:
                session.RetryCount++;
                session.LastRetryAtUtc = retry.TimestampUtc;
                _logger.LogDebug(
                    "Session {SessionId}: Retry recorded (total: {Total})",
                    session.SessionId,
                    session.RetryCount);
                break;

            case InputSampleEvent input:
                session.RecentApmSamples.Enqueue(input.ActionsPerMinute);
                while (session.RecentApmSamples.Count > 10)
                {
                    session.RecentApmSamples.Dequeue();
                }
                break;

            case PauseEvent pause:
                if (pause.IsPaused)
                {
                    session.LastPauseStartedAtUtc = pause.TimestampUtc;
                }
                else if (pause.Duration.HasValue)
                {
                    session.TotalPausedTime += pause.Duration.Value;
                    session.PauseCount++;
                }
                break;
        }
    }

    /// <summary>
    /// Updates session metrics from current state.
    /// </summary>
    private void UpdateMetrics(Guid sessionId)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session) ||
            !_sessionMetrics.TryGetValue(sessionId, out var currentMetrics))
        {
            return;
        }

        var nowUtc = _timeProvider.UtcNow;
        var events = _sessionEvents.TryGetValue(sessionId, out var e) ? e : new List<GameplayEvent>();
        
        // Calculate metrics from recent events
        var recentEvents = GetRecentEvents(events, TimeSpan.FromMinutes(2));
        var inputEvents = recentEvents.OfType<InputSampleEvent>().ToList();
        
        var avgApm = inputEvents.Any() ? inputEvents.Average(i => i.ActionsPerMinute) : 0;
        var avgErrorRate = inputEvents.Any() ? inputEvents.Average(i => i.ErrorRate) : 0;
        var hasRapidBursts = inputEvents.Any(i => i.IsRapidBurst);
        var hasIdleSpikes = inputEvents.Any(i => i.IsIdleSpike);
        
        var timeSinceLastDeath = session.LastDeathAtUtc.HasValue
            ? nowUtc - session.LastDeathAtUtc.Value
            : nowUtc - session.StartTimeUtc;

        var updatedMetrics = new SessionMetrics(
            CurrentActionsPerMinute: (float)avgApm,
            AverageErrorRate: (float)avgErrorRate,
            HasRecentRapidBursts: hasRapidBursts,
            HasRecentIdleSpikes: hasIdleSpikes,
            TimeSinceLastDeath: timeSinceLastDeath,
            PauseCount: session.PauseCount,
            TotalPausedTime: session.TotalPausedTime);

        _sessionMetrics[sessionId] = updatedMetrics;
    }

    /// <summary>
    /// Emits metrics update event.
    /// </summary>
    private void EmitMetricsUpdate(Guid sessionId, DateTime timestampUtc)
    {
        if (!_sessionMetrics.TryGetValue(sessionId, out var metrics))
        {
            return;
        }

        var eventArgs = new SessionMetricsUpdatedEventArgs
        {
            SessionId = sessionId,
            Metrics = metrics,
            TimestampUtc = timestampUtc
        };

        try
        {
            SessionMetricsUpdated?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emitting metrics update event");
        }
    }

    /// <summary>
    /// Builds player behavior metrics for analysis.
    /// </summary>
    private PlayerBehaviorMetrics BuildPlayerBehaviorMetrics(MonitoredSession session, DateTime nowUtc)
    {
        var metrics = _sessionMetrics.TryGetValue(session.SessionId, out var m)
            ? m
            : new SessionMetrics(0, 0, false, false, TimeSpan.Zero, 0, TimeSpan.Zero);

        var events = _sessionEvents.TryGetValue(session.SessionId, out var e)
            ? e
            : new List<GameplayEvent>();

        var recentInputEvents = GetRecentEvents(events, TimeSpan.FromMinutes(5))
            .OfType<InputSampleEvent>()
            .ToList();

        return new PlayerBehaviorMetrics
        {
            SessionId = session.SessionId,
            GameId = session.GameId,
            SessionStartTimeUtc = session.StartTimeUtc,
            TimestampUtc = nowUtc,
            DeathCount = session.DeathCount,
            RetryCount = session.RetryCount,
            TimeInCurrentSection = session.TimeInCurrentSection,
            TotalSessionDuration = nowUtc - session.StartTimeUtc - session.TotalPausedTime,
            ActionsPerMinute = metrics.CurrentActionsPerMinute,
            InputErrorRate = metrics.AverageErrorRate,
            HasRapidInputBursts = metrics.HasRecentRapidBursts,
            HasIdleSpikes = metrics.HasRecentIdleSpikes,
            PauseCount = session.PauseCount,
            TotalPausedTime = session.TotalPausedTime,
            CurrentDifficultyLevel = null // Could be populated from game integration
        };
    }

    /// <summary>
    /// Gets events from the specified time window.
    /// </summary>
    private List<GameplayEvent> GetRecentEvents(List<GameplayEvent> events, TimeSpan window)
    {
        var cutoff = _timeProvider.UtcNow - window;
        lock (events)
        {
            return events.Where(e => e.TimestampUtc >= cutoff).ToList();
        }
    }

    /// <summary>
    /// Calculates total play time (excluding pauses).
    /// </summary>
    private TimeSpan GetTotalPlayTime(MonitoredSession session)
    {
        if (session.EndTimeUtc.HasValue)
        {
            return session.EndTimeUtc.Value - session.StartTimeUtc - session.TotalPausedTime;
        }
        return _timeProvider.UtcNow - session.StartTimeUtc - session.TotalPausedTime;
    }

    /// <summary>
    /// Internal representation of a monitored session.
    /// </summary>
    private class MonitoredSession
    {
        public Guid SessionId { get; set; }
        public Guid GameId { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public bool IsActive { get; set; }
        
        // Gameplay stats
        public int DeathCount { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan TimeInCurrentSection { get; set; }
        
        // Pause tracking
        public int PauseCount { get; set; }
        public TimeSpan TotalPausedTime { get; set; }
        public DateTime? LastPauseStartedAtUtc { get; set; }
        
        // Timestamps
        public DateTime? LastDeathAtUtc { get; set; }
        public DateTime? LastRetryAtUtc { get; set; }
        public DateTime LastAnalysisAtUtc { get; set; }
        public DateTime LastBreakReminderAtUtc { get; set; }
        public DateTime? LastDifficultySuggestionAtUtc { get; set; }
        
        // Break tracking
        public int BreaksTaken { get; set; }
        
        // Recent samples
        public Queue<float> RecentApmSamples { get; set; } = new();
    }
}
