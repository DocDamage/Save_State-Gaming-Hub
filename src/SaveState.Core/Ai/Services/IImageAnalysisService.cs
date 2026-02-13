using SaveState.Core.Common;

namespace SaveState.Core.Ai.Services;

/// <summary>
/// Service for analyzing images and screenshots using AI/Vision capabilities.
/// PHASE 1: Core Services - Image Analysis Interface.
/// </summary>
public interface IImageAnalysisService
{
    /// <summary>
    /// Analyzes an image from a file path and returns detected labels, text, and objects.
    /// </summary>
    /// <param name="imagePath">Local file path to the image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing analysis data or failure.</returns>
    Task<Result<ImageAnalysisResult>> AnalyzeImageAsync(string imagePath, CancellationToken ct = default);

    /// <summary>
    /// Analyzes an image from a URI and returns detected labels, text, and objects.
    /// </summary>
    /// <param name="imageUri">URI to the image (local file:// or remote https://).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing analysis data or failure.</returns>
    Task<Result<ImageAnalysisResult>> AnalyzeImageFromUriAsync(string imageUri, CancellationToken ct = default);

    /// <summary>
    /// Analyzes an image and returns suggested tags for game categorization.
    /// </summary>
    /// <param name="imagePath">Local file path to the image.</param>
    /// <param name="maxTags">Maximum number of tags to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing suggested tags or failure.</returns>
    Task<Result<IReadOnlyList<string>>> GetSuggestedTagsAsync(string imagePath, int maxTags = 10, CancellationToken ct = default);
}

/// <summary>
/// Result of image analysis containing detected content.
/// </summary>
/// <param name="Labels">Labels/categories detected in the image.</param>
/// <param name="DetectedText">Text detected in the image via OCR.</param>
/// <param name="Objects">Objects detected and localized in the image.</param>
/// <param name="Confidence">Confidence score for the overall analysis (0-1).</param>
public sealed record ImageAnalysisResult(
    IReadOnlyList<string> Labels,
    string DetectedText,
    IReadOnlyList<string> Objects,
    float Confidence);
