using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Services.DTOs;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of ISpeechRecognitionService for integration testing.
/// Simulates speech recognition without requiring actual audio hardware.
/// </summary>
public class FakeSpeechRecognitionService : ISpeechRecognitionService
{
    private bool _isContinuousRecognitionActive;
    private string _currentLanguage = "en-US";

    /// <inheritdoc />
    public bool IsContinuousRecognitionActive => _isContinuousRecognitionActive;

    /// <inheritdoc />
    public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;

    /// <inheritdoc />
    public event EventHandler<SpeechRecognitionErrorEventArgs>? SpeechRecognitionError;

    /// <inheritdoc />
    public Task<Result<SpeechRecognitionResult>> RecognizeSpeechAsync(Stream audioStream, CancellationToken ct = default)
    {
        // Simulate successful speech recognition
        var result = new SpeechRecognitionResult(
            RecognizedText: "simulated speech",
            Confidence: 0.95f,
            Duration: TimeSpan.FromSeconds(2),
            LanguageCode: _currentLanguage,
            IsFinal: true);
        return Task.FromResult(Result.Success(result));
    }

    /// <inheritdoc />
    public Task<Result> StartContinuousRecognitionAsync(CancellationToken ct = default)
    {
        _isContinuousRecognitionActive = true;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> StopContinuousRecognitionAsync(CancellationToken ct = default)
    {
        _isContinuousRecognitionActive = false;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<LanguageInfo>>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        var languages = new List<LanguageInfo>
        {
            new("en-US", "English (United States)", "English"),
            new("en-GB", "English (United Kingdom)", "English"),
            new("es-ES", "Spanish (Spain)", "Español"),
            new("fr-FR", "French (France)", "Français"),
            new("de-DE", "German (Germany)", "Deutsch")
        };
        return Task.FromResult(Result.Success<IReadOnlyList<LanguageInfo>>(languages));
    }

    /// <inheritdoc />
    public Task<Result> SetLanguageAsync(string languageCode, CancellationToken ct = default)
    {
        _currentLanguage = languageCode;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<MicrophoneCalibrationResult>> CalibrateMicrophoneAsync(CancellationToken ct = default)
    {
        var result = new MicrophoneCalibrationResult(
            Success: true,
            OptimalSensitivity: 0.75f,
            ErrorMessage: null);
        return Task.FromResult(Result.Success(result));
    }

    /// <inheritdoc />
    public Task<Result<MicrophoneStatus>> GetMicrophoneStatusAsync(CancellationToken ct = default)
    {
        var status = new MicrophoneStatus(
            IsAvailable: true,
            CurrentSensitivity: 0.75f,
            SampleRate: 16000,
            DeviceName: "Fake Microphone",
            IsMuted: false);
        return Task.FromResult(Result.Success(status));
    }

    /// <summary>
    /// Simulates a speech recognition event with the specified text.
    /// </summary>
    public void SimulateSpeechRecognized(string text, float confidence = 0.95f)
    {
        var result = new SpeechRecognitionResult(
            RecognizedText: text,
            Confidence: confidence,
            Duration: TimeSpan.FromSeconds(2),
            LanguageCode: _currentLanguage,
            IsFinal: true);
        var args = new SpeechRecognizedEventArgs
        {
            Result = result
        };
        SpeechRecognized?.Invoke(this, args);
    }

    /// <summary>
    /// Simulates a speech recognition error.
    /// </summary>
    public void SimulateError(string errorMessage)
    {
        var args = new SpeechRecognitionErrorEventArgs
        {
            ErrorMessage = errorMessage
        };
        SpeechRecognitionError?.Invoke(this, args);
    }
}
