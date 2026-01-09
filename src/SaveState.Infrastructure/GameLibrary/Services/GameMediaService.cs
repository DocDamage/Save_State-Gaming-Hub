using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.UserManagement.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Implementation of game media management service.
/// </summary>
public partial class GameMediaService : IGameMediaService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<GameMediaService> _logger;

    public GameMediaService(
        SaveStateDbContext dbContext,
        IUserContextService userContextService,
        ILogger<GameMediaService> logger)
    {
        _dbContext = dbContext;
        _userContextService = userContextService;
        _logger = logger;
    }

    public async Task<Result<List<GameMedia>>> GetGameMediaAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var media = await _dbContext.GameMedia
                .Where(m => m.GameId == gameId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(ct);

            return Result.Success(media);
        }
        catch (Exception ex)
        {
            LogGetMediaFailed(_logger, gameId, ex);
            return Result.Failure<List<GameMedia>>($"Failed to get media: {ex.Message}");
        }
    }

    public async Task<Result<GameMedia>> AddMediaAsync(Guid gameId, string filePath, MediaType type, CancellationToken ct = default)
    {
        try
        {
            LogAddingMedia(_logger, gameId, type);

            // Get file info for metadata
            var fileInfo = new FileInfo(filePath);
            var fileFormat = fileInfo.Extension.TrimStart('.');
            var fileSizeBytes = fileInfo.Length;

            // Get actual user ID from context
            var userIdValue = _userContextService.GetCurrentUserId()
                ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
            var userId = SaveState.Core.Common.ValueObjects.UserId.From(userIdValue);
            var gameIdValue = SaveState.Core.Common.ValueObjects.GameId.From(gameId);

            var media = GameMedia.Create(
                gameIdValue,
                userId,
                type,
                filePath,
                fileSizeBytes,
                fileFormat);

            _dbContext.GameMedia.Add(media);
            await _dbContext.SaveChangesAsync(ct);

            LogMediaAdded(_logger, media.Id, gameId);
            return Result.Success(media);
        }
        catch (Exception ex)
        {
            LogAddMediaFailed(_logger, gameId, ex);
            return Result.Failure<GameMedia>($"Failed to add media: {ex.Message}");
        }
    }

    public async Task<Result> UpdateMediaAsync(Guid mediaId, string? title = null, string? description = null, bool? isFavorite = null, CancellationToken ct = default)
    {
        try
        {
            var media = await _dbContext.GameMedia.FindAsync(new object[] { mediaId }, ct);
            if (media == null)
            {
                return Result.Failure("Media not found");
            }

            if (title != null || description != null)
            {
                media.Update(title ?? media.Title, description ?? media.Description);
            }

            if (isFavorite.HasValue)
            {
                if (isFavorite.Value)
                    media.MarkAsFavorite();
                else
                    media.UnmarkAsFavorite();
            }

            await _dbContext.SaveChangesAsync(ct);

            LogMediaUpdated(_logger, mediaId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogUpdateMediaFailed(_logger, mediaId, ex);
            return Result.Failure($"Failed to update media: {ex.Message}");
        }
    }

    public async Task<Result> DeleteMediaAsync(Guid mediaId, CancellationToken ct = default)
    {
        try
        {
            var media = await _dbContext.GameMedia.FindAsync(new object[] { mediaId }, ct);
            if (media == null)
            {
                return Result.Failure("Media not found");
            }

            _dbContext.GameMedia.Remove(media);
            await _dbContext.SaveChangesAsync(ct);

            LogMediaDeleted(_logger, mediaId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogDeleteMediaFailed(_logger, mediaId, ex);
            return Result.Failure($"Failed to delete media: {ex.Message}");
        }
    }

    public async Task<Result> DeleteMediaBatchAsync(IEnumerable<Guid> mediaIds, CancellationToken ct = default)
    {
        try
        {
            var ids = mediaIds.ToList();
            var media = await _dbContext.GameMedia
                .Where(m => ids.Contains(m.Id))
                .ToListAsync(ct);

            _dbContext.GameMedia.RemoveRange(media);
            await _dbContext.SaveChangesAsync(ct);

            LogMediaBatchDeleted(_logger, media.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogDeleteMediaBatchFailed(_logger, ex);
            return Result.Failure($"Failed to delete media batch: {ex.Message}");
        }
    }

    public async Task<Result> SetFavoriteAsync(Guid mediaId, CancellationToken ct = default)
    {
        return await UpdateMediaAsync(mediaId, isFavorite: true, ct: ct);
    }

    public async Task<Result> UnsetFavoriteAsync(Guid mediaId, CancellationToken ct = default)
    {
        return await UpdateMediaAsync(mediaId, isFavorite: false, ct: ct);
    }

    public async Task<Result<long>> GetStorageUsageAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var media = await _dbContext.GameMedia
                .Where(m => m.GameId == gameId)
                .ToListAsync(ct);

            long totalSize = 0;
            foreach (var item in media)
            {
                if (File.Exists(item.FilePath))
                {
                    var fileInfo = new FileInfo(item.FilePath);
                    totalSize += fileInfo.Length;
                }
            }

            return Result.Success(totalSize);
        }
        catch (Exception ex)
        {
            LogGetStorageFailed(_logger, gameId, ex);
            return Result.Failure<long>($"Failed to calculate storage: {ex.Message}");
        }
    }

    #region LoggerMessage Definitions

    [LoggerMessage(Level = LogLevel.Information, Message = "Adding media for game {GameId}, type {Type}")]
    private static partial void LogAddingMedia(ILogger logger, Guid gameId, MediaType type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Media {MediaId} added for game {GameId}")]
    private static partial void LogMediaAdded(ILogger logger, Guid mediaId, Guid gameId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to add media for game {GameId}")]
    private static partial void LogAddMediaFailed(ILogger logger, Guid gameId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Media {MediaId} updated")]
    private static partial void LogMediaUpdated(ILogger logger, Guid mediaId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update media {MediaId}")]
    private static partial void LogUpdateMediaFailed(ILogger logger, Guid mediaId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Media {MediaId} deleted")]
    private static partial void LogMediaDeleted(ILogger logger, Guid mediaId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete media {MediaId}")]
    private static partial void LogDeleteMediaFailed(ILogger logger, Guid mediaId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch deleted {Count} media items")]
    private static partial void LogMediaBatchDeleted(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete media batch")]
    private static partial void LogDeleteMediaBatchFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get media for game {GameId}")]
    private static partial void LogGetMediaFailed(ILogger logger, Guid gameId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to calculate storage for game {GameId}")]
    private static partial void LogGetStorageFailed(ILogger logger, Guid gameId, Exception ex);

    #endregion
}

