using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services.DTOs;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Implementation of game briefing service with AI-powered content generation.
/// </summary>
public class GameBriefingService : IGameBriefingService
{
    private readonly IGameRepository _gameRepository;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly ISessionTrackingService _sessionTrackingService;
    private readonly ILogger<GameBriefingService> _logger;
    private readonly ITimeProvider _timeProvider;

    public GameBriefingService(
        IGameRepository gameRepository,
        IAiOrchestrator aiOrchestrator,
        ISessionTrackingService sessionTrackingService,
        ILogger<GameBriefingService> logger,
        ITimeProvider timeProvider)
    {
        _gameRepository = gameRepository;
        _aiOrchestrator = aiOrchestrator;
        _sessionTrackingService = sessionTrackingService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Generates a comprehensive game briefing with session summary, objectives, and tips.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the game briefing or an error.</returns>
    public async Task<Result<GameBriefing>> GenerateBriefingAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            // Verify game exists
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<GameBriefing>($"Game with ID {gameId} not found");
            }

            // Generate all briefing components in parallel
            var lastSessionTask = GenerateLastSessionSummaryAsync(gameId, ct);
            var objectivesTask = GetCurrentObjectivesAsync(gameId, ct);
            var tipsTask = GetGameTipsAsync(gameId, ct);
            var timeSinceLastPlayed = CalculateTimeSinceLastPlayed(game.LastPlayedAt);

            await Task.WhenAll(lastSessionTask, objectivesTask, tipsTask).ConfigureAwait(false);

            var lastSessionResult = await lastSessionTask;
            var objectivesResult = await objectivesTask;
            var tipsResult = await tipsTask;

            var lastSessionSummary = lastSessionResult.IsSuccess
                ? lastSessionResult.Value
                : "No recent session data available.";

            var currentObjectives = objectivesResult.IsSuccess
                ? objectivesResult.Value
                : Array.Empty<string>();

            var tips = tipsResult.IsSuccess
                ? tipsResult.Value
                : Array.Empty<string>();

            var briefing = new GameBriefing(
                GameId: gameId,
                LastSessionSummary: lastSessionSummary,
                CurrentObjectives: currentObjectives,
                Tips: tips,
                TimeSinceLastPlayed: timeSinceLastPlayed);

            _logger.LogInformation("Generated briefing for game {GameId} ({GameTitle})",
                gameId, game.Title);

