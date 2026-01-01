using SaveState.Core.Common.Base;

namespace SaveState.Core.Social.Entities;

/// <summary>
/// Represents a friend from a social platform.
/// </summary>
public class Friend : EntityBase
{
    /// <summary>
    /// Gets the display name of the friend.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the URL to the friend's avatar/profile picture.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// Gets the social platform this friend is from.
    /// </summary>
    public SocialPlatform Platform { get; private set; }

    /// <summary>
    /// Gets the platform-specific user ID.
    /// </summary>
    public string PlatformUserId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the friend is currently online.
    /// </summary>
    public bool IsOnline { get; private set; }

    /// <summary>
    /// Gets the title of the game the friend is currently playing.
    /// </summary>
    public string? CurrentGame { get; private set; }

    /// <summary>
    /// Gets the date and time when the friend was last seen.
    /// </summary>
    public DateTime? LastSeenAt { get; private set; }

    /// <summary>
    /// Gets the date and time when this friend record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    private Friend() { }

    /// <summary>
    /// Creates a new friend record.
    /// </summary>
    public static Friend Create(
        string name,
        SocialPlatform platform,
        string platformUserId,
        string? avatarUrl = null)
    {
        return new Friend
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Platform = platform,
            PlatformUserId = Guard.Against.NullOrWhiteSpace(platformUserId, nameof(platformUserId)),
            AvatarUrl = avatarUrl,
            IsOnline = false,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the friend's online status and current game.
    /// </summary>
    public void UpdateStatus(bool isOnline, string? currentGame = null)
    {
        IsOnline = isOnline;
        CurrentGame = currentGame;
        LastSeenAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the friend's profile information.
    /// </summary>
    public void UpdateProfile(string? name = null, string? avatarUrl = null)
    {
        if (name is not null)
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        }

        if (avatarUrl is not null)
        {
            AvatarUrl = avatarUrl;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Social platforms supported for friend activity.
/// </summary>
public enum SocialPlatform
{
    Discord,
    Steam,
    // Future: XboxLive, PlayStationNetwork, etc.
}