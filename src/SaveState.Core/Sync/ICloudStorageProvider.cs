using SaveState.Core.Common;

namespace SaveState.Core.Sync;

/// <summary>
/// Provides cloud storage operations for sync.
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>
    /// Uploads content to cloud storage.
    /// </summary>
    Task<Result> UploadAsync(string path, Stream content, CancellationToken ct = default);

    /// <summary>
    /// Downloads content from cloud storage.
    /// </summary>
    Task<Result<Stream>> DownloadAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Deletes content from cloud storage.
    /// </summary>
    Task<Result> DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Lists items in a cloud storage path.
    /// </summary>
    Task<Result<IReadOnlyList<CloudStorageItem>>> ListAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Checks if a path exists in cloud storage.
    /// </summary>
    Task<Result<bool>> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string ProviderName { get; }
}

public sealed record CloudStorageItem(
    string Path,
    long Size,
    DateTimeOffset LastModified,
    string? ContentHash);
