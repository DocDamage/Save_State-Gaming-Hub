using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Infrastructure.External;
using SaveState.Infrastructure.Persistence;
using System.Text;
using System.Text.Json;

namespace SaveState.Infrastructure.Analytics;

/// <summary>
/// AI-powered service for predicting game completion times.
/// </summary>
public sealed class CompletionPredictionService : ICompletionPredictionService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IHowLongToBeatService _hltbService;
    private readonly ILogger<CompletionPredictionService> _logger;
    private static readonly Dictionary<GameId, CompletionMetrics> _completionHistory = new();
    private static readonly Dictionary<string, HowLongToBeatData> _hltbCache = new();

    public CompletionPredictionService(
        SaveStateDbContext dbContext,
        IAiOrchestrator aiOrchestrator,
        IHowLongToBeatService hltbService,
        ILogger<CompletionPredictionService> logger)
    {
        _dbContext = dbContext;
        _aiOrchestrator = aiOrchestrator;
        _hltbService = hltbService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<GameCompletionPrediction>>> GetPredictionsAsync(
        int count = 5,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating {Count} AI-powered completion predictions", count);

            // Get active games with playtime
            var activeGames = await _dbContext.Games
                .Where(g => g.Status == GameStatus.Running || g.Status == GameStatus.Installed)
                .Where(g => g.TotalPlayTime.TotalHours > 0)
                .Include(g => g.Platform)
                .ToListAsync(ct);

            if (activeGames.Count == 0)
            {
                return Result.Success<IReadOnlyList<GameCompletionPrediction>>(
                    Array.Empty<GameCompletionPrediction>());
            }

            // Get session history for pattern analysis
            var sessions = await _dbContext.GameSessions
                .Where(s => s.EndedAt.HasValue)
                .Include(s => s.Game)
                .ToListAsync(ct);

            // Build analytics context for AI
            var analyticsContext = BuildAnalyticsContext(activeGames, sessions);

            // Generate predictions using AI
            var predictions = new List<GameCompletionPrediction>();

            foreach (var game in activeGames.Take(count * 2)) // Get more for filtering
            {
                var prediction = await GeneratePredictionForGame(game, analyticsContext, ct);
                if (prediction != null)
                {
                    predictions.Add(prediction);
                }
            }

            // Sort by estimated time remaining and take top N
            var topPredictions = predictions
                .OrderBy(p => p.EstimatedTimeRemaining)
                .Take(count)
                .ToList();

            _logger.LogInformation("Generated {Count} predictions from {Total} active games",
                topPredictions.Count, activeGames.Count);

            return Result.Success<IReadOnlyList<GameCompletionPrediction>>(topPredictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate completion predictions");
            return Result.Failure<IReadOnlyList<GameCompletionPrediction>>(
                $"Failed to generate predictions: {ex.Message}");
        }
    }

    public async Task<Result<GameCompletionPrediction>> GetPredictionForGameAsync(
        GameId gameId,
        CancellationToken ct = default)
    {
        try
        {
            var game = await _dbContext.Games
                .Include(g => g.Platform)
                .FirstOrDefaultAsync(g => g.Id == gameId, ct);

            if (game == null)
            {
                return Result.Failure<GameCompletionPrediction>("Game not found");
            }

            var sessions = await _dbContext.GameSessions
                .Where(s => s.EndedAt.HasValue)
                .Include(s => s.Game)
                .ToListAsync(ct);

            var analyticsContext = BuildAnalyticsContext(new[] { game }, sessions);
            var prediction = await GeneratePredictionForGame(game, analyticsContext, ct);

            if (prediction == null)
            {
                return Result.Failure<GameCompletionPrediction>(
                    "Could not generate prediction for this game");
            }

            return Result.Success(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate prediction for game {GameId}", gameId);
            return Result.Failure<GameCompletionPrediction>(
                $"Failed to generate prediction: {ex.Message}");
        }
    }

    public Task<Result> RecordCompletionAsync(
        GameId gameId,
        TimeSpan actualTimeToComplete,
        CancellationToken ct = default)
    {
        try
        {
            _completionHistory[gameId] = new CompletionMetrics(
                actualTimeToComplete,
                DateTime.UtcNow);

            _logger.LogInformation(
                "Recorded completion for game {GameId}: {Time} hours",
                gameId,
                actualTimeToComplete.TotalHours);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record completion for game {GameId}", gameId);
            return Task.FromResult(Result.Failure($"Failed to record completion: {ex.Message}"));
        }
    }

    private async Task<GameCompletionPrediction?> GeneratePredictionForGame(
        Core.GameLibrary.Entities.Game game,
        AnalyticsContext context,
        CancellationToken ct)
    {
        try
        {
            var currentPlaytime = game.TotalPlayTime.TotalHours;
            var factors = new List<string>();
            string basedOn;
            double estimatedTotalTime;
            double confidence;

            // Try to get HLTB data first (most accurate)
            var hltbData = await GetHltbDataAsync(game.Title, ct);

            if (hltbData != null && hltbData.MainPlusExtras.HasValue)
            {
                // Use HLTB as primary source
                estimatedTotalTime = hltbData.MainPlusExtras.Value.TotalHours;
                basedOn = "HowLongToBeat";
                confidence = 85.0; // HLTB data is highly reliable

                factors.Add($"HLTB Main + Extras: {HowLongToBeatService.FormatPlaytime(hltbData.MainPlusExtras)}");

                if (hltbData.MainStory.HasValue)
                    factors.Add($"HLTB Main Story: {HowLongToBeatService.FormatPlaytime(hltbData.MainStory)}");
                if (hltbData.Completionist.HasValue)
                    factors.Add($"HLTB Completionist: {HowLongToBeatService.FormatPlaytime(hltbData.Completionist)}");

                _logger.LogDebug("Using HLTB data for {GameName}: {Hours}h", game.Title, estimatedTotalTime);
            }
            else if (hltbData != null && hltbData.MainStory.HasValue)
            {
                // Fall back to Main Story if Main+Extras unavailable
                estimatedTotalTime = hltbData.MainStory.Value.TotalHours;
                basedOn = "HowLongToBeat (Main Story)";
                confidence = 80.0;

                factors.Add($"HLTB Main Story: {HowLongToBeatService.FormatPlaytime(hltbData.MainStory)}");
            }
            else
            {
                // Fallback to platform averages
                var platformGames = context.GamesByPlatform.GetValueOrDefault(
                    game.Platform?.Name.Value ?? "Unknown", new List<GameMetrics>());

                estimatedTotalTime = platformGames.Count > 0
                    ? platformGames.Average(g => g.PlaytimeHours)
                    : 15.0; // Generic fallback

                basedOn = "Platform averages";
                confidence = CalculateBaseConfidence(currentPlaytime, estimatedTotalTime);

                factors.Add($"Platform average: {estimatedTotalTime:F1} hours");
                factors.Add("HLTB data unavailable");
            }

            // Adjust confidence based on current progress
            var progressRatio = currentPlaytime / estimatedTotalTime;
            if (progressRatio > 0.5)
            {
                // More playtime = higher confidence in remaining estimate
                confidence = Math.Min(95, confidence + (progressRatio * 10));
            }

            // Add user-specific factors
            factors.Add($"Current playtime: {currentPlaytime:F1} hours");
            factors.Add($"Play frequency: {CalculatePlayFrequency(game, context):F1} sessions/week");

            // Try AI enhancement for personalized adjustment
            if (_aiOrchestrator != null)
            {
                try
                {
                    var aiResult = await EnhancePredictionWithAI(
                        game, context, currentPlaytime, estimatedTotalTime, ct);

                    if (aiResult != null)
                    {
                        // Blend AI estimate with HLTB (HLTB weighted higher)
                        var blendedEstimate = hltbData != null
                            ? (estimatedTotalTime * 0.7 + aiResult.EstimatedHours * 0.3)
                            : aiResult.EstimatedHours;

                        estimatedTotalTime = blendedEstimate;
                        confidence = Math.Min(95, (confidence + aiResult.Confidence) / 2);
                        basedOn = hltbData != null ? "HowLongToBeat + AI" : "AI Analysis";

                        // Add AI factors if meaningful
                        if (aiResult.Factors.Count > 0)
                            factors.AddRange(aiResult.Factors.Take(2));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "AI enhancement failed for {GameName}, using base prediction", game.Title);
                }
            }

            // Calculate remaining time
            var estimatedRemaining = Math.Max(0, estimatedTotalTime - currentPlaytime);

            return new GameCompletionPrediction(
                GameId.From(game.Id),
                game.Title,
                TimeSpan.FromHours(estimatedRemaining),
                Math.Min(100, Math.Max(0, confidence)),
                basedOn,
                factors);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate prediction for game {GameName}", game.Title);
            return null;
        }
    }

    private async Task<HowLongToBeatData?> GetHltbDataAsync(string gameTitle, CancellationToken ct)
    {
        try
        {
            // Check in-memory cache first
            var cacheKey = gameTitle.ToLowerInvariant().Trim();
            if (_hltbCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            // Fetch from HLTB service
            var result = await _hltbService.SearchGameAsync(gameTitle, ct);

            if (result.IsSuccess)
            {
                _hltbCache[cacheKey] = result.Value;
                return result.Value;
            }

            _logger.LogDebug("HLTB data not found for {GameTitle}", gameTitle);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch HLTB data for {GameTitle}", gameTitle);
            return null;
        }
    }

    private async Task<AiPredictionResult?> EnhancePredictionWithAI(
        Core.GameLibrary.Entities.Game game,
        AnalyticsContext context,
        double currentPlaytime,
        double avgCompletionTime,
        CancellationToken ct)
    {
        var prompt = BuildPredictionPrompt(game, context, currentPlaytime, avgCompletionTime);

        var result = await _aiOrchestrator.GenerateTextAsync(prompt, ct);

        if (!result.IsSuccess || result.Value == null)
        {
            return null;
        }

        // Parse AI response - expecting JSON format
        try
        {
            var response = result.Value.Trim();

            // Extract JSON if wrapped in markdown code blocks
            if (response.Contains("```json"))
            {
                var startIdx = response.IndexOf("```json") + 7;
                var endIdx = response.IndexOf("```", startIdx);
                response = response.Substring(startIdx, endIdx - startIdx).Trim();
            }
            else if (response.Contains("```"))
            {
                var startIdx = response.IndexOf("```") + 3;
                var endIdx = response.IndexOf("```", startIdx);
                response = response.Substring(startIdx, endIdx - startIdx).Trim();
            }

            var aiResponse = JsonSerializer.Deserialize<AiPredictionResponse>(response);

            if (aiResponse != null)
            {
                return new AiPredictionResult(
                    aiResponse.EstimatedTotalHours,
                    aiResponse.Confidence,
                    aiResponse.Factors ?? new List<string>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI prediction response for {GameName}", game.Title);
        }

        return null;
    }

    private string BuildPredictionPrompt(
        Core.GameLibrary.Entities.Game game,
        AnalyticsContext context,
        double currentPlaytime,
        double avgCompletionTime)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Predict the total completion time for this game based on player data:");
        sb.AppendLine();
        sb.AppendLine($"Game: {game.Title}");
        sb.AppendLine($"Platform: {game.Platform?.Name.Value ?? "Unknown"}");
        sb.AppendLine($"Current playtime: {currentPlaytime:F1} hours");
        sb.AppendLine($"Platform average: {avgCompletionTime:F1} hours");
        sb.AppendLine($"Player's avg session: {context.AverageSessionLength:F1} hours");
        sb.AppendLine($"Sessions per week: {context.SessionsPerWeek:F1}");
        sb.AppendLine();
        sb.AppendLine("Respond ONLY with JSON in this exact format (no other text):");
        sb.AppendLine("{");
        sb.AppendLine("  \"estimatedTotalHours\": <number>,");
        sb.AppendLine("  \"confidence\": <0-100>,");
        sb.AppendLine("  \"factors\": [\"<reason1>\", \"<reason2>\", \"<reason3>\"]");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private AnalyticsContext BuildAnalyticsContext(
        IEnumerable<Core.GameLibrary.Entities.Game> games,
        List<Core.GameLibrary.Entities.GameSession> sessions)
    {
        var gamesByPlatform = games
            .Where(g => g.Platform?.Name?.Value is not null)
            .GroupBy(g => g.Platform!.Name!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(game => new GameMetrics(
                    GameId.From(game.Id),
                    game.Title,
                    game.TotalPlayTime.TotalHours)).ToList());

        var avgSessionLength = sessions.Count > 0
            ? sessions.Where(s => s.EndedAt.HasValue).Average(s => (s.EndedAt!.Value - s.StartedAt).TotalHours)
            : 0;

        var totalDays = sessions.Count > 0
            ? (sessions.Max(s => s.StartedAt) - sessions.Min(s => s.StartedAt)).TotalDays
            : 1;

        var sessionsPerWeek = totalDays > 0
            ? (sessions.Count / totalDays) * 7
            : 0;

        return new AnalyticsContext(
            gamesByPlatform,
            avgSessionLength,
            sessionsPerWeek);
    }

    private double CalculateBaseConfidence(double currentPlaytime, double avgCompletionTime)
    {
        // Higher playtime = higher confidence
        var playtimeRatio = currentPlaytime / avgCompletionTime;

        // Confidence increases with playtime but caps at 95%
        var baseConfidence = Math.Min(0.95, playtimeRatio * 0.75 + 0.25);

        return baseConfidence * 100;
    }

    private double CalculatePlayFrequency(
        Core.GameLibrary.Entities.Game game,
        AnalyticsContext context)
    {
        // Simple heuristic based on overall player behavior
        return context.SessionsPerWeek;
    }

    private sealed record AnalyticsContext(
        Dictionary<string, List<GameMetrics>> GamesByPlatform,
        double AverageSessionLength,
        double SessionsPerWeek);

    private sealed record GameMetrics(GameId Id, string Title, double PlaytimeHours);

    private sealed record CompletionMetrics(TimeSpan ActualTime, DateTime CompletedAt);

    private sealed record AiPredictionResult(double EstimatedHours, double Confidence, List<string> Factors);

    private sealed record AiPredictionResponse(
        double EstimatedTotalHours,
        double Confidence,
        List<string>? Factors);
}

