using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Voice;

namespace SaveState.Presentation.Services.Voice;

/// <summary>
/// Service interface for voice visualizer operations.
/// </summary>
public interface IVoiceVisualizerService
{
    /// <summary>
    /// Starts visualization.
    /// </summary>
    void StartVisualization();

    /// <summary>
    /// Stops visualization.
    /// </summary>
    void StopVisualization();

    /// <summary>
    /// Sets the current visualizer state.
    /// </summary>
    void SetState(VoiceVisualizerState state);

    /// <summary>
    /// Updates the audio level.
    /// </summary>
    void UpdateAudioLevel(float level);

    /// <summary>
    /// Updates full audio data including frequency bands.
    /// </summary>
    void UpdateAudioData(AudioLevelData data);

    /// <summary>
    /// Shows recognized text.
    /// </summary>
    void ShowRecognizedText(string text);

    /// <summary>
    /// Shows command result.
    /// </summary>
    void ShowCommandResult(VoiceCommandResult result);

    /// <summary>
    /// Gets or sets whether the visualizer is visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets the current state.
    /// </summary>
    VoiceVisualizerState CurrentState { get; }

    /// <summary>
    /// Gets the current audio level.
    /// </summary>
    float CurrentAudioLevel { get; }

    /// <summary>
    /// Event raised when state changes.
    /// </summary>
    event EventHandler<VoiceVisualizerStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when audio level updates.
    /// </summary>
    event EventHandler<AudioLevelUpdatedEventArgs>? AudioLevelUpdated;

    /// <summary>
    /// Event raised when recognized text changes.
    /// </summary>
    event EventHandler<string>? RecognizedTextChanged;

    /// <summary>
    /// Event raised when command result is received.
    /// </summary>
    event EventHandler<VoiceCommandResult>? CommandResultReceived;

    /// <summary>
    /// Event raised when visibility changes.
    /// </summary>
    event EventHandler<bool>? VisibilityChanged;
}

/// <summary>
/// Implementation of the voice visualizer service.
/// </summary>
public sealed class VoiceVisualizerService : IVoiceVisualizerService, IDisposable
{
    private readonly ILogger<VoiceVisualizerService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Timer _animationTimer;
    private readonly ConcurrentQueue<AudioLevelData> _audioDataQueue;

    private VoiceVisualizerState _currentState = VoiceVisualizerState.Idle;
    private float _currentAudioLevel;
    private string _recognizedText = string.Empty;
    private bool _isVisible;
    private bool _isDisposed;

    /// <inheritdoc />
    public VoiceVisualizerState CurrentState => _currentState;

    /// <inheritdoc />
    public float CurrentAudioLevel => _currentAudioLevel;

    /// <inheritdoc />
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                VisibilityChanged?.Invoke(this, value);
                _logger.LogDebug("Voice visualizer visibility changed to {IsVisible}", value);
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<VoiceVisualizerStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<AudioLevelUpdatedEventArgs>? AudioLevelUpdated;

    /// <inheritdoc />
    public event EventHandler<string>? RecognizedTextChanged;

    /// <inheritdoc />
    public event EventHandler<VoiceCommandResult>? CommandResultReceived;

    /// <inheritdoc />
    public event EventHandler<bool>? VisibilityChanged;

    /// <summary>
    /// Creates a new voice visualizer service.
    /// </summary>
    public VoiceVisualizerService(
        ILogger<VoiceVisualizerService> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _audioDataQueue = new ConcurrentQueue<AudioLevelData>();
        _animationTimer = new Timer(OnAnimationTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc />
    public void StartVisualization()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(VoiceVisualizerService));

        IsVisible = true;
        _animationTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)); // ~60fps
        _logger.LogDebug("Voice visualization started");
    }

    /// <inheritdoc />
    public void StopVisualization()
    {
        if (_isDisposed)
        {
            return;
        }

        _animationTimer.Change(Timeout.Infinite, Timeout.Infinite);
        IsVisible = false;
        _audioDataQueue.Clear();
        _logger.LogDebug("Voice visualization stopped");
    }

    /// <inheritdoc />
    public void SetState(VoiceVisualizerState state)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_currentState != state)
        {
            var previousState = _currentState;
            _currentState = state;

            StateChanged?.Invoke(this, new VoiceVisualizerStateChangedEventArgs(
                previousState, state, _timeProvider));

            _logger.LogDebug("Voice visualizer state changed from {PreviousState} to {NewState}",
                previousState, state);

            // Auto-hide after success or error after a delay
            if (state is VoiceVisualizerState.Success or VoiceVisualizerState.Error)
            {
                ScheduleAutoHide();
            }
        }
    }

    /// <inheritdoc />
    public void UpdateAudioLevel(float level)
    {
        if (_isDisposed)
        {
            return;
        }

        _currentAudioLevel = Math.Clamp(level, 0f, 1f);

        // Generate frequency bands based on overall level
        var data = new AudioLevelData(_timeProvider)
        {
            OverallLevel = _currentAudioLevel,
            FrequencyBands = GenerateFrequencyBands(_currentAudioLevel)
        };

        _audioDataQueue.Enqueue(data);
    }

    /// <inheritdoc />
    public void UpdateAudioData(AudioLevelData data)
    {
        if (_isDisposed)
        {
            return;
        }

        _currentAudioLevel = data.OverallLevel;
        _audioDataQueue.Enqueue(data);
    }

    /// <inheritdoc />
    public void ShowRecognizedText(string text)
    {
        if (_isDisposed)
        {
            return;
        }

        _recognizedText = text;
        RecognizedTextChanged?.Invoke(this, text);
        _logger.LogDebug("Recognized text updated: {Text}", text);
    }

    /// <inheritdoc />
    public void ShowCommandResult(VoiceCommandResult result)
    {
        if (_isDisposed)
        {
            return;
        }

        CommandResultReceived?.Invoke(this, result);

        // Update state based on result
        SetState(result.IsSuccess ? VoiceVisualizerState.Success : VoiceVisualizerState.Error);

        _logger.LogDebug("Command result received: {Result}, Confidence: {Confidence:F2}",
            result.IsSuccess ? "Success" : "Error", result.Confidence);
    }

    private void OnAnimationTick(object? state)
    {
        if (_isDisposed || !_isVisible)
        {
            return;
        }

        // Process queued audio data
        while (_audioDataQueue.TryDequeue(out var data))
        {
            try
            {
                AudioLevelUpdated?.Invoke(this, new AudioLevelUpdatedEventArgs(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio level update");
            }
        }
    }

    private void ScheduleAutoHide()
    {
        // Auto-hide after 3 seconds for success/error states
        Task.Run(async () =>
        {
            await Task.Delay(3000);
            if (_currentState is VoiceVisualizerState.Success or VoiceVisualizerState.Error)
            {
                IsVisible = false;
            }
        });
    }

    private static float[] GenerateFrequencyBands(float overallLevel)
    {
        // Generate 16 frequency bands with some randomness for visual effect
        var bands = new float[16];
        var random = new Random();

        for (int i = 0; i < bands.Length; i++)
        {
            // Create a wave-like pattern centered in the middle
            var position = (i / (float)(bands.Length - 1)) * 2 - 1; // -1 to 1
            var waveFactor = 1f - Math.Abs(position) * 0.3f; // Center emphasis
            var noise = (float)(random.NextDouble() * 0.3f + 0.7f);
            bands[i] = overallLevel * waveFactor * noise;
        }

        return bands;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _animationTimer.Dispose();
            _audioDataQueue.Clear();
        }
    }
}

