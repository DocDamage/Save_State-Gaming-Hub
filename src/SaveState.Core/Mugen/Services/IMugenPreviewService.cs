using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for generating visual previews of MUGEN moves.
/// </summary>
public interface IMugenPreviewService
{
    /// <summary>
    /// Generates preview data for a move.
    /// </summary>
    /// <param name="move">The move to preview.</param>
    /// <param name="options">Preview options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview data with frames and metadata.</returns>
    Task<Result<MovePreviewData>> GeneratePreviewAsync(
        MugenMoveDefinition move,
        PreviewOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a static thumbnail for a move.
    /// </summary>
    /// <param name="move">The move to generate a thumbnail for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Base64 encoded image or path.</returns>
    Task<Result<string>> GenerateThumbnailAsync(
        MugenMoveDefinition move,
        CancellationToken ct = default);

    /// <summary>
    /// Validates that sprites and animations are available for preview.
    /// </summary>
    /// <param name="move">The move to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if preview can be generated.</returns>
    Task<Result<bool>> ValidatePreviewAssetsAsync(
        MugenMoveDefinition move,
        CancellationToken ct = default);
}
