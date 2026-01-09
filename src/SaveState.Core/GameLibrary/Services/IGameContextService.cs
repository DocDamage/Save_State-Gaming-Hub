using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Service for tracking the current game context (currently playing, last played).
/// </summary>
public interface IGameContextService
{
    /// <summary>
    /// Gets the ID of the currently playing game, if any.
    /// </summary>
    Guid? GetCurrentGameId();

    /// <summary>
    /// Gets the ID of the last played game.
    /// </summary>
    Task<Guid?> GetLastPlayedGameIdAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the current game being played.
    /// </summary>
    void SetCurrentGame(Guid gameId);

    /// <summary>
    /// Clears the current game context.
    /// </summary>
    void ClearCurrentGame();

    /// <summary>
    /// Gets whether a game is currently being played.
    /// </summary>
    bool IsGamePlaying();
}
