using SaveState.Core.Common.Base;

namespace SaveState.Core.UserManagement.Entities;

/// <summary>
/// Represents a specific permission that can be granted to roles.
/// </summary>
public class Permission : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Permission() { }

    public static Permission Create(string name, string description, string resource, string action)
    {
        return new Permission
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Description = Guard.Against.NullOrWhiteSpace(description, nameof(description)),
            Resource = Guard.Against.NullOrWhiteSpace(resource, nameof(resource)),
            Action = Guard.Against.NullOrWhiteSpace(action, nameof(action))
        };
    }

    public string GetFullPermissionName()
    {
        return $"{Resource}:{Action}";
    }

    public override string ToString()
    {
        return GetFullPermissionName();
    }
}
