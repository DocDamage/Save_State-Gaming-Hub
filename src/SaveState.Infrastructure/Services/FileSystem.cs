using SaveState.Core.Common.Interfaces;

namespace SaveState.Infrastructure.Services;

public class FileSystem : IFileSystem
{
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(File.Exists(path));
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(Directory.Exists(path));
    }

    public Task<long> GetFileSizeAsync(string path, CancellationToken ct = default)
    {
        var fileInfo = new FileInfo(path);
        return Task.FromResult(fileInfo.Length);
    }

    public Task<string[]> GetFilesAsync(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly, CancellationToken ct = default)
    {
        return Task.FromResult(Directory.GetFiles(path, searchPattern, searchOption));
    }

    public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
    }
}
