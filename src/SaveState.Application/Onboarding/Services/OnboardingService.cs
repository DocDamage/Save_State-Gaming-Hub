namespace SaveState.Application.Onboarding.Services;

using SaveState.Core.GameLibrary;
using SaveState.Core.Ai.Services;

/// <summary>
/// Service responsible for generating personalized onboarding experiences using AI.
/// Analyzes the user's game library and provides tailored welcome messages and feature suggestions.
/// </summary>
public class OnboardingService
{
    private readonly IAiOrchestrator _ai;
    private readonly IGameRepository _games;

    /// <summary>
    /// Initializes a new instance of the OnboardingService.
    /// </summary>
    /// <param name="ai">The AI orchestrator for generating personalized content.</param>
    /// <param name="games">The game repository for analyzing the user's library.</param>
    public OnboardingService(IAiOrchestrator ai, IGameRepository games)
    {
        _ai = ai;
        _games = games;
    }

    /// <summary>
    /// Generates a personalized welcome message based on the user's game library.
    /// Analyzes game count, platforms, and suggests high-impact features to try first.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A personalized welcome message with feature suggestions.</returns>
    public async Task<string> GeneratePersonalizedWelcomeAsync(CancellationToken ct = default)
    {
        // Use efficient count instead of loading all games
        var gameCount = await _games.CountAsync(ct);

        // Get a small sample of games to extract platform information
        var sampleGames = await _games.GetGamesAsync(
            pageNumber: 1,
            pageSize: 100, // Reasonable sample size for platform diversity
            ct: ct);

        var platformNames = sampleGames.Items
            .Where(g => g.Platform != null)
            .Select(g => g.Platform?.Name ?? "Unknown")
            .Distinct()
            .Take(5)
            .ToList();

        var platformText = platformNames.Any()
            ? $"across platforms: {string.Join(", ", platformNames)}"
            : "in their collection";

        var prompt = $"User has {gameCount} games {platformText}. " +
                     $"Create a persona-driven welcome message for SaveState Reborn and suggest 3 high-impact features to try first. " +
                     $"Keep it friendly, informative, and exciting.";

        var request = new AiRequest(AiRequestType.Chat, Prompt: prompt);
        var response = await _ai.ProcessRequestAsync(request, ct);

        return response.Content;
    }

}
