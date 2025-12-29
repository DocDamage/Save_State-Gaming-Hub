using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Onboarding.Services;

namespace SaveState.Presentation.ViewModels;

/// <summary>
/// Main view model that manages navigation between different application views.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly OnboardingService _onboardingService;
    private readonly ILogger<Onboarding.OnboardingViewModel> _onboardingLogger;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    public MainViewModel(
        IMediator mediator,
        OnboardingService onboardingService,
        ILogger<Onboarding.OnboardingViewModel> onboardingLogger)
    {
        _mediator = mediator;
        _onboardingService = onboardingService;
        _onboardingLogger = onboardingLogger;

        // Start with onboarding for first-time users
        // TODO: Check user preferences/settings to determine if onboarding should be shown
        CurrentViewModel = new Onboarding.OnboardingViewModel(
            _onboardingService,
            this,
            _onboardingLogger);
    }

    /// <summary>
    /// Navigates to the main game library view.
    /// </summary>
    public void NavigateToGameLibrary()
    {
        CurrentViewModel = new GameLibraryViewModel(_mediator);
    }

    /// <summary>
    /// Navigates to the onboarding view.
    /// </summary>
    public void NavigateToOnboarding()
    {
        CurrentViewModel = new Onboarding.OnboardingViewModel(
            _onboardingService,
            this,
            _onboardingLogger);
    }
}
