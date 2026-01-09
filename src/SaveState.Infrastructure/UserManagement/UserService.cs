using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.UserManagement.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.UserManagement;

/// <summary>
/// Implementation of user service for managing user information.
/// </summary>
public partial class UserService : IUserService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<UserService> _logger;
    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public UserService(SaveStateDbContext dbContext, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<string>> GetUsernameAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _dbContext.Users.FindAsync(new object[] { userId }, ct);

            if (user == null)
            {
                LogUserNotFound(_logger, userId);
                // Return a default username based on user ID for missing users
                return Result.Success($"User-{userId.ToString().Substring(0, 8)}");
            }

            return Result.Success(user.Username);
        }
        catch (Exception ex)
        {
            LogGetUsernameFailed(_logger, userId, ex);
            return Result.Failure<string>($"Failed to retrieve username: {ex.Message}");
        }
    }

    public Guid GetCurrentUserId()
    {
        // In a real application, this would retrieve the ID from authentication context
        // For now, return a default user ID
        return DefaultUserId;
    }

    public async Task<Result<string>> GetCurrentUsernameAsync(CancellationToken ct = default)
    {
        var currentUserId = GetCurrentUserId();
        return await GetUsernameAsync(currentUserId, ct);
    }

    public async Task<Result> UpdateUsernameAsync(Guid userId, string username, CancellationToken ct = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username, nameof(username));

            var user = await _dbContext.Users.FindAsync(new object[] { userId }, ct);

            if (user == null)
            {
                LogUserNotFound(_logger, userId);
                return Result.Failure($"User {userId} not found");
            }

            user.UpdateUsername(username);
            await _dbContext.SaveChangesAsync(ct);

            LogUsernameUpdated(_logger, userId, username);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogUpdateUsernameFailed(_logger, userId, ex);
            return Result.Failure($"Failed to update username: {ex.Message}");
        }
    }

    #region LoggerMessage Definitions
    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} not found")]
    private static partial void LogUserNotFound(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get username for user {UserId}")]
    private static partial void LogGetUsernameFailed(ILogger logger, Guid userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated username for user {UserId} to {Username}")]
    private static partial void LogUsernameUpdated(ILogger logger, Guid userId, string username);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update username for user {UserId}")]
    private static partial void LogUpdateUsernameFailed(ILogger logger, Guid userId, Exception ex);
    #endregion
}

