using SaveState.Core.Common;
using SaveState.Core.Ai.Services.DTOs;

namespace SaveState.Core.Ai.Services;

/// <summary>
/// Service for converting speech to text and processing audio input.
/// </summary>
public interface ISpeechRecognitionService
{
    /// <summary>
    /// Recognizes speech from an audio stream.
    /// </summary>
    Task<Result<SpeechRecognitionResult>> RecognizeSpeechAsync(
        Stream audioStream,
        CancellationToken ct = default);

    /// <summary>
    /// Starts continuous speech recognition.
    /// </summary>
    Task<Result> StartContinuousRecognitionAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Stops continuous speech recognition.
    /// </summary>
    Task<Result> StopContinuousRecognitionAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets available speech recognition languages.
    /// </summary>
    Task<Result<IReadOnlyList<LanguageInfo>>> GetAvailableLanguagesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Sets the speech recognition language.
    /// </summary>
    Task<Result> SetLanguageAsync(
        string languageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Calibrates the microphone for better recognition.
    /// </summary>
    Task<Result<MicrophoneCalibrationResult>> CalibrateMicrophoneAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets current microphone status and settings.
    /// </summary>
    Task<Result<MicrophoneStatus>> GetMicrophoneStatusAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Event raised when speech is recognized.
    /// </summary>
    event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;

    /// <summary>
    /// Event raised when speech recognition encounters an error.
    /// </summary>
    event EventHandler<SpeechRecognitionErrorEventArgs>? SpeechRecognitionError;

    /// <summary>
    /// Gets whether continuous recognition is currently active.
    /// </summary>
    bool IsContinuousRecognitionActive { get; }
}