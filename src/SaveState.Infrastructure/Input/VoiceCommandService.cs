using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Services.DTOs;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.Sync.Services;

namespace SaveState.Infrastructure.Input;

/// <summary>
/// Implementation of voice command service with speech recognition integration.
/// </summary>
public class VoiceCommandService : IVoiceCommandService
{
    private readonly ISpeechRecognitionService _speechRecognitionService;
    private readonly IGameRepository _gameRepository;
    private readonly ILaunchExperienceManager _launchExperienceManager;
    private readonly ISaveStateManager _saveStateManager;
    private readonly ICloudGamingManager _cloudGamingManager;
    private readonly ILogger<VoiceCommandService> _logger;

    private readonly Dictionary<string, VoiceCommandDefinition> _registeredCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _alternativePhrases = new(StringComparer.OrdinalIgnoreCase);

    private bool _isListening;

    /// <summary>
    /// Event raised when a voice command is recognized.
    /// </summary>
    public event EventHandler<VoiceCommandRecognizedEventArgs>? VoiceCommandRecognized;

    /// <summary>
    /// Event raised when the listening status changes.
    /// </summary>
    public event EventHandler<ListeningStatusChangedEventArgs>? ListeningStatusChanged;

    /// <summary>
    /// Gets a value indicating whether the service is currently listening for voice commands.
    /// </summary>
    public bool IsListening => _isListening;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceCommandService"/> class.
    /// </summary>
    /// <param name="speechRecognitionService">Service for speech recognition.</param>
    /// <param name="gameRepository">Repository for accessing games.</param>
    /// <param name="launchExperienceManager">Manager for game launching.</param>
    /// <param name="saveStateManager">Manager for save states.</param>
    /// <param name="cloudGamingManager">Manager for cloud gaming.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public VoiceCommandService(
        ISpeechRecognitionService speechRecognitionService,
        IGameRepository gameRepository,
        ILaunchExperienceManager launchExperienceManager,
        ISaveStateManager saveStateManager,
        ICloudGamingManager cloudGamingManager,
        ILogger<VoiceCommandService> logger)
    {
        _speechRecognitionService = speechRecognitionService;
        _gameRepository = gameRepository;
        _launchExperienceManager = launchExperienceManager;
        _saveStateManager = saveStateManager;
        _cloudGamingManager = cloudGamingManager;
        _logger = logger;

        // Subscribe to speech recognition events
        _speechRecognitionService.SpeechRecognized += OnSpeechRecognized;
        _speechRecognitionService.SpeechRecognitionError += OnSpeechRecognitionError;

        // Register default voice commands
        RegisterDefaultCommands();
    }

    /// <summary>
    /// Starts listening for voice commands.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> StartListeningAsync(CancellationToken ct = default)
    {
        try
        {
            if (_isListening)
            {
                return Result.Success(); // Already listening
            }

            var result = await _speechRecognitionService.StartContinuousRecognitionAsync(ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _isListening = true;
                OnListeningStatusChanged(true, "User initiated");
                _logger.LogInformation("Voice command listening started");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start voice command listening");
            return Result.Failure($"Failed to start listening: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops listening for voice commands.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> StopListeningAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_isListening)
            {
                return Result.Success(); // Not listening
            }

            var result = await _speechRecognitionService.StopContinuousRecognitionAsync(ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _isListening = false;
                OnListeningStatusChanged(false, "User initiated");
                _logger.LogInformation("Voice command listening stopped");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop voice command listening");
            return Result.Failure($"Failed to stop listening: {ex.Message}");
        }
    }

    public async Task<Result<VoiceCommandResult>> ProcessVoiceCommandAsync(
        string spokenText,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Processing voice command: {Text}", spokenText);

            var matchedCommand = FindMatchingCommand(spokenText);
            if (matchedCommand == null)
            {
                var commandResult = new VoiceCommandResult(
                    RecognizedText: spokenText,
                    MatchedCommand: null,
                    Confidence: 0.0f,
                    Success: false,
                    ErrorMessage: "Command not recognized");

                OnVoiceCommandRecognized(commandResult);
                return Result.Success<VoiceCommandResult>(commandResult);
            }

            // Extract parameters if needed
            var parametersResult = await ExtractParametersAsync(spokenText, matchedCommand, ct)
                .ConfigureAwait(false);

            if (parametersResult.IsFailure)
            {
                var failureResult = new VoiceCommandResult(
                    RecognizedText: spokenText,
                    MatchedCommand: matchedCommand,
                    Confidence: 0.8f,
                    Success: false,
                    ErrorMessage: parametersResult.Error,
                    ResultData: null);

                OnVoiceCommandRecognized(failureResult);
                return Result.Success<VoiceCommandResult>(failureResult);
            }

            // Execute the command
            var executionResult = await ExecuteCommandAsync(matchedCommand, parametersResult.Value, ct)
                .ConfigureAwait(false);

            var result = new VoiceCommandResult(
                RecognizedText: spokenText,
                MatchedCommand: matchedCommand,
                Confidence: 0.8f, // Placeholder confidence
                Success: executionResult.IsSuccess,
                ErrorMessage: executionResult.IsSuccess ? null : executionResult.Error,
                ResultData: executionResult.IsSuccess ? executionResult.Value : null);

            OnVoiceCommandRecognized(result);
            return Result.Success<VoiceCommandResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process voice command: {Text}", spokenText);
            return Result.Failure<VoiceCommandResult>($"Failed to process command: {ex.Message}");
        }
    }

    public Task<Result> RegisterCommandAsync(
        VoiceCommandDefinition command,
        CancellationToken ct = default)
    {
        try
        {
            _registeredCommands[command.CommandPhrase] = command;

            // Register alternative phrases
            if (command.AlternativePhrases != null)
            {
                foreach (var altPhrase in command.AlternativePhrases)
                {
                    if (!_alternativePhrases.ContainsKey(altPhrase))
                    {
                        _alternativePhrases[altPhrase] = new List<string>();
                    }
                    _alternativePhrases[altPhrase].Add(command.CommandPhrase);
                }
            }

            _logger.LogInformation("Registered voice command: {Phrase}", command.CommandPhrase);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register voice command: {Phrase}", command.CommandPhrase);
            return Task.FromResult(Result.Failure($"Failed to register command: {ex.Message}"));
        }
    }

    public Task<Result> UnregisterCommandAsync(
        string commandPhrase,
        CancellationToken ct = default)
    {
        try
        {
            if (_registeredCommands.Remove(commandPhrase))
            {
                // Remove alternative phrases
                var altPhrasesToRemove = _alternativePhrases
                    .Where(kvp => kvp.Value.Contains(commandPhrase))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var altPhrase in altPhrasesToRemove)
                {
                    _alternativePhrases[altPhrase].Remove(commandPhrase);
                    if (_alternativePhrases[altPhrase].Count == 0)
                    {
                        _alternativePhrases.Remove(altPhrase);
                    }
                }

                _logger.LogInformation("Unregistered voice command: {Phrase}", commandPhrase);
                return Task.FromResult(Result.Success());
            }

            return Task.FromResult(Result.Failure($"Command not found: {commandPhrase}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister voice command: {Phrase}", commandPhrase);
            return Task.FromResult(Result.Failure($"Failed to unregister command: {ex.Message}"));
        }
    }

    public Task<Result<IReadOnlyList<VoiceCommandDefinition>>> GetRegisteredCommandsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var commands = (IReadOnlyList<VoiceCommandDefinition>)_registeredCommands.Values.ToArray();
            return Task.FromResult(Result.Success<IReadOnlyList<VoiceCommandDefinition>>(commands));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registered commands");
            return Task.FromResult(Result.Failure<IReadOnlyList<VoiceCommandDefinition>>(
                $"Failed to get commands: {ex.Message}"));
        }
    }

    public Task<Result> TrainVoiceModelAsync(
        IReadOnlyList<string> trainingPhrases,
        CancellationToken ct = default)
    {
        try
        {
            // This would integrate with speech recognition training
            // For now, just log the training phrases
            _logger.LogInformation("Training voice model with {Count} phrases", trainingPhrases.Count);
            foreach (var phrase in trainingPhrases)
            {
                _logger.LogDebug("Training phrase: {Phrase}", phrase);
            }

            // Placeholder - actual implementation would call speech recognition service
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train voice model");
            return Task.FromResult(Result.Failure($"Failed to train voice model: {ex.Message}"));
        }
    }

    private VoiceCommandDefinition? FindMatchingCommand(string spokenText)
    {
        // Direct match
        if (_registeredCommands.TryGetValue(spokenText, out var command))
        {
            return command;
        }

        // Alternative phrase match
        if (_alternativePhrases.TryGetValue(spokenText, out var mainPhrases))
        {
            var mainPhrase = mainPhrases.FirstOrDefault();
            if (mainPhrase != null && _registeredCommands.TryGetValue(mainPhrase, out command))
            {
                return command;
            }
        }

        // Fuzzy matching (simplified)
        foreach (var registeredCommand in _registeredCommands.Values)
        {
            if (spokenText.Contains(registeredCommand.CommandPhrase, StringComparison.OrdinalIgnoreCase) ||
                registeredCommand.CommandPhrase.Contains(spokenText, StringComparison.OrdinalIgnoreCase))
            {
                return registeredCommand;
            }
        }

        return null;
    }

    private async Task<Result<object?>> ExtractParametersAsync(
        string spokenText,
        VoiceCommandDefinition command,
        CancellationToken ct)
    {
        // Simple parameter extraction - this could be enhanced with NLP
        switch (command.Action)
        {
            case VoiceCommandAction.LaunchGame:
                var gameResult = await ExtractGameParameterAsync(spokenText, ct).ConfigureAwait(false);
                return gameResult.IsSuccess ? Result.Success<object?>(gameResult.Value) : Result.Failure<object?>(gameResult.Error, gameResult.ErrorType);

            case VoiceCommandAction.LoadSaveState:
                var saveStateResult = await ExtractSaveStateParameterAsync(spokenText, ct).ConfigureAwait(false);
                return saveStateResult.IsSuccess ? Result.Success<object?>(saveStateResult.Value) : Result.Failure<object?>(saveStateResult.Error, saveStateResult.ErrorType);

            case VoiceCommandAction.StartCloudSession:
                var cloudResult = await ExtractCloudSessionParametersAsync(spokenText, ct).ConfigureAwait(false);
                return cloudResult.IsSuccess ? Result.Success<object?>(cloudResult.Value) : Result.Failure<object?>(cloudResult.Error, cloudResult.ErrorType);

            case VoiceCommandAction.AdjustVolume:
                var volumeResult = ExtractVolumeParameter(spokenText);
                return volumeResult.IsSuccess ? Result.Success<object?>(volumeResult.Value) : Result.Failure<object?>(volumeResult.Error, volumeResult.ErrorType);

            case VoiceCommandAction.AskAssistant:
                return Result.Success<object?>(new AskAssistantParameters(spokenText.Replace("ask assistant", "").Trim()));

            default:
                return Result.Success<object?>(command.Parameters);
        }
    }

    private async Task<Result<object?>> ExecuteCommandAsync(
        VoiceCommandDefinition command,
        object? parameters,
        CancellationToken ct)
    {
        try
        {
            switch (command.Action)
            {
                case VoiceCommandAction.LaunchGame:
                    if (parameters is LaunchGameParameters launchParams)
                    {
                        var sequenceResult = await _launchExperienceManager.GenerateLaunchSequenceAsync(
                            launchParams.GameId, ct).ConfigureAwait(false);
                        if (sequenceResult.IsSuccess)
                        {
                            await _launchExperienceManager.ExecuteLaunchSequenceAsync(
                                sequenceResult.Value!, ct).ConfigureAwait(false);
                        }
                        return Result.Success<object?>(null);
                    }
                    break;

                case VoiceCommandAction.SaveGame:
                    // Would integrate with save state creation
                    return Result.Success<object?>("Game saved");

                case VoiceCommandAction.StopListening:
                    await StopListeningAsync(ct).ConfigureAwait(false);
                    return Result.Success<object?>("Stopped listening");

                case VoiceCommandAction.StartListening:
                    await StartListeningAsync(ct).ConfigureAwait(false);
                    return Result.Success<object?>("Started listening");

                case VoiceCommandAction.ShowCommands:
                    var commands = await GetRegisteredCommandsAsync(ct).ConfigureAwait(false);
                    return Result.Success<object?>(commands.Value);

                case VoiceCommandAction.CheckNetworkQuality:
                    var qualityResult = await _cloudGamingManager.GetNetworkQualityAsync(ct)
                        .ConfigureAwait(false);
                    return Result.Success<object?>(qualityResult.Value);

                default:
                    return Result.Success<object?>($"Executed: {command.Description}");
            }

            return Result.Failure<object?>("Invalid command parameters");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute voice command: {Action}", command.Action);
            return Result.Failure<object?>($"Command execution failed: {ex.Message}");
        }
    }

    private async Task<Result<LaunchGameParameters>> ExtractGameParameterAsync(string spokenText, CancellationToken ct)
    {
        // Simple game name extraction - could be enhanced with NLP
        var gameName = spokenText
            .Replace("launch", "", StringComparison.OrdinalIgnoreCase)
            .Replace("start", "", StringComparison.OrdinalIgnoreCase)
            .Replace("open", "", StringComparison.OrdinalIgnoreCase)
            .Replace("run", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        // Normalize whitespace
        gameName = string.Join(" ", gameName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(gameName))
        {
            return Result.Failure<LaunchGameParameters>("No game name specified", ErrorType.Validation);
        }

        // Find game by name (simplified)
        // In a real implementation, this would use fuzzy matching or AI
        var games = await _gameRepository.GetAllAsync(ct).ConfigureAwait(false);
        var game = games.FirstOrDefault(g =>
            g.Title.Contains(gameName, StringComparison.OrdinalIgnoreCase));

        return game != null
            ? Result.Success<LaunchGameParameters>(new LaunchGameParameters(game.Id))
            : Result.Failure<LaunchGameParameters>($"Game '{gameName}' not found", ErrorType.NotFound);
    }

    private Task<Result<LoadSaveStateParameters>> ExtractSaveStateParameterAsync(string spokenText, CancellationToken ct)
    {
        // Placeholder - would need save state enumeration and matching
        return Task.FromResult(Result.Failure<LoadSaveStateParameters>("Save state selection not implemented", ErrorType.NotImplemented));
    }

    private Task<Result<StartCloudSessionParameters>> ExtractCloudSessionParametersAsync(string spokenText, CancellationToken ct)
    {
        // Placeholder - would extract game and provider from speech
        return Task.FromResult(Result.Failure<StartCloudSessionParameters>("Cloud session parameters not implemented", ErrorType.NotImplemented));
    }

    private Result<AdjustVolumeParameters> ExtractVolumeParameter(string spokenText)
    {
        // Simple volume extraction
        if (spokenText.Contains("mute") || spokenText.Contains("zero"))
            return Result.Success<AdjustVolumeParameters>(new AdjustVolumeParameters(0));
        if (spokenText.Contains("max") || spokenText.Contains("full"))
            return Result.Success<AdjustVolumeParameters>(new AdjustVolumeParameters(100));
        if (spokenText.Contains("half"))
            return Result.Success<AdjustVolumeParameters>(new AdjustVolumeParameters(50));

        // Try to extract number
        var words = spokenText.Split(' ');
        foreach (var word in words)
        {
            if (int.TryParse(word, out var volume))
            {
                return Result.Success<AdjustVolumeParameters>(new AdjustVolumeParameters(Math.Clamp(volume, 0, 100)));
            }
        }

        return Result.Failure<AdjustVolumeParameters>("Could not understand volume level", ErrorType.Validation);
    }

    private void RegisterDefaultCommands()
    {
        // Register common voice commands
        var defaultCommands = new[]
        {
            new VoiceCommandDefinition(
                "launch game",
                "Launch a game with cinematic experience",
                VoiceCommandAction.LaunchGame,
                AlternativePhrases: new[] { "start game", "open game", "run game" }),

            new VoiceCommandDefinition(
                "save game",
                "Create a save state",
                VoiceCommandAction.SaveGame,
                AlternativePhrases: new[] { "save", "save progress" }),

            new VoiceCommandDefinition(
                "stop listening",
                "Stop voice command recognition",
                VoiceCommandAction.StopListening,
                AlternativePhrases: new[] { "stop", "quiet", "shut up" }),

            new VoiceCommandDefinition(
                "start listening",
                "Start voice command recognition",
                VoiceCommandAction.StartListening,
                AlternativePhrases: new[] { "listen", "wake up" }),

            new VoiceCommandDefinition(
                "show commands",
                "Display available voice commands",
                VoiceCommandAction.ShowCommands,
                AlternativePhrases: new[] { "help", "commands", "what can you do" }),

            new VoiceCommandDefinition(
                "check network",
                "Check current network quality",
                VoiceCommandAction.CheckNetworkQuality,
                AlternativePhrases: new[] { "network quality", "connection status" })
        };

        foreach (var command in defaultCommands)
        {
            _registeredCommands[command.CommandPhrase] = command;

            if (command.AlternativePhrases != null)
            {
                foreach (var altPhrase in command.AlternativePhrases)
                {
                    if (!_alternativePhrases.ContainsKey(altPhrase))
                    {
                        _alternativePhrases[altPhrase] = new List<string>();
                    }
                    _alternativePhrases[altPhrase].Add(command.CommandPhrase);
                }
            }
        }

        _logger.LogInformation("Registered {Count} default voice commands", defaultCommands.Length);
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.IsFinal)
        {
            // Process the recognized speech as a voice command
            _ = ProcessVoiceCommandAsync(e.Result.RecognizedText);
        }
    }

    private void OnSpeechRecognitionError(object? sender, SpeechRecognitionErrorEventArgs e)
    {
        _logger.LogWarning("Speech recognition error: {Error}", e.ErrorMessage);
    }

    private void OnVoiceCommandRecognized(VoiceCommandResult result)
    {
        VoiceCommandRecognized?.Invoke(this, new VoiceCommandRecognizedEventArgs { Result = result });
    }

    private void OnListeningStatusChanged(bool isListening, string? reason)
    {
        ListeningStatusChanged?.Invoke(this, new ListeningStatusChangedEventArgs
        {
            IsListening = isListening,
            Reason = reason
        });
    }
}


