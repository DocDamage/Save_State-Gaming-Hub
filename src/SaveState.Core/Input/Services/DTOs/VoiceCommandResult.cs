using SaveState.Core.Common.Services;

namespace SaveState.Core.Input.Services.DTOs;

/// <summary>
/// Result of processing a voice command.
/// </summary>
public sealed record VoiceCommandResult(
    string RecognizedText,
    VoiceCommandDefinition? MatchedCommand,
    float Confidence,
    bool Success,
    string? ErrorMessage = null,
    object? ResultData = null);

/// <summary>
/// Event arguments for voice command recognition.
/// </summary>
public sealed class VoiceCommandRecognizedEventArgs : EventArgs
{
    public required VoiceCommandResult Result { get; init; }
    public DateTime Timestamp { get; init; }

    public VoiceCommandRecognizedEventArgs(ITimeProvider? timeProvider = null)
    {
        Timestamp = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event arguments for listening status changes.
/// </summary>
public sealed class ListeningStatusChangedEventArgs : EventArgs
{
    public bool IsListening { get; init; }
    public string? Reason { get; init; }
    public DateTime Timestamp { get; init; }

    public ListeningStatusChangedEventArgs(ITimeProvider? timeProvider = null)
    {
        Timestamp = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}