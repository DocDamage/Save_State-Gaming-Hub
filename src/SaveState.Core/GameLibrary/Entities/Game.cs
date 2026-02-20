namespace SaveState.Core.GameLibrary.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using SaveState.Core.Common.Base;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.ValueObjects;

/// <summary>
/// Represents a game in the SaveState library system.
/// Contains metadata about the game, its platform, files, and status.
/// </summary>
public class Game : EntityBase, ISoftDelete
{
    private static DateTime UtcNow => SystemTimeProvider.Instance.UtcNow;

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? CoverImagePath { get; private set; }
    public string? InstallPath { get; private set; }
    public GameStatus Status { get; private set; }
    public Guid? PlatformId { get; private set; }
    public Platform? Platform { get; private set; }
    public string? Source { get; private set; }
    public string? SourceId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastPlayedAt { get; private set; }
    public TimeSpan TotalPlayTime { get; private set; }
    public TimeSpan? EstimatedTimeToComplete { get; private set; }
    public DateOnly? ReleaseDate { get; private set; }
    public double? UserRating { get; private set; }
    public double? Rating => UserRating;
    public string? LaunchArguments { get; private set; }
    public string? ExecutablePath { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    [NotMapped]
    public IReadOnlyList<Platform> Platforms => Platform is null ? [] : [Platform];

    // Navigation properties
    public ICollection<GameFile> Files { get; private set; } = new List<GameFile>();
    public ICollection<Genre> Genres { get; private set; } = new List<Genre>();
    public ICollection<string> Tags { get; private set; } = new List<string>();

    private Game() { } // EF Core

    /// <summary>
    /// Creates a new Game entity with the specified title and optional metadata.
    /// </summary>
    /// <param name="title">The title of the game (required).</param>
    /// <param name="platformId">The ID of the platform this game runs on.</param>
    /// <param name="description">A description of the game.</param>
    /// <param name="coverImagePath">Path to the game's cover image.</param>
    /// <param name="source">The source of the game metadata (e.g., "Steam", "GOG").</param>
    /// <param name="sourceId">The unique identifier from the source system.</param>
    /// <returns>A new Game entity.</returns>
    public static Game Create(string title, Guid? platformId = null, string? description = null, string? coverImagePath = null, string? source = null, string? sourceId = null)
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Title = Guard.Against.NullOrWhiteSpace(title, nameof(title)),
            PlatformId = platformId,
            Description = description,
            CoverImagePath = coverImagePath,
            Source = source,
            SourceId = sourceId,
            Status = GameStatus.NotInstalled,
            CreatedAt = UtcNow
        };
    }

    public void Update(string? title = null, string? description = null, string? coverImagePath = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        Description = description;
        CoverImagePath = coverImagePath;
        UpdatedAt = UtcNow;
    }

    public void SetInstallPath(string installPath)
    {
        InstallPath = Guard.Against.NullOrWhiteSpace(installPath, nameof(installPath));
        Status = GameStatus.Installed;
        UpdatedAt = UtcNow;
    }

    public void SetSourceInfo(string source, string? sourceId = null)
    {
        Source = Guard.Against.NullOrWhiteSpace(source, nameof(source));
        SourceId = sourceId;
        UpdatedAt = UtcNow;
    }

    public void MarkAsRunning()
    {
        if (Status == GameStatus.Installed)
        {
            Status = GameStatus.Running;
        }
    }

    public void MarkAsNotRunning()
    {
        if (Status == GameStatus.Running)
        {
            Status = GameStatus.Installed;
        }
    }

    public void MarkAsDeleted()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = UtcNow;
    }

    public void UpdateLaunchConfiguration(string? launchArguments)
    {
        LaunchArguments = launchArguments;
        UpdatedAt = UtcNow;
    }

    public void SetExecutablePath(string? executablePath)
    {
        ExecutablePath = executablePath;
        UpdatedAt = UtcNow;
    }

    public void MarkAsCompleted()
    {
        IsCompleted = true;
        CompletedAt = UtcNow;
        UpdatedAt = UtcNow;
    }

    public void MarkAsIncomplete()
    {
        IsCompleted = false;
        CompletedAt = null;
        UpdatedAt = UtcNow;
    }

    public void SetEstimatedTimeToComplete(TimeSpan? value)
    {
        EstimatedTimeToComplete = value;
        UpdatedAt = UtcNow;
    }

    public void UpdateTags(IEnumerable<string> tags)
    {
        Tags = tags.ToList();
        UpdatedAt = UtcNow;
    }

    public void AddTag(string tag)
    {
        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdatedAt = UtcNow;
        }
    }

    public void RemoveTag(string tag)
    {
        if (Tags.Remove(tag))
        {
            UpdatedAt = UtcNow;
        }
    }
}
