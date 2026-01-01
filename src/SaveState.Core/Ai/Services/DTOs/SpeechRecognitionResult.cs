namespace SaveState.Core.Ai.Services.DTOs;

/// <summary>
/// Result of speech recognition.
/// </summary>
public sealed record SpeechRecognitionResult(
    string RecognizedText,
    float Confidence,
    TimeSpan Duration,
    string LanguageCode,
    bool IsFinal,
    IReadOnlyList<AlternativeResult>? Alternatives = null);

/// <summary>
/// Alternative recognition result.
/// </summary>
public sealed record AlternativeResult(
    string Text,
    float Confidence);

/// <summary>
/// Information about available languages.
/// </summary>
public sealed record LanguageInfo(
    string Code,
    string DisplayName,
    string NativeName);

/// <summary>
/// Result of microphone calibration.
/// </summary>
public sealed record MicrophoneCalibrationResult(
    bool Success,
    float OptimalSensitivity,
    string? ErrorMessage = null);

/// <summary>
/// Current microphone status.
/// </summary>
public sealed record MicrophoneStatus(
    bool IsAvailable,
    float CurrentSensitivity,
    int SampleRate,
    string? DeviceName = null,
    bool IsMuted = false);

/// <summary>
/// Event arguments for speech recognition.
/// </summary>
public sealed class SpeechRecognizedEventArgs : EventArgs
{
    public SpeechRecognitionResult Result { get; init; } = null!;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for speech recognition errors.
/// </summary>
public sealed class SpeechRecognitionErrorEventArgs : EventArgs
{
    public string ErrorMessage { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}