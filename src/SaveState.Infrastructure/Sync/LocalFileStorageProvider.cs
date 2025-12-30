using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Local file system implementation for testing sync without cloud.
/// </summary>
public sealed class LocalFileStorageProvider : ICloudStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageProvider> _logger;

    public string ProviderName => "LocalFile";

    public LocalFileStorageProvider(
        string basePath,
        ILogger<LocalFileStorageProvider> logger)
    {
        _basePath = basePath;
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<Result> UploadAsync(string path, Stream content, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using var file = File.Create(fullPath);
            await content.CopyToAsync(file, ct);

            _logger.LogDebug("Uploaded {Path}", path);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Path}", path);
            return Result.Failure(ex.Message);
        }
    }

    public Task<Result<Stream>> DownloadAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, path);
            if (!File.Exists(fullPath))
                return Task.FromResult(Result<Stream>.Failure("File not found"));

            var stream = File.OpenRead(fullPath);
            return Task.FromResult(Result<Stream>.Success(stream));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<Stream>.Failure(ex.Message));
        }
    }

    public Task<Result> DeleteAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure(ex.Message));
        }
    }

    public Task<Result<IReadOnlyList<CloudStorageItem>>> ListAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var fullPath = Path.Combine(_basePath, path);
            if (!Directory.Exists(fullPath))
                return Task.FromResult(Result<IReadOnlyList<CloudStorageItem>>.Success(
                    Array.Empty<CloudStorageItem>()));

            var items = Directory.GetFiles(fullPath)
                .Select(f => new FileInfo(f))
                .Select(fi => new CloudStorageItem(
                    fi.Name,
                    fi.Length,
                    fi.LastWriteTimeUtc,
                    null))
                .ToList();

            return Task.FromResult(Result<IReadOnlyList<CloudStorageItem>>.Success(items));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IReadOnlyList<CloudStorageItem>>.Failure(ex.Message));
        }
    }

    public Task<Result<bool>> ExistsAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path);
        return Task.FromResult(Result<bool>.Success(File.Exists(fullPath)));
    }
}
