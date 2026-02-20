namespace SaveState.Core.GameLibrary.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;

/// <summary>
/// Represents media (screenshots, videos, artwork) associated with a game.
/// </summary>
public class GameMedia : EntityBase
{
    /// <summary>
    /// Gets the unique identifier for this media item.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the ID of the game this media belongs to.
    /// </summary>
    public GameId GameId { get; private set; }

    /// <summary>
    /// Gets the ID of the user who created this media.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets the type of media (Screenshot, Video, Artwork, Thumbnail).
    /// </summary>
    public MediaType MediaType { get; private set; }

    /// <summary>
    /// Gets the title or caption of the media.
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Gets the description of the media.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the file path where the media is stored.
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; private set; }

    /// <summary>
    /// Gets the file format/extension (e.g., "png", "jpg", "mp4").
    /// </summary>
    public string FileFormat { get; private set; }

    /// <summary>
    /// Gets the width in pixels (for images and videos).
    /// </summary>
    public int? Width { get; private set; }

    /// <summary>
    /// Gets the height in pixels (for images and videos).
    /// </summary>
    public int? Height { get; private set; }

    /// <summary>
    /// Gets the duration in seconds (for videos).
    /// </summary>
    public int? DurationSeconds { get; private set; }

    /// <summary>
    /// Gets the tags associated with this media.
    /// </summary>
    public IReadOnlyList<string> Tags { get; private set; }

    /// <summary>
    /// Gets whether this media is marked as a favorite.
    /// </summary>
    public bool IsFavorite { get; private set; }

    /// <summary>
    /// Gets whether this media is publicly shared.
    /// </summary>
    public bool IsPublic { get; private set; }

    /// <summary>
    /// Gets the thumbnail file path (for videos and large images).
    /// </summary>
    public string? ThumbnailPath { get; private set; }

    /// <summary>
    /// Gets the date and time when the media was created/captured.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the media was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private GameMedia()
    {
        FilePath = string.Empty;
        FileFormat = string.Empty;
        Tags = new List<string>();
        GameId = default!;
        UserId = default!;
    }

    /// <summary>
    /// Creates a new game media item.
    /// </summary>
    public static GameMedia Create(
        GameId gameId,
        UserId userId,
        MediaType mediaType,
        string filePath,
        long fileSizeBytes,
        string fileFormat,
        string? title = null,
        string? description = null,
        int? width = null,
        int? height = null,
        int? durationSeconds = null,
        IEnumerable<string>? tags = null,
        string? thumbnailPath = null)
    {
        var media = new GameMedia
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            UserId = userId,
            MediaType = mediaType,
            Title = title,
            Description = description,
            FilePath = filePath,
            FileSizeBytes = fileSizeBytes,
            FileFormat = fileFormat,
            Width = width,
            Height = height,
            DurationSeconds = durationSeconds,
            Tags = tags?.ToList() ?? new List<string>(),
            IsFavorite = false,
            IsPublic = false,
            ThumbnailPath = thumbnailPath,
            CreatedAt = SystemTimeProvider.Instance.UtcNow,
            UpdatedAt = SystemTimeProvider.Instance.UtcNow
        };

        return media;
    }

    /// <summary>
    /// Updates the media metadata.
    /// </summary>
    public void Update(
        string? title = null,
        string? description = null,
        IEnumerable<string>? tags = null)
    {
        Title = title;
        Description = description;
        Tags = tags?.ToList() ?? new List<string>();
        UpdatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    /// <summary>
    /// Marks the media as a favorite.
    /// </summary>
    public void MarkAsFavorite()
    {
        IsFavorite = true;
        UpdatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    /// <summary>
    /// Removes the favorite status.
    /// </summary>
    public void UnmarkAsFavorite()
    {
        IsFavorite = false;
        UpdatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    /// <summary>
    /// Makes the media publicly visible.
    /// </summary>
    public void MakePublic()
    {
        IsPublic = true;
        UpdatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    /// <summary>
    /// Makes the media private.
    /// </summary>
    public void MakePrivate()
    {
        IsPublic = false;
        UpdatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    /// <summary>
    /// Sets the thumbnail path for this media.
    /// </summary>
    public void SetThumbnail(string thumbnailPath)
    {
        ThumbnailPath = thumbnailPath;
        UpdatedAt = SystemTimeProvider.Instance.UtcNow;
    }
}

/// <summary>
/// Types of media that can be associated with a game.
/// </summary>
public enum MediaType
{
    Screenshot,
    Video,
    Artwork,
    Thumbnail
}
