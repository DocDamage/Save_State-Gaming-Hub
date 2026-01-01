using Microsoft.Extensions.Logging;
using SaveState.Core.Sync;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Local file system implementation of ICloudStorageProvider for testing and offline mode.
/// </summary>
public class LocalFileStorageProvider : ICloudStorageProvider
{
    private readonly string _rootPath;
    private readonly ILogger<LocalFileStorageProvider> _logger;

    /// <summary>
    /// Gets the display name of this storage provider.
    /// </summary>
    public string ProviderName => "Local Storage";

    /// <summary>
    /// Gets a value indicating whether this provider is authenticated.
    /// Local storage is always considered authenticated.
    /// </summary>
    public bool IsAuthenticated => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileStorageProvider"/> class.
    /// </summary>
    /// <param name="rootPath">The root directory path for file storage.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public LocalFileStorageProvider(string rootPath, ILogger<LocalFileStorageProvider> logger)
    {
        _rootPath = rootPath;
        _logger = logger;

        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }
    }

    /// <summary>
    /// Authenticates with the local storage provider.
    /// Local storage authentication always succeeds.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>Always returns true for local storage.</returns>
    public Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Uploads a local file to the remote storage path.
    /// </summary>
    /// <param name="localPath">The path to the local file to upload.</param>
    /// <param name="remotePath">The remote storage path where the file will be stored.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if the upload succeeded, false otherwise.</returns>
    public async Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken ct = default)
    {
        try
        {
            var targetPath = GetFullPath(remotePath);
            var targetDir = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            await using var source = File.OpenRead(localPath);
            await using var target = File.Create(targetPath);
            await source.CopyToAsync(target, ct).ConfigureAwait(false);

            _logger.LogDebug("Uploaded {LocalPath} to {RemotePath}", localPath, remotePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload {LocalPath}", localPath);
            return false;
        }
    }

    /// <summary>
    /// Downloads a file from remote storage to a local path.
    /// </summary>
    /// <param name="remotePath">The remote storage path of the file to download.</param>
    /// <param name="localPath">The local path where the file will be saved.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if the download succeeded, false otherwise.</returns>
    public async Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken ct = default)
    {
        try
        {
            var sourcePath = GetFullPath(remotePath);
            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Remote file not found: {RemotePath}", remotePath);
                return false;
            }

            var targetDir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            await using var source = File.OpenRead(sourcePath);
            await using var target = File.Create(localPath);
            await source.CopyToAsync(target, ct).ConfigureAwait(false);

            _logger.LogDebug("Downloaded {RemotePath} to {LocalPath}", remotePath, localPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download {RemotePath}", remotePath);
            return false;
        }
    }

    /// <summary>
    /// Deletes a file from remote storage.
    /// </summary>
    /// <param name="remotePath">The remote storage path of the file to delete.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if the deletion succeeded or the file didn't exist, false otherwise.</returns>
    public Task<bool> DeleteFileAsync(string remotePath, CancellationToken ct = default)
    {
        try
        {
            var fullPath = GetFullPath(remotePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogDebug("Deleted {RemotePath}", remotePath);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {RemotePath}", remotePath);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Lists all files in the specified remote directory.
    /// </summary>
    /// <param name="remotePath">The remote directory path to list files from.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A list of file information for all files in the directory.</returns>
    public Task<IReadOnlyList<CloudFileInfo>> ListFilesAsync(string remotePath, CancellationToken ct = default)
    {
        var results = new List<CloudFileInfo>();

        try
        {
            var fullPath = GetFullPath(remotePath);
            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult<IReadOnlyList<CloudFileInfo>>(results);
            }

            foreach (var file in Directory.GetFiles(fullPath))
            {
                var info = new FileInfo(file);
                results.Add(new CloudFileInfo(
                    Path: GetRelativePath(file),
                    Name: info.Name,
                    SizeBytes: info.Length,
                    ModifiedAt: info.LastWriteTimeUtc
                ));
            }

            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                var info = new DirectoryInfo(dir);
                results.Add(new CloudFileInfo(
                    Path: GetRelativePath(dir),
                    Name: info.Name,
                    SizeBytes: 0,
                    ModifiedAt: info.LastWriteTimeUtc,
                    IsDirectory: true
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files at {RemotePath}", remotePath);
        }

        return Task.FromResult<IReadOnlyList<CloudFileInfo>>(results);
    }

    /// <summary>
    /// Gets information about a file in remote storage.
    /// </summary>
    /// <param name="remotePath">The remote storage path of the file.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>File information if the file exists, null otherwise.</returns>
    public Task<CloudFileInfo?> GetFileInfoAsync(string remotePath, CancellationToken ct = default)
    {
        try
        {
            var fullPath = GetFullPath(remotePath);
            if (!File.Exists(fullPath))
            {
                return Task.FromResult<CloudFileInfo?>(null);
            }

            var info = new FileInfo(fullPath);
            return Task.FromResult<CloudFileInfo?>(new CloudFileInfo(
                Path: remotePath,
                Name: info.Name,
                SizeBytes: info.Length,
                ModifiedAt: info.LastWriteTimeUtc
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file info for {RemotePath}", remotePath);
            return Task.FromResult<CloudFileInfo?>(null);
        }
    }

    /// <summary>
    /// Checks if a file exists in remote storage.
    /// </summary>
    /// <param name="remotePath">The remote storage path to check.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    public Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(remotePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    private string GetFullPath(string remotePath)
    {
        return Path.Combine(_rootPath, remotePath.TrimStart('/', '\\'));
    }

    private string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(_rootPath, fullPath);
    }
}
