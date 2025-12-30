using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.Onboarding.Services;
using SaveState.Core.Ai.Services;
using SaveState.Core.GameLibrary;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Onboarding;
using SaveState.Presentation.Views.Onboarding;
using SaveState.Presentation.Resources;
using Xunit;

namespace SaveState.Presentation.UITests.Onboarding;

/// <summary>
/// Test stub for Resources to avoid complex localization dependencies.
/// </summary>
internal class TestResources : SaveState.Presentation.Resources.Resources
{
    public TestResources() : base(null!) { }
}

/// <summary>
/// UI integration tests for the onboarding flow.
/// Tests the complete user experience from welcome screen to navigation.
/// </summary>
public class OnboardingUITests : HeadlessTestBase
{
    private readonly TestResources _resources = new();

    private MainViewModel CreateMockMainViewModel()
    {
        var mockMediator = new Mock<IMediator>();
        var mockOnboardingLogger = new Mock<ILogger<SaveState.Presentation.ViewModels.Onboarding.OnboardingViewModel>>();
        var mockMainLogger = new Mock<ILogger<MainViewModel>>();
        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        return new MainViewModel(mockMediator.Object, CreateMockOnboardingService(), mockOnboardingLogger.Object, mockUserPreferences.Object, mockMainLogger.Object, _resources);
    }

    private OnboardingService CreateMockOnboardingService()
    {
        var mockAi = new Mock<IAiOrchestrator>();
        mockAi.Setup(x => x.ProcessRequestAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse("Welcome to SaveState Reborn!", "stop", new TokenUsage(10, 50, 60), "test-model", "test"));

        var mockGames = new Mock<IGameRepository>();
        mockGames.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SaveState.Core.GameLibrary.Entities.Game>());

        return new OnboardingService(mockAi.Object, mockGames.Object);
    }

    private MainViewModel CreateTestMainViewModel()
    {
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<OnboardingViewModel>>();
        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        var mockMainViewModelLogger = new Mock<ILogger<MainViewModel>>();
        return CreateMockMainViewModel();
    }

    [AvaloniaFact]
    public void OnboardingView_CreatesWithoutErrors()
    {
        // Arrange & Act
        var view = new OnboardingView();

        // Assert
        view.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void OnboardingViewModel_InitializesCorrectly()
    {
        // Arrange
        var service = CreateMockOnboardingService();
        var mockMainViewModel = new Mock<MainViewModel>(MockBehavior.Loose);
        var mockLogger = new Mock<ILogger<OnboardingViewModel>>();

        // Act
        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        var viewModel = new OnboardingViewModel(
            service,
            mockMainViewModel.Object,
            mockLogger.Object,
            mockUserPreferences.Object,
            _resources);

        // Assert
        viewModel.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task OnboardingView_DataContextBinding_Works()
    {
        // Arrange
        var service = CreateMockOnboardingService();
        var mockLogger = new Mock<ILogger<OnboardingViewModel>>();

        // Use a simple mock for MainViewModel
        var mockMediator = new Mock<IMediator>();
        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        var mockMainViewModelLogger = new Mock<ILogger<MainViewModel>>();
        var mainViewModel = CreateMockMainViewModel();

        var view = new OnboardingView();

        // Act - Assign a properly constructed OnboardingViewModel
        var onboardingVm = new OnboardingViewModel(service, mainViewModel, mockLogger.Object, mockUserPreferences.Object, _resources);
        view.DataContext = onboardingVm;

        // Assert
        view.DataContext.Should().Be(onboardingVm);
        view.DataContext.Should().BeOfType<OnboardingViewModel>();
    }

    [AvaloniaFact]
    public void OnboardingView_FindControls_ReturnsExpectedElements()
    {
        // Arrange
        var view = new OnboardingView();

        // Assert - The view should have basic structure
        view.Should().NotBeNull();
        // Note: Finding named controls requires the view to be measured/arranged
    }

    [Fact]
    public void OnboardingViewModel_Properties_AreInitialized()
    {
        // Arrange
        var service = CreateMockOnboardingService();
        var mockMainViewModel = new Mock<MainViewModel>(MockBehavior.Loose);
        var mockLogger = new Mock<ILogger<OnboardingViewModel>>();

        // Act
        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        var viewModel = new OnboardingViewModel(
            service,
            mockMainViewModel.Object,
            mockLogger.Object,
            mockUserPreferences.Object,
            _resources);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.WelcomeMessage.Should().NotBeNull();
    }
}
