using SaveState.Core.Common;
using SaveState.Core.Input.Services.DTOs;

namespace SaveState.Core.Input.Services;

/// <summary>
/// Service for processing voice commands and controlling gaming operations.
/// </summary>
public interface IVoiceCommandService
{
    /// <summary>
    /// Starts listening for voice commands.
    /// </summary>
    Task<Result> StartListeningAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops listening for voice commands.
    /// </summary>
    Task<Result> StopListeningAsync(CancellationToken ct = default);

    /// <summary>
    /// Processes a voice command and executes the appropriate action.
    /// </summary>
    Task<Result<VoiceCommandResult>> ProcessVoiceCommandAsync(
        string spokenText,
        CancellationToken ct = default);

    /// <summary>
    /// Registers a new voice command with its associated action.
    /// </summary>
    Task<Result> RegisterCommandAsync(
        VoiceCommandDefinition command,
        CancellationToken ct = default);

    /// <summary>
    /// Unregisters a voice command.
    /// </summary>
    Task<Result> UnregisterCommandAsync(
        string commandPhrase,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all registered voice commands.
    /// </summary>
    Task<Result<IReadOnlyList<VoiceCommandDefinition>>> GetRegisteredCommandsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Trains the voice recognition system with additional phrases.
    /// </summary>
    Task<Result> TrainVoiceModelAsync(
        IReadOnlyList<string> trainingPhrases,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current listening status.
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Event raised when a voice command is recognized and processed.
    /// </summary>
    event EventHandler<VoiceCommandRecognizedEventArgs>? VoiceCommandRecognized;

    /// <summary>
    /// Event raised when listening status changes.
    /// </summary>
    event EventHandler<ListeningStatusChangedEventArgs>? ListeningStatusChanged;
}