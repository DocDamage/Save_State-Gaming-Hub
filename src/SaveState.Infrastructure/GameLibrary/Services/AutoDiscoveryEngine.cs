using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;
using static SaveState.Infrastructure.Logging.CorrelationIdExtensions;
using static SaveState.Infrastructure.Logging.LoggingExtensions;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// AI-powered memory pattern auto-discovery engine.
/// Automatically discovers game values without prior knowledge or signatures.
/// </summary>
public sealed class AutoDiscoveryEngine : IAutoDiscoveryEngine, IDisposable
{
    private readonly ILogger<AutoDiscoveryEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly DiscoverySessionManager _sessionManager;
    private readonly MemoryScanningManager _memoryScanningManager;
    private readonly HeuristicAnalysisManager _heuristicAnalysisManager;
    private readonly ChangeDetectionManager _changeDetectionManager;
    private readonly FeedbackLearningManager _feedbackLearningManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoDiscoveryEngine"/> class.
    /// </summary>
    public AutoDiscoveryEngine(
        ILogger<AutoDiscoveryEngine> logger,
        ITimeProvider timeProvider,
        DiscoverySessionManager sessionManager,
        MemoryScanningManager memoryScanningManager,
        HeuristicAnalysisManager heuristicAnalysisManager,
        ChangeDetectionManager changeDetectionManager,
        FeedbackLearningManager feedbackLearningManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _memoryScanningManager = memoryScanningManager ?? throw new ArgumentNullException(nameof(memoryScanningManager));
        _heuristicAnalysisManager = heuristicAnalysisManager ?? throw new ArgumentNullException(nameof(heuristicAnalysisManager));
        _changeDetectionManager = changeDetectionManager ?? throw new ArgumentNullException(nameof(changeDetectionManager));
        _feedbackLearningManager = feedbackLearningManager ?? throw new ArgumentNullException(nameof(feedbackLearningManager));

        _logger.LogInformation("AutoDiscoveryEngine initialized with manager pattern");
    }

    /// <inheritdoc />
    public Task<Result<DiscoverySession>> StartDiscoverySessionAsync(int processId, DiscoveryOptions options, CancellationToken ct = default)
    {
        return _sessionManager.StartSessionAsync(processId, options, ct);
    }

