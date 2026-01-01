using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Onboarding.Services;
using SaveState.Core.Common.Services;
using Splat;

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
        CurrentViewModel = new GameLibraryViewModel(_mediator, _resources);
        IsInitialized = false;

        // Start async initialization - fire and forget with proper exception handling
        _ = InitializeViewAsync();
    }

    private async Task InitializeViewAsync()
    {
        try
        {
            var shouldShowOnboarding = await _userPreferences.ShouldShowOnboardingAsync().ConfigureAwait(false);

            CurrentViewModel = shouldShowOnboarding
                ? new Onboarding.OnboardingViewModel(_onboardingService, this, _onboardingLogger, _userPreferences, _resources)
                : new GameLibraryViewModel(_mediator, _resources);

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
    public void NavigateToGameLibrary()
    {
        CurrentViewModel = new GameLibraryViewModel(_mediator, _resources);
    }

    /// <summary>
    /// Navigates to the onboarding view.
    /// </summary>
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
    public void NavigateToSettings()
    {
        // Note: SettingsViewModel will be created by DI when the view is resolved
        // For now, we'll create it manually to match the existing pattern
        CurrentViewModel = new SettingsViewModel(
            Locator.Current.GetService<SaveState.Core.Common.Services.ICultureManager>()!,
            _resources,
            Locator.Current.GetService<SaveState.Presentation.Services.IThemeService>()!);
    }
}
