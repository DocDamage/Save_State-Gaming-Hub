using SaveState.Core.Common;
using SaveState.Core.ContentGeneration.Models;

namespace SaveState.Core.ContentGeneration.Services;

/// <summary>
/// Service for generating AI-powered thumbnails for games.
/// </summary>
public interface IThumbnailGeneratorService
{
    /// <summary>
    /// Generates a thumbnail based on the provided request.
    /// </summary>
    /// <param name="request">Thumbnail generation request with style, dimensions, and context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated image or failure.</returns>
    Task<Result<GeneratedImage>> GenerateThumbnailAsync(
        ThumbnailRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generates multiple thumbnail variations with different styles.
    /// </summary>
    /// <param name="request">Base thumbnail request.</param>
    /// <param name="count">Number of variations to generate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing a list of generated images or failure.</returns>
    Task<Result<IReadOnlyList<GeneratedImage>>> GenerateThumbnailVariationsAsync(
        ThumbnailRequest request,
        int count = 4,
        CancellationToken ct = default);

    /// <summary>
    /// Generates an AI prompt for thumbnail creation based on game context.
    /// </summary>
    /// <param name="gameTitle">Title of the game.</param>
    /// <param name="style">Desired thumbnail style.</param>
    /// <param name="gameTags">Tags describing the game.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated prompt or failure.</returns>
    Task<Result<string>> GeneratePromptAsync(
        string gameTitle,
        ThumbnailStyle style,
        IReadOnlyList<string> gameTags,
        CancellationToken ct = default);
}
