using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.Services;

namespace SaveState.Core.GameLibrary.Entities;

public class GameFile : EntityBase, ISoftDelete
{
    public Guid GameId { get; private set; }
    public Game? Game { get; private set; }
    public string Path { get; private set; } = string.Empty;
    public string? FileName { get; private set; }
    public long? FileSize { get; private set; }
    public DateTime AddedAt { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    protected GameFile() { } // EF Core

    public GameFile(Guid gameId, string path, ITimeProvider timeProvider, string? fileName = null, long? fileSize = null)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        GameId = gameId;
        Path = Guard.Against.NullOrWhiteSpace(path, nameof(path));
        FileName = fileName ?? System.IO.Path.GetFileName(path);
        FileSize = fileSize;
        AddedAt = timeProvider.UtcNow;
    }

    [Obsolete("Use constructor with ITimeProvider parameter")]
    public GameFile(Guid gameId, string path, string? fileName = null, long? fileSize = null)
    {
        GameId = gameId;
        Path = Guard.Against.NullOrWhiteSpace(path, nameof(path));
        FileName = fileName ?? System.IO.Path.GetFileName(path);
        FileSize = fileSize;
        AddedAt = SystemTimeProvider.Instance.UtcNow;
    }

    public void UpdatePath(string newPath)
    {
        Path = Guard.Against.NullOrWhiteSpace(newPath, nameof(newPath));
        FileName = System.IO.Path.GetFileName(newPath);
    }

    public void UpdateFileSize(long fileSize)
    {
        FileSize = fileSize;
    }
}
