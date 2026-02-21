using SaveState.Core.Common;

namespace SaveState.Core.ContentGeneration.Services;

/// <summary>
/// Client for generating images using OpenAI's DALL-E or similar image generation APIs.
/// </summary>
public interface IOpenAiImageClient
{
    /// <summary>
    /// Generates an image based on a text prompt.
    /// </summary>
    /// <param name="prompt">The text prompt describing the desired image.</param>
    /// <param name="width">Desired width of the generated image.</param>
    /// <param name="height">Desired height of the generated image.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing base64-encoded image data or failure.</returns>
    Task<Result<string>> GenerateImageAsync(
        string prompt,
        int width,
        int height,
        CancellationToken ct = default);

    /// <summary>
    /// Generates an image variation based on an existing image.
    /// </summary>
    /// <param name="baseImageData">Base64-encoded source image.</param>
    /// <param name="prompt">The text prompt for the variation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing base64-encoded image data or failure.</returns>
    Task<Result<string>> GenerateImageVariationAsync(
        string baseImageData,
        string prompt,
        CancellationToken ct = default);
}
