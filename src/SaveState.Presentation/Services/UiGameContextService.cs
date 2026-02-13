using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Implementation of UI game context service.
/// </summary>
public class UiGameContextService : IUiGameContextService
{
    private readonly ILogger<UiGameContextService> _logger;
    private Game? _currentGame;
    private Guid? _currentGameId;

    public Game? CurrentGame => _currentGame;
    public Guid? CurrentGameId => _currentGameId ?? _currentGame?.Id;

    public event EventHandler<Game?>? CurrentGameChanged;

    public UiGameContextService(ILogger<UiGameContextService> logger)
    {
        _logger = logger;
    }

    public Task<Result> SetCurrentGameAsync(Game game, CancellationToken ct = default)
    {
        try
        {
            _currentGame = game;
            _currentGameId = game.Id;
            CurrentGameChanged?.Invoke(this, game);
            _logger.LogInformation("Current game set to: {GameTitle}", game.Title);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set current game");
            return Task.FromResult(Result.Failure($"Failed to set current game: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> SetCurrentGameIdAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            _currentGameId = gameId;
            // Note: We only set the ID here. The full game object will be loaded when needed.
            // This allows setting context before the game is fully loaded.
            _logger.LogInformation("Current game ID set to: {GameId}", gameId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set current game ID");
            return Task.FromResult(Result.Failure($"Failed to set current game ID: {ex.Message}", ErrorType.Internal));
        }
    }

    public void ClearCurrentGame()
    {
        _currentGame = null;
        _currentGameId = null;
        CurrentGameChanged?.Invoke(this, null);
        _logger.LogInformation("Current game context cleared");
    }
}
