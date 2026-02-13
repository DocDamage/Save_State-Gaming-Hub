using SaveState.Core.Common.Interfaces;

namespace SaveState.Infrastructure.Services;

public class FileSystem : IFileSystem
{
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        EnsurePath(path);
        return Task.FromResult(File.Exists(path));
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        EnsurePath(path);
        return Task.FromResult(Directory.Exists(path));
    }

    public Task<long> GetFileSizeAsync(string path, CancellationToken ct = default)
    {
        EnsurePath(path);
        var fileInfo = new FileInfo(path);
        return Task.FromResult(fileInfo.Length);
    }

    public Task<string[]> GetFilesAsync(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly, CancellationToken ct = default)
    {
        EnsurePath(path);
        return Task.FromResult(Directory.GetFiles(path, searchPattern, searchOption));
    }

    public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        EnsurePath(path);
        ct.ThrowIfCancellationRequested();

        try
        {
            return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }
    }

    private static void EnsurePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }
    }
}
