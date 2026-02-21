using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Security;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for user management interface.
/// Provides functionality for creating, editing, and managing user accounts.
/// </summary>
public partial class UserManagementViewModel : ObservableObject
{
    /// <summary>Collection of user accounts.</summary>
    [ObservableProperty]
    private ObservableCollection<UserAccount> _users = new();

    /// <summary>Currently selected user account.</summary>
    [ObservableProperty]
    private UserAccount? _selectedUser;

    /// <summary>Whether the create user dialog is visible.</summary>
    [ObservableProperty]
    private bool _isCreatingUser;

    /// <summary>Username for the new user being created.</summary>
    [ObservableProperty]
    private string _newUsername = string.Empty;

    /// <summary>Email for the new user being created.</summary>
    [ObservableProperty]
    private string _newUserEmail = string.Empty;

    /// <summary>Role for the new user being created.</summary>
    [ObservableProperty]
    private string _newUserRole = "User";

    /// <summary>Available roles for user assignment.</summary>
    public List<string> AvailableRoles { get; } = new() { "Admin", "User", "Guest" };

    /// <summary>
    /// Initializes a new instance of the <see cref="UserManagementViewModel"/> class.
    /// </summary>
    public UserManagementViewModel()
    {
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        Users = new ObservableCollection<UserAccount>
        {
            new() { Id = Guid.NewGuid(), Username = "admin", Email = "admin@savestate.local", Role = "Admin", CreatedAt = DateTime.Now.AddYears(-1), LastLogin = DateTime.Now.AddHours(-1), IsActive = true },
            new() { Id = Guid.NewGuid(), Username = "gamer1", Email = "gamer@example.com", Role = "User", CreatedAt = DateTime.Now.AddMonths(-6), LastLogin = DateTime.Now.AddDays(-2), IsActive = true }
        };
    }

    /// <summary>
    /// Shows the create user dialog.
    /// </summary>
    [RelayCommand]
    private void ShowCreateUser()
    {
        IsCreatingUser = true;
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
        if (string.IsNullOrWhiteSpace(NewUsername)) return;

        Users.Add(new UserAccount
        {
            Id = Guid.NewGuid(),
            Username = NewUsername,
            Email = NewUserEmail,
            Role = NewUserRole,
            CreatedAt = DateTime.Now,
            IsActive = true
        });

        CancelCreateUser();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Resets the password for a user.
    /// </summary>
    /// <param name="user">The user whose password should be reset.</param>
    [RelayCommand]
    private async Task ResetPasswordAsync(UserAccount? user)
    {
        if (user is null) return;
        // TODO: Reset password through service
        await Task.CompletedTask;
    }

    /// <summary>
    /// Toggles the active status of a user account.
    /// </summary>
    /// <param name="user">The user to toggle.</param>
    [RelayCommand]
    private void ToggleUserStatus(UserAccount? user)
    {
        if (user is null) return;
        user.IsActive = !user.IsActive;
    }
}
