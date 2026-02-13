using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Assistant;

/// <summary>
/// AI-powered game assistant service.
/// Provides intelligent game recommendations, tips, and analysis.
/// </summary>
public class GameAssistantService : IGameAssistantService
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IGameRepository _gameRepository;
    private readonly ISmartCategorizationService _categorizationService;
    private readonly IEyeTrackingMonitor _eyeTrackingMonitor;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<GameAssistantService> _logger;
    private SmartPauseOptions _smartPauseOptions = new(
        Enabled: false,
        LookAwayThresholdSeconds: 5,
        ResumeOnGazeReturn: true,
        RequireEyeTracking: false);

    public GameAssistantService(
        IAiOrchestrator aiOrchestrator,
        IGameRepository gameRepository,
        ISmartCategorizationService categorizationService,
        IEyeTrackingMonitor eyeTrackingMonitor,
        ITimeProvider timeProvider,
        ILogger<GameAssistantService> logger)
    {
        _aiOrchestrator = aiOrchestrator;
        _gameRepository = gameRepository;
        _categorizationService = categorizationService;
        _eyeTrackingMonitor = eyeTrackingMonitor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<AssistantRecommendation>> AnalyzeSessionAsync(
        SessionContext context,
        CancellationToken ct = default)
    {
        if (context.GameId == Guid.Empty)
        {
            return Result.Failure<AssistantRecommendation>("Game ID is required", ErrorType.Validation);
        }

        var game = await _gameRepository.GetByIdAsync(GameId.From(context.GameId), ct);
        if (game == null)
        {
            return Result.Failure<AssistantRecommendation>("Game not found", ErrorType.NotFound);
        }

        var nowUtc = _timeProvider.UtcNow;
        var sessionDuration = nowUtc - context.SessionStartTimeUtc;
        if (sessionDuration < TimeSpan.Zero)
        {
            sessionDuration = TimeSpan.Zero;
        }

        var lookAwayDurationSeconds = context.LookAwayDurationSeconds;
        if (!lookAwayDurationSeconds.HasValue &&
            _smartPauseOptions.Enabled &&
            _eyeTrackingMonitor.IsMonitoring)
        {
            var snapshotResult = await _eyeTrackingMonitor.GetSnapshotAsync(ct);
            if (snapshotResult.IsSuccess && snapshotResult.Value is not null)
            {
                lookAwayDurationSeconds = snapshotResult.Value.LookAwayDurationSeconds;
            }
        }

        if (_smartPauseOptions.Enabled &&
            lookAwayDurationSeconds.HasValue &&
            lookAwayDurationSeconds.Value >= _smartPauseOptions.LookAwayThresholdSeconds)
        {
            return Result.Success(new AssistantRecommendation(
                AssistantRecommendationType.SmartPause,
                $"Auto-pause recommended for {game.Title}: gaze away for {lookAwayDurationSeconds.Value}s.",
                0.92f,
                new[]
                {
                    "Pause now to avoid unwanted gameplay input.",
                    _smartPauseOptions.ResumeOnGazeReturn
                        ? "Resume automatically on gaze return."
                        : "Resume manually when ready."
                },
                nowUtc,
                ShouldInterruptGameplay: true));
        }

        if (sessionDuration >= TimeSpan.FromMinutes(90) && context.BreaksTaken <= 0)
        {
            return Result.Success(new AssistantRecommendation(
                AssistantRecommendationType.BreakReminder,
                $"You've been playing {game.Title} for {sessionDuration.TotalMinutes:F0} minutes without a break.",
                0.87f,
                new[]
                {
                    "Take a 5-minute break.",
                    "Use the 20-20-20 eye rule before resuming."
                },
                nowUtc,
                ShouldInterruptGameplay: false));
        }

        var frustrationScore = CalculateFrustrationScore(
            context.RecentDeaths,
            context.RecentRetries,
            TimeSpan.Zero,
            context.InputPattern,
            sessionDuration);

        if (frustrationScore >= 0.65f)
        {
            var difficultySuggestion = BuildDifficultySuggestion(
                new GameplayMetrics(
                    context.RecentDeaths,
                    TimeSpan.Zero,
                    context.RecentRetries,
                    context.InputPattern,
                    context.SessionStartTimeUtc),
                sessionDuration);

            if (difficultySuggestion.Difficulty == SuggestedDifficulty.Decrease)
            {
                return Result.Success(new AssistantRecommendation(
                    AssistantRecommendationType.DifficultyAdjustment,
                    "Performance trends indicate growing frustration. Consider lowering difficulty temporarily.",
                    difficultySuggestion.Confidence,
                    new[]
                    {
                        "Lower the difficulty by one step.",
                        "Re-enable previous difficulty after this section."
                    },
                    nowUtc,
                    ShouldInterruptGameplay: false));
            }
        }

        return Result.Success(new AssistantRecommendation(
            AssistantRecommendationType.None,
            "Session health is stable. No assistant intervention recommended.",
            0.75f,
            Array.Empty<string>(),
            nowUtc,
            ShouldInterruptGameplay: false));
    }

    public Task<Result> EnableSmartPauseAsync(
        SmartPauseOptions options,
        CancellationToken ct = default)
    {
        if (options.LookAwayThresholdSeconds < 2 || options.LookAwayThresholdSeconds > 120)
        {
            return Task.FromResult(Result.Failure(
                "Look-away threshold must be between 2 and 120 seconds.",
                ErrorType.Validation));
        }

        if (options.Enabled && options.RequireEyeTracking && !_eyeTrackingMonitor.IsAvailable)
        {
            return Task.FromResult(Result.Failure(
                "Eye tracking is required for Smart Pause but no supported eye-tracking provider is available.",
                ErrorType.NotImplemented));
        }

        if (options.Enabled &&
            (options.RequireEyeTracking || _eyeTrackingMonitor.IsAvailable) &&
            !_eyeTrackingMonitor.IsMonitoring)
        {
            return EnableSmartPauseWithMonitoringAsync(options, ct);
        }

        if (!options.Enabled && _eyeTrackingMonitor.IsMonitoring)
        {
            return DisableSmartPauseMonitoringAsync(options, ct);
        }

        _smartPauseOptions = options;
        _logger.LogInformation(
            "Smart pause updated. Enabled: {Enabled}, Threshold: {Threshold}s, AutoResume: {AutoResume}, RequireEyeTracking: {RequireEyeTracking}",
            options.Enabled,
            options.LookAwayThresholdSeconds,
            options.ResumeOnGazeReturn,
            options.RequireEyeTracking);

        return Task.FromResult(Result.Success());
    }

    private async Task<Result> EnableSmartPauseWithMonitoringAsync(
        SmartPauseOptions options,
        CancellationToken ct)
    {
        var monitoringResult = await _eyeTrackingMonitor.StartMonitoringAsync(ct);
        if (monitoringResult.IsFailure && options.RequireEyeTracking)
        {
            return Result.Failure(
                monitoringResult.Error ?? "Failed to start eye-tracking monitor.",
                monitoringResult.ErrorType == ErrorType.None ? ErrorType.External : monitoringResult.ErrorType);
        }

        if (monitoringResult.IsFailure)
        {
            _logger.LogWarning(
                "Smart pause enabled without active eye-tracking monitoring. Error: {Error}",
                monitoringResult.Error);
        }

        _smartPauseOptions = options;
        _logger.LogInformation(
            "Smart pause updated. Enabled: {Enabled}, Threshold: {Threshold}s, AutoResume: {AutoResume}, RequireEyeTracking: {RequireEyeTracking}",
            options.Enabled,
            options.LookAwayThresholdSeconds,
            options.ResumeOnGazeReturn,
            options.RequireEyeTracking);

        return Result.Success();
    }

    private async Task<Result> DisableSmartPauseMonitoringAsync(
        SmartPauseOptions options,
        CancellationToken ct)
    {
        var stopResult = await _eyeTrackingMonitor.StopMonitoringAsync(ct);
        if (stopResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to stop eye-tracking monitor while disabling smart pause: {Error}",
                stopResult.Error);
        }

        _smartPauseOptions = options;
        _logger.LogInformation(
            "Smart pause updated. Enabled: {Enabled}, Threshold: {Threshold}s, AutoResume: {AutoResume}, RequireEyeTracking: {RequireEyeTracking}",
            options.Enabled,
            options.LookAwayThresholdSeconds,
            options.ResumeOnGazeReturn,
            options.RequireEyeTracking);

        return Result.Success();
    }

    public async Task<Result<DifficultySuggestion>> AnalyzeDifficultyAsync(
        Guid gameId,
        GameplayMetrics metrics,
        CancellationToken ct = default)
    {
        if (gameId == Guid.Empty)
        {
            return Result.Failure<DifficultySuggestion>("Game ID is required", ErrorType.Validation);
        }

        var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
        if (game == null)
        {
            return Result.Failure<DifficultySuggestion>("Game not found", ErrorType.NotFound);
        }

        var nowUtc = _timeProvider.UtcNow;
        var sessionDuration = nowUtc - metrics.SessionStartTimeUtc;
        if (sessionDuration < TimeSpan.Zero)
        {
            sessionDuration = TimeSpan.Zero;
        }

        return Result.Success(BuildDifficultySuggestion(metrics, sessionDuration));
    }

    /// <summary>
    /// Asks the AI assistant a question about a game.
    /// </summary>
    public async Task<Result<AssistantResponse>> AskAsync(
        Guid gameId,
        string question,
        CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result.Failure<AssistantResponse>("Game not found", ErrorType.NotFound);

            var sessionId = $"game-assistant-{gameId}";
            var contextPrompt = await BuildContextPromptAsync(game, ct);

            var fullPrompt = $"{contextPrompt}\n\nUser question: {question}\n\nProvide a helpful, accurate answer based on the game information above.";

            var response = await _aiOrchestrator.ProcessRequestWithContextAsync(
                sessionId,
                new AiRequest(AiRequestType.Completion, Prompt: fullPrompt),
                ct);

            if (!response.IsSuccessful)
            {
                _logger.LogWarning("AI assistant failed for game {GameId}: {Error}", gameId, response.Error);
                return Result.Failure<AssistantResponse>($"AI assistant failed: {response.Error}", ErrorType.Internal);
            }

            var assistantResponse = ParseAssistantResponse(response.Content, question);
            return Result.Success<AssistantResponse>(assistantResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get assistant response for game {GameId}", gameId);
            return Result.Failure<AssistantResponse>($"Assistant query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<string>>> GetQuickTipsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result.Failure<IReadOnlyList<string>>("Game not found", ErrorType.NotFound);

            var sessionId = $"game-assistant-{gameId}";
            var contextPrompt = await BuildContextPromptAsync(game, ct);

            var prompt = $"{contextPrompt}\n\nProvide 5 quick tips for playing this game effectively. Focus on general strategies, not specific spoilers.";

            var response = await _aiOrchestrator.ProcessRequestWithContextAsync(
                sessionId,
                new AiRequest(AiRequestType.Completion, Prompt: prompt),
                ct);

            if (!response.IsSuccessful)
            {
                _logger.LogWarning("AI tips generation failed for game {GameId}", gameId);
                return Result.Failure<IReadOnlyList<string>>("Could not generate tips", ErrorType.Internal);
            }

            var tips = ParseTipsResponse(response.Content);
            return Result.Success<IReadOnlyList<string>>(tips);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quick tips for game {GameId}", gameId);
            return Result.Failure<IReadOnlyList<string>>($"Tips generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<string>> GetWalkthroughHintAsync(
        Guid gameId,
        string currentLocation,
        bool avoidSpoilers = true,
        CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result.Failure<string>("Game not found", ErrorType.NotFound);

            var sessionId = $"game-assistant-{gameId}";
            var contextPrompt = await BuildContextPromptAsync(game, ct);

            var spoilerNote = avoidSpoilers
                ? "IMPORTANT: Avoid any major spoilers. Only provide hints that won't ruin the experience."
                : "";

            var prompt = $"{contextPrompt}\n\nCurrent location/context: {currentLocation}\n\n{spoilerNote}\n\nProvide a helpful hint for progressing from this point in the game.";

            var response = await _aiOrchestrator.ProcessRequestWithContextAsync(
                sessionId,
                new AiRequest(AiRequestType.Completion, Prompt: prompt),
                ct);

            if (!response.IsSuccessful)
            {
                _logger.LogWarning("AI walkthrough hint failed for game {GameId}", gameId);
                return Result.Failure<string>("Could not generate hint", ErrorType.Internal);
            }

            return Result.Success<string>(response.Content.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get walkthrough hint for game {GameId}", gameId);
            return Result.Failure<string>($"Hint generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result> ClearContextAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            // In a full implementation, this would clear the conversation context
            // For now, just log that context would be cleared
            _logger.LogInformation("Clearing assistant context for game {GameId}", gameId);

            // The IAiOrchestrator would need a method to clear context
            // For now, we simulate this by creating a new session implicitly

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear context for game {GameId}", gameId);
            return Task.FromResult(Result.Failure($"Context clearing failed: {ex.Message}", ErrorType.Internal));
        }
    }

    private static DifficultySuggestion BuildDifficultySuggestion(
        GameplayMetrics metrics,
        TimeSpan sessionDuration)
    {
        var supportingMetrics = new List<string>();
        var frustrationScore = CalculateFrustrationScore(
            metrics.DeathCount,
            metrics.RetryCount,
            metrics.TimeInCurrentSection,
            metrics.InputPattern,
            sessionDuration,
            supportingMetrics);

        if (frustrationScore >= 0.65f)
        {
            var confidence = Math.Min(0.96f, 0.72f + (frustrationScore * 0.24f));
            return new DifficultySuggestion(
                SuggestedDifficulty.Decrease,
                confidence,
                "Player appears stuck and input patterns indicate frustration.",
                supportingMetrics);
        }

        var masteryScore = 0f;
        if (metrics.DeathCount <= 2)
        {
            masteryScore += 0.28f;
        }

        if (metrics.RetryCount <= 2)
        {
            masteryScore += 0.24f;
        }

        if (metrics.TimeInCurrentSection <= TimeSpan.FromMinutes(8))
        {
            masteryScore += 0.18f;
        }

        if (metrics.InputPattern.ErrorRate <= 0.1f)
        {
            masteryScore += 0.2f;
        }

        if (metrics.InputPattern.ActionsPerMinute >= 55 && !metrics.InputPattern.HasIdleSpikes)
        {
            masteryScore += 0.1f;
        }

        if (masteryScore >= 0.7f)
        {
            var confidence = Math.Min(0.92f, 0.68f + (masteryScore * 0.22f));
            var metricsForIncrease = new List<string>
            {
                $"{metrics.DeathCount} deaths",
                $"{metrics.RetryCount} retries",
                $"{metrics.TimeInCurrentSection.TotalMinutes:F0} minutes in current section",
                $"{metrics.InputPattern.ErrorRate:P0} input error rate"
            };

            return new DifficultySuggestion(
                SuggestedDifficulty.Increase,
                confidence,
                "Recent performance is consistently strong with low failure indicators.",
                metricsForIncrease);
        }

        var maintainMetrics = supportingMetrics.Count > 0
            ? supportingMetrics
            : new List<string>
            {
                $"{metrics.DeathCount} deaths",
                $"{metrics.RetryCount} retries",
                $"{metrics.TimeInCurrentSection.TotalMinutes:F0} minutes in current section"
            };

        return new DifficultySuggestion(
            SuggestedDifficulty.Maintain,
            0.7f,
            "Current performance trends are mixed; keeping current difficulty is recommended.",
            maintainMetrics);
    }

    private static float CalculateFrustrationScore(
        int deathCount,
        int retryCount,
        TimeSpan timeInCurrentSection,
        InputPattern inputPattern,
        TimeSpan sessionDuration,
        List<string>? supportingMetrics = null)
    {
        var frustrationScore = 0f;

        if (deathCount >= 10)
        {
            frustrationScore += 0.34f;
            supportingMetrics?.Add($"{deathCount} deaths");
        }
        else if (deathCount >= 5)
        {
            frustrationScore += 0.2f;
            supportingMetrics?.Add($"{deathCount} deaths");
        }

        if (retryCount >= 8)
        {
            frustrationScore += 0.28f;
            supportingMetrics?.Add($"{retryCount} retries");
        }
        else if (retryCount >= 4)
        {
            frustrationScore += 0.14f;
            supportingMetrics?.Add($"{retryCount} retries");
        }

        if (timeInCurrentSection >= TimeSpan.FromMinutes(20))
        {
            frustrationScore += 0.2f;
            supportingMetrics?.Add($"{timeInCurrentSection.TotalMinutes:F0} minutes stuck in section");
        }
        else if (timeInCurrentSection >= TimeSpan.FromMinutes(12))
        {
            frustrationScore += 0.1f;
            supportingMetrics?.Add($"{timeInCurrentSection.TotalMinutes:F0} minutes in section");
        }

        if (inputPattern.ErrorRate >= 0.35f)
        {
            frustrationScore += 0.2f;
            supportingMetrics?.Add($"{inputPattern.ErrorRate:P0} input error rate");
        }
        else if (inputPattern.ErrorRate >= 0.2f)
        {
            frustrationScore += 0.1f;
            supportingMetrics?.Add($"{inputPattern.ErrorRate:P0} input error rate");
        }

        if (inputPattern.HasRapidInputBursts)
        {
            frustrationScore += 0.08f;
            supportingMetrics?.Add("rapid input bursts detected");
        }

        if (inputPattern.HasIdleSpikes)
        {
            frustrationScore += 0.06f;
            supportingMetrics?.Add("input idle spikes detected");
        }

        if (sessionDuration >= TimeSpan.FromHours(2))
        {
            frustrationScore += 0.08f;
            supportingMetrics?.Add($"{sessionDuration.TotalMinutes:F0} minutes session duration");
        }

        return Math.Min(1f, frustrationScore);
    }

    private async Task<string> BuildContextPromptAsync(Game game, CancellationToken ct)
    {
        var gameTags = await _categorizationService.AnalyzeGameAsync(game.Id, ct);
        var tagsInfo = gameTags.IsSuccess
            ? $"Genres: {string.Join(", ", gameTags.Value.Genres)}\nThemes: {string.Join(", ", gameTags.Value.Themes)}\nMechanics: {string.Join(", ", gameTags.Value.Mechanics)}"
            : "Game analysis not available";

        return $@"You are an expert gaming assistant for the game: {game.Title}

Game Information:
- Title: {game.Title}
- Platform: {game.Platform?.Name ?? "Unknown"}
- Source: {game.Source ?? "Unknown"}
- Created: {game.CreatedAt.Year}
- Total Playtime: {game.TotalPlayTime:hh\\:mm}
- Description: {game.Description ?? "No description available"}

Game Analysis:
{tagsInfo}

Instructions:
- Be helpful and knowledgeable about this specific game
- Provide accurate information based on the game details above
- If you don't have specific information, say so rather than making things up
- Respect spoiler preferences when asked
- Keep responses conversational and engaging";
    }

    private static AssistantResponse ParseAssistantResponse(string aiResponse, string originalQuestion)
    {
        // Simple response parsing - in production would use more sophisticated parsing
        var containsSpoilers = aiResponse.ToLower().Contains("spoiler") ||
                              aiResponse.ToLower().Contains("ending") ||
                              aiResponse.ToLower().Contains("final boss");

        var relatedQuestions = GenerateRelatedQuestions(originalQuestion);

        return new AssistantResponse(
            Answer: aiResponse.Trim(),
            Sources: new[] { "Game knowledge base", "Player community insights" },
            ContainsSpoilers: containsSpoilers,
            RelatedQuestions: relatedQuestions,
            Confidence: 0.85f // Would be determined by AI model confidence
        );
    }

    private static IReadOnlyList<string> ParseTipsResponse(string aiResponse)
    {
        // Simple parsing of tips - split by numbers or bullet points
        var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => line.Trim().Length > 10) // Filter out very short lines
            .Select(line => line.Trim())
            .Take(5) // Limit to 5 tips
            .ToList();

        return lines.AsReadOnly();
    }

    private static IReadOnlyList<string> GenerateRelatedQuestions(string originalQuestion)
    {
        // Generate some follow-up questions based on the original question type
        var question = originalQuestion.ToLower();

        if (question.Contains("how") || question.Contains("strategy") || question.Contains("tips"))
        {
            return new[]
            {
                "What are some advanced strategies for this area?",
                "Are there any secrets or hidden items nearby?",
                "How does the combat system work here?"
            };
        }
        else if (question.Contains("where") || question.Contains("location") || question.Contains("find"))
        {
            return new[]
            {
                "What items can I find in this area?",
                "Are there any shortcuts or alternate paths?",
                "What's the best route to the next objective?"
            };
        }
        else if (question.Contains("what") || question.Contains("explain"))
        {
            return new[]
            {
                "Can you explain the game mechanics in more detail?",
                "What are the different difficulty options?",
                "How do I unlock new abilities?"
            };
        }
        else
        {
            return new[]
            {
                "Can you give me some general tips for this game?",
                "What should I focus on early in the game?",
                "Are there any common mistakes to avoid?"
            };
        }
    }
}

