using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.AutoSave;
using SaveState.Core.AutoSave.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.AutoSave.Services;

/// <summary>
/// Service for managing auto-save functionality.
/// </summary>
public class AutoSaveService : IAutoSaveService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<AutoSaveService> _logger;
    private readonly Dictionary<Guid, AutoSaveSession> _activeSessions = new();

    public AutoSaveService(
        SaveStateDbContext dbContext,
        ILogger<AutoSaveService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<AutoSaveConfiguration>> ConfigureAutoSaveAsync(
        ConfigureAutoSaveRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var config = await _dbContext.AutoSaveConfigurations
                .FirstOrDefaultAsync(c => c.GameId == request.GameId, ct);

            if (config == null)
            {
                config = new AutoSaveConfiguration
                {
                    GameId = request.GameId,
                    IsEnabled = request.IsEnabled ?? true,
                    IntervalMinutes = request.IntervalMinutes ?? 10,
                    MaxAutoSaves = request.MaxAutoSaves ?? 5,
                    SaveOnLevelComplete = request.SaveOnLevelComplete ?? true,
                    SaveBeforeBoss = request.SaveBeforeBoss ?? true,
                    SaveOnCheckpoint = request.SaveOnCheckpoint ?? true,
                    NamingPattern = request.NamingPattern ?? "{GameName} - {Level} - {Time}",
                    Tags = request.Tags ?? new List<string> { "auto-save" }
                };
                _dbContext.AutoSaveConfigurations.Add(config);
            }
            else
            {
                if (request.IsEnabled.HasValue) config.IsEnabled = request.IsEnabled.Value;
                if (request.IntervalMinutes.HasValue) config.IntervalMinutes = request.IntervalMinutes.Value;
                if (request.MaxAutoSaves.HasValue) config.MaxAutoSaves = request.MaxAutoSaves.Value;
                if (request.SaveOnLevelComplete.HasValue) config.SaveOnLevelComplete = request.SaveOnLevelComplete.Value;
                if (request.SaveBeforeBoss.HasValue) config.SaveBeforeBoss = request.SaveBeforeBoss.Value;
                if (request.SaveOnCheckpoint.HasValue) config.SaveOnCheckpoint = request.SaveOnCheckpoint.Value;
                if (!string.IsNullOrEmpty(request.NamingPattern)) config.NamingPattern = request.NamingPattern;
                if (request.Tags != null) config.Tags = request.Tags;
                config.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
            return Result<AutoSaveConfiguration>.Success(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure auto-save for game {GameId:B}", request.GameId);
            return Result<AutoSaveConfiguration>.Failure($"Configuration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<AutoSaveConfiguration>> GetConfigurationAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var config = await _dbContext.AutoSaveConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.GameId == gameId, ct);

            if (config == null)
            {
                // Return default configuration
                config = new AutoSaveConfiguration
                {
                    GameId = gameId,
                    IsEnabled = true,
                    IntervalMinutes = 10,
                    MaxAutoSaves = 5
                };
            }

            return Result<AutoSaveConfiguration>.Success(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto-save configuration for game {GameId:B}", gameId);
            return Result<AutoSaveConfiguration>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> EnableAutoSaveAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        return await ConfigureAutoSaveAsync(
            new ConfigureAutoSaveRequest { GameId = gameId, IsEnabled = true }, ct)
            .ContinueWith(t => t.Result.IsSuccess ? Result.Success() : Result.Failure(t.Result.Error!, t.Result.ErrorType));
    }

    public async Task<Result> DisableAutoSaveAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        return await ConfigureAutoSaveAsync(
            new ConfigureAutoSaveRequest { GameId = gameId, IsEnabled = false }, ct)
            .ContinueWith(t => t.Result.IsSuccess ? Result.Success() : Result.Failure(t.Result.Error!, t.Result.ErrorType));
    }

    public async Task<Result<AutoSaveEntry>> TriggerAutoSaveAsync(
        TriggerAutoSaveRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var config = await GetConfigurationAsync(request.GameId, ct);
            if (config.IsFailure || !config.Value.IsEnabled)
            {
                return Result<AutoSaveEntry>.Failure("Auto-save is disabled", ErrorType.Validation);
            }

            var game = await _dbContext.Games
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == request.GameId, ct);

            var gameName = game?.Title ?? $"Game {request.GameId}";
            var level = request.Level ?? "Unknown";

            var name = !string.IsNullOrEmpty(request.CustomName)
                ? request.CustomName
                : AutoSaveNamingHelper.GenerateDefaultName(gameName, level, request.TriggerType, DateTime.Now);

            var entry = new AutoSaveEntry
            {
                GameId = request.GameId,
                Name = name,
                TriggerType = request.TriggerType,
                Level = level,
                Checkpoint = request.Checkpoint,
                PlayTimeSeconds = request.PlayTimeSeconds ?? 0,
                Tags = new List<string> { "auto-save", request.TriggerType.ToString().ToLower() }
            };

            _dbContext.AutoSaveEntries.Add(entry);
            await _dbContext.SaveChangesAsync(ct);

            // Cleanup old saves if needed
            await CleanupOldSavesAsync(request.GameId, ct);

            _logger.LogInformation("Created auto-save {AutoSaveId} for game {GameId:B}", entry.Id, request.GameId);
            return Result<AutoSaveEntry>.Success(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger auto-save for game {GameId:B}", request.GameId);
            return Result<AutoSaveEntry>.Failure($"Trigger failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<AutoSaveSession>> StartSessionAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var session = new AutoSaveSession
            {
                GameId = gameId,
                StartedAt = DateTime.UtcNow,
                IsActive = true
            };

            _activeSessions[session.Id] = session;

            _logger.LogInformation("Started auto-save session {SessionId} for game {GameId:B}", session.Id, gameId);
            return Task.FromResult(Result<AutoSaveSession>.Success(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start auto-save session for game {GameId:B}", gameId);
            return Task.FromResult(Result<AutoSaveSession>.Failure($"Start failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> StopSessionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            _activeSessions.Remove(sessionId);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop auto-save session {SessionId}", sessionId);
            return Task.FromResult(Result.Failure($"Stop failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<AutoSaveSession>> GetActiveSessionAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.IsActive);
        
        if (session == null)
            return Task.FromResult(Result<AutoSaveSession>.Failure("No active session found", ErrorType.NotFound));
        
        return Task.FromResult(Result<AutoSaveSession>.Success(session));
    }

    public Task<Result> UpdateSessionAsync(
        Guid sessionId,
        string? currentLevel,
        int playTimeSeconds,
        CancellationToken ct = default)
    {
        try
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                session.CurrentLevel = currentLevel;
                session.CurrentPlayTimeSeconds = playTimeSeconds;
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Update failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public async Task<Result<List<AutoSaveEntry>>> GetAutoSavesAsync(
        Guid gameId,
        AutoSaveFilter? filter = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.AutoSaveEntries
                .AsNoTracking()
                .Where(e => e.GameId == gameId);

            if (filter?.TriggerType.HasValue == true)
                query = query.Where(e => e.TriggerType == filter.TriggerType.Value);

            if (filter?.FromDate.HasValue == true)
                query = query.Where(e => e.CreatedAt >= filter.FromDate.Value);

            if (filter?.ToDate.HasValue == true)
                query = query.Where(e => e.CreatedAt <= filter.ToDate.Value);

            if (filter?.OnlyLocked == true)
                query = query.Where(e => e.IsLocked);

            if (filter?.IncludePruned != true)
                query = query.Where(e => !e.IsPruned);

            var entries = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(ct);

            return Result<List<AutoSaveEntry>>.Success(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto-saves for game {GameId:B}", gameId);
            return Result<List<AutoSaveEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<AutoSaveEntry>> GetAutoSaveAsync(
        Guid autoSaveId,
        CancellationToken ct = default)
    {
        try
        {
            var entry = await _dbContext.AutoSaveEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == autoSaveId, ct);

            if (entry == null)
                return Result<AutoSaveEntry>.Failure($"Auto-save {autoSaveId} not found", ErrorType.NotFound);

            return Result<AutoSaveEntry>.Success(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto-save {AutoSaveId}", autoSaveId);
            return Result<AutoSaveEntry>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> LockAutoSaveAsync(
        Guid autoSaveId,
        CancellationToken ct = default)
    {
        try
        {
            var entry = await _dbContext.AutoSaveEntries
                .FirstOrDefaultAsync(e => e.Id == autoSaveId, ct);

            if (entry == null)
                return Result.Failure($"Auto-save {autoSaveId} not found", ErrorType.NotFound);

            entry.IsLocked = true;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock auto-save {AutoSaveId}", autoSaveId);
            return Result.Failure($"Lock failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> UnlockAutoSaveAsync(
        Guid autoSaveId,
        CancellationToken ct = default)
    {
        try
        {
            var entry = await _dbContext.AutoSaveEntries
                .FirstOrDefaultAsync(e => e.Id == autoSaveId, ct);

            if (entry == null)
                return Result.Failure($"Auto-save {autoSaveId} not found", ErrorType.NotFound);

            entry.IsLocked = false;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock auto-save {AutoSaveId}", autoSaveId);
            return Result.Failure($"Unlock failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DeleteAutoSaveAsync(
        Guid autoSaveId,
        CancellationToken ct = default)
    {
        try
        {
            var entry = await _dbContext.AutoSaveEntries
                .FirstOrDefaultAsync(e => e.Id == autoSaveId, ct);

            if (entry == null)
                return Result.Failure($"Auto-save {autoSaveId} not found", ErrorType.NotFound);

            if (entry.IsLocked)
                return Result.Failure("Cannot delete a locked auto-save", ErrorType.Validation);

            _dbContext.AutoSaveEntries.Remove(entry);
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete auto-save {AutoSaveId}", autoSaveId);
            return Result.Failure($"Delete failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<int>> DeleteAllAutoSavesAsync(
        Guid gameId,
        bool includeLocked = false,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.AutoSaveEntries.Where(e => e.GameId == gameId);
            
            if (!includeLocked)
                query = query.Where(e => !e.IsLocked);

            var entries = await query.ToListAsync(ct);
            var count = entries.Count;

            _dbContext.AutoSaveEntries.RemoveRange(entries);
            await _dbContext.SaveChangesAsync(ct);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all auto-saves for game {GameId:B}", gameId);
            return Result<int>.Failure($"Delete failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<int>> CleanupOldSavesAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var config = await GetConfigurationAsync(gameId, ct);
            if (config.IsFailure) return Result<int>.Failure(config.Error!, config.ErrorType);

            var maxSaves = config.Value.MaxAutoSaves;

            var allSaves = await _dbContext.AutoSaveEntries
                .Where(e => e.GameId == gameId && !e.IsLocked)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(ct);

            if (allSaves.Count <= maxSaves)
                return Result<int>.Success(0);

            var toDelete = allSaves.Skip(maxSaves).ToList();
            
            _dbContext.AutoSaveEntries.RemoveRange(toDelete);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Cleaned up {Count} old auto-saves for game {GameId:B}", toDelete.Count, gameId);
            return Result<int>.Success(toDelete.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old saves for game {GameId:B}", gameId);
            return Result<int>.Failure($"Cleanup failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<AutoSaveStatistics>> GetStatisticsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var entries = await _dbContext.AutoSaveEntries
                .AsNoTracking()
                .Where(e => e.GameId == gameId)
                .ToListAsync(ct);

            var stats = new AutoSaveStatistics
            {
                TotalAutoSaves = entries.Count,
                IntervalSaves = entries.Count(e => e.TriggerType == AutoSaveTriggerType.Interval),
                LevelCompleteSaves = entries.Count(e => e.TriggerType == AutoSaveTriggerType.LevelComplete),
                BossSaves = entries.Count(e => e.TriggerType == AutoSaveTriggerType.BossApproach),
                CheckpointSaves = entries.Count(e => e.TriggerType == AutoSaveTriggerType.Checkpoint),
                TotalStorageUsed = entries.Sum(e => e.FileSize),
                FirstSaveDate = entries.Any() ? entries.Min(e => e.CreatedAt) : DateTime.MinValue,
                LastSaveDate = entries.Any() ? entries.Max(e => e.CreatedAt) : DateTime.MinValue,
                AverageSaveSize = entries.Any() ? (int)entries.Average(e => e.FileSize) : 0
            };

            return Result<AutoSaveStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics for game {GameId:B}", gameId);
            return Result<AutoSaveStatistics>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<string>> RestoreAutoSaveAsync(
        Guid autoSaveId,
        string? targetPath = null,
        CancellationToken ct = default)
    {
        // Placeholder - would implement actual file restoration
        return Task.FromResult(Result<string>.Success(targetPath ?? $"restored_{autoSaveId}"));
    }

    public Task<Result<AutoSaveEntry>> HandleLevelCompleteAsync(
        Guid gameId,
        string levelName,
        CancellationToken ct = default)
    {
        return TriggerAutoSaveAsync(new TriggerAutoSaveRequest
        {
            GameId = gameId,
            TriggerType = AutoSaveTriggerType.LevelComplete,
            Level = levelName
        }, ct);
    }

    public Task<Result<AutoSaveEntry>> HandleCheckpointAsync(
        Guid gameId,
        string checkpointName,
        CancellationToken ct = default)
    {
        return TriggerAutoSaveAsync(new TriggerAutoSaveRequest
        {
            GameId = gameId,
            TriggerType = AutoSaveTriggerType.Checkpoint,
            Checkpoint = checkpointName
        }, ct);
    }

    public Task<Result<bool>> DetectBossFightAsync(
        Guid gameId,
        Dictionary<string, object> gameState,
        CancellationToken ct = default)
    {
        // Placeholder - would implement actual detection logic
        var detected = gameState.ContainsKey("boss_approaching") && (bool)gameState["boss_approaching"];
        return Task.FromResult(Result<bool>.Success(detected));
    }

    public Task<Result<AutoSaveEntry>> HandleBossApproachAsync(
        Guid gameId,
        string? bossName = null,
        CancellationToken ct = default)
    {
        return TriggerAutoSaveAsync(new TriggerAutoSaveRequest
        {
            GameId = gameId,
            TriggerType = AutoSaveTriggerType.BossApproach,
            CustomName = bossName != null ? $"Boss: {bossName}" : "Boss Approach"
        }, ct);
    }

    public Task<Result<string>> ExportAutoSaveAsync(
        Guid autoSaveId,
        string outputPath,
        CancellationToken ct = default)
    {
        // Placeholder - would implement actual export
        return Task.FromResult(Result<string>.Success(outputPath));
    }

    public async Task<Result<long>> GetStorageUsageAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var totalSize = await _dbContext.AutoSaveEntries
                .AsNoTracking()
                .Where(e => e.GameId == gameId)
                .SumAsync(e => e.FileSize, ct);

            return Result<long>.Success(totalSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage usage for game {GameId:B}", gameId);
            return Result<long>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<int>> PruneAutoSavesAsync(
        Guid gameId,
        long targetFreeSpace,
        CancellationToken ct = default)
    {
        try
        {
            var entries = await _dbContext.AutoSaveEntries
                .Where(e => e.GameId == gameId && !e.IsLocked)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            var freedSpace = 0L;
            var deletedCount = 0;

            foreach (var entry in entries)
            {
                if (freedSpace >= targetFreeSpace) break;
                
                freedSpace += entry.FileSize;
                _dbContext.AutoSaveEntries.Remove(entry);
                deletedCount++;
            }

            await _dbContext.SaveChangesAsync(ct);
            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prune auto-saves for game {GameId:B}", gameId);
            return Result<int>.Failure($"Prune failed: {ex.Message}", ErrorType.Internal);
        }
    }
}
