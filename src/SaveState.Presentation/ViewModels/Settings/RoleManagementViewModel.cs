using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Presentation.Models.Security;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// Service for role management operations.
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Gets all roles.
    /// </summary>
    Task<Result<IReadOnlyList<Role>>> GetRolesAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    Task<Result<Role>> CreateRoleAsync(string name, string description, List<string> permissions, CancellationToken ct = default);

    /// <summary>
    /// Updates a role.
    /// </summary>
    Task<Result> UpdateRoleAsync(Role role, CancellationToken ct = default);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    Task<Result> DeleteRoleAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Gets all available permissions.
    /// </summary>
    Task<Result<IReadOnlyList<Permission>>> GetAvailablePermissionsAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents a permission in the system.
/// </summary>
public class Permission
{
    /// <summary>Permission identifier (e.g., "read:library").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the permission.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of what this permission allows.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Category/group this permission belongs to.</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for role management interface.
/// Provides functionality for creating, editing, and managing roles and permissions.
/// </summary>
public partial class RoleManagementViewModel : ObservableObject
{
    private readonly IRoleManagementService? _roleManagementService;
    private readonly IDialogService? _dialogService;
    private readonly INotificationService? _notificationService;

    /// <summary>Collection of roles.</summary>
    [ObservableProperty]
    private ObservableCollection<Role> _roles = new();

    /// <summary>Currently selected role.</summary>
    [ObservableProperty]
    private Role? _selectedRole;

    /// <summary>Collection of available permissions.</summary>
    [ObservableProperty]
    private ObservableCollection<Permission> _availablePermissions = new();

    /// <summary>Whether a role is being edited.</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>Whether creating a new role.</summary>
    [ObservableProperty]
    private bool _isCreatingRole;

    /// <summary>Role name for editing.</summary>
    [ObservableProperty]
    private string _editRoleName = string.Empty;

    /// <summary>Role description for editing.</summary>
    [ObservableProperty]
    private string _editRoleDescription = string.Empty;

    /// <summary>Selected permissions for the role being edited.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _editRolePermissions = new();

    /// <summary>Search query for filtering roles.</summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>New role name for creation.</summary>
    [ObservableProperty]
    private string _newRoleName = string.Empty;

    /// <summary>New role description for creation.</summary>
    [ObservableProperty]
    private string _newRoleDescription = string.Empty;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public RoleManagementViewModel()
    {
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleManagementViewModel"/> class.
    /// </summary>
    public RoleManagementViewModel(
        IRoleManagementService? roleManagementService = null,
        IDialogService? dialogService = null,
        INotificationService? notificationService = null)
    {
        _roleManagementService = roleManagementService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Initialize permissions
        AvailablePermissions = new ObservableCollection<Permission>
        {
            new() { Id = "*", Name = "Full Access", Description = "All permissions", Category = "System" },
            new() { Id = "read:library", Name = "Read Library", Description = "View games and library data", Category = "Library" },
            new() { Id = "write:games", Name = "Manage Games", Description = "Add, edit, and remove games", Category = "Library" },
            new() { Id = "read:savestates", Name = "Read Save States", Description = "View save states", Category = "Save States" },
            new() { Id = "write:savestates", Name = "Manage Save States", Description = "Create and manage save states", Category = "Save States" },
            new() { Id = "read:achievements", Name = "Read Achievements", Description = "View achievements", Category = "Achievements" },
            new() { Id = "write:achievements", Name = "Manage Achievements", Description = "Manage achievement data", Category = "Achievements" },
            new() { Id = "read:collections", Name = "Read Collections", Description = "View collections", Category = "Collections" },
            new() { Id = "write:collections", Name = "Manage Collections", Description = "Create and edit collections", Category = "Collections" },
            new() { Id = "read:user", Name = "Read User Data", Description = "View user profiles", Category = "User" },
            new() { Id = "write:user", Name = "Manage User Data", Description = "Edit user profiles", Category = "User" },
            new() { Id = "admin:users", Name = "User Administration", Description = "Manage user accounts", Category = "Administration" },
            new() { Id = "admin:settings", Name = "System Settings", Description = "Modify system settings", Category = "Administration" },
            new() { Id = "plugin:install", Name = "Install Plugins", Description = "Install and manage plugins", Category = "Plugins" },
            new() { Id = "plugin:manage", Name = "Manage Plugins", Description = "Configure and remove plugins", Category = "Plugins" },
            new() { Id = "api:full", Name = "Full API Access", Description = "Complete API access", Category = "API" },
            new() { Id = "moderate:content", Name = "Content Moderation", Description = "Moderate user content", Category = "Moderation" }
        };

        // Initialize roles
        Roles = new ObservableCollection<Role>
        {
            new()
            {
                Id = "admin",
                Name = "Administrator",
                Description = "Full system access with all permissions",
                Permissions = new() { "*" },
                IsSystem = true,
                UserCount = 2
            },
            new()
            {
                Id = "moderator",
                Name = "Moderator",
                Description = "Can moderate content and manage user submissions",
                Permissions = new() { "read:library", "write:games", "moderate:content", "read:achievements", "read:collections" },
                IsSystem = false,
                UserCount = 3
            },
            new()
            {
                Id = "user",
                Name = "User",
                Description = "Standard user with access to library and save states",
                Permissions = new() { "read:library", "write:savestates", "read:achievements", "read:collections", "read:user" },
                IsSystem = true,
                UserCount = 45
            },
            new()
            {
                Id = "developer",
                Name = "Developer",
                Description = "Access to API and plugin development features",
                Permissions = new() { "read:library", "write:games", "api:full", "plugin:install", "plugin:manage" },
                IsSystem = false,
                UserCount = 5
            },
            new()
            {
                Id = "guest",
                Name = "Guest",
                Description = "Limited read-only access for unregistered users",
                Permissions = new() { "read:library" },
                IsSystem = true,
                UserCount = 0
            }
        };
    }

    /// <summary>
    /// Partial method called when SearchQuery changes.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        // Filter logic could be implemented here
    }

