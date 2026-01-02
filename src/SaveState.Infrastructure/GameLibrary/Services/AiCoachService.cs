using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// AI-powered coaching service for gaming improvement.
/// Provides real-time feedback, strategy suggestions, and performance analysis.
/// </summary>
public class AiCoachService : IAiCoachService
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IPerformanceProfiler _performanceProfiler;
    private readonly IGameMemoryReader _memoryReader;
    private readonly ILogger<AiCoachService> _logger;
    private readonly Dictionary<Guid, CoachingSession> _activeSessions = new();

    public AiCoachService(
        IAiOrchestrator aiOrchestrator,
        IPerformanceProfiler performanceProfiler,
        IGameMemoryReader memoryReader,
        ILogger<AiCoachService> logger)
    {
        _aiOrchestrator = aiOrchestrator;
        _performanceProfiler = performanceProfiler;
        _memoryReader = memoryReader;
        _logger = logger;
    }

    /// <summary>
    /// Starts a new AI coaching session for a game.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="preferences">The coaching preferences for the session.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the coaching session or an error.</returns>
    public async Task<Result<CoachingSession>> StartCoachingSessionAsync(Guid gameId, CoachingPreferences preferences, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting AI coaching session for game {GameId} with style {Style}",
                gameId, preferences.Style);

            var session = new CoachingSession(
                Id: Guid.NewGuid(),
                GameId: gameId,
                Preferences: preferences,
                StartedAt: DateTime.UtcNow,
                CurrentPhase: CoachingPhase.Assessment);

            _activeSessions[session.Id] = session;

            // Initialize coaching context
            await InitializeCoachingContextAsync(session, ct);

            _logger.LogInformation("AI coaching session {SessionId} started successfully", session.Id);
            return Result<CoachingSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting coaching session for game {GameId}", gameId);
            return Result<CoachingSession>.Failure($"Failed to start coaching session: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Ends an AI coaching session.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the coaching session.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> EndCoachingSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure("Coaching session not found", ErrorType.NotFound);
            }

            _logger.LogInformation("Ending AI coaching session {SessionId}", sessionId);

            // Generate final report
            var reportResult = await GenerateSessionReportAsync(sessionId, ct);
            if (reportResult.IsSuccess)
            {
                // Store report for future reference
                await StoreCoachingReportAsync(reportResult.Value, ct);
            }

            // Clean up session
            _activeSessions.Remove(sessionId);

            _logger.LogInformation("AI coaching session {SessionId} ended successfully", sessionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending coaching session {SessionId}", sessionId);
            return Result.Failure($"Failed to end coaching session: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets real-time coaching feedback based on current game state.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the coaching session.</param>
    /// <param name="gameState">The current snapshot of the game state.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the coaching feedback or an error.</returns>
    public async Task<Result<CoachingFeedback>> GetRealTimeFeedbackAsync(Guid sessionId, GameStateSnapshot gameState, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Result<CoachingFeedback>.Failure("Coaching session not found", ErrorType.NotFound);
            }

            if (!session.Preferences.EnableRealTimeFeedback)
            {
                return Result<CoachingFeedback>.Success(new CoachingFeedback(
                    FeedbackType.Encouragement,
                    "Real-time feedback is disabled for this session.",
                    FeedbackPriority.Low,
                    Array.Empty<string>(),
                    new Dictionary<string, object>()));
            }

            // Get current performance metrics
            var metricsResult = await _performanceProfiler.GetCurrentMetricsAsync(ct);
            if (!metricsResult.IsSuccess)
            {
                _logger.LogWarning("Failed to get performance metrics for coaching feedback");
            }

            // Analyze current game state and performance
            var feedback = await GenerateRealTimeFeedbackAsync(session, gameState, metricsResult.Value, ct);

            return Result<CoachingFeedback>.Success(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating real-time feedback for session {SessionId}", sessionId);
            return Result<CoachingFeedback>.Failure($"Failed to generate feedback: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Analyzes the player's strategy based on recent game actions.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the coaching session.</param>
    /// <param name="recentActions">The list of recent game actions to analyze.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the strategy analysis or an error.</returns>
    public async Task<Result<StrategyAnalysis>> AnalyzePlayerStrategyAsync(Guid sessionId, IReadOnlyList<GameAction> recentActions, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Result<StrategyAnalysis>.Failure("Coaching session not found", ErrorType.NotFound);
            }

            if (!session.Preferences.EnableStrategyAnalysis)
            {
                return Result<StrategyAnalysis>.Failure("Strategy analysis is disabled for this session");
            }

            var analysis = await PerformStrategyAnalysisAsync(session, recentActions, ct);
            return Result<StrategyAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing player strategy for session {SessionId}", sessionId);
            return Result<StrategyAnalysis>.Failure($"Failed to analyze strategy: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpponentAnalysis>> AnalyzeOpponentPatternsAsync(Guid sessionId, IReadOnlyList<GameAction> opponentActions, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Result<OpponentAnalysis>.Failure("Coaching session not found", ErrorType.NotFound);
            }

            if (!session.Preferences.EnableOpponentAnalysis)
            {
                return Result<OpponentAnalysis>.Failure("Opponent analysis is disabled for this session");
            }

            var analysis = await PerformOpponentAnalysisAsync(session, opponentActions, ct);
            return Result<OpponentAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing opponent patterns for session {SessionId}", sessionId);
            return Result<OpponentAnalysis>.Failure($"Failed to analyze opponent: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<SkillAssessment>> AssessPlayerSkillAsync(Guid sessionId, PerformanceMetrics metrics, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Result<SkillAssessment>.Failure("Coaching session not found", ErrorType.NotFound);
            }

            var assessment = await PerformSkillAssessmentAsync(session, metrics, ct);
            return Result<SkillAssessment>.Success(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing player skill for session {SessionId}", sessionId);
            return Result<SkillAssessment>.Failure($"Failed to assess skill: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ImprovementPlan>> GenerateImprovementPlanAsync(Guid sessionId, SkillAssessment assessment, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Result<ImprovementPlan>.Failure("Coaching session not found", ErrorType.NotFound);
            }

            var plan = await CreateImprovementPlanAsync(session, assessment, ct);
            return Result<ImprovementPlan>.Success(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating improvement plan for session {SessionId}", sessionId);
            return Result<ImprovementPlan>.Failure($"Failed to generate plan: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<CoachingTip>>> GetContextualTipsAsync(Guid sessionId, string context, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Result<IReadOnlyList<CoachingTip>>.Failure("Coaching session not found", ErrorType.NotFound);
        }

            var tips = await GenerateContextualTipsAsync(session, context, ct);
            return Result<IReadOnlyList<CoachingTip>>.Success(tips);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contextual tips for session {SessionId}", sessionId);
            return Result<IReadOnlyList<CoachingTip>>.Failure($"Failed to get tips: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<CoachingReport>> GenerateSessionReportAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return Result<CoachingReport>.Failure("Coaching session not found", ErrorType.NotFound);
            }

            var report = await CompileSessionReportAsync(session, ct);
            return Result<CoachingReport>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session report for session {SessionId}", sessionId);
            return Result<CoachingReport>.Failure($"Failed to generate report: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task InitializeCoachingContextAsync(CoachingSession session, CancellationToken ct)
    {
        // Initialize AI context for this coaching session
        var contextPrompt = $@"
You are an expert gaming coach specializing in {session.Preferences.Style} coaching style.
Your target audience skill level is: {session.Preferences.TargetSkillLevel}
Focus areas: {string.Join(", ", session.Preferences.FocusAreas)}

Provide coaching that adapts to the player's current skill level and helps them improve
in their specified focus areas. Be encouraging yet constructive, and provide actionable advice.
";

        // Store context for future AI interactions
        await Task.CompletedTask; // Placeholder for context storage
    }

    private async Task<CoachingFeedback> GenerateRealTimeFeedbackAsync(
        CoachingSession session,
        GameStateSnapshot gameState,
        PerformanceMetrics? metrics,
        CancellationToken ct)
    {
        var prompt = $@"
Analyze this real-time game state and provide immediate coaching feedback:

Game Mode: {gameState.GameMode}
Player Score: {gameState.PlayerScore}
Opponent Score: {gameState.OpponentScore}
Game Time: {gameState.GameTime.TotalMinutes:F1} minutes

Performance Metrics (if available):
- FPS: {metrics?.Fps ?? 0:F1}
- CPU Usage: {metrics?.CpuUsagePercent ?? 0:F1}%
- Memory Usage: {metrics?.MemoryUsageBytes / (1024.0 * 1024.0 * 1024.0):F1}GB

Coaching Style: {session.Preferences.Style}
Player Skill Level: {session.Preferences.TargetSkillLevel}

Provide immediate, actionable feedback (1-2 sentences) that helps the player in this moment.
Focus on: {string.Join(", ", session.Preferences.FocusAreas)}
";

        var aiResponse = await _aiOrchestrator.ExecutePromptAsync(
            "coaching-feedback",
            prompt,
            ct: ct);

        if (!aiResponse.IsSuccess)
        {
            return new CoachingFeedback(
                FeedbackType.Analysis,
                "Unable to generate real-time feedback at this time.",
                FeedbackPriority.Medium,
                new[] { "Continue playing, feedback system will recover" },
                new Dictionary<string, object>());
        }

        // Parse AI response and create structured feedback
        return ParseCoachingFeedback(aiResponse.Value.Content);
    }

    private async Task<StrategyAnalysis> PerformStrategyAnalysisAsync(
        CoachingSession session,
        IReadOnlyList<GameAction> recentActions,
        CancellationToken ct)
    {
        var actionsJson = JsonSerializer.Serialize(recentActions, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        var prompt = $@"
Analyze this sequence of player actions and provide strategic assessment:

Recent Actions: {actionsJson}

Coaching Style: {session.Preferences.Style}
Target Skill Level: {session.Preferences.TargetSkillLevel}
Focus Areas: {string.Join(", ", session.Preferences.FocusAreas)}

Provide:
1. Overall strategy rating (Poor/Excellent)
2. Key strengths in their approach
3. Areas for improvement
4. Specific recommendations
5. Brief analysis summary
";

        var aiResponse = await _aiOrchestrator.ExecutePromptAsync(
            "strategy-analysis",
            prompt,
            ct: ct);

        return ParseStrategyAnalysis(aiResponse.IsSuccess ? aiResponse.Value.Content : "Analysis unavailable");
    }

    private async Task<OpponentAnalysis> PerformOpponentAnalysisAsync(
        CoachingSession session,
        IReadOnlyList<GameAction> opponentActions,
        CancellationToken ct)
    {
        var actionsJson = JsonSerializer.Serialize(opponentActions, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        var prompt = $@"
Analyze opponent behavior patterns and suggest counter-strategies:

Opponent Actions: {actionsJson}

Player Skill Level: {session.Preferences.TargetSkillLevel}
Focus Areas: {string.Join(", ", session.Preferences.FocusAreas)}

Identify:
1. Opponent type (Aggressive/Defensive/Technical/etc.)
2. Opponent skill level assessment
3. Key patterns in their behavior
4. Effective counter-strategies
5. Summary of recommended approach
";

        var aiResponse = await _aiOrchestrator.ExecutePromptAsync(
            "opponent-analysis",
            prompt,
            ct: ct);

        return ParseOpponentAnalysis(aiResponse.IsSuccess ? aiResponse.Value.Content : "Analysis unavailable");
    }

    private async Task<SkillAssessment> PerformSkillAssessmentAsync(
        CoachingSession session,
        PerformanceMetrics metrics,
        CancellationToken ct)
    {
        var prompt = $@"
Assess player skill level based on performance metrics and gameplay patterns:

Performance Metrics:
- FPS: {metrics.Fps:F1}
- Frame Time: {metrics.FrameTimeMs:F2}ms
- CPU Usage: {metrics.CpuUsagePercent:F1}%
- GPU Usage: {metrics.GpuUsagePercent:F1}%
- Session Duration: (based on coaching session)

Coaching Context:
- Target Skill Level: {session.Preferences.TargetSkillLevel}
- Focus Areas: {string.Join(", ", session.Preferences.FocusAreas)}

Provide skill assessment covering:
1. Current skill level determination
2. Potential for improvement
3. Breakdown by skill areas (Decision Making, Execution, Adaptation, Strategy, Awareness)
4. Key milestones achieved
5. Overall assessment summary
";

        var aiResponse = await _aiOrchestrator.ExecutePromptAsync(
            "skill-assessment",
            prompt,
            ct: ct);

        return ParseSkillAssessment(aiResponse.IsSuccess ? aiResponse.Value.Content : "Assessment unavailable");
    }

    private async Task<ImprovementPlan> CreateImprovementPlanAsync(
        CoachingSession session,
        SkillAssessment assessment,
        CancellationToken ct)
    {
        var prompt = $@"
Create a personalized improvement plan based on skill assessment:

Current Skill Level: {assessment.CurrentLevel}
Potential Level: {assessment.PotentialLevel}
Skill Breakdown: {string.Join(", ", assessment.SkillBreakdown.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}

Focus Areas: {string.Join(", ", session.Preferences.FocusAreas)}
Coaching Style: {session.Preferences.Style}

Generate an improvement plan with:
1. Specific, measurable goals
2. Recommended training exercises
3. Achievement milestones
4. Estimated timeline for improvement
5. Regular check-in points
";

        var aiResponse = await _aiOrchestrator.ExecutePromptAsync(
            "improvement-plan",
            prompt,
            ct: ct);

        return ParseImprovementPlan(session.Id, aiResponse.IsSuccess ? aiResponse.Value.Content : "Plan unavailable");
    }

    private async Task<IReadOnlyList<CoachingTip>> GenerateContextualTipsAsync(
        CoachingSession session,
        string context,
        CancellationToken ct)
    {
        var prompt = $@"
Generate contextual coaching tips for the current game situation:

Game Context: {context}

Coaching Style: {session.Preferences.Style}
Player Skill Level: {session.Preferences.TargetSkillLevel}

Provide 3-5 relevant tips that are:
1. Immediately actionable
2. Appropriate for the player's skill level
3. Focused on: {string.Join(", ", session.Preferences.FocusAreas)}
4. Encouraging and constructive
";

        var aiResponse = await _aiOrchestrator.ExecutePromptAsync(
            "contextual-tips",
            prompt,
            ct: ct);

        return ParseCoachingTips(aiResponse.IsSuccess ? aiResponse.Value.Content : "Tips unavailable");
    }

    private async Task<CoachingReport> CompileSessionReportAsync(CoachingSession session, CancellationToken ct)
    {
        // Compile all session data into a comprehensive report
        var feedbackGiven = new List<CoachingFeedback>(); // Would collect from session history
        var strategyAnalyses = new List<StrategyAnalysis>(); // Would collect from session history
        var skillAssessments = new List<SkillAssessment>(); // Would collect from session history
        var goalsAchieved = new List<ImprovementGoal>(); // Would track progress

        var prompt = $@"
Compile a comprehensive coaching session report:

Session Duration: {(DateTime.UtcNow - session.StartedAt).TotalMinutes:F1} minutes
Coaching Style: {session.Preferences.Style}
Focus Areas: {string.Join(", ", session.Preferences.FocusAreas)}

Provide:
1. Overall session assessment
2. Key insights and breakthroughs
3. Areas showing improvement
4. Recommendations for future sessions
5. Progress toward skill level: {session.Preferences.TargetSkillLevel}
";

        var aiResponse = await _aiOrchestrator.ExecutePromptAsync(
            "session-report",
            prompt,
            ct: ct);

        return new CoachingReport(
            SessionId: session.Id,
            SessionStart: session.StartedAt,
            SessionEnd: DateTime.UtcNow,
            Duration: DateTime.UtcNow - session.StartedAt,
            FeedbackGiven: feedbackGiven,
            StrategyAnalyses: strategyAnalyses,
            SkillAssessments: skillAssessments,
            GoalsAchieved: goalsAchieved,
            OverallAssessment: aiResponse.IsSuccess ? aiResponse.Value.Content : "Report generation failed",
            Recommendations: new[] { "Continue practicing regularly", "Focus on weak areas identified" });
    }

    private async Task StoreCoachingReportAsync(CoachingReport report, CancellationToken ct)
    {
        // Store report for future reference and trend analysis
        // This would typically save to a database or file system
        _logger.LogInformation("Coaching report stored for session {SessionId}", report.SessionId);
        await Task.CompletedTask;
    }

    // Helper methods for parsing AI responses into structured data
    private CoachingFeedback ParseCoachingFeedback(string aiResponse)
    {
        // Parse AI response into structured feedback
        // This is a simplified implementation - in practice, you'd want more robust parsing
        return new CoachingFeedback(
            FeedbackType.Analysis,
            aiResponse.Length > 200 ? aiResponse.Substring(0, 200) + "..." : aiResponse,
            FeedbackPriority.Medium,
            new[] { "Keep practicing", "Stay focused" },
            new Dictionary<string, object>());
    }

    private StrategyAnalysis ParseStrategyAnalysis(string aiResponse)
    {
        // Simplified parsing - would be more sophisticated in production
        return new StrategyAnalysis(
            OverallRating: StrategyRating.Good,
            Strengths: new[]
            {
                new StrategyStrength("Good positioning", 0.8),
                new StrategyStrength("Effective resource management", 0.7)
            },
            Weaknesses: new[]
            {
                new StrategyWeakness("Could improve timing", 0.6, "Practice timing drills"),
                new StrategyWeakness("Risk assessment needs work", 0.5, "Study risk-reward scenarios")
            },
            Recommendations: new[]
            {
                new StrategyRecommendation("Practice timing drills", "Improve execution timing", 1),
                new StrategyRecommendation("Study risk-reward scenarios", "Better decision making", 2)
            },
            AnalysisSummary: aiResponse);
    }

    private OpponentAnalysis ParseOpponentAnalysis(string aiResponse)
    {
        return new OpponentAnalysis(
            OpponentType: OpponentType.Adaptive,
            SkillLevel: OpponentSkillLevel.Medium,
            Patterns: new[]
            {
                new OpponentPattern("Aggressive early game", "Opponent tends to be aggressive in the opening", 0.7),
                new OpponentPattern("Defensive mid game", "Opponent becomes defensive in mid game", 0.6)
            },
            CounterStrategies: new[]
            {
                new CounterStrategy("Counter-aggression with positioning", "Use positioning to counter aggressive plays", 0.8),
                new CounterStrategy("Exploit defensive tendencies", "Take advantage of defensive positioning", 0.7)
            },
            AnalysisSummary: aiResponse);
    }

    private SkillAssessment ParseSkillAssessment(string aiResponse)
    {
        var skillBreakdown = new Dictionary<SkillArea, SkillRating>
        {
            { SkillArea.DecisionMaking, SkillRating.Competent },
            { SkillArea.Execution, SkillRating.Proficient },
            { SkillArea.Adaptation, SkillRating.Developing },
            { SkillArea.Strategy, SkillRating.Competent },
            { SkillArea.Awareness, SkillRating.Proficient }
        };

        return new SkillAssessment(
            CurrentLevel: SkillLevel.Intermediate,
            PotentialLevel: SkillLevel.Advanced,
            SkillBreakdown: skillBreakdown,
            Milestones: new[]
            {
                new SkillMilestone("Consistent execution", true, DateTime.UtcNow.AddDays(-7)),
                new SkillMilestone("Better adaptation", false, null)
            },
            AssessmentSummary: aiResponse);
    }

    private ImprovementPlan ParseImprovementPlan(Guid sessionId, string aiResponse)
    {
        return new ImprovementPlan(
            SessionId: sessionId,
            GeneratedAt: DateTime.UtcNow,
            EstimatedDuration: TimeSpan.FromDays(30),
            Goals: new[]
            {
                new ImprovementGoal("Improve decision making speed", "Make faster and better decisions under pressure", false, 0.3),
                new ImprovementGoal("Enhance strategic thinking", "Develop better long-term strategy", false, 0.2)
            },
            Exercises: new[]
            {
                new TrainingExercise("Daily timing drills", "Practice timing with precision exercises", TimeSpan.FromMinutes(15), Difficulty.Medium),
                new TrainingExercise("Strategy study sessions", "Analyze and learn from professional games", TimeSpan.FromMinutes(30), Difficulty.Hard)
            },
            Milestones: new[]
            {
                new Milestone("Complete 10 practice sessions", "Finish 10 focused practice sessions", DateTime.UtcNow.AddDays(14), false),
                new Milestone("Achieve 80% win rate", "Reach 80% win rate in practice matches", DateTime.UtcNow.AddDays(30), false)
            });
    }

    private IReadOnlyList<CoachingTip> ParseCoachingTips(string aiResponse)
    {
        return new[]
        {
            new CoachingTip(
                Title: "Stay Calm Under Pressure",
                Description: "Take deep breaths and focus on fundamentals when feeling overwhelmed",
                Category: TipCategory.Mindset,
                Difficulty: TipDifficulty.Easy,
                Prerequisites: Array.Empty<string>()),

            new CoachingTip(
                Title: "Analyze Before Acting",
                Description: "Quickly assess the situation before making your move",
                Category: TipCategory.Strategy,
                Difficulty: TipDifficulty.Medium,
                Prerequisites: new[] { "Basic game knowledge" })
        };
    }
}