using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for managing the UI context of the currently selected game.
/// </summary>
public interface IUiGameContextService
{
    /// <summary>
    /// Gets the currently selected game.
    /// </summary>
    Game? CurrentGame { get; }

    /// <summary>
    /// Gets the currently selected game ID (may be set without the full Game object).
    /// </summary>
    Guid? CurrentGameId { get; }

    /// <summary>
    /// Sets the current game context.
    /// </summary>
    Task<Result> SetCurrentGameAsync(Game game, CancellationToken ct = default);

    /// <summary>
    /// Sets the current game context by ID (loads the game if needed).
    /// </summary>
    Task<Result> SetCurrentGameIdAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Clears the current game context.
    /// </summary>
    void ClearCurrentGame();

    /// <summary>
    /// Event raised when the current game changes.
    /// </summary>
    event EventHandler<Game?>? CurrentGameChanged;
}
