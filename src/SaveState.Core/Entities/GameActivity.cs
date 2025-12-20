namespace SaveState.Core.Entities;

public class GameActivity
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    
    public ActivityType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Notes { get; set; }
}

public enum ActivityType
{
    Launched,
    Closed,
    Installed,
    Uninstalled,
    MetadataFetched,
    AddedToCollection,
    MarkedComplete,
    AchievementUnlocked
}

public enum CompletionStatus
{
    NotStarted,
    Playing,
    OnHold,
    Dropped,
    Completed,
    Mastered
}
