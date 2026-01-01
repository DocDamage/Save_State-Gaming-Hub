namespace SaveState.Core.Social;

/// <summary>
/// Service for managing Discord Rich Presence integration.
/// </summary>
public interface IDiscordPresenceService : IAsyncDisposable
{
    /// <summary>
    /// Gets whether Discord is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Initializes the Discord connection with the application ID.
    /// </summary>
    Task InitializeAsync(string applicationId, CancellationToken ct = default);

    /// <summary>
    /// Updates the rich presence to show the currently playing game.
    /// </summary>
    Task SetPlayingGameAsync(
        string gameTitle,
        string? details = null,
        string? largeImageKey = null,
        string? largeImageText = null,
        string? smallImageKey = null,
        string? smallImageText = null,
        DateTime? startTimestamp = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the rich presence to show idle status.
    /// </summary>
    Task SetIdleAsync(string? status = null, CancellationToken ct = default);

    /// <summary>
    /// Clears the current rich presence.
    /// </summary>
    Task ClearPresenceAsync(CancellationToken ct = default);

    /// <summary>
    /// Disconnects from Discord.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when connection status changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    event EventHandler<string>? ErrorOccurred;
}
