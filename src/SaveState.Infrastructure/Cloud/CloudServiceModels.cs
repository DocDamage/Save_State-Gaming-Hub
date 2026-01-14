namespace SaveState.Infrastructure.Cloud;

/// <summary>
/// Result of speech recognition from cloud services.
/// </summary>
public record SpeechRecognitionResult(
    string RecognizedText,
    float Confidence,
    TimeSpan Duration,
    string LanguageCode,
    bool IsFinal);

/// <summary>
/// Result of content analysis.
/// </summary>
public record ContentAnalysisResult(
    string[] Labels,
    string DetectedText,
    string[] Objects);
