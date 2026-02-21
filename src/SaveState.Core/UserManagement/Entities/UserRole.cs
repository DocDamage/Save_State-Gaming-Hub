using SaveState.Core.Common.Base;

namespace SaveState.Core.UserManagement.Entities;

/// <summary>
/// Junction entity representing the many-to-many relationship between users and roles.
/// </summary>
public class UserRole : EntityBase
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!; // Set via Create factory method

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!; // Set via Create factory method

    public DateTimeOffset AssignedAt { get; private set; }

    private UserRole() { }

    public static UserRole Create(User user, Role role)
    {
        Guard.Against.Null(user, nameof(user));
        Guard.Against.Null(role, nameof(role));

        return new UserRole
        {
            UserId = user.Id,
            User = user,
            RoleId = role.Id,
            Role = role,
            AssignedAt = DateTimeOffset.UtcNow
        };
    }
}
