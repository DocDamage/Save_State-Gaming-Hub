using Microsoft.Extensions.Logging;
using SaveState.Core.Social;
using DiscordRPC;
using DiscordRPC.Logging;

namespace SaveState.Infrastructure.Social;

/// <summary>
/// Discord Rich Presence implementation using the DiscordRichPresence NuGet package.
/// </summary>
public class DiscordPresenceService : IDiscordPresenceService
{
    private readonly ILogger<DiscordPresenceService> _logger;
    private bool _isConnected;
    private bool _isDisposed;
    private string? _applicationId;

    private DiscordRpcClient? _client;

    /// <summary>
    /// Gets a value indicating whether the service is connected to Discord.
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Event raised when the Discord connection status changes.
    /// </summary>
    public event EventHandler<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Event raised when an error occurs during Discord operations.
    /// </summary>
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordPresenceService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public DiscordPresenceService(ILogger<DiscordPresenceService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the Discord Rich Presence client with the specified application ID.
    /// </summary>
    /// <param name="applicationId">The Discord application ID to use for presence.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InitializeAsync(string applicationId, CancellationToken ct = default)
    {
        _applicationId = applicationId;

        try
        {
            _client = new DiscordRpcClient(applicationId);

            // Set up logging
            _client.Logger = new ConsoleLogger(DiscordRPC.Logging.LogLevel.Warning);

            _client.OnReady += (s, e) =>
            {
                _isConnected = true;
                _logger.LogInformation("Discord RPC Ready. User: {User}", e.User.Username);
                ConnectionStatusChanged?.Invoke(this, true);
            };

            _client.OnConnectionFailed += (s, e) =>
            {
                _isConnected = false;
                _logger.LogWarning("Discord RPC Connection Failed: {Error}", e);
                ConnectionStatusChanged?.Invoke(this, false);
            };

            _client.Initialize();

            _logger.LogInformation("Discord Rich Presence initialized with Application ID: {AppId}", applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Discord Rich Presence");
            ErrorOccurred?.Invoke(this, ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the Discord Rich Presence to show the user is playing a game.
    /// </summary>
    /// <param name="gameTitle">The title of the game being played.</param>
    /// <param name="details">Additional details about the current game state.</param>
    /// <param name="largeImageKey">Key for the large image asset in Discord.</param>
    /// <param name="largeImageText">Tooltip text for the large image.</param>
    /// <param name="smallImageKey">Key for the small image asset in Discord.</param>
    /// <param name="smallImageText">Tooltip text for the small image.</param>
    /// <param name="startTimestamp">When the game session started.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SetPlayingGameAsync(
        string gameTitle,
        string? details = null,
        string? largeImageKey = null,
        string? largeImageText = null,
        string? smallImageKey = null,
        string? smallImageText = null,
        DateTime? startTimestamp = null,
        CancellationToken ct = default)
    {
        if (_client == null || !IsConnected) // Check client existance
        {
            // _logger.LogWarning("Cannot set presence: Discord not connected");
            // Allow setting presence even if not fully connected yet, client handles it
        }

        try
        {
             _client?.SetPresence(new RichPresence
             {
                 Details = gameTitle,
                 State = details,
                 Assets = new Assets
                 {
                     LargeImageKey = largeImageKey ?? "default_game",
                     LargeImageText = largeImageText ?? gameTitle,
                     SmallImageKey = smallImageKey ?? "savestate_icon",
                     SmallImageText = smallImageText ?? "SaveState Gaming Hub"
                 },
                 Timestamps = startTimestamp.HasValue
                     ? new Timestamps(startTimestamp.Value)
                     : Timestamps.Now
             });

            _logger.LogInformation("Discord presence updated: Playing {GameTitle}{Details}",
                gameTitle,
                details != null ? $" - {details}" : "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Discord presence");
            ErrorOccurred?.Invoke(this, ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the Discord Rich Presence to an idle state.
    /// </summary>
    /// <param name="status">Optional custom idle status message.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SetIdleAsync(string? status = null, CancellationToken ct = default)
    {
        if (_client == null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var idleStatus = status ?? "Browsing Library";

             _client.SetPresence(new RichPresence
             {
                 Details = idleStatus,
                 Assets = new Assets
                 {
                     LargeImageKey = "savestate_logo",
                     LargeImageText = "SaveState Gaming Hub"
                 }
             });

            _logger.LogInformation("Discord presence set to idle: {Status}", idleStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set idle presence");
            ErrorOccurred?.Invoke(this, ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears the current Discord Rich Presence.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ClearPresenceAsync(CancellationToken ct = default)
    {
        try
        {
            _client?.ClearPresence();
            _logger.LogInformation("Discord presence cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear Discord presence");
            ErrorOccurred?.Invoke(this, ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disconnects from Discord and shuts down the Rich Presence client.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            _client?.Dispose();
            _client = null;
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(this, false);
            _logger.LogInformation("Discord Rich Presence disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect Discord");
            ErrorOccurred?.Invoke(this, ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the Discord presence service asynchronously.
    /// </summary>
    /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        await DisconnectAsync().ConfigureAwait(false);
        _isDisposed = true;

        GC.SuppressFinalize(this);
    }
}
