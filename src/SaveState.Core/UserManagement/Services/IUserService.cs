using SaveState.Core.Common;

namespace SaveState.Core.UserManagement.Services;

/// <summary>
/// Service for managing user information and profiles.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets the username for a given user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The username if found, otherwise a failure result.</returns>
    Task<Result<string>> GetUsernameAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    /// <returns>The current user's ID.</returns>
    Guid GetCurrentUserId();

    /// <summary>
    /// Gets the current user's username.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current username.</returns>
    Task<Result<string>> GetCurrentUsernameAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the username for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="username">The new username.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> UpdateUsernameAsync(Guid userId, string username, CancellationToken ct = default);
}
