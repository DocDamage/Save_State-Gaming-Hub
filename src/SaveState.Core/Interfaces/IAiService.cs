using SaveState.Core.Entities;
using SaveState.Core.Models;

namespace SaveState.Core.Interfaces;

public interface IAiService
{
    /// <summary>
    /// Chat with AI about gaming, get tips, walkthroughs, etc.
    /// </summary>
    Task<string> ChatAsync(string message, IEnumerable<AiChatMessage>? history = null);

    /// <summary>
    /// Get game recommendations based on user's library and preferences
    /// </summary>
    Task<IEnumerable<GameRecommendation>> GetRecommendationsAsync(IEnumerable<Game> libraryGames, int count = 5);

    /// <summary>
    /// Generate AI description for a game
    /// </summary>
    Task<AiAnalysisResult> AnalyzeGameAsync(string gameTitle);

    /// <summary>
    /// Find similar games based on a source game
    /// </summary>
    Task<IEnumerable<string>> FindSimilarGamesAsync(Game game, int count = 5);

    /// <summary>
    /// Get gaming tips for a specific game
    /// </summary>
    Task<string> GetGameTipsAsync(string gameTitle);

    /// <summary>
    /// Check if AI service is properly configured
    /// </summary>
    bool IsConfigured { get; }
}
