using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
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
    private readonly ILogger<GameAssistantService> _logger;

    public GameAssistantService(
        IAiOrchestrator aiOrchestrator,
        IGameRepository gameRepository,
        ISmartCategorizationService categorizationService,
        ILogger<GameAssistantService> logger)
    {
        _aiOrchestrator = aiOrchestrator;
        _gameRepository = gameRepository;
        _categorizationService = categorizationService;
        _logger = logger;
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
                return Result<AssistantResponse>.Failure("Game not found", ErrorType.NotFound);

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
                return Result<AssistantResponse>.Failure($"AI assistant failed: {response.Error}", ErrorType.Internal);
            }

            var assistantResponse = ParseAssistantResponse(response.Content, question);
            return Result<AssistantResponse>.Success(assistantResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get assistant response for game {GameId}", gameId);
            return Result<AssistantResponse>.Failure($"Assistant query failed: {ex.Message}", ErrorType.Internal);
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
                return Result<IReadOnlyList<string>>.Failure("Game not found", ErrorType.NotFound);

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
                return Result<IReadOnlyList<string>>.Failure("Could not generate tips", ErrorType.Internal);
            }

            var tips = ParseTipsResponse(response.Content);
            return Result<IReadOnlyList<string>>.Success(tips);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quick tips for game {GameId}", gameId);
            return Result<IReadOnlyList<string>>.Failure($"Tips generation failed: {ex.Message}", ErrorType.Internal);
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
                return Result<string>.Failure("Game not found", ErrorType.NotFound);

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
                return Result<string>.Failure("Could not generate hint", ErrorType.Internal);
            }

            return Result<string>.Success(response.Content.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get walkthrough hint for game {GameId}", gameId);
            return Result<string>.Failure($"Hint generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result> ClearContextAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var sessionId = $"game-assistant-{gameId}";

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