/// <summary>
/// Static helper class for voice command help content.
/// </summary>
public static class VoiceCommandHelpContent
{
    /// <summary>
    /// Gets the default voice command categories.
    /// </summary>
    public static List<VoiceCommandCategory> GetDefaultCategories()
    {
        return new List<VoiceCommandCategory>
        {
            new()
            {
                Name = "Game Launch",
                Icon = "🎮",
                Commands = new List<VoiceCommandHelpItem>
                {
                    new() { Phrase = "Launch [game name]", Description = "Launch a specific game", Example = "Launch Elden Ring", RequiresParameter = true, ParameterHint = "game name" },
                    new() { Phrase = "Play [game name]", Description = "Alternative to launch", Example = "Play Cyberpunk 2077", RequiresParameter = true, ParameterHint = "game name" },
                    new() { Phrase = "Start [game name]", Description = "Start a game", Example = "Start The Witcher 3", RequiresParameter = true, ParameterHint = "game name" },
                    new() { Phrase = "Resume last game", Description = "Resume the most recently played game" },
                    new() { Phrase = "Close game", Description = "Close the currently running game" }
                }
            },
            new()
            {
                Name = "Save States",
                Icon = "💾",
                Commands = new List<VoiceCommandHelpItem>
                {
                    new() { Phrase = "Create save state", Description = "Create a new save state for the current game" },
                    new() { Phrase = "Save state", Description = "Shortcut for create save state" },
                    new() { Phrase = "Quick save", Description = "Create a quick save" },
                    new() { Phrase = "Load save state", Description = "Load the most recent save state" },
                    new() { Phrase = "Load save [number]", Description = "Load a specific save state", Example = "Load save 1", RequiresParameter = true, ParameterHint = "save number" },
                    new() { Phrase = "Quick load", Description = "Load the quick save" }
                }
            },
            new()
            {
                Name = "Media",
                Icon = "📷",
                Commands = new List<VoiceCommandHelpItem>
                {
                    new() { Phrase = "Take screenshot", Description = "Capture a screenshot" },
                    new() { Phrase = "Screenshot", Description = "Shortcut for take screenshot" },
                    new() { Phrase = "Start recording", Description = "Start gameplay recording" },
                    new() { Phrase = "Stop recording", Description = "Stop gameplay recording" },
                    new() { Phrase = "Clip that", Description = "Save the last gameplay clip" }
                }
            },
            new()
            {
                Name = "Navigation",
                Icon = "🧭",
                Commands = new List<VoiceCommandHelpItem>
                {
                    new() { Phrase = "Go to library", Description = "Navigate to game library" },
                    new() { Phrase = "Show recent", Description = "Show recently played games" },
                    new() { Phrase = "Show favorites", Description = "Show favorite games" },
                    new() { Phrase = "Search for [query]", Description = "Search the game library", Example = "Search for RPG games", RequiresParameter = true, ParameterHint = "search query" },
                    new() { Phrase = "Go back", Description = "Navigate back" }
                }
            },
            new()
            {
                Name = "System",
                Icon = "⚙️",
                Commands = new List<VoiceCommandHelpItem>
                {
                    new() { Phrase = "Mute", Description = "Mute the microphone" },
                    new() { Phrase = "Unmute", Description = "Unmute the microphone" },
                    new() { Phrase = "Volume up", Description = "Increase system volume" },
                    new() { Phrase = "Volume down", Description = "Decrease system volume" },
                    new() { Phrase = "Open settings", Description = "Open application settings" },
                    new() { Phrase = "What can I say", Description = "Show voice command help" }
                }
            }
        };
    }
}
