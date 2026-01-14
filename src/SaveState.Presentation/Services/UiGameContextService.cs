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

    public Game? CurrentGame => _currentGame;

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

    public void ClearCurrentGame()
    {
        _currentGame = null;
        CurrentGameChanged?.Invoke(this, null);
        _logger.LogInformation("Current game context cleared");
    }
}
