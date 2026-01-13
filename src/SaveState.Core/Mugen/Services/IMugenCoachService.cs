namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service interface for AI-powered MUGEN coaching.
/// Provides matchup advice, character guides, and training assistance.
/// </summary>
public interface IMugenCoachService
{
    /// <summary>
    /// Gets a simple coaching advice string for a character.
    /// </summary>
    /// <param name="characterId">Character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Personalized coaching advice.</returns>
    Task<string?> GetCoachingAdviceAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets matchup advice for two specific characters.
    /// </summary>
    /// <param name="characterId">Your character ID.</param>
    /// <param name="opponentId">Opponent character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matchup advice.</returns>
    Task<Result<MatchupAdvice>> GetMatchupAdviceAsync(Guid characterId, Guid opponentId, CancellationToken ct = default);

    /// <summary>
    /// Gets recommended counter-picks for an opponent character.
    /// </summary>
    /// <param name="opponentId">Opponent character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of recommended counter-pick character IDs.</returns>
    Task<Result<IReadOnlyList<Guid>>> GetCounterPicksAsync(Guid opponentId, CancellationToken ct = default);

    /// <summary>
    /// Gets a detailed character guide.
    /// </summary>
    /// <param name="characterId">Character ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The character guide.</returns>
    Task<Result<CharacterGuide>> GetCharacterGuideAsync(Guid characterId, CancellationToken ct = default);

    /// <summary>
    /// Analyzes a replay file and provides feedback.
    /// </summary>
    /// <param name="replayPath">Path to the replay file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of analysis feedback.</returns>
    Task<Result<IReadOnlyList<string>>> AnalyzeReplayAsync(string replayPath, CancellationToken ct = default);

    /// <summary>
    /// Sends a chat message to the AI coach and gets a response.
    /// </summary>
    /// <param name="userMessage">The user's message/question.</param>
    /// <param name="context">Optional context (character ID, recent match, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The coach's response.</returns>
    Task<Result<string>> SendChatMessageAsync(string userMessage, string? context = null, CancellationToken ct = default);
}
