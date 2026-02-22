using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Social.Entities;

/// <summary>
/// Represents a collection of games that can be shared with others.
/// </summary>
public class SharedCollection : EntityBase
{
    /// <summary>
    /// Gets the title of the shared collection.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional description of the collection.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the unique share code for this collection.
    /// </summary>
    public string ShareCode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether this collection is publicly discoverable.
    /// </summary>
    public bool IsPublic { get; private set; }

    /// <summary>
    /// Gets the collection of games in this shared collection.
    /// </summary>
    public ICollection<SharedCollectionItem> Items { get; private set; } = new List<SharedCollectionItem>();

    /// <summary>
    /// Gets the number of times this collection has been downloaded/viewed.
    /// </summary>
    public int DownloadCount { get; private set; }

    /// <summary>
    /// Gets the date and time when this collection was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when this collection was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    private SharedCollection() { }

    /// <summary>
    /// Creates a new shared collection.
    /// </summary>
    public static SharedCollection Create(string title, ITimeProvider timeProvider, string? description = null, bool isPublic = false)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new SharedCollection
        {
            Id = Guid.NewGuid(),
            Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)),
            Description = description,
            ShareCode = GenerateShareCode(),
            IsPublic = isPublic,
            CreatedAt = timeProvider.UtcNow,
            DownloadCount = 0
        };
    }

    [Obsolete("Use Create(string, ITimeProvider, string?, bool) instead")]
    public static SharedCollection Create(string title, string? description = null, bool isPublic = false)
    {
        return new SharedCollection
        {
            Id = Guid.NewGuid(),
            Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)),
            Description = description,
            ShareCode = GenerateShareCode(),
            IsPublic = isPublic,
            CreatedAt = SystemTimeProvider.Instance.UtcNow,
            DownloadCount = 0
        };
    }

    /// <summary>
    /// Updates the collection's title and description.
    /// </summary>
    public void Update(ITimeProvider timeProvider, string? title = null, string? description = null, bool? isPublic = null)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        if (title is not null)
        {
            Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
        }

        if (description is not null)
        {
            Description = description;
        }

        if (isPublic.HasValue)
        {
            IsPublic = isPublic.Value;
        }

        UpdatedAt = timeProvider.UtcNow;
    }

    [Obsolete("Use Update(ITimeProvider, string?, string?, bool?) instead")]
    public void Update(string? title = null, string? description = null, bool? isPublic = null)
    {
        if (title is not null)
        {
            Title = Guard.Against.NullOrWhiteSpace(title, nameof(title));
        }

        if (description is not null)
        {
            Description = description;
        }

        if (isPublic.HasValue)
        {
            IsPublic = isPublic.Value;
        }

        UpdatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    /// <summary>
    /// Increments the download count.
    /// </summary>
    public void IncrementDownloadCount() => DownloadCount++;

    /// <summary>
    /// Regenerates the share code.
    /// </summary>
    public void RegenerateShareCode() => ShareCode = GenerateShareCode();

    /// <summary>
    /// Generates a unique share code for the collection.
    /// </summary>
    private static string GenerateShareCode()
    {
        // Create a URL-safe base64 string from a GUID, truncated to 8 characters
        var base64 = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=')
            [..8]
            .ToUpperInvariant();

        return base64;
    }
}

/// <summary>
/// Represents an item in a shared collection.
/// </summary>
public class SharedCollectionItem
{
    /// <summary>
    /// Gets the ID of the shared collection.
    /// </summary>
    public Guid CollectionId { get; set; }

    /// <summary>
    /// Gets the shared collection.
    /// </summary>
    public required SharedCollection Collection { get; set; }  // Set by EF Core

    /// <summary>
    /// Gets the title of the game in this collection.
    /// </summary>
    public string GameTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets optional notes about this game in the collection.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets the sort order of this item in the collection.
    /// </summary>
    public int SortOrder { get; set; }
}