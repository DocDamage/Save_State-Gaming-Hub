using SaveState.Core.AiCoOp.Models;
using SaveState.Core.Common;

namespace SaveState.Core.AiCoOp.Services;

/// <summary>
/// Service that provides AI Co-Op companion functionality for gaming assistance.
/// </summary>
public interface IAiCoOpCompanionService
{
    /// <summary>
    /// Initializes the companion for a specific game session.
    /// </summary>
    /// <param name="gameId">The game identifier.</param>
    /// <param name="personality">The companion personality configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeCompanionAsync(string gameId, CompanionPersonality personality, CancellationToken ct = default);

    /// <summary>
    /// Parses the current game state to provide context to the AI.
    /// </summary>
    /// <param name="rawGameData">Raw game state data from memory reader or API.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing parsed game context snapshot.</returns>
    Task<Result<GameContextSnapshot>> ParseGameStateAsync(byte[] rawGameData, CancellationToken ct = default);

    /// <summary>
    /// Processes game context and generates appropriate companion actions.
    /// </summary>
    /// <param name="context">The current game context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing list of suggested actions.</returns>
    Task<Result<IReadOnlyList<CompanionAction>>> ProcessGameContextAsync(GameContextSnapshot context, CancellationToken ct = default);

    /// <summary>
    /// Executes a companion action.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the action execution.</returns>
    Task<Result<ActionExecutionResult>> ExecuteActionAsync(CompanionAction action, CancellationToken ct = default);

    /// <summary>
    /// Generates a contextual suggestion based on current game state.
    /// </summary>
    /// <param name="context">The current game context.</param>
    /// <param name="suggestionType">Type of suggestion requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the companion suggestion.</returns>
    Task<Result<CompanionSuggestion>> GenerateSuggestionAsync(GameContextSnapshot context, SuggestionType? suggestionType = null, CancellationToken ct = default);

    /// <summary>
    /// Records player behavior for learning patterns.
    /// </summary>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="action">The player action taken.</param>
    /// <param name="context">The game context when action was taken.</param>
    /// <param name="outcome">The outcome of the action.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> RecordPlayerBehaviorAsync(string playerId, string action, GameContextSnapshot context, string outcome, CancellationToken ct = default);

    /// <summary>
    /// Analyzes recorded behaviors and learns patterns.
    /// </summary>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing learned behavior patterns.</returns>
    Task<Result<IReadOnlyList<PlayerBehaviorPattern>>> LearnPatternsAsync(string playerId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current behavior profile for a player.
    /// </summary>
    /// <param name="playerId">The player identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the player behavior profile.</returns>
    Task<Result<PlayerBehaviorProfile>> GetPlayerBehaviorProfileAsync(string playerId, CancellationToken ct = default);

    /// <summary>
    /// Updates the companion personality configuration.
    /// </summary>
    /// <param name="personality">The new personality configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdatePersonalityAsync(CompanionPersonality personality, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active companion personality.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the active personality configuration.</returns>
    Task<Result<CompanionPersonality>> GetActivePersonalityAsync(CancellationToken ct = default);

    /// <summary>
    /// Notifies the companion of a game event.
    /// </summary>
    /// <param name="eventType">Type of game event.</param>
    /// <param name="eventData">Event data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing any companion response actions.</returns>
    Task<Result<IReadOnlyList<CompanionAction>>> NotifyGameEventAsync(string eventType, IReadOnlyDictionary<string, object> eventData, CancellationToken ct = default);

    /// <summary>
    /// Shuts down the companion and saves learning data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ShutdownAsync(CancellationToken ct = default);
}
