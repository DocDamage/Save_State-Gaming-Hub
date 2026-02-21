using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.ContentGeneration.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.ContentGeneration.Services;

/// <summary>
/// Implementation of the AI game summary service.
/// </summary>
public class GameSummaryService : IGameSummaryService
{
    private readonly ILlmProvider _llmProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<GameSummaryService> _logger;

    public GameSummaryService(
        ILlmProvider llmProvider,
        ITimeProvider timeProvider,
        ILogger<GameSummaryService> logger)
    {
        _llmProvider = llmProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<GameJourneySummary>> GenerateJourneySummaryAsync(
        Guid gameId,
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating journey summary for game {GameId}, user {UserId}", gameId, userId);

            // In a real implementation, this would fetch actual game session data
            // For now, we'll create a template-based summary
            var milestones = new List<Milestone>
            {
                new()
                {
                    Title = "Journey Begins",
                    Description = "First steps into the game world",
                    Date = _timeProvider.UtcNow.AddDays(-30),
                    Icon = "🎮"
                },
                new()
                {
                    Title = "First Victory",
                    Description = "Completed the first major challenge",
                    Date = _timeProvider.UtcNow.AddDays(-25),
                    Icon = "🏆"
                }
            };

            var summary = new GameJourneySummary
            {
                Narrative = "Your adventure has been epic! From humble beginnings to becoming a seasoned player.",
                KeyMoments = milestones,
                PlaytimeSummary = "You've spent quality time mastering this game.",
                AchievementSummary = "Great progress on achievements!",
                FunnyMoment = "That time you accidentally discovered the secret skip..."
            };

            // Try to enhance with AI if available
            if (_llmProvider.IsAvailable)
            {
                var aiSummary = await TryGenerateAiSummaryAsync(gameId, userId, ct);
                if (aiSummary.IsSuccess)
                {
                    summary = summary with { Narrative = aiSummary.Value };
                }
            }

            return Result<GameJourneySummary>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate journey summary for game {GameId}", gameId);
            return Result<GameJourneySummary>.Failure("Failed to generate journey summary", ErrorType.Internal);
        }
    }

    public async Task<Result<string>> GenerateStatsStoryAsync(
        GameStats stats,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating stats story for {GameTitle}", stats.GameTitle);

            if (!_llmProvider.IsAvailable)
            {
                // Fallback to template-based generation
                var story = GenerateTemplateStory(stats);
                return Result<string>.Success(story);
            }

            var prompt = $@"
Create an engaging, personalized story about a player's journey in {stats.GameTitle}.

Player Stats:
- Total Playtime: {stats.TotalPlaytime.TotalHours:F1} hours
- Number of Sessions: {stats.SessionsCount}
- Achievements: {stats.AchievementsUnlocked}/{stats.TotalAchievements}
- First Played: {stats.FirstPlayed:MMM dd, yyyy}
- Last Played: {stats.LastPlayed:MMM dd, yyyy}
- Favorite Activities: {string.Join(", ", stats.FavoriteActivities)}

Write a 2-3 paragraph narrative that:
1. Captures the essence of their gaming journey
2. Highlights their dedication and achievements
3. Includes a touch of humor or personality
4. Ends with encouragement for future play

Make it feel personal and celebratory!";

            var result = await _llmProvider.CompleteAsync(
                new CompletionRequest(prompt, "gpt-3.5-turbo", 500, 0.8f),
                ct);

            if (result.IsFailure)
            {
                _logger.LogWarning("AI story generation failed, using template fallback");
                var fallbackStory = GenerateTemplateStory(stats);
                return Result<string>.Success(fallbackStory);
            }

            return Result<string>.Success(result.Value.Text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate stats story for {GameTitle}", stats.GameTitle);
            return Result<string>.Failure("Failed to generate stats story", ErrorType.Internal);
        }
    }

    private async Task<Result<string>> TryGenerateAiSummaryAsync(
        Guid gameId,
        Guid userId,
        CancellationToken ct)
    {
        try
        {
            var prompt = $@"
Create a brief, engaging summary (2-3 sentences) of a player's journey in their game.
This should feel personal and highlight their dedication as a gamer.
Make it celebratory and fun!";

            var result = await _llmProvider.CompleteAsync(
                new CompletionRequest(prompt, "gpt-3.5-turbo", 200, 0.7f),
                ct);

            return result.IsSuccess
                ? Result<string>.Success(result.Value.Text)
                : Result<string>.Failure("AI generation failed", ErrorType.External);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI summary generation failed");
            return Result<string>.Failure("AI generation failed", ErrorType.Internal);
        }
    }

    private string GenerateTemplateStory(GameStats stats)
    {
        var daysPlayed = (stats.LastPlayed - stats.FirstPlayed).Days;
        var avgSessionLength = stats.SessionsCount > 0
            ? stats.TotalPlaytime.TotalHours / stats.SessionsCount
            : 0;

        var story = $"""
            Your adventure in {stats.GameTitle} has been nothing short of legendary!

            Over the past {daysPlayed} days, you've dedicated {stats.TotalPlaytime.TotalHours:F1} hours across {stats.SessionsCount} gaming sessions—that's an average of {avgSessionLength:F1} hours per session! Your commitment to mastering this game shows true dedication.

            You've unlocked {stats.AchievementsUnlocked} out of {stats.TotalAchievements} achievements, proving your skills and determination. Whether you were {string.Join(", ", stats.FavoriteActivities.Take(2))}, every moment contributed to your unique journey.

            From your first play on {stats.FirstPlayed:MMM dd, yyyy} to your most recent session on {stats.LastPlayed:MMM dd, yyyy}, you've grown as a player and created memories that will last. Keep gaming, keep exploring, and most importantly—keep having fun!
            """;

        return story;
    }
}
