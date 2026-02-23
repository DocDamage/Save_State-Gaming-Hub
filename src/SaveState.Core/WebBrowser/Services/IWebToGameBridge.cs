using SaveState.Core.Common;

namespace SaveState.Core.WebBrowser.Services;

/// <summary>
/// Bridge interface that allows web pages to interact with SaveState via JavaScript.
/// </summary>
public interface IWebToGameBridge
{
    /// <summary>
    /// Launches a game by its ID.
    /// </summary>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> LaunchGameAsync(string gameId);

    /// <summary>
    /// Creates a save state for the specified game.
    /// </summary>
    /// <param name="gameId">The game identifier.</param>
    /// <param name="description">Description of the save state.</param>
    /// <returns>Result containing the save state ID or error information.</returns>
    Task<Result<string>> CreateSaveStateAsync(string gameId, string description);

    /// <summary>
    /// Loads a save state by its ID.
    /// </summary>
    /// <param name="saveStateId">The save state identifier.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> LoadSaveStateAsync(string saveStateId);

    /// <summary>
    /// Takes a screenshot of the current game.
    /// </summary>
    /// <returns>Result containing the screenshot path or error information.</returns>
    Task<Result<string>> TakeScreenshotAsync();

    /// <summary>
    /// Starts recording gameplay.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> StartRecordingAsync();

    /// <summary>
    /// Stops recording gameplay.
    /// </summary>
    /// <returns>Result containing the recording path or error information.</returns>
    Task<Result<string>> StopRecordingAsync();

    /// <summary>
    /// Gets the currently playing game.
    /// </summary>
    /// <returns>The game ID if a game is playing, null otherwise.</returns>
    Task<string?> GetCurrentlyPlayingGameAsync();

    /// <summary>
    /// Gets the last save state for a game.
    /// </summary>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>The save state ID or null if none exists.</returns>
    Task<string?> GetLastSaveStateAsync(string gameId);

    /// <summary>
    /// Gets information about a game.
    /// </summary>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>JSON string containing game information.</returns>
    Task<string?> GetGameInfoAsync(string gameId);

    /// <summary>
    /// Gets playtime statistics for a game.
    /// </summary>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>JSON string containing playtime statistics.</returns>
    Task<string?> GetGameStatsAsync(string gameId);

    /// <summary>
    /// Opens the SaveState overlay.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> OpenOverlayAsync();

    /// <summary>
    /// Closes the SaveState overlay.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> CloseOverlayAsync();

    /// <summary>
    /// Event raised when a game launch is requested from the web.
    /// </summary>
    event EventHandler<GameLaunchRequest>? OnGameLaunchRequested;

    /// <summary>
    /// Event raised when a save state is requested from the web.
    /// </summary>
    event EventHandler<SaveStateRequest>? OnSaveStateRequested;

    /// <summary>
    /// Event raised when a save state load is requested from the web.
    /// </summary>
    event EventHandler<LoadSaveStateRequest>? OnLoadSaveStateRequested;
}

/// <summary>
/// Request to launch a game from the web.
/// </summary>
public class GameLaunchRequest : EventArgs
{
    /// <summary>
    /// The game identifier.
    /// </summary>
    public required string GameId { get; init; }

    /// <summary>
    /// Optional launch parameters.
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>
    /// The source URL that requested the launch.
    /// </summary>
    public string? SourceUrl { get; init; }
}

/// <summary>
/// Request to create a save state from the web.
/// </summary>
public class SaveStateRequest : EventArgs
{
    /// <summary>
    /// The game identifier.
    /// </summary>
    public required string GameId { get; init; }

    /// <summary>
    /// Description of the save state.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Whether to include a screenshot.
    /// </summary>
    public bool IncludeScreenshot { get; init; } = true;

    /// <summary>
    /// Custom tags for the save state.
    /// </summary>
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// Request to load a save state from the web.
/// </summary>
public class LoadSaveStateRequest : EventArgs
{
    /// <summary>
    /// The save state identifier.
    /// </summary>
    public required string SaveStateId { get; init; }

    /// <summary>
    /// Whether to launch the game if not running.
    /// </summary>
    public bool AutoLaunch { get; init; } = true;
}
