using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Presentation.Models.Security;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// Service for user management operations.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Resets a user's password.
    /// </summary>
    Task<Result<string>> ResetPasswordAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    Task<Result<UserAccount>> CreateUserAsync(string username, string email, string role, CancellationToken ct = default);

    /// <summary>
    /// Updates a user's active status.
    /// </summary>
    Task<Result> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken ct = default);

    /// <summary>
    /// Gets all user accounts.
    /// </summary>
    Task<Result<IReadOnlyList<UserAccount>>> GetUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates a user account.
    /// </summary>
    Task<Result> UpdateUserAsync(UserAccount user, CancellationToken ct = default);

    /// <summary>
    /// Deletes a user account.
    /// </summary>
    Task<Result> DeleteUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all available roles.
    /// </summary>
    Task<Result<IReadOnlyList<Role>>> GetRolesAsync(CancellationToken ct = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task<Result> AssignRoleAsync(Guid userId, string roleId, CancellationToken ct = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task<Result> RemoveRoleAsync(Guid userId, string roleId, CancellationToken ct = default);

    /// <summary>
    /// Gets active sessions for a user.
    /// </summary>
    Task<Result<IReadOnlyList<UserSession>>> GetUserSessionsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Terminates a user session.
    /// </summary>
    Task<Result> TerminateSessionAsync(string sessionId, CancellationToken ct = default);
}

/// <summary>
/// ViewModel for user management interface.
/// Provides functionality for creating, editing, and managing user accounts.
/// </summary>
public partial class UserManagementViewModel : ObservableObject
{
    private readonly IUserManagementService? _userManagementService;
    private readonly IDialogService? _dialogService;
    private readonly INotificationService? _notificationService;

    /// <summary>Collection of user accounts.</summary>
    [ObservableProperty]
    private ObservableCollection<UserAccount> _users = new();

    /// <summary>Filtered collection of users based on search query.</summary>
    [ObservableProperty]
    private ObservableCollection<UserAccount> _filteredUsers = new();

    /// <summary>Currently selected user account.</summary>
    [ObservableProperty]
    private UserAccount? _selectedUser;

    /// <summary>Collection of available roles.</summary>
    [ObservableProperty]
    private ObservableCollection<Role> _availableRoles = new();

    /// <summary>Whether a user is being edited.</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>Whether creating a new user.</summary>
    [ObservableProperty]
    private bool _isCreatingUser;

    /// <summary>Search query for filtering users.</summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>Username for the new/edit user.</summary>
    [ObservableProperty]
    private string _editUsername = string.Empty;

    /// <summary>Email for the new/edit user.</summary>
    [ObservableProperty]
    private string _editEmail = string.Empty;

    /// <summary>Roles assigned to the user being edited.</summary>
    [ObservableProperty]
    private ObservableCollection<Role> _editUserRoles = new();

    /// <summary>Whether the user being edited is active.</summary>
    [ObservableProperty]
    private bool _editIsActive = true;

    /// <summary>Role for the new user being created.</summary>
    [ObservableProperty]
    private string _newUserRole = "User";

    /// <summary>Email for the new user being created.</summary>
    [ObservableProperty]
    private string _newUserEmail = string.Empty;

    /// <summary>Username for the new user being created.</summary>
    [ObservableProperty]
    private string _newUsername = string.Empty;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public UserManagementViewModel()
    {
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserManagementViewModel"/> class.
    /// </summary>
    public UserManagementViewModel(
        IUserManagementService? userManagementService = null,
        IDialogService? dialogService = null,
        INotificationService? notificationService = null)
    {
        _userManagementService = userManagementService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Initialize available roles
        AvailableRoles = new ObservableCollection<Role>
        {
            new() { Id = "admin", Name = "Admin", Description = "Full system access", IsSystem = true, Permissions = new() { "*" } },
            new() { Id = "moderator", Name = "Moderator", Description = "Can moderate content", Permissions = new() { "read:library", "write:games", "moderate:content" } },
            new() { Id = "user", Name = "User", Description = "Standard user access", IsSystem = true, Permissions = new() { "read:library", "write:savestates" } },
            new() { Id = "developer", Name = "Developer", Description = "API and plugin development access", Permissions = new() { "read:library", "write:games", "api:access", "plugin:install" } },
            new() { Id = "guest", Name = "Guest", Description = "Limited read-only access", Permissions = new() { "read:library" } }
        };

        // Initialize sample users
        var adminRole = AvailableRoles.First(r => r.Id == "admin");
        var userRole = AvailableRoles.First(r => r.Id == "user");
        var moderatorRole = AvailableRoles.First(r => r.Id == "moderator");

        Users = new ObservableCollection<UserAccount>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@savestate.local",
                Role = "Admin",
                Roles = new() { adminRole },
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                LastLogin = DateTime.UtcNow.AddHours(-1),
                IsActive = true,
                ProfileImageUrl = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "gamer123",
                Email = "gamer@example.com",
                Role = "User",
                Roles = new() { userRole },
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                LastLogin = DateTime.UtcNow.AddDays(-2),
                IsActive = true,
                ProfileImageUrl = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "mod_user",
                Email = "moderator@savestate.local",
                Role = "Moderator",
                Roles = new() { moderatorRole, userRole },
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                LastLogin = DateTime.UtcNow.AddDays(-5),
                IsActive = true,
                ProfileImageUrl = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "inactive_user",
                Email = "old@email.com",
                Role = "User",
                Roles = new() { userRole },
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                LastLogin = DateTime.UtcNow.AddMonths(-2),
                IsActive = false,
                ProfileImageUrl = null
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "dev_user",
                Email = "developer@example.com",
                Role = "Developer",
                Roles = new() { AvailableRoles.First(r => r.Id == "developer"), userRole },
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                LastLogin = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                ProfileImageUrl = null
            }
        };

        FilteredUsers = new ObservableCollection<UserAccount>(Users);
    }

    /// <summary>
    /// Partial method called when SearchQuery changes.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        FilterUsers();
    }

    /// <summary>
    /// Partial method called when SelectedUser changes.
    /// </summary>
    partial void OnSelectedUserChanged(UserAccount? value)
    {
        if (value is not null && !IsEditing)
        {
            // Auto-populate edit fields when selecting a user
            EditUsername = value.Username;
            EditEmail = value.Email;
            EditIsActive = value.IsActive;
            EditUserRoles = new ObservableCollection<Role>(value.Roles);
        }
    }

    /// <summary>
    /// Filters the user list based on search query.
    /// </summary>
    private void FilterUsers()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            FilteredUsers = new ObservableCollection<UserAccount>(Users);
        }
        else
        {
            var query = SearchQuery.ToLowerInvariant();
            var filtered = Users.Where(u =>
                u.Username.ToLowerInvariant().Contains(query) ||
                u.Email.ToLowerInvariant().Contains(query) ||
                u.Role.ToLowerInvariant().Contains(query));
            FilteredUsers = new ObservableCollection<UserAccount>(filtered);
        }
    }

    /// <summary>
    /// Loads users from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        try
        {
            if (_userManagementService is not null)
            {
                var result = await _userManagementService.GetUsersAsync();
                if (result.IsSuccess && result.Value is not null)
                {
                    Users = new ObservableCollection<UserAccount>(result.Value);
                    FilterUsers();
                    _notificationService?.ShowSuccess($"Loaded {Users.Count} users", "Users Loaded");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to load users: {result.Error}");
                }
            }
            else
            {
                // Refresh from sample data
                FilterUsers();
                _notificationService?.ShowNotificationAsync("Users refreshed (sample data)", "Refresh");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error loading users: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the create user dialog.
    /// </summary>
    [RelayCommand]
    private void ShowCreateUser()
    {
        IsCreatingUser = true;
        IsEditing = false;
        NewUsername = string.Empty;
        NewUserEmail = string.Empty;
        NewUserRole = "User";
    }

    /// <summary>
    /// Cancels the user creation process.
    /// </summary>
    [RelayCommand]
    private void CancelCreateUser()
    {
        IsCreatingUser = false;
        NewUsername = string.Empty;
        NewUserEmail = string.Empty;
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUsername))
        {
            _notificationService?.ShowNotificationAsync("Username is required", "Validation Error");
            return;
        }

        try
        {
            if (_userManagementService is not null)
            {
                var result = await _userManagementService.CreateUserAsync(NewUsername, NewUserEmail, NewUserRole);
                if (result.IsSuccess && result.Value is not null)
                {
                    Users.Add(result.Value);
                    FilterUsers();
                    _notificationService?.ShowSuccess($"User '{NewUsername}' created successfully", "User Created");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to create user: {result.Error}");
                    return;
                }
            }
            else
            {
                // Create locally
                var newUser = new UserAccount
                {
                    Id = Guid.NewGuid(),
                    Username = NewUsername,
                    Email = NewUserEmail,
                    Role = NewUserRole,
                    Roles = new() { AvailableRoles.FirstOrDefault(r => r.Name == NewUserRole) ?? AvailableRoles.First(r => r.Id == "user") },
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                Users.Add(newUser);
                FilterUsers();
                _notificationService?.ShowSuccess($"User '{NewUsername}' created (sample mode)", "User Created");
            }

            CancelCreateUser();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error creating user: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts editing a user.
    /// </summary>
    [RelayCommand]
    private void EditUserAsync(UserAccount? user)
    {
        if (user is null) return;

        SelectedUser = user;
        IsEditing = true;
        IsCreatingUser = false;
        EditUsername = user.Username;
        EditEmail = user.Email;
        EditIsActive = user.IsActive;
        EditUserRoles = new ObservableCollection<Role>(user.Roles);
    }

    /// <summary>
    /// Saves the edited user.
    /// </summary>
    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (SelectedUser is null) return;

        try
        {
            SelectedUser.Username = EditUsername;
            SelectedUser.Email = EditEmail;
            SelectedUser.IsActive = EditIsActive;
            SelectedUser.Roles = new List<Role>(EditUserRoles);
            SelectedUser.Role = EditUserRoles.FirstOrDefault()?.Name ?? "User";

            if (_userManagementService is not null)
            {
                var result = await _userManagementService.UpdateUserAsync(SelectedUser);
                if (result.IsSuccess)
                {
                    _notificationService?.ShowSuccess($"User '{SelectedUser.Username}' updated", "User Updated");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to update user: {result.Error}");
                    return;
                }
            }
            else
            {
                _notificationService?.ShowSuccess($"User '{SelectedUser.Username}' updated (sample mode)", "User Updated");
            }

            // Refresh the user in the list
            var index = Users.IndexOf(SelectedUser);
            if (index >= 0)
            {
                Users[index] = SelectedUser;
            }

            FilterUsers();
            CancelEdit();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error saving user: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels the edit operation.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        IsCreatingUser = false;
        EditUsername = string.Empty;
        EditEmail = string.Empty;
        EditUserRoles.Clear();
        EditIsActive = true;
    }

    /// <summary>
    /// Deletes a user account.
    /// </summary>
    [RelayCommand]
    private async Task DeleteUserAsync(UserAccount? user)
    {
        if (user is null) return;

        try
        {
            var confirmed = await (_dialogService?.ShowConfirmationAsync(
                "Delete User",
                $"Are you sure you want to delete user '{user.Username}'?\n\nThis action cannot be undone.",
                "Delete",
                "Cancel") ?? Task.FromResult(false));

            if (!confirmed) return;

            if (_userManagementService is not null)
            {
                var result = await _userManagementService.DeleteUserAsync(user.Id);
                if (result.IsSuccess)
                {
                    Users.Remove(user);
                    FilterUsers();
                    _notificationService?.ShowSuccess($"User '{user.Username}' deleted", "User Deleted");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to delete user: {result.Error}");
                }
            }
            else
            {
                Users.Remove(user);
                FilterUsers();
                _notificationService?.ShowSuccess($"User '{user.Username}' deleted (sample mode)", "User Deleted");
            }

            if (SelectedUser == user)
            {
                SelectedUser = null;
                CancelEdit();
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error deleting user: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets the password for a user.
    /// </summary>
    [RelayCommand]
    private async Task ResetPasswordAsync(UserAccount? user)
    {
        if (user is null) return;

        try
        {
            var confirmed = await (_dialogService?.ShowConfirmationAsync(
                "Reset Password",
                $"Are you sure you want to reset the password for {user.Username}?\n\nA new temporary password will be generated.",
                "Reset",
                "Cancel") ?? Task.FromResult(false));

            if (!confirmed) return;

            if (_userManagementService is not null)
            {
                var result = await _userManagementService.ResetPasswordAsync(user.Id);
                if (result.IsSuccess)
                {
                    await (_dialogService?.ShowInformationAsync(
                        "Password Reset",
                        $"New temporary password for {user.Username}:\n\n{result.Value}\n\nPlease share this securely with the user.") ?? Task.CompletedTask);
                    _notificationService?.ShowSuccess($"Password reset for {user.Username}", "Password Reset");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to reset password: {result.Error}");
                }
            }
            else
            {
                var tempPassword = Guid.NewGuid().ToString("N")[..12];
                await (_dialogService?.ShowInformationAsync(
                    "Password Reset (Sample)",
                    $"New temporary password for {user.Username}:\n\n{tempPassword}\n\n(In production, this would be a real reset)") ?? Task.CompletedTask);
                _notificationService?.ShowSuccess($"Password reset for {user.Username} (sample mode)", "Password Reset");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error resetting password: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles the active status of a user account.
    /// </summary>
    [RelayCommand]
    private async Task ToggleUserActiveAsync(UserAccount? user)
    {
        if (user is null) return;

        try
        {
            var newStatus = !user.IsActive;
            var action = newStatus ? "activate" : "deactivate";

            var confirmed = await (_dialogService?.ShowConfirmationAsync(
                $"{(newStatus ? "Activate" : "Deactivate")} User",
                $"Are you sure you want to {action} user '{user.Username}'?",
                newStatus ? "Activate" : "Deactivate",
                "Cancel") ?? Task.FromResult(false));

            if (!confirmed) return;

            if (_userManagementService is not null)
            {
                var result = await _userManagementService.UpdateUserStatusAsync(user.Id, newStatus);
                if (result.IsSuccess)
                {
                    user.IsActive = newStatus;
                    _notificationService?.ShowSuccess($"User '{user.Username}' {(newStatus ? "activated" : "deactivated")}", "Status Updated");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to update status: {result.Error}");
                }
            }
            else
            {
                user.IsActive = newStatus;
                _notificationService?.ShowSuccess($"User '{user.Username}' {(newStatus ? "activated" : "deactivated")} (sample mode)", "Status Updated");
            }

            // Refresh the user in the list
            var index = Users.IndexOf(user);
            if (index >= 0)
            {
                Users[index] = user;
            }
            FilterUsers();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error updating user status: {ex.Message}");
        }
    }

    /// <summary>
    /// Assigns a role to the user being edited.
    /// </summary>
    [RelayCommand]
    private void AssignRoleAsync(Role? role)
    {
        if (role is null) return;
        if (EditUserRoles.Any(r => r.Id == role.Id)) return;

        EditUserRoles.Add(role);
    }

    /// <summary>
    /// Removes a role from the user being edited.
    /// </summary>
    [RelayCommand]
    private void RemoveRoleAsync(Role? role)
    {
        if (role is null) return;
        if (role.IsSystem && EditUserRoles.Count == 1)
        {
            _notificationService?.ShowNotificationAsync("Cannot remove the last system role", "Role Required");
            return;
        }

        EditUserRoles.Remove(role);
    }

    /// <summary>
    /// Views active sessions for a user.
    /// </summary>
    [RelayCommand]
    private async Task ViewUserSessionsAsync(UserAccount? user)
    {
        if (user is null) return;

        try
        {
            if (_userManagementService is not null)
            {
                var result = await _userManagementService.GetUserSessionsAsync(user.Id);
                if (result.IsSuccess && result.Value is not null)
                {
                    var sessions = result.Value;
                    var sessionInfo = string.Join("\n\n", sessions.Select(s =>
                        $"Device: {s.DeviceInfo}\n" +
                        $"IP: {s.IpAddress}\n" +
                        $"Location: {s.Location}\n" +
                        $"Last Active: {s.LastActiveAt:g}\n" +
                        $"{(s.IsCurrentSession ? "[Current Session]" : "")}"));

                    await (_dialogService?.ShowInformationAsync(
                        $"Active Sessions - {user.Username}",
                        $"{sessions.Count} active session(s):\n\n{sessionInfo}") ?? Task.CompletedTask);
                }
                else
                {
                    _notificationService?.ShowError($"Failed to load sessions: {result.Error}");
                }
            }
            else
            {
                // Sample session data
                var sampleSessions = $"Device: Windows PC - Chrome\n" +
                    $"IP: 192.168.1.100\n" +
                    $"Location: Local Network\n" +
                    $"Last Active: {DateTime.UtcNow.AddMinutes(-5):g}\n\n" +
                    $"Device: Mobile - iOS App\n" +
                    $"IP: 203.0.113.42\n" +
                    $"Location: Remote\n" +
                    $"Last Active: {DateTime.UtcNow.AddDays(-1):g}";

                await (_dialogService?.ShowInformationAsync(
                    $"Active Sessions - {user.Username} (Sample)",
                    $"2 active session(s):\n\n{sampleSessions}") ?? Task.CompletedTask);
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error loading sessions: {ex.Message}");
        }
    }
}
