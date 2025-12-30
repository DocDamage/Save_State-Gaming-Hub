namespace SaveState.Core.Ai.Voice;

/// <summary>
/// Result of voice transcription.
/// </summary>
public sealed record VoiceTranscription(
    string Text,
    string Language,
    float Duration,
    float Confidence);

/// <summary>
/// Configuration for voice processing.
/// </summary>
public sealed record VoiceProcessingOptions
{
    public string Model { get; init; } = "whisper-1";
    public string? Language { get; init; }
    public float Temperature { get; init; } = 0.0f;
    public string ResponseFormat { get; init; } = "verbose_json";
}
