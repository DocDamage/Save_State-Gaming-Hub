using SaveState.Core.Common;

namespace SaveState.Core.Common.Services;

/// <summary>
/// Service for extracting archives in various formats.
/// </summary>
public interface IExtractionService
{
    /// <summary>
    /// Extracts the specified archive to the destination directory.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <param name="destinationDirectory">Directory to extract to.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="progress">Progress reporter (0.0 to 1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExtractAsync(
        string archivePath,
        string destinationDirectory,
        bool overwrite = true,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if the service supports the given archive format.
    /// </summary>
    /// <param name="extension">Archive file extension (including dot).</param>
    /// <returns>True if supported.</returns>
    bool SupportsExtension(string extension);
}
