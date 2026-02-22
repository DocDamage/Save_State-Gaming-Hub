using SaveState.Core.Common.Base;

namespace SaveState.Core.UserManagement.Entities;

/// <summary>
/// Junction entity representing the many-to-many relationship between roles and permissions.
/// </summary>
public class RolePermission : EntityBase
{
    public Guid RoleId { get; private set; }
    public required Role Role { get; init; }

    public Guid PermissionId { get; private set; }
    public required Permission Permission { get; init; }

    public DateTimeOffset AssignedAt { get; private set; }

    private RolePermission() { }

    public static RolePermission Create(Role role, Permission permission)
    {
        Guard.Against.Null(role, nameof(role));
        Guard.Against.Null(permission, nameof(permission));

        return new RolePermission
        {
            RoleId = role.Id,
            Role = role,
            PermissionId = permission.Id,
            Permission = permission,
            AssignedAt = DateTimeOffset.UtcNow
        };
    }
}
