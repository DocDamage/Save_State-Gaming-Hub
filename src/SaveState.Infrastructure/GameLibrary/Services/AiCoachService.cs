using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// AI-powered coaching service for gaming improvement.
/// Provides real-time feedback, strategy suggestions, and performance analysis.
/// Acts as a coordinator for specialized coaching engines.
/// </summary>
public sealed class AiCoachService : IAiCoachService
{
    private readonly ICoachingEngine _coachingEngine;
    private readonly IAnalysisEngine _analysisEngine;
    private readonly IAiRecommendationEngine _recommendationEngine;
    private readonly ITipGenerationEngine _tipGenerationEngine;
    private readonly IFeedbackEngine _feedbackEngine;
    private readonly ILogger<AiCoachService> _logger;

    public AiCoachService(
        ICoachingEngine coachingEngine,
        IAnalysisEngine analysisEngine,
        IAiRecommendationEngine recommendationEngine,
        ITipGenerationEngine tipGenerationEngine,
        IFeedbackEngine feedbackEngine,
        ILogger<AiCoachService> logger)
    {
        _coachingEngine = coachingEngine;
        _analysisEngine = analysisEngine;
        _recommendationEngine = recommendationEngine;
        _tipGenerationEngine = tipGenerationEngine;
        _feedbackEngine = feedbackEngine;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<CoachingSession>> StartCoachingSessionAsync(
        Guid gameId,
        CoachingPreferences preferences,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting AI coaching session for game {GameId} with style {Style}",
                gameId, preferences.Style);

            var result = await _coachingEngine.CreateSessionAsync(gameId, preferences, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "AI coaching session {SessionId} started successfully",
                    result.Value.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting coaching session for game {GameId}", gameId);
            return Result.Failure<CoachingSession>(
                $"Failed to start coaching session: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> EndCoachingSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Ending AI coaching session {SessionId}", sessionId);

            var result = await _coachingEngine.EndSessionAsync(sessionId, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "AI coaching session {SessionId} ended successfully",
                    sessionId);
                return Result.Success();
            }

            return Result.Failure(result.Error ?? "Failed to end session", result.ErrorType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending coaching session {SessionId}", sessionId);
            return Result.Failure(
                $"Failed to end coaching session: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CoachingFeedback>> GetRealTimeFeedbackAsync(
        Guid sessionId,
        GameStateSnapshot gameState,
        CancellationToken ct = default)
    {
        try
        {
            var sessionResult = _coachingEngine.GetSession(sessionId);
            if (!sessionResult.IsSuccess)
            {
                return Result.Failure<CoachingFeedback>(
                    sessionResult.Error ?? "Session not found",
                    sessionResult.ErrorType);
            }

            var session = sessionResult.Value;

            if (!_feedbackEngine.IsRealTimeFeedbackEnabled(session))
            {
                return Result.Success(new CoachingFeedback(
                    FeedbackType.Encouragement,
                    "Real-time feedback is disabled for this session.",
                    FeedbackPriority.Low,
                    Array.Empty<string>(),
                    new Dictionary<string, object>()));
            }

            var metrics = new SessionMetrics(
                session.Id,
                session.StartedAt,
                DateTime.UtcNow - session.StartedAt,
                0, 0, 0, 0.5);

            return await _feedbackEngine.GenerateRealTimeFeedbackAsync(
                session, gameState, metrics, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating real-time feedback for session {SessionId}", sessionId);
            return Result.Failure<CoachingFeedback>(
                $"Failed to generate feedback: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<StrategyAnalysis>> AnalyzePlayerStrategyAsync(
        Guid sessionId,
        IReadOnlyList<GameAction> recentActions,
        CancellationToken ct = default)
    {
        try
        {
            var sessionResult = _coachingEngine.GetSession(sessionId);
            if (!sessionResult.IsSuccess)
            {
                return Result.Failure<StrategyAnalysis>(
                    sessionResult.Error ?? "Session not found",
                    sessionResult.ErrorType);
            }

            var session = sessionResult.Value;

            if (!session.Preferences.EnableStrategyAnalysis)
            {
                return Result.Failure<StrategyAnalysis>(
                    "Strategy analysis is disabled for this session",
                    ErrorType.Validation);
            }

            return await _analysisEngine.AnalyzeStrategyAsync(session, recentActions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing player strategy for session {SessionId}", sessionId);
            return Result.Failure<StrategyAnalysis>(
                $"Failed to analyze strategy: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<OpponentAnalysis>> AnalyzeOpponentPatternsAsync(
        Guid sessionId,
        IReadOnlyList<GameAction> opponentActions,
        CancellationToken ct = default)
    {
        try
        {
            var sessionResult = _coachingEngine.GetSession(sessionId);
            if (!sessionResult.IsSuccess)
            {
                return Result.Failure<OpponentAnalysis>(
                    sessionResult.Error ?? "Session not found",
                    sessionResult.ErrorType);
            }

            var session = sessionResult.Value;

            if (!session.Preferences.EnableOpponentAnalysis)
            {
                return Result.Failure<OpponentAnalysis>(
                    "Opponent analysis is disabled for this session",
                    ErrorType.Validation);
            }

            return await _analysisEngine.AnalyzeOpponentAsync(session, opponentActions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing opponent patterns for session {SessionId}", sessionId);
            return Result.Failure<OpponentAnalysis>(
                $"Failed to analyze opponent: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<SkillAssessment>> AssessPlayerSkillAsync(
        Guid sessionId,
        SessionMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            var sessionResult = _coachingEngine.GetSession(sessionId);
            if (!sessionResult.IsSuccess)
            {
                return Result.Failure<SkillAssessment>(
                    sessionResult.Error ?? "Session not found",
                    sessionResult.ErrorType);
            }

            return await _recommendationEngine.AssessSkillAsync(sessionResult.Value, metrics, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing player skill for session {SessionId}", sessionId);
            return Result.Failure<SkillAssessment>(
                $"Failed to assess skill: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ImprovementPlan>> GenerateImprovementPlanAsync(
        Guid sessionId,
        SkillAssessment assessment,
        CancellationToken ct = default)
    {
        try
        {
            var sessionResult = _coachingEngine.GetSession(sessionId);
            if (!sessionResult.IsSuccess)
            {
                return Result.Failure<ImprovementPlan>(
                    sessionResult.Error ?? "Session not found",
                    sessionResult.ErrorType);
            }

            return await _recommendationEngine.CreateImprovementPlanAsync(
                sessionResult.Value, assessment, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating improvement plan for session {SessionId}", sessionId);
            return Result.Failure<ImprovementPlan>(
                $"Failed to generate plan: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CoachingTip>>> GetContextualTipsAsync(
        Guid sessionId,
        string context,
        CancellationToken ct = default)
    {
        try
        {
            var sessionResult = _coachingEngine.GetSession(sessionId);
            if (!sessionResult.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<CoachingTip>>(
                    sessionResult.Error ?? "Session not found",
                    sessionResult.ErrorType);
            }

            return await _tipGenerationEngine.GenerateContextualTipsAsync(
                sessionResult.Value, context, 5, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contextual tips for session {SessionId}", sessionId);
            return Result.Failure<IReadOnlyList<CoachingTip>>(
                $"Failed to get tips: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CoachingReport>> GenerateSessionReportAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            var sessionResult = _coachingEngine.GetSession(sessionId);
            if (!sessionResult.IsSuccess)
            {
                return Result.Failure<CoachingReport>(
                    sessionResult.Error ?? "Session not found",
                    sessionResult.ErrorType);
            }

            return await _coachingEngine.CompileSessionReportAsync(sessionResult.Value, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session report for session {SessionId}", sessionId);
            return Result.Failure<CoachingReport>(
                $"Failed to generate report: {ex.Message}",
                ErrorType.Internal);
        }
    }
}
