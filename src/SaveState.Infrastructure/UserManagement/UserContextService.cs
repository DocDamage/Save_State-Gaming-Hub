using SaveState.Core.UserManagement.Services;

namespace SaveState.Infrastructure.UserManagement;

/// <summary>
/// Simple user context service implementation.
/// For now, returns a default user ID since this is a single-user desktop application.
/// </summary>
public class UserContextService : IUserContextService
{
    // Default user ID for single-user application
    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <inheritdoc />
    public Guid? CurrentUserId => DefaultUserId;

    /// <inheritdoc />
    public string? CurrentUsername => "LocalUser";

    /// <inheritdoc />
    public Guid? GetCurrentUserId() => DefaultUserId;

    /// <inheritdoc />
    public Guid GetCurrentUserIdRequired() => DefaultUserId;
}