    /// <inheritdoc />
    public async Task<Result<DiscoveryResult>> AnalyzeChangeAsync(DiscoverySession session, PlayerAction action, CancellationToken ct = default)
    {
        using (_logger.BeginCorrelationScope())
        using (_logger.BeginDiscoveryAnalysisScope(action.ToString(), session.SessionId))
        {
            var beforeCount = session.Candidates.Count;
            
            _logger.LogInformation(
                "Analyzing player action {Action} in session {SessionId}. Candidates before: {CandidateCount}",
                action,
                session.SessionId,
                beforeCount);
                
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                if (session == null)
                    return Result.Failure<DiscoveryResult>("Session cannot be null", ErrorType.Validation);

                if (!session.IsActive)
                    return Result.Failure<DiscoveryResult>("Session is not active", ErrorType.Validation);

                var context = _sessionManager.GetSessionContext(session.SessionId);
                if (context == null)
                    return Result.Failure<DiscoveryResult>("Session not found", ErrorType.NotFound);

                // Record the action
                var actionRecord = new PlayerActionRecord
                {
                    Timestamp = _timeProvider.UtcNow,
                    Action = action
                };
                session.ActionHistory.Add(actionRecord);

                // Perform a scan pass
                await PerformDiscoveryPassAsync(session, context, action, ct).ConfigureAwait(false);

                // Apply heuristics and rank candidates
                var rankedCandidates = _heuristicAnalysisManager.ApplyHeuristicsAndRank(session);

                // Update session with top candidates
                session.Candidates.Clear();
                session.Candidates.AddRange(rankedCandidates.Take(session.Options.MaxCandidates));

                // Build result
                var afterCount = session.Candidates.Count;
                var topConfidence = rankedCandidates.FirstOrDefault()?.ConfidenceScore ?? 0;
                
                var result = new DiscoveryResult
                {
                    SessionId = session.SessionId,
                    AnalyzedAction = action,
                    RemainingCandidates = afterCount,
                    EliminatedCandidates = Math.Max(0, beforeCount - afterCount),
                    TopValues = rankedCandidates.Take(10).ToList(),
                    ConfidenceImproved = session.Candidates.Any(c => c.ConfidenceScore > 0.5)
                };

                stopwatch.Stop();
                
                _logger.LogInformation(
                    "Action analysis complete. Filtered from {BeforeCount} to {AfterCount} candidates in {ElapsedMs}ms. " +
                    "Top confidence: {TopConfidence:P}",
                    beforeCount,
                    afterCount,
                    stopwatch.ElapsedMilliseconds,
                    topConfidence);
                    
                return Result.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Action analysis failed for {Action} after {ElapsedMs}ms", action, stopwatch.ElapsedMilliseconds);
                return Result.Failure<DiscoveryResult>($"Failed to analyze change: {ex.Message}", ErrorType.Internal);
            }
        }
    }

    /// <inheritdoc />
    public Task<Result<List<DiscoveredValue>>> GetRankedResultsAsync(DiscoverySession session, CancellationToken ct = default)
    {
        try
        {
            if (session == null)
                return Task.FromResult(Result.Failure<List<DiscoveredValue>>("Session cannot be null", ErrorType.Validation));

            if (!session.IsActive)
                return Task.FromResult(Result.Failure<List<DiscoveredValue>>("Session is not active", ErrorType.Validation));

            _logger.LogDebug(
                "Getting ranked results for session {SessionId}. Threshold: {Threshold}, MaxResults: {MaxResults}",
                session.SessionId,
                session.Options.MinConfidenceThreshold,
                session.Options.MaxResults);

            // Return ranked results filtered by confidence threshold
            var results = _heuristicAnalysisManager.GetRankedResults(session);

            _logger.LogInformation(
                "Returning {Count} ranked results for session {SessionId}",
                results.Count,
                session.SessionId);

            return Task.FromResult(Result.Success(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ranked results for session {SessionId}", session?.SessionId);
            return Task.FromResult(Result.Failure<List<DiscoveredValue>>(
                $"Failed to get ranked results: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> StopDiscoverySessionAsync(DiscoverySession session, CancellationToken ct = default)
    {
        return _sessionManager.StopSessionAsync(session, ct);
    }

    /// <inheritdoc />
    public Task<Result> SubmitFeedbackAsync(DiscoveryFeedback feedback, CancellationToken ct = default)
    {
        return _feedbackLearningManager.SubmitFeedbackAsync(feedback, ct);
    }

    /// <summary>
    /// Performs a discovery pass - scans memory and updates candidates.
    /// </summary>
    private async Task PerformDiscoveryPassAsync(DiscoverySession session, DiscoverySessionContext context, PlayerAction action, CancellationToken ct)
    {
        session.CurrentPass++;
        _logger.LogDebug("Starting discovery pass {Pass} for session {SessionId}", session.CurrentPass, session.SessionId);

        // Pass 1: Initial scan (if first pass)
        if (session.CurrentPass == 1)
        {
            await _changeDetectionManager.PerformInitialScanAsync(session, context, ct).ConfigureAwait(false);
            
            // Apply initial heuristics to new candidates
            foreach (var candidate in session.Candidates)
            {
                _heuristicAnalysisManager.ApplyInitialHeuristicScoring(candidate);
            }
        }
        else
        {
            // Subsequent passes: monitor for changes
            await _changeDetectionManager.MonitorForChangesAsync(session, context, action, ct).ConfigureAwait(false);
        }

        // Small delay to prevent overwhelming the system
        await Task.Delay(session.Options.ScanIntervalMs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sessionManager.Dispose();
    }
}
