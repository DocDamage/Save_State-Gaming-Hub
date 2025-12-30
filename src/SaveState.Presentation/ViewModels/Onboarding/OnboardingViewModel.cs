using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Application.Onboarding.Services;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Onboarding;

/// <summary>
/// View model for the interactive AI-powered onboarding experience.
/// Generates personalized welcome messages and guides users through initial setup.
/// </summary>
public partial class OnboardingViewModel : ObservableObject
{
    private readonly OnboardingService _onboardingService;
    private readonly MainViewModel _mainViewModel;
    private readonly ILogger<OnboardingViewModel> _logger;
    private readonly IUserPreferencesService _userPreferences;
    private readonly SaveState.Presentation.Resources.Resources _resources;

    [ObservableProperty]
    private string _welcomeMessage;

    [ObservableProperty]
    private bool _isLoading = true;

    // Localized properties
    public string Title => _resources.Onboarding_Welcome;
    public string Subtitle => _resources.App_Description;
    public string GetStartedButtonText => _resources.Onboarding_GetStarted;
    public string SkipButtonText => _resources.Onboarding_Skip;

    /// <summary>
    /// Initializes a new instance of the OnboardingViewModel.
    /// </summary>
    /// <param name="onboardingService">Service for generating personalized onboarding content.</param>
    /// <param name="mainViewModel">Main view model for navigation.</param>
    /// <param name="logger">Logger for this view model.</param>
    /// <param name="userPreferences">User preferences service.</param>
    /// <param name="resources">Localized resources.</param>
    public OnboardingViewModel(
        OnboardingService onboardingService,
        MainViewModel mainViewModel,
        ILogger<OnboardingViewModel> logger,
        IUserPreferencesService userPreferences,
        SaveState.Presentation.Resources.Resources resources)
    {
        _onboardingService = onboardingService;
        _mainViewModel = mainViewModel;
        _logger = logger;
        _userPreferences = userPreferences;
        _resources = resources;

        // Set initial localized loading message
        WelcomeMessage = _resources.Status_Loading;
        _ = LoadWelcomeMessageAsync();
    }

    /// <summary>
    /// Loads the personalized welcome message using AI analysis of the user's game library.
    /// </summary>
    private async Task LoadWelcomeMessageAsync()
    {
        try
        {
            IsLoading = true;
            WelcomeMessage = await _onboardingService.GeneratePersonalizedWelcomeAsync();
        }
        catch (Exception ex)
        {
            // Fallback message if AI generation fails
            _logger.LogWarning(ex, "Failed to generate personalized welcome message, using fallback");
            WelcomeMessage = $"{_resources.Onboarding_Welcome} 🎮\n\n" +
                           $"{_resources.App_Description}\n\n" +
                           "Try loading your games to get started!";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Command to proceed with the main application.
    /// </summary>
    [RelayCommand]
    private async Task GetStartedAsync()
    {
        try
        {
            // Mark onboarding as complete
            await _userPreferences.CompleteOnboardingAsync();

            // Navigate to the main game library view
            _mainViewModel.NavigateToGameLibrary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete onboarding");
            // Still navigate even if saving preferences fails
            _mainViewModel.NavigateToGameLibrary();
        }
    }

    /// <summary>
    /// Command to skip the onboarding and go directly to the main application.
    /// </summary>
    [RelayCommand]
    private async Task SkipAsync()
    {
        try
        {
            // Mark onboarding as complete
            await _userPreferences.CompleteOnboardingAsync();

            // Navigate to the main game library view
            _mainViewModel.NavigateToGameLibrary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete onboarding");
            // Still navigate even if saving preferences fails
            _mainViewModel.NavigateToGameLibrary();
        }
    }
}
