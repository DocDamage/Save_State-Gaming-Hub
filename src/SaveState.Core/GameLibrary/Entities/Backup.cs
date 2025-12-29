using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.RomManagement.ValueObjects;

namespace SaveState.Core.GameLibrary.Entities;

public class Backup : EntityBase, IAggregateRoot, ISoftDelete
{
    public Guid GameId { get; private set; }
    public Game Game { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public FilePath FilePath { get; private set; } = null!;
    public long FileSize { get; private set; }
    public BackupType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    protected Backup() { } // EF Core

    public Backup(Guid gameId, string name, string description, FilePath filePath, long fileSize, BackupType type)
    {
        GameId = Guard.Against.Default(gameId, nameof(gameId));
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        FilePath = Guard.Against.Null(filePath, nameof(filePath));
        FileSize = Guard.Against.Negative(fileSize, nameof(fileSize));
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
    }

    public void MarkAsDeleted()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}

public enum BackupType
{
    SaveState,
    SaveFile,
    Configuration,
    FullBackup
}
