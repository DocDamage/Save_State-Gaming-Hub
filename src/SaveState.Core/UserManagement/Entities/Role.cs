using SaveState.Core.Common.Base;

namespace SaveState.Core.UserManagement.Entities;

/// <summary>
/// Represents a role with associated permissions in the system.
/// </summary>
public class Role : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { }

    public static Role Create(string name, string description, bool isSystemRole = false)
    {
        return new Role
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Description = Guard.Against.NullOrWhiteSpace(description, nameof(description)),
            IsSystemRole = isSystemRole
        };
    }

    public void Update(string name, string description)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
    }

    public void AddPermission(Permission permission)
    {
        Guard.Against.Null(permission, nameof(permission));

        if (!_rolePermissions.Any(rp => rp.PermissionId == permission.Id))
        {
            _rolePermissions.Add(RolePermission.Create(this, permission));
        }
    }

    public void RemovePermission(Permission permission)
    {
        Guard.Against.Null(permission, nameof(permission));

        var rolePermission = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permission.Id);
        if (rolePermission != null)
        {
            _rolePermissions.Remove(rolePermission);
        }
    }

    public bool HasPermission(string permissionName)
    {
        return _rolePermissions.Any(rp => rp.Permission.Name == permissionName);
    }

    public IEnumerable<string> GetPermissionNames()
    {
        return _rolePermissions.Select(rp => rp.Permission.Name);
    }
}
