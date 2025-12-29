using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Application.Onboarding.Services;
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

    [ObservableProperty]
    private string _welcomeMessage = "Loading your personalized welcome message...";

    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>
    /// Initializes a new instance of the OnboardingViewModel.
    /// </summary>
    /// <param name="onboardingService">Service for generating personalized onboarding content.</param>
    /// <param name="mainViewModel">Main view model for navigation.</param>
    /// <param name="logger">Logger for this view model.</param>
    public OnboardingViewModel(OnboardingService onboardingService, MainViewModel mainViewModel, ILogger<OnboardingViewModel> logger)
    {
        _onboardingService = onboardingService;
        _mainViewModel = mainViewModel;
        _logger = logger;
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
            WelcomeMessage = "Welcome to SaveState Reborn! 🎮\n\n" +
                           "Your AI-powered gaming companion is ready to help you manage your game library, " +
                           "discover new games, and enhance your gaming experience.\n\n" +
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
    private void GetStarted()
    {
        // Navigate to the main game library view
        _mainViewModel.NavigateToGameLibrary();
    }

    /// <summary>
    /// Command to skip the onboarding and go directly to the main application.
    /// </summary>
    [RelayCommand]
    private void Skip()
    {
        // Navigate to the main game library view (same as GetStarted for now)
        _mainViewModel.NavigateToGameLibrary();
    }
}
