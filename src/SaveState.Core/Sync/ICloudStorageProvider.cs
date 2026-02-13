using SaveState.Core.Common;

namespace SaveState.Core.Sync;

/// <summary>
/// Abstraction for cloud storage providers (OneDrive, Google Drive, etc.)
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>
    /// Gets the provider name (e.g., "OneDrive", "Google Drive").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets whether the provider is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Authenticates with the cloud provider.
    /// </summary>
    Task<bool> AuthenticateAsync(CancellationToken ct = default);

    /// <summary>
    /// Uploads a file to the cloud storage.
    /// </summary>
    Task<bool> UploadFileAsync(
        string localPath,
        string remotePath,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads a file from cloud storage.
    /// </summary>
    Task<bool> DownloadFileAsync(
        string remotePath,
        string localPath,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a file from cloud storage.
    /// </summary>
    Task<bool> DeleteFileAsync(
        string remotePath,
        CancellationToken ct = default);

    /// <summary>
    /// Lists files in a cloud storage directory.
    /// </summary>
    Task<IReadOnlyList<CloudFileInfo>> ListFilesAsync(
        string remotePath,
        CancellationToken ct = default);

    /// <summary>
    /// Gets file metadata from cloud storage.
    /// </summary>
    Task<Result<CloudFileInfo>> GetFileInfoAsync(
        string remotePath,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a file exists in cloud storage.
    /// </summary>
    Task<bool> FileExistsAsync(
        string remotePath,
        CancellationToken ct = default);
}

/// <summary>
/// Information about a file in cloud storage.
/// </summary>
public sealed record CloudFileInfo(
    string Path,
    string Name,
    long SizeBytes,
    DateTime ModifiedAt,
    string? Checksum = null,
    bool IsDirectory = false);