            return Result.Success<GameBriefing>(briefing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate briefing for game {GameId}", gameId);
            return Result.Failure<GameBriefing>($"Failed to generate briefing: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a summary of the player's last gaming session.
    /// </summary>
    public async Task<Result<string>> GenerateLastSessionSummaryAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var statsResult = await _sessionTrackingService.GetStatisticsAsync(gameId, ct)
                .ConfigureAwait(false);

            if (!statsResult.IsSuccess || statsResult.Value == null)
            {
                return Result.Success<string>("No session data available yet.");
            }

            var stats = statsResult.Value;

            if (stats.LastPlayedAt == null)
            {
                return Result.Success<string>("Game has not been played yet.");
            }

            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<string>("Game not found");
            }

            // Use AI to generate a natural language summary
            var prompt = $"Generate a brief, engaging summary of a gaming session. " +
                        $"Game: {game.Title}. " +
                        $"Total playtime: {stats.TotalPlaytime.TotalHours:F1} hours. " +
                        $"Sessions this week: {stats.SessionsThisWeek}. " +
                        $"Average session: {stats.AverageSessionDuration.TotalMinutes:F0} minutes. " +
                        $"Longest session: {stats.LongestSessionDuration.TotalMinutes:F0} minutes. " +
                        $"Keep the summary to 1-2 sentences, make it sound natural and encouraging.";

            var aiResult = await _aiOrchestrator.GenerateTextAsync(prompt, ct: ct)
                .ConfigureAwait(false);

            var summary = aiResult.IsSuccess && !string.IsNullOrWhiteSpace(aiResult.Value)
                ? aiResult.Value.Trim()
                : $"You've played {game.Title} for {stats.TotalPlaytime.TotalHours:F1} hours total, " +
                  $"with {stats.SessionsThisWeek} sessions this week.";

            return Result.Success<string>(summary);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate last session summary for game {GameId}", gameId);
            return Result.Success<string>("Session summary unavailable.");
        }
    }

    /// <summary>
    /// Retrieves current objectives and goals for the game.
    /// </summary>
    public async Task<Result<IReadOnlyList<string>>> GetCurrentObjectivesAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<IReadOnlyList<string>>("Game not found");
            }

            var statsResult = await _sessionTrackingService.GetStatisticsAsync(gameId, ct)
                .ConfigureAwait(false);

            var totalPlaytime = TimeSpan.Zero;
            if (statsResult.IsSuccess && statsResult.Value != null)
            {
                totalPlaytime = statsResult.Value.TotalPlaytime;
            }

            // Use AI to suggest objectives based on game and playtime
            var prompt = $"Suggest 2-3 achievable objectives for the game '{game.Title}' " +
                        $"based on {totalPlaytime.TotalHours:F1} hours of playtime. " +
                        $"Consider progression, exploration, achievements, or skill improvement. " +
                        $"Make objectives specific and motivating. Keep each to 1 sentence.";

            var aiResult = await _aiOrchestrator.GenerateTextAsync(prompt, ct: ct)
                .ConfigureAwait(false);

            if (!aiResult.IsSuccess || string.IsNullOrWhiteSpace(aiResult.Value))
            {
                // Fallback objectives
                var fallbackObjectives = new[]
                {
                    "Continue exploring the game world and discovering new areas.",
                    "Focus on improving your skills and combat techniques.",
                    "Work towards completing main story objectives."
                };
                return Result.Success<IReadOnlyList<string>>(fallbackObjectives);
            }

            // Parse AI response into objectives
            var objectives = aiResult.Value
                .Split(new[] { '\n', '.', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(obj => !string.IsNullOrWhiteSpace(obj))
                .Select(obj => obj.Trim())
                .Take(3)
                .ToArray();

            return Result.Success<IReadOnlyList<string>>(objectives);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current objectives for game {GameId}", gameId);
            return Result.Success<IReadOnlyList<string>>(Array.Empty<string>());
        }
    }

    /// <summary>
    /// Retrieves helpful tips and strategies for the game.
    /// </summary>
    public async Task<Result<IReadOnlyList<string>>> GetGameTipsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<IReadOnlyList<string>>("Game not found");
            }

            // Use AI to generate helpful tips
            var prompt = $"Generate 2-3 helpful, practical tips for playing '{game.Title}'. " +
                        $"Focus on gameplay mechanics, strategies, or quality-of-life improvements. " +
                        $"Keep each tip concise (1 sentence) and actionable.";

            var aiResult = await _aiOrchestrator.GenerateTextAsync(prompt, ct: ct)
                .ConfigureAwait(false);

            if (!aiResult.IsSuccess || string.IsNullOrWhiteSpace(aiResult.Value))
            {
                // Fallback tips
                var fallbackTips = new[]
                {
                    "Take regular breaks to maintain focus and enjoyment.",
                    "Experiment with different playstyles to discover what works best.",
                    "Pay attention to the game's mechanics and learn from each session."
                };
                return Result.Success<IReadOnlyList<string>>(fallbackTips);
            }

            // Parse AI response into tips
            var tips = aiResult.Value
                .Split(new[] { '\n', '.', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(tip => !string.IsNullOrWhiteSpace(tip))
                .Select(tip => tip.Trim())
                .Take(3)
                .ToArray();

            return Result.Success<IReadOnlyList<string>>(tips);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get game tips for game {GameId}", gameId);
            return Result.Success<IReadOnlyList<string>>(Array.Empty<string>());
        }
    }

    /// <summary>
    /// Generates a simplified briefing with essential information only.
    /// </summary>
    public async Task<Result<GameBriefing>> GenerateQuickBriefingAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            // Get basic game info
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct)
                .ConfigureAwait(false);

            if (game == null)
            {
                return Result.Failure<GameBriefing>($"Game with ID {gameId} not found");
            }

            // Simplified briefing for mobile/Big Picture mode
            var timeSinceLastPlayed = CalculateTimeSinceLastPlayed(game.LastPlayedAt);

            // Get minimal session info
            var lastSessionSummary = game.LastPlayedAt.HasValue
                ? $"Last played {timeSinceLastPlayed.TotalDays:F0} days ago"
                : "Not played yet";

            var currentObjectives = new[] { "Continue your adventure" };
            var tips = new[] { "Enjoy the game at your own pace" };

            var briefing = new GameBriefing(
                GameId: gameId,
                LastSessionSummary: lastSessionSummary,
                CurrentObjectives: currentObjectives,
                Tips: tips,
                TimeSinceLastPlayed: timeSinceLastPlayed);

            return Result.Success<GameBriefing>(briefing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate quick briefing for game {GameId}", gameId);
            return Result.Failure<GameBriefing>($"Failed to generate quick briefing: {ex.Message}");
        }
    }

    private TimeSpan CalculateTimeSinceLastPlayed(DateTime? lastPlayedAt)
    {
        return lastPlayedAt.HasValue
            ? _timeProvider.UtcNow - lastPlayedAt.Value
            : TimeSpan.MaxValue; // Indicate never played
    }
}

