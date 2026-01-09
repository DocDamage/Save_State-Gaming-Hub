using SaveState.Core.Common.Base;

namespace SaveState.Core.SaveStates.Entities;

public class SaveState : EntityBase
{
    public Guid GameId { get; private set; }
    public string FilePath { get; private set; } = string.Empty;
    public string? ThumbnailPath { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? Description { get; private set; }
    public TimeSpan PlaytimeAtSave { get; private set; }
    public string? GameLocation { get; private set; }
    public Guid? ParentStateId { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool IsAutoSave { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string? BranchName { get; private set; }
    public bool IsCurrent { get; private set; }

    private SaveState() { }

    public static SaveState Create(Guid gameId, string filePath, TimeSpan playtimeAtSave, bool isAutoSave = false)
    {
        return new SaveState
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            FilePath = Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath)),
            CreatedAt = DateTime.UtcNow,
            PlaytimeAtSave = playtimeAtSave,
            IsAutoSave = isAutoSave,
            IsFavorite = false
        };
    }

    public void SetThumbnail(string path) => ThumbnailPath = path;
    public void SetDescription(string? description) => Description = description;
    public void SetGameLocation(string? location) => GameLocation = location;
    public void SetParent(Guid? parentId) => ParentStateId = parentId;
    public void ToggleFavorite() => IsFavorite = !IsFavorite;
    public void SetFileSize(long bytes) => FileSizeBytes = bytes;
    public void SetBranch(string? branchName) => BranchName = branchName;
    public void MarkAsCurrent(bool isCurrent = true) => IsCurrent = isCurrent;
}