    /// <summary>
    /// Loads roles from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadRolesAsync()
    {
        try
        {
            if (_roleManagementService is not null)
            {
                var result = await _roleManagementService.GetRolesAsync();
                if (result.IsSuccess && result.Value is not null)
                {
                    Roles = new ObservableCollection<Role>(result.Value);
                    _notificationService?.ShowSuccess($"Loaded {Roles.Count} roles", "Roles Loaded");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to load roles: {result.Error}");
                }
            }
            else
            {
                _notificationService?.ShowNotificationAsync("Roles refreshed (sample data)", "Refresh");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error loading roles: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the create role dialog.
    /// </summary>
    [RelayCommand]
    private void ShowCreateRole()
    {
        IsCreatingRole = true;
        IsEditing = false;
        NewRoleName = string.Empty;
        NewRoleDescription = string.Empty;
        EditRolePermissions.Clear();
    }

    /// <summary>
    /// Cancels the role creation process.
    /// </summary>
    [RelayCommand]
    private void CancelCreateRole()
    {
        IsCreatingRole = false;
        NewRoleName = string.Empty;
        NewRoleDescription = string.Empty;
        EditRolePermissions.Clear();
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    [RelayCommand]
    private async Task CreateRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoleName))
        {
            _notificationService?.ShowNotificationAsync("Role name is required", "Validation Error");
            return;
        }

        try
        {
            if (_roleManagementService is not null)
            {
                var result = await _roleManagementService.CreateRoleAsync(NewRoleName, NewRoleDescription, EditRolePermissions.ToList());
                if (result.IsSuccess && result.Value is not null)
                {
                    Roles.Add(result.Value);
                    _notificationService?.ShowSuccess($"Role '{NewRoleName}' created", "Role Created");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to create role: {result.Error}");
                    return;
                }
            }
            else
            {
                var newRole = new Role
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Name = NewRoleName,
                    Description = NewRoleDescription,
                    Permissions = new List<string>(EditRolePermissions),
                    IsSystem = false,
                    UserCount = 0
                };
                Roles.Add(newRole);
                _notificationService?.ShowSuccess($"Role '{NewRoleName}' created (sample mode)", "Role Created");
            }

            CancelCreateRole();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error creating role: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts editing a role.
    /// </summary>
    [RelayCommand]
    private void EditRole(Role? role)
    {
        if (role is null) return;
        if (role.IsSystem)
        {
            _notificationService?.ShowNotificationAsync("System roles cannot be edited", "Cannot Edit");
            return;
        }

        SelectedRole = role;
        IsEditing = true;
        IsCreatingRole = false;
        EditRoleName = role.Name;
        EditRoleDescription = role.Description;
        EditRolePermissions = new ObservableCollection<string>(role.Permissions);
    }

    /// <summary>
    /// Saves the edited role.
    /// </summary>
    [RelayCommand]
    private async Task SaveRoleAsync()
    {
        if (SelectedRole is null) return;

        try
        {
            SelectedRole.Name = EditRoleName;
            SelectedRole.Description = EditRoleDescription;
            SelectedRole.Permissions = new List<string>(EditRolePermissions);

            if (_roleManagementService is not null)
            {
                var result = await _roleManagementService.UpdateRoleAsync(SelectedRole);
                if (result.IsSuccess)
                {
                    _notificationService?.ShowSuccess($"Role '{SelectedRole.Name}' updated", "Role Updated");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to update role: {result.Error}");
                    return;
                }
            }
            else
            {
                _notificationService?.ShowSuccess($"Role '{SelectedRole.Name}' updated (sample mode)", "Role Updated");
            }

            // Refresh the role in the list
            var index = Roles.IndexOf(SelectedRole);
            if (index >= 0)
            {
                Roles[index] = SelectedRole;
            }

            CancelEdit();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error saving role: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels the edit operation.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        IsCreatingRole = false;
        EditRoleName = string.Empty;
        EditRoleDescription = string.Empty;
        EditRolePermissions.Clear();
    }

    /// <summary>
    /// Deletes a role.
    /// </summary>
    [RelayCommand]
    private async Task DeleteRoleAsync(Role? role)
    {
        if (role is null) return;
        if (role.IsSystem)
        {
            _notificationService?.ShowNotificationAsync("System roles cannot be deleted", "Cannot Delete");
            return;
        }

        if (role.UserCount > 0)
        {
            var reassignConfirmed = await (_dialogService?.ShowConfirmationAsync(
                "Role In Use",
                $"This role is assigned to {role.UserCount} user(s). Deleting it will remove the role from all users. Continue?",
                "Continue",
                "Cancel") ?? Task.FromResult(false));

            if (!reassignConfirmed) return;
        }

        try
        {
            var confirmed = await (_dialogService?.ShowConfirmationAsync(
                "Delete Role",
                $"Are you sure you want to delete the role '{role.Name}'?\n\nThis action cannot be undone.",
                "Delete",
                "Cancel") ?? Task.FromResult(false));

            if (!confirmed) return;

            if (_roleManagementService is not null)
            {
                var result = await _roleManagementService.DeleteRoleAsync(role.Id);
                if (result.IsSuccess)
                {
                    Roles.Remove(role);
                    _notificationService?.ShowSuccess($"Role '{role.Name}' deleted", "Role Deleted");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to delete role: {result.Error}");
                }
            }
            else
            {
                Roles.Remove(role);
                _notificationService?.ShowSuccess($"Role '{role.Name}' deleted (sample mode)", "Role Deleted");
            }

            if (SelectedRole == role)
            {
                SelectedRole = null;
                CancelEdit();
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error deleting role: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles a permission for the role being edited.
    /// </summary>
    [RelayCommand]
    private void TogglePermission(string? permissionId)
    {
        if (permissionId is null) return;

        if (EditRolePermissions.Contains(permissionId))
        {
            EditRolePermissions.Remove(permissionId);
        }
        else
        {
            EditRolePermissions.Add(permissionId);
        }
    }

    /// <summary>
    /// Views users with a specific role.
    /// </summary>
    [RelayCommand]
    private void ViewUsersWithRole(Role? role)
    {
        if (role is null) return;
        _notificationService?.ShowNotificationAsync($"{role.UserCount} users have the {role.Name} role", "Role Users");
    }

    /// <summary>
    /// Duplicates a role.
    /// </summary>
    [RelayCommand]
    private async Task DuplicateRoleAsync(Role? role)
    {
        if (role is null) return;

        var newName = $"{role.Name} (Copy)";
        var newRole = new Role
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = newName,
            Description = role.Description,
            Permissions = new List<string>(role.Permissions),
            IsSystem = false,
            UserCount = 0
        };

        try
        {
            if (_roleManagementService is not null)
            {
                var result = await _roleManagementService.CreateRoleAsync(newRole.Name, newRole.Description, newRole.Permissions);
                if (result.IsSuccess && result.Value is not null)
                {
                    Roles.Add(result.Value);
                    _notificationService?.ShowSuccess($"Role '{newName}' created", "Role Duplicated");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to duplicate role: {result.Error}");
                }
            }
            else
            {
                Roles.Add(newRole);
                _notificationService?.ShowSuccess($"Role '{newName}' created (sample mode)", "Role Duplicated");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error duplicating role: {ex.Message}");
        }
    }
}
