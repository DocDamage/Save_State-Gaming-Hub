using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.UserManagement.Repositories;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class UserProfileOverlayViewModel : ObservableObject
{
    private readonly IUserContextService _userContext;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IOverlayService _overlayService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _username = "Guest";

    [ObservableProperty]
    private string _displayName = "Anonymous Player";

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private string _rank = "Newcomer";

    public UserProfileOverlayViewModel(
        IUserContextService userContext,
        IUserRepository userRepository,
        INotificationService notificationService,
        IOverlayService overlayService,
        INavigationService navigationService)
    {
        _userContext = userContext;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _overlayService = overlayService;
        _navigationService = navigationService;

        _ = LoadUserProfileAsync();
    }

    private async Task LoadUserProfileAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user != null)
            {
                Username = user.Username;
                DisplayName = user.Username; // Using username as display name for now
                // Rank/Avatar could be hardcoded or from a settings/profile service
                Rank = "Elite Pioneer";
            }
        }
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        _overlayService.HideUserProfileOverlay();
        await _navigationService.NavigateTo("Settings");
    }

    [RelayCommand]
    private void Logout()
    {
        _notificationService.ShowInfo("Logging out...", "Profile");
        _overlayService.HideUserProfileOverlay();
        // Trigger actual logout logic here
    }

    [RelayCommand]
    private void SwitchUser()
    {
        _notificationService.ShowInfo("Switching user...", "Profile");
        _overlayService.HideUserProfileOverlay();
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideUserProfileOverlay();
    }
}
