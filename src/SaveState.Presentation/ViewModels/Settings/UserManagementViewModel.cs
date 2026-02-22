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
        Users = new ObservableCollection<UserAccount>
        {
            new() { Id = Guid.NewGuid(), Username = "admin", Email = "admin@savestate.local", Role = "Admin", CreatedAt = DateTimeOffset.UtcNow.AddYears(-1).DateTime, LastLogin = DateTimeOffset.UtcNow.AddHours(-1).DateTime, IsActive = true },
            new() { Id = Guid.NewGuid(), Username = "gamer1", Email = "gamer@example.com", Role = "User", CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6).DateTime, LastLogin = DateTimeOffset.UtcNow.AddDays(-2).DateTime, IsActive = true }
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
            CreatedAt = DateTimeOffset.UtcNow.DateTime,
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

        try
        {
            if (_userManagementService is not null)
            {
                var confirmed = await (_dialogService?.ShowConfirmationAsync(
                    "Reset Password",
                    $"Are you sure you want to reset the password for {user.Username}?",
                    "Reset",
                    "Cancel") ?? Task.FromResult(false));

                if (!confirmed) return;

                var result = await _userManagementService.ResetPasswordAsync(user.Id);
                if (result.IsSuccess)
                {
                    await (_dialogService?.ShowInformationAsync(
                        "Password Reset",
                        $"New temporary password for {user.Username}: {result.Value}") ?? Task.CompletedTask);
                    _notificationService?.ShowSuccess($"Password reset for {user.Username}", "Password Reset");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to reset password: {result.Error}");
                }
            }
            else
            {
                _notificationService?.ShowNotificationAsync(
                    "Password reset not available - service not configured",
                    "Password Reset");
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
    /// <param name="user">The user to toggle.</param>
    [RelayCommand]
    private void ToggleUserStatus(UserAccount? user)
    {
        if (user is null) return;
        user.IsActive = !user.IsActive;
    }
}
