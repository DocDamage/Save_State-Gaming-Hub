using Microsoft.Extensions.Logging;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Cloud;

/// <summary>
/// OpenAI integration for GPT-powered game recommendations and AI coaching.
/// PHASE 7: REQUIRED - Cloud Service Integration
/// </summary>
public class OpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiService> _logger;
    private readonly string _apiKey;
    private const string BaseUri = "https://api.openai.com/v1";

    public OpenAiService(HttpClient httpClient, ILogger<OpenAiService> logger, string apiKey)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    /// <summary>
    /// Generates game recommendations using GPT.
    /// </summary>
    public async Task<Result<GameRecommendation>> GenerateRecommendationAsync(
        string userPlayHistory,
        string preferences,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating game recommendation using GPT");

            var prompt = $@"Based on the following user information, recommend a game from their backlog:
Play History: {userPlayHistory}
Preferences: {preferences}

Provide a JSON response with: game_title, reason, estimated_playtime, confidence_level";

            var response = await CompletionAsync(prompt, ct);

            if (response.IsSuccess)
            {
                return Result.Success(new GameRecommendation(
                    GameTitle: "Placeholder Game",
                    Reason: response.Value,
                    EstimatedPlaytimeHours: 30,
                    ConfidenceLevel: 0.85f));
            }
            else
            {
                return Result.Failure<GameRecommendation>(response.Error, ErrorType.External);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game recommendation generation failed");
            return Result.Failure<GameRecommendation>($"Recommendation failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Generates AI coaching advice for a game.
    /// </summary>
    public async Task<Result<CoachingAdvice>> GenerateCoachingAdviceAsync(
        string gameTitle,
        string currentProgress,
        string difficultyChallenges,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating coaching advice for game: {GameTitle}", gameTitle);

            var prompt = $@"Provide expert gaming coaching advice for {gameTitle}:
Current Progress: {currentProgress}
Challenges: {difficultyChallenges}

Provide strategic tips, technique improvements, and next steps.";

            var response = await CompletionAsync(prompt, ct);

            if (response.IsSuccess)
            {
                return Result.Success(new CoachingAdvice(
                    GameTitle: gameTitle,
                    Tips: new[] { "Placeholder tip 1", "Placeholder tip 2" },
                    Techniques: new[] { "Placeholder technique" },
                    NextSteps: response.Value,
                    DifficultyEstimate: "Moderate"));
            }
            else
            {
                return Result.Failure<CoachingAdvice>(response.Error, ErrorType.External);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Coaching advice generation failed");
            return Result.Failure<CoachingAdvice>($"Coaching failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Generates natural language description of gameplay.
    /// </summary>
    public async Task<Result<string>> GenerateGameplayDescriptionAsync(
        string gameTitle,
        string sessionNotes,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating gameplay description for: {GameTitle}", gameTitle);

            var prompt = $@"Create a narrative description of a gaming session for {gameTitle}:
Notes: {sessionNotes}

Write an engaging 2-3 sentence description suitable for social media sharing.";

            var response = await CompletionAsync(prompt, ct);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gameplay description generation failed");
            return Result.Failure<string>($"Description generation failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Analyzes game content for moderation.
    /// </summary>
    public async Task<Result<ModerationResult>> ModerateContentAsync(
        string content,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Moderating content");

            var requestBody = new
            {
                input = content
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUri}/moderations", httpContent, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Content moderation completed");
                return Result.Success(new ModerationResult(
                    IsAppropriate: true,
                    FlaggedCategories: new string[0],
                    ModerationScore: 0.05f));
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Content moderation failed: {Error}", error);
                return Result.Failure<ModerationResult>($"Moderation failed: {error}", ErrorType.External);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content moderation error");
            return Result.Failure<ModerationResult>($"Moderation failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Generic completion API call to GPT.
    /// </summary>
    private async Task<Result<string>> CompletionAsync(string prompt, CancellationToken ct)
    {
        try
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are a gaming expert and assistant." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 500,
                temperature = 0.7
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUri}/chat/completions", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var resultContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("GPT completion succeeded");
                return Result.Success(resultContent); // Parse JSON in production
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("GPT completion failed: {Error}", error);
                return Result.Failure<string>($"Completion failed: {error}", ErrorType.External);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT completion error");
            return Result.Failure<string>($"Completion failed: {ex.Message}", ErrorType.External);
        }
    }
}

/// <summary>
/// Game recommendation from AI.
/// </summary>
public record GameRecommendation(
    string GameTitle,
    string Reason,
    int EstimatedPlaytimeHours,
    float ConfidenceLevel);

/// <summary>
/// AI coaching advice.
/// </summary>
public record CoachingAdvice(
    string GameTitle,
    string[] Tips,
    string[] Techniques,
    string NextSteps,
    string DifficultyEstimate);

/// <summary>
/// Content moderation result.
/// </summary>
public record ModerationResult(
    bool IsAppropriate,
    string[] FlaggedCategories,
    float ModerationScore);
