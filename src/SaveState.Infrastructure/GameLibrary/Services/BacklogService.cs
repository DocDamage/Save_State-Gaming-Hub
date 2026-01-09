using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Service for managing gaming backlog and wishlists.
/// Tracks games to play, prioritizes them, and manages completion status.
/// </summary>
public class BacklogService : IBacklogService
{
    private readonly IBacklogRepository _backlogRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<BacklogService> _logger;

    public BacklogService(
        IBacklogRepository backlogRepository,
        IGameRepository gameRepository,
        ILogger<BacklogService> logger)
    {
        _backlogRepository = backlogRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    /// <summary>
    /// Adds a game to the user's backlog with specified priority.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game to add.</param>
    /// <param name="priority">The priority level (default: 50).</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the backlog entry or an error.</returns>
    public async Task<Result<BacklogEntry>> AddToBacklogAsync(Guid gameId, int priority = 50, CancellationToken ct = default)
    {
        try
        {
    // Check if game exists
    var gameIdValue = SaveState.Core.Common.ValueObjects.GameId.From(gameId);
    var game = await _gameRepository.GetByIdAsync(gameIdValue, ct).ConfigureAwait(false);
    if (game == null)
    {
        return Result.Failure<BacklogEntry>($"Game with ID {gameId} not found", ErrorType.NotFound);
    }

            // Check if already in backlog
            var existingEntry = await _backlogRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            if (existingEntry != null)
            {
                return Result.Failure<BacklogEntry>($"Game '{game.Title}' is already in the backlog", ErrorType.Conflict);
            }

            var entry = BacklogEntry.Create(gameId, priority);
            await _backlogRepository.AddAsync(entry, ct).ConfigureAwait(false);

            _logger.LogInformation("Added game '{Title}' (ID: {GameId}) to backlog with priority {Priority}",
                game.Title, gameId, priority);

            return Result.Success<BacklogEntry>(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add game {GameId} to backlog", gameId);
            return Result.Failure<BacklogEntry>($"Failed to add game to backlog: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Removes a game from the user's backlog.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game to remove.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> RemoveFromBacklogAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var entry = await _backlogRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            if (entry == null)
            {
                return Result.Failure($"Game with ID {gameId} not found in backlog", ErrorType.NotFound);
            }

            await _backlogRepository.DeleteAsync(entry.Id, ct).ConfigureAwait(false);

            _logger.LogInformation("Removed game '{Title}' (ID: {GameId}) from backlog",
                entry.Game.Title, gameId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove game {GameId} from backlog", gameId);
            return Result.Failure($"Failed to remove game from backlog: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Updates the status of a backlog entry.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="status">The new backlog status.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> UpdateBacklogStatusAsync(Guid gameId, BacklogStatus status, CancellationToken ct = default)
    {
        try
        {
            var entry = await _backlogRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            if (entry == null)
            {
                return Result.Failure($"Game with ID {gameId} not found in backlog", ErrorType.NotFound);
            }

            entry.UpdateStatus(status);
            await _backlogRepository.UpdateAsync(entry, ct).ConfigureAwait(false);

            _logger.LogInformation("Updated backlog status for game '{Title}' (ID: {GameId}) to {Status}",
                entry.Game.Title, gameId, status);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update backlog status for game {GameId}", gameId);
            return Result.Failure($"Failed to update backlog status: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> UpdatePriorityAsync(Guid gameId, int priority, CancellationToken ct = default)
    {
        try
        {
            var entry = await _backlogRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            if (entry == null)
            {
                return Result.Failure($"Game with ID {gameId} not found in backlog", ErrorType.NotFound);
            }

            entry.UpdatePriority(priority);
            await _backlogRepository.UpdateAsync(entry, ct).ConfigureAwait(false);

            _logger.LogInformation("Updated priority for game '{Title}' (ID: {GameId}) to {Priority}",
                entry.Game.Title, gameId, priority);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update priority for game {GameId}", gameId);
            return Result.Failure($"Failed to update priority: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> UpdateNotesAsync(Guid gameId, string? notes, CancellationToken ct = default)
    {
        try
        {
            var entry = await _backlogRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            if (entry == null)
            {
                return Result.Failure($"Game with ID {gameId} not found in backlog", ErrorType.NotFound);
            }

            entry.SetNotes(notes);
            await _backlogRepository.UpdateAsync(entry, ct).ConfigureAwait(false);

            _logger.LogInformation("Updated notes for game '{Title}' (ID: {GameId})",
                entry.Game.Title, gameId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notes for game {GameId}", gameId);
            return Result.Failure($"Failed to update notes: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> SetEstimatedPlaytimeAsync(Guid gameId, TimeSpan? playtime, CancellationToken ct = default)
    {
        try
        {
            var entry = await _backlogRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            if (entry == null)
            {
                return Result.Failure($"Game with ID {gameId} not found in backlog", ErrorType.NotFound);
            }

            entry.SetEstimatedPlaytime(playtime);
            await _backlogRepository.UpdateAsync(entry, ct).ConfigureAwait(false);

            _logger.LogInformation("Set estimated playtime for game '{Title}' (ID: {GameId}) to {Playtime}",
                entry.Game.Title, gameId, playtime?.ToString() ?? "null");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set estimated playtime for game {GameId}", gameId);
            return Result.Failure($"Failed to set estimated playtime: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> SetTargetCompletionDateAsync(Guid gameId, DateTime? date, CancellationToken ct = default)
    {
        try
        {
            var entry = await _backlogRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            if (entry == null)
            {
                return Result.Failure($"Game with ID {gameId} not found in backlog", ErrorType.NotFound);
            }

            entry.SetTargetDate(date);
            await _backlogRepository.UpdateAsync(entry, ct).ConfigureAwait(false);

            _logger.LogInformation("Set target completion date for game '{Title}' (ID: {GameId}) to {Date}",
                entry.Game.Title, gameId, date?.ToString("yyyy-MM-dd") ?? "null");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set target completion date for game {GameId}", gameId);
            return Result.Failure($"Failed to set target completion date: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<BacklogEntry?>> GetBacklogEntryAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var entry = await _backlogRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
            return Result.Success<BacklogEntry?>(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backlog entry for game {GameId}", gameId);
            return Result.Failure<BacklogEntry?>($"Failed to get backlog entry: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<PagedResult<BacklogEntry>>> GetBacklogAsync(
        int pageNumber = 1,
        int pageSize = 50,
        BacklogStatus? status = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _backlogRepository.GetBacklogAsync(pageNumber, pageSize, status, ct).ConfigureAwait(false);
            return Result.Success<PagedResult<BacklogEntry>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backlog (page {PageNumber}, size {PageSize}, status {Status})",
                pageNumber, pageSize, status);
            return Result.Failure<PagedResult<BacklogEntry>>($"Failed to get backlog: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<BacklogStatistics>> GetStatisticsAsync(CancellationToken ct = default)
    {
        try
        {
            var stats = await _backlogRepository.GetStatisticsAsync(ct).ConfigureAwait(false);
            return Result.Success<BacklogStatistics>(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backlog statistics");
            return Result.Failure<BacklogStatistics>($"Failed to get backlog statistics: {ex.Message}", ErrorType.Internal);
        }
    }
}

