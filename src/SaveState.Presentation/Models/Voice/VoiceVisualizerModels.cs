using SaveState.Core.Common.Services;

namespace SaveState.Presentation.Models.Voice;

/// <summary>
/// Represents the current state of the voice visualizer.
/// </summary>
public enum VoiceVisualizerState
{
    /// <summary>
    /// Waiting for wake word.
    /// </summary>
    Idle,

    /// <summary>
    /// Wake word detected, listening for command.
    /// </summary>
    Listening,

    /// <summary>
    /// Processing speech to text.
    /// </summary>
    Processing,

    /// <summary>
    /// Executing matched command.
    /// </summary>
    Executing,

    /// <summary>
    /// Command executed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Command failed or not recognized.
    /// </summary>
    Error,

    /// <summary>
    /// Microphone muted.
    /// </summary>
    Muted
}

/// <summary>
/// Result of processing a voice command for visualization.
/// </summary>
public sealed record VoiceCommandResult
{
    /// <summary>
    /// The raw text recognized from speech.
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// The matched command name, if any.
    /// </summary>
    public string? MatchedCommand { get; set; }

    /// <summary>
    /// Confidence level (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Time taken to process the command.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Whether the command was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the command failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the result was received.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Creates a new voice command result.
    /// </summary>
    public VoiceCommandResult(ITimeProvider? timeProvider = null)
    {
        Timestamp = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Audio level data for visualization.
/// </summary>
public sealed record AudioLevelData
{
    /// <summary>
    /// Frequency band levels for visualization.
    /// </summary>
    public float[] FrequencyBands { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Overall audio level (0.0 to 1.0).
    /// </summary>
    public float OverallLevel { get; set; }

    /// <summary>
    /// Timestamp when the data was captured.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Creates new audio level data.
    /// </summary>
    public AudioLevelData(ITimeProvider? timeProvider = null)
    {
        Timestamp = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Available voice command category.
/// </summary>
public sealed record VoiceCommandCategory
{
    /// <summary>
    /// Category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category icon.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Commands in this category.
    /// </summary>
    public List<VoiceCommandHelpItem> Commands { get; set; } = new();
}

/// <summary>
/// Individual voice command help item.
/// </summary>
public sealed record VoiceCommandHelpItem
{
    /// <summary>
    /// The voice command phrase.
    /// </summary>
    public string Phrase { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the command does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Example usage.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Whether this command requires a parameter.
    /// </summary>
    public bool RequiresParameter { get; set; }

    /// <summary>
    /// Parameter hint if required.
    /// </summary>
    public string? ParameterHint { get; set; }
}

/// <summary>
/// Event arguments for voice visualizer state changes.
/// </summary>
public sealed class VoiceVisualizerStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous state.
    /// </summary>
    public VoiceVisualizerState PreviousState { get; }

    /// <summary>
    /// New state.
    /// </summary>
    public VoiceVisualizerState NewState { get; }

    /// <summary>
    /// Timestamp of the change.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Creates new state changed event args.
    /// </summary>
    public VoiceVisualizerStateChangedEventArgs(
        VoiceVisualizerState previousState,
        VoiceVisualizerState newState,
        ITimeProvider? timeProvider = null)
    {
        PreviousState = previousState;
        NewState = newState;
        Timestamp = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event arguments for audio level updates.
/// </summary>
public sealed class AudioLevelUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Audio level data.
    /// </summary>
    public AudioLevelData Data { get; }

    /// <summary>
    /// Creates new audio level updated event args.
    /// </summary>
    public AudioLevelUpdatedEventArgs(AudioLevelData data)
    {
        Data = data;
    }
}
