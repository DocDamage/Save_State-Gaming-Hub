using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.Social.Entities;

/// <summary>
/// Represents a user's review and rating for a game.
/// </summary>
public class GameReview : EntityBase
{
    /// <summary>
    /// Gets the ID of the game being reviewed.
    /// </summary>
    public Guid GameId { get; private set; }

    /// <summary>
    /// Gets the game being reviewed.
    /// </summary>
    public Game? Game { get; private set; }

    /// <summary>
    /// Gets the rating given by the user (1-10 scale).
    /// </summary>
    public int Rating { get; private set; }

    /// <summary>
    /// Gets the optional title of the review.
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Gets the optional content of the review.
    /// </summary>
    public string? Content { get; private set; }

    /// <summary>
    /// Gets whether the user recommends this game.
    /// </summary>
    public bool IsRecommended { get; private set; }

    /// <summary>
    /// Gets the date and time when the review was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the review was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the playtime the user had when writing this review.
    /// </summary>
    public TimeSpan PlaytimeAtReview { get; private set; }

    /// <summary>
    /// Gets whether this review contains spoilers.
    /// </summary>
    public bool ContainsSpoilers { get; private set; }

    private GameReview() { }

    /// <summary>
    /// Creates a new game review.
    /// </summary>
    public static GameReview Create(
        Guid gameId,
        int rating,
        TimeSpan playtimeAtReview,
        bool isRecommended,
        ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        if (rating < 1 || rating > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 10.");
        }

        return new GameReview
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Rating = rating,
            IsRecommended = isRecommended,
            PlaytimeAtReview = playtimeAtReview,
            CreatedAt = timeProvider.UtcNow,
            ContainsSpoilers = false
        };
    }

    [Obsolete("Use Create(Guid, int, TimeSpan, bool, ITimeProvider) instead")]
    public static GameReview Create(
        Guid gameId,
        int rating,
        TimeSpan playtimeAtReview,
        bool isRecommended)
    {
        if (rating < 1 || rating > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 10.");
        }

        return new GameReview
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            Rating = rating,
            IsRecommended = isRecommended,
            PlaytimeAtReview = playtimeAtReview,
            CreatedAt = SystemTimeProvider.Instance.UtcNow,
            ContainsSpoilers = false
        };
    }

    /// <summary>
    /// Updates the review content.
    /// </summary>
    public void Update(
        ITimeProvider timeProvider,
        int? rating = null,
        string? title = null,
        string? content = null,
        bool? containsSpoilers = null)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        if (rating.HasValue && (rating.Value < 1 || rating.Value > 10))
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 10.");
        }

        if (rating.HasValue) Rating = rating.Value;
        if (title is not null) Title = title;
        if (content is not null) Content = content;
        if (containsSpoilers.HasValue) ContainsSpoilers = containsSpoilers.Value;

        UpdatedAt = timeProvider.UtcNow;
    }

    [Obsolete("Use Update(ITimeProvider, int?, string?, string?, bool?) instead")]
    public void Update(
        int? rating = null,
        string? title = null,
        string? content = null,
        bool? containsSpoilers = null)
    {
        if (rating.HasValue && (rating.Value < 1 || rating.Value > 10))
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 10.");
        }

        if (rating.HasValue) Rating = rating.Value;
        if (title is not null) Title = title;
        if (content is not null) Content = content;
        if (containsSpoilers.HasValue) ContainsSpoilers = containsSpoilers.Value;

        UpdatedAt = SystemTimeProvider.Instance.UtcNow;
    }
}