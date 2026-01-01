using SaveState.Core.Common.Base;

namespace SaveState.Core.GameLibrary.Entities;

public class BacklogEntry : EntityBase
{
    public Guid GameId { get; private set; }
    public Game Game { get; private set; } = null!;
    public BacklogStatus Status { get; private set; }
    public int Priority { get; private set; }
    public DateTime AddedAt { get; private set; }
    public string? Notes { get; private set; }
    public TimeSpan? EstimatedPlaytime { get; private set; }
    public DateTime? TargetCompletionDate { get; private set; }

    private BacklogEntry() { }

    public static BacklogEntry Create(Guid gameId, int priority = 50)
    {
        return new BacklogEntry
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Status = BacklogStatus.NotStarted,
            Priority = Math.Clamp(priority, 1, 100),
            AddedAt = DateTime.UtcNow
        };
    }

    public void UpdateStatus(BacklogStatus status) => Status = status;
    public void UpdatePriority(int priority) => Priority = Math.Clamp(priority, 1, 100);
    public void SetNotes(string? notes) => Notes = notes;
    public void SetEstimatedPlaytime(TimeSpan? playtime) => EstimatedPlaytime = playtime;
    public void SetTargetDate(DateTime? date) => TargetCompletionDate = date;
}

public enum BacklogStatus
{
    NotStarted,
    InProgress,
    OnHold,
    Completed,
    Abandoned,
    Wishlisted
}