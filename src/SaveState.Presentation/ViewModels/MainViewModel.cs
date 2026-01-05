using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Onboarding.Services;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell;
using SaveState.Presentation.ViewModels.Library.GameDetail;
using Splat;

using CommunityToolkit.Mvvm.Input;

namespace SaveState.Presentation.ViewModels;

/// <summary>
/// Main view model that manages navigation between different application views.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly OnboardingService _onboardingService;
    private readonly ILogger<Onboarding.OnboardingViewModel> _onboardingLogger;
    private readonly IUserPreferencesService _userPreferences;
    private readonly ILogger<MainViewModel> _logger;
    private readonly SaveState.Presentation.Resources.Resources _resources;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private bool _isInitialized;

    // Localized properties
    /// <summary>
    /// Gets the application title for display.
    /// </summary>
    public string Title => _resources.App_Name;

    public MainViewModel(
        IMediator mediator,
        OnboardingService onboardingService,
        ILogger<Onboarding.OnboardingViewModel> onboardingLogger,
        IUserPreferencesService userPreferences,
        ILogger<MainViewModel> logger,
        SaveState.Presentation.Resources.Resources resources)
    {
        _mediator = mediator;
        _onboardingService = onboardingService;
        _onboardingLogger = onboardingLogger;
        _userPreferences = userPreferences;
        _logger = logger;
        _resources = resources;

        // Set default view initially to avoid null reference issues
        CurrentViewModel = Locator.Current.GetService<GameLibraryViewModel>()!;
        IsInitialized = false;

        // Start async initialization - fire and forget with proper exception handling
        _ = InitializeViewAsync();
    }

    private async Task InitializeViewAsync()
    {
        try
        {
            var shouldShowOnboarding = await _userPreferences.ShouldShowOnboardingAsync().ConfigureAwait(false);

            if (shouldShowOnboarding)
            {
                CurrentViewModel = new Onboarding.OnboardingViewModel(_onboardingService, this, _onboardingLogger, _userPreferences, _resources);
            }
            else
            {
                // Get services from the DI container
                CurrentViewModel = Locator.Current.GetService<GameLibraryViewModel>()!;
            }

            IsInitialized = true;
            _logger.LogInformation("MainViewModel initialization completed successfully. Showing {ViewType}",
                shouldShowOnboarding ? "Onboarding" : "GameLibrary");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MainViewModel. Using default GameLibrary view as fallback.");
            // Keep the default GameLibrary view that was set in constructor
            // This ensures the application remains functional even if preferences loading fails
            IsInitialized = true;
        }
    }

    /// <summary>
    /// Navigates to the main game library view.
    /// </summary>
    [RelayCommand]
    public void NavigateToGameLibrary()
    {
        CurrentViewModel = Locator.Current.GetService<GameLibraryViewModel>()!;
    }

    /// <summary>
    /// Navigates to the dashboard view.
    /// </summary>
    [RelayCommand]
    public void NavigateToDashboard()
    {
        CurrentViewModel = Locator.Current.GetService<DashboardViewModel>()!;
    }

    /// <summary>
    /// Navigates to the onboarding view.
    /// </summary>
    [RelayCommand]
    public void NavigateToOnboarding()
    {
        CurrentViewModel = new Onboarding.OnboardingViewModel(
            _onboardingService,
            this,
            _onboardingLogger,
            _userPreferences,
            _resources);
    }

    /// <summary>
    /// Navigates to the settings view.
    /// </summary>
    [RelayCommand]
    public void NavigateToSettings()
    {
        CurrentViewModel = Locator.Current.GetService<SettingsViewModel>()!;
    }

    /// <summary>
    /// Navigates to the MUGEN management view.
    /// </summary>
    [RelayCommand]
    public void NavigateToMugen()
    {
        var vm = Locator.Current.GetService<MugenViewModel>();
        vm.SetParent(this);
        CurrentViewModel = vm;
    }

    /// <summary>
    /// Navigates to the game detail view for a specific game.
    /// </summary>
    /// <param name="gameId">The ID of the game to display.</param>
    public void NavigateToGameDetail(Core.Common.ValueObjects.GameId gameId)
    {
        var mediator = Locator.Current.GetService<IMediator>()!;
        var navigationService = Locator.Current.GetService<INavigationService>()!;
        var overlayService = Locator.Current.GetService<IOverlayService>()!;
        var notificationService = Locator.Current.GetService<INotificationService>()!;
        var aiOrchestrator = Locator.Current.GetService<Core.Ai.Services.IAiOrchestrator>()!;
        var userContextService = Locator.Current.GetService<Core.UserManagement.Services.IUserContextService>()!;
        var modService = Locator.Current.GetService<Core.GameLibrary.Services.IModManagementService>()!;
        var dialogService = Locator.Current.GetService<IDialogService>()!;
        var loggerFactory = Locator.Current.GetService<ILoggerFactory>()!;

        CurrentViewModel = new GameDetailViewModel(
            mediator,
            navigationService,
            overlayService,
            notificationService,
            aiOrchestrator,
            userContextService,
            modService,
            dialogService,
            gameId,
            loggerFactory);
    }
}
