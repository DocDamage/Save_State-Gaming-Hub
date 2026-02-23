namespace SaveState.Presentation.Models.Replay;

/// <summary>
/// Represents a save state replay with video, metadata, and bookmarks.
/// </summary>
public record SaveStateReplay
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SaveStateId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string? GameCover { get; set; }
    public DateTime CreatedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public int FileSize { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? VideoPath { get; set; }
    public ReplayMetadata Metadata { get; set; } = new();
    public List<ReplayBookmark> Bookmarks { get; set; } = new();
    public bool IsFavorite { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Metadata associated with a replay, capturing game state at the time of save.
/// </summary>
public record ReplayMetadata
{
    public DateTime GameDate { get; set; }
    public TimeSpan PlayTimeAtSave { get; set; }
    public string? Location { get; set; }
    public int? PlayerLevel { get; set; }
    public float? CompletionPercentage { get; set; }
    public Dictionary<string, string> CustomData { get; set; } = new();
}

/// <summary>
/// Represents a bookmark within a replay for quick navigation.
/// </summary>
public record ReplayBookmark
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TimeSpan Timestamp { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Supported export formats for replay videos.
/// </summary>
public enum ReplayExportFormat
{
    Mp4,
    WebM,
    Gif,
    Zip // With save state file
}
