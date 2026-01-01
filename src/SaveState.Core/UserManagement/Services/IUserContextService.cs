namespace SaveState.Core.UserManagement.Services;

/// <summary>
/// Service for accessing the current user context.
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    /// <returns>The current user's ID, or null if no user is authenticated.</returns>
    Guid? GetCurrentUserId();

    /// <summary>
    /// Gets the current user's ID, throwing an exception if no user is authenticated.
    /// </summary>
    /// <returns>The current user's ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no user is authenticated.</exception>
    Guid GetCurrentUserIdRequired();
}