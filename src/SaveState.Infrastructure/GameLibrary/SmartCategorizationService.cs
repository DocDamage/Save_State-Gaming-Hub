using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Entities;
using System.Text.Json;

namespace SaveState.Infrastructure.GameLibrary;

/// <summary>
/// AI-powered service for intelligent game categorization and tagging.
/// Automatically categorizes games based on metadata, content analysis, and user patterns.
/// </summary>
public class SmartCategorizationService : ISmartCategorizationService
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<SmartCategorizationService> _logger;

    public SmartCategorizationService(
        IAiOrchestrator aiOrchestrator,
        IGameRepository gameRepository,
        ILogger<SmartCategorizationService> logger)
    {
        _aiOrchestrator = aiOrchestrator;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<GameTags>> AnalyzeGameAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
                return Result<GameTags>.Failure("Game not found", ErrorType.NotFound);

            var prompt = BuildAnalysisPrompt(game);
            var sessionId = $"game-analysis-{gameId}";

            var response = await _aiOrchestrator.ProcessRequestWithContextAsync(
                sessionId,
                new AiRequest(AiRequestType.Completion, Prompt: prompt),
                ct);

            if (!response.IsSuccessful)
            {
                _logger.LogWarning("AI analysis failed for game {GameId}: {Error}", gameId, response.Error);
                return Result<GameTags>.Failure($"AI analysis failed: {response.Error}", ErrorType.Internal);
            }

            var tags = ParseAiResponse(response.Content);
            if (tags == null)
            {
                _logger.LogWarning("Failed to parse AI response for game {GameId}", gameId);
                return Result<GameTags>.Failure("Failed to parse AI response", ErrorType.Internal);
            }

            _logger.LogInformation("Successfully analyzed game '{Title}' with confidence {Confidence}",
                game.Title, tags.Confidence);

            return Result<GameTags>.Success(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze game {GameId}", gameId);
            return Result<GameTags>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> AutoTagLibraryAsync(IProgress<TaggingProgress>? progress = null, CancellationToken ct = default)
    {
        try
        {
            var games = await _gameRepository.GetGamesAsync(pageSize: int.MaxValue, ct: ct);
            var totalGames = games.TotalCount;

            _logger.LogInformation("Starting auto-tagging of {Count} games", totalGames);

            for (int i = 0; i < games.Items.Count; i++)
            {
                var game = games.Items[i];

                progress?.Report(new TaggingProgress(i + 1, totalGames, game.Title));

                // Skip if game already has tags (basic check - in real implementation, check a Tags field)
                if (ShouldSkipTagging(game))
                    continue;

                var analysisResult = await AnalyzeGameAsync(game.Id, ct);
                if (analysisResult.IsSuccess)
                {
                    // In a real implementation, you'd save the tags to the game entity
                    // For now, just log the results
                    _logger.LogInformation("Tagged game '{Title}': Genres={Genres}, Confidence={Confidence}",
                        game.Title,
                        string.Join(", ", analysisResult.Value!.Genres),
                        analysisResult.Value.Confidence);
                }

                // Small delay to avoid overwhelming the AI service
                await Task.Delay(100, ct);
            }

            _logger.LogInformation("Completed auto-tagging of library");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-tag library");
            return Result.Failure($"Auto-tagging failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<string>>> SuggestTagsAsync(string gameTitle, string? description, CancellationToken ct = default)
    {
        try
        {
            var prompt = BuildSuggestionPrompt(gameTitle, description);
            var sessionId = $"tag-suggestion-{Guid.NewGuid()}";

            var response = await _aiOrchestrator.ProcessRequestWithContextAsync(
                sessionId,
                new AiRequest(AiRequestType.Completion, Prompt: prompt),
                ct);

            if (!response.IsSuccessful)
            {
                _logger.LogWarning("AI tag suggestion failed for '{Title}': {Error}", gameTitle, response.Error);
                return Result<IReadOnlyList<string>>.Failure($"AI suggestion failed: {response.Error}", ErrorType.Internal);
            }

            var suggestions = ParseTagSuggestions(response.Content);
            return Result<IReadOnlyList<string>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suggest tags for '{Title}'", gameTitle);
            return Result<IReadOnlyList<string>>.Failure($"Suggestion failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private static string BuildAnalysisPrompt(Game game)
    {
        var description = string.IsNullOrEmpty(game.Description)
            ? "No description available"
            : game.Description;

        return $@"Analyze this game and provide categorization:

Title: {game.Title}
Description: {description}
Platform: {game.Platform?.Name ?? "Unknown"}
Description: {game.Description ?? "No description available"}
Source: {game.Source ?? "Unknown"}
Created: {game.CreatedAt.Year}
Total Playtime: {game.TotalPlayTime:hh\\:mm}

Respond with a JSON object containing:
{{
  ""genres"": [""array of genre tags""],
  ""themes"": [""array of thematic elements like post-apocalyptic, fantasy""],
  ""moods"": [""array of mood descriptors like relaxing, intense""],
  ""mechanics"": [""array of gameplay mechanics like turn-based, crafting""],
  ""suggestedRating"": ""ESRB-style rating or null"",
  ""confidence"": 0.0-1.0
}}

Be specific and accurate. Only include tags that clearly apply based on the game information.";
    }

    private static string BuildSuggestionPrompt(string gameTitle, string? description)
    {
        var desc = string.IsNullOrEmpty(description)
            ? "No description available"
            : description;

        return $@"Suggest relevant tags for this game:

Title: {gameTitle}
Description: {desc}

Provide 5-10 relevant tags that would help categorize this game.
Focus on genres, themes, gameplay styles, and mood.
Return as a JSON array of strings.

Example: [""Action"", ""Adventure"", ""Fantasy"", ""Single-player"", ""Atmospheric""]";
    }

    private static GameTags? ParseAiResponse(string aiResponse)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');

            if (jsonStart == -1 || jsonEnd == -1 || jsonEnd <= jsonStart)
                return null;

            var json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var genres = root.TryGetProperty("genres", out var genresProp) && genresProp.ValueKind == JsonValueKind.Array
                ? genresProp.EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();

            var themes = root.TryGetProperty("themes", out var themesProp) && themesProp.ValueKind == JsonValueKind.Array
                ? themesProp.EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();

            var moods = root.TryGetProperty("moods", out var moodsProp) && moodsProp.ValueKind == JsonValueKind.Array
                ? moodsProp.EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();

            var mechanics = root.TryGetProperty("mechanics", out var mechanicsProp) && mechanicsProp.ValueKind == JsonValueKind.Array
                ? mechanicsProp.EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrEmpty(s)).ToList()
                : new List<string>();

            var suggestedRating = root.TryGetProperty("suggestedRating", out var ratingProp) && ratingProp.ValueKind == JsonValueKind.String
                ? ratingProp.GetString()
                : null;

            var confidence = root.TryGetProperty("confidence", out var confidenceProp) && confidenceProp.TryGetSingle(out var conf)
                ? Math.Clamp(conf, 0f, 1f)
                : 0.5f;

            return new GameTags(
                Genres: genres.AsReadOnly(),
                Themes: themes.AsReadOnly(),
                Moods: moods.AsReadOnly(),
                Mechanics: mechanics.AsReadOnly(),
                SuggestedRating: suggestedRating,
                Confidence: confidence);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ParseTagSuggestions(string aiResponse)
    {
        try
        {
            // Try to extract JSON array from the response
            var jsonStart = aiResponse.IndexOf('[');
            var jsonEnd = aiResponse.LastIndexOf(']');

            if (jsonStart == -1 || jsonEnd == -1 || jsonEnd <= jsonStart)
                return Array.Empty<string>();

            var json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);

            var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray()
                .Select(e => e.GetString()!)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    private static bool ShouldSkipTagging(Game game)
    {
        // In a real implementation, check if the game already has tags stored
        // For now, always attempt tagging
        return false;
    }
}