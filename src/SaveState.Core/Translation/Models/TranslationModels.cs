using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Translation.Models;

/// <summary>
/// Represents a translation result.
/// </summary>
public record TranslationResult
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string OriginalText { get; init; } = string.Empty;
    public string TranslatedText { get; init; } = string.Empty;
    public string SourceLanguage { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public TranslationSource Source { get; init; }
    public DateTime Timestamp { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public TimeSpan ProcessingTime { get; init; }
}

/// <summary>
/// Source of translation.
/// </summary>
public enum TranslationSource
{
    Cache,
    MachineTranslation,
    AiTranslation,
    HumanVerified,
    Community
}

/// <summary>
/// Represents OCR (Optical Character Recognition) result.
/// </summary>
public record OcrResult
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string RecognizedText { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public IReadOnlyList<TextRegion> TextRegions { get; init; } = Array.Empty<TextRegion>();
    public DateTime Timestamp { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Represents a region of detected text.
/// </summary>
public record TextRegion
{
    public string Text { get; init; } = string.Empty;
    public BoundingBox Bounds { get; init; } = new();
    public float Confidence { get; init; }
}

/// <summary>
/// Bounding box for text region.
/// </summary>
public record BoundingBox
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

/// <summary>
/// Represents a screen capture with text for translation.
/// </summary>
public record ScreenTextCapture
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public byte[] ScreenImage { get; init; } = Array.Empty<byte>();
    public OcrResult OcrResult { get; init; } = new();
    public IReadOnlyList<TranslationResult> Translations { get; init; } = Array.Empty<TranslationResult>();
    public string GameContext { get; init; } = string.Empty;
    public DateTime CapturedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Represents voice dubbing data.
/// </summary>
public record VoiceDubbingData
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string OriginalText { get; init; } = string.Empty;
    public string TranslatedText { get; init; } = string.Empty;
    public byte[] AudioData { get; init; } = Array.Empty<byte>();
    public string VoiceProfile { get; init; } = string.Empty;
    public string SourceLanguage { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public DateTime GeneratedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Represents a translation memory entry.
/// </summary>
public record TranslationMemoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string SourceText { get; init; } = string.Empty;
    public string TranslatedText { get; init; } = string.Empty;
    public string SourceLanguage { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public string? Context { get; init; }
    public string? GameId { get; init; }
    public int UseCount { get; init; }
    public DateTime CreatedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public DateTime LastUsedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public TranslationQuality Quality { get; init; } = TranslationQuality.Machine;
}

/// <summary>
/// Quality levels for translations.
/// </summary>
public enum TranslationQuality
{
    Machine,
    Reviewed,
    Verified,
    Professional
}

/// <summary>
/// Configuration for real-time translation.
/// </summary>
public record TranslationConfiguration
{
    public string SourceLanguage { get; init; } = "auto";
    public string TargetLanguage { get; init; } = "en";
    public bool EnableOcr { get; init; } = true;
    public bool EnableVoiceDubbing { get; init; } = false;
    public bool UseTranslationMemory { get; init; } = true;
    public OcrConfiguration OcrSettings { get; init; } = new();
    public DubbingConfiguration DubbingSettings { get; init; } = new();
    public float ConfidenceThreshold { get; init; } = 0.7f;
}

/// <summary>
/// OCR configuration.
/// </summary>
public record OcrConfiguration
{
    public int CaptureIntervalMs { get; init; } = 1000;
    public IReadOnlyList<string> TargetRegions { get; init; } = Array.Empty<string>();
    public bool DetectSubtitles { get; init; } = true;
    public bool DetectDialogue { get; init; } = true;
    public bool DetectUiElements { get; init; } = false;
    public int MinTextLength { get; init; } = 3;
    public float ConfidenceThreshold { get; init; } = 0.6f;
}

/// <summary>
/// Dubbing configuration.
/// </summary>
public record DubbingConfiguration
{
    public string VoiceProfile { get; init; } = "default";
    public float SpeechRate { get; init; } = 1.0f;
    public float Pitch { get; init; } = 1.0f;
    public int Volume { get; init; } = 100;
    public bool SyncWithLipMovement { get; init; } = false;
}

/// <summary>
/// Represents a supported language.
/// </summary>
public record SupportedLanguage
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NativeName { get; init; } = string.Empty;
    public bool SupportsOcr { get; init; }
    public bool SupportsDubbing { get; init; }
}

/// <summary>
/// Statistics for translation usage.
/// </summary>
public record TranslationStatistics
{
    public string UserId { get; init; } = string.Empty;
    public int TotalTranslations { get; init; }
    public int OcrTranslations { get; init; }
    public int VoiceDubbings { get; init; }
    public int CacheHits { get; init; }
    public double AverageConfidence { get; init; }
    public IReadOnlyDictionary<string, int> TranslationsByLanguage { get; init; } = new Dictionary<string, int>();
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
}
