namespace SaveState.Core.GameLibrary.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.ValueObjects;

/// <summary>
/// Represents a user-created note for a game.
/// Notes can be used for walkthroughs, tips, reminders, or any game-related information.
/// </summary>
public class GameNote : EntityBase
{
    /// <summary>
    /// Gets the unique identifier for this note.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the ID of the game this note belongs to.
    /// </summary>
    public GameId GameId { get; private set; }

    /// <summary>
    /// Gets the ID of the user who created this note.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets the title of the note.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the content of the note.
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Gets the category of the note (e.g., "Walkthrough", "Tips", "Bugs", "Reminders").
    /// </summary>
    public string? Category { get; private set; }

    /// <summary>
    /// Gets the tags associated with this note for organization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; private set; }

    /// <summary>
    /// Gets whether this note is pinned to the top.
    /// </summary>
    public bool IsPinned { get; private set; }

    /// <summary>
    /// Gets the date and time when this note was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when this note was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private GameNote()
    {
        Title = string.Empty;
        Content = string.Empty;
        Tags = new List<string>();
        GameId = default!;
        UserId = default!;
    }

    /// <summary>
    /// Creates a new game note.
    /// </summary>
    public static GameNote Create(
        GameId gameId,
        UserId userId,
        string title,
        string content,
        string? category = null,
        IEnumerable<string>? tags = null,
        DateTime? createdAt = null)
    {
        var timestamp = createdAt ?? DateTime.UtcNow;
        var note = new GameNote
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            UserId = userId,
            Title = title,
            Content = content,
            Category = category,
            Tags = tags?.ToList() ?? new List<string>(),
            IsPinned = false,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        return note;
    }

    /// <summary>
    /// Updates the note content and metadata.
    /// </summary>
    public void Update(string title, string content, string? category = null, IEnumerable<string>? tags = null, DateTime? updatedAt = null)
    {
        Title = title;
        Content = content;
        Category = category;
        Tags = tags?.ToList() ?? new List<string>();
        UpdatedAt = updatedAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Toggles the pinned state of the note.
    /// </summary>
    public void TogglePin(DateTime? updatedAt = null)
    {
        IsPinned = !IsPinned;
        UpdatedAt = updatedAt ?? DateTime.UtcNow;
    }
}
