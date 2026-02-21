using SaveState.Core.Common;
using SaveState.Core.AiCoOp.Models;

namespace SaveState.Core.AiCoOp.Services;

/// <summary>
/// Service interface for the AI Co-Op Companion that plays alongside users in single-player games.
/// Provides voice interaction, adaptive playstyle, and intelligent assistance.
/// </summary>
public interface IAiCoOpCompanionService
{
    /// <summary>
    /// Initializes the companion with the specified configuration.
    /// </summary>
    /// <param name="config">The companion configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> InitializeCompanionAsync(
        CompanionConfiguration config,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the next action the companion should take based on current game state.
    /// </summary>
    /// <param name="gameState">The current snapshot of the game state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the companion action.</returns>
    Task<Result<CompanionAction>> GetNextActionAsync(
        GameStateSnapshot gameState,
        CancellationToken ct = default);

    /// <summary>
    /// Processes a voice command from the player.
    /// </summary>
    /// <param name="voiceInput">The transcribed voice input.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the companion's response.</returns>
    Task<Result<string>> ProcessVoiceCommandAsync(
        string voiceInput,
        CancellationToken ct = default);

    /// <summary>
    /// Learns from a player behavior sample to adapt the companion's playstyle.
    /// </summary>
    /// <param name="sample">The player behavior sample.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> LearnFromPlayerAsync(
        PlayerBehaviorSample sample,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a conversational response to a player message.
    /// </summary>
    /// <param name="playerMessage">The message from the player.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the companion's response text.</returns>
    Task<Result<string>> GenerateResponseAsync(
        string playerMessage,
        CancellationToken ct = default);

    /// <summary>
    /// Enables voice output for the companion.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> EnableVoiceAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Disables voice output for the companion.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DisableVoiceAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets the chat history between player and companion.
    /// </summary>
    /// <param name="count">Maximum number of messages to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the list of chat messages.</returns>
    Task<Result<IReadOnlyList<CompanionChatMessage>>> GetChatHistoryAsync(
        int count = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Clears the chat history.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ClearChatHistoryAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current companion configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the current configuration.</returns>
    Task<Result<CompanionConfiguration>> GetConfigurationAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Updates the companion configuration.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> UpdateConfigurationAsync(
        CompanionConfiguration config,
        CancellationToken ct = default);
}
