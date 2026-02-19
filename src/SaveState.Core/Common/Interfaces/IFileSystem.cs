namespace SaveState.Core.Common.Interfaces;

public interface IFileSystem
{
    Task<bool> FileExistsAsync(string path, CancellationToken ct = default);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default);
    Task<long> GetFileSizeAsync(string path, CancellationToken ct = default);
    Task<string[]> GetFilesAsync(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly, CancellationToken ct = default);
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);
    Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);
    Task WriteAllTextAsync(string path, string content, CancellationToken ct = default);
}
