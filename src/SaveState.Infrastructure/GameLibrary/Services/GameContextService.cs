using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Implementation of game context service for tracking current/last played games.
/// </summary>
public partial class GameContextService : IGameContextService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<GameContextService> _logger;
    private Guid? _currentGameId;

    public GameContextService(
        SaveStateDbContext dbContext,
        ILogger<GameContextService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Guid? GetCurrentGameId()
    {
        return _currentGameId;
    }

    public async Task<Guid?> GetLastPlayedGameIdAsync(CancellationToken ct = default)
    {
        try
        {
            // Get the most recent game session
            var lastSession = await _dbContext.GameSessions
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(ct);

            if (lastSession != null)
            {
                LogLastPlayedGameFound(_logger, lastSession.GameId);
                return lastSession.GameId;
            }

            LogNoLastPlayedGame(_logger);
            return null;
        }
        catch (Exception ex)
        {
            LogGetLastPlayedFailed(_logger, ex);
            return null;
        }
    }

    public void SetCurrentGame(Guid gameId)
    {
        _currentGameId = gameId;
        LogCurrentGameSet(_logger, gameId);
    }

    public void ClearCurrentGame()
    {
        var previousGameId = _currentGameId;
        _currentGameId = null;

        if (previousGameId.HasValue)
        {
            LogCurrentGameCleared(_logger, previousGameId.Value);
        }
    }

    public bool IsGamePlaying()
    {
        return _currentGameId.HasValue;
    }

    #region LoggerMessage Definitions

    [LoggerMessage(Level = LogLevel.Information, Message = "Current game set to {GameId}")]
    private static partial void LogCurrentGameSet(ILogger logger, Guid gameId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Current game cleared (was {GameId})")]
    private static partial void LogCurrentGameCleared(ILogger logger, Guid gameId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Last played game found: {GameId}")]
    private static partial void LogLastPlayedGameFound(ILogger logger, Guid gameId);

    [LoggerMessage(Level = LogLevel.Information, Message = "No last played game found")]
    private static partial void LogNoLastPlayedGame(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get last played game")]
    private static partial void LogGetLastPlayedFailed(ILogger logger, Exception ex);

    #endregion
}
