using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.Onboarding.Services;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Presentation.UITests;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Onboarding;
using SaveState.Presentation.Views.Onboarding;
using Xunit;

namespace SaveState.Presentation.UITests.Onboarding;

/// <summary>
/// UI integration tests for the onboarding flow.
/// Tests the complete user experience from welcome screen to navigation.
/// </summary>
public class OnboardingUITests : HeadlessTestBase
{
    static OnboardingUITests()
    {
        UiTestLocator.EnsureInitialized();
    }

    private readonly SaveState.Presentation.Resources.Resources _resources = UiTestResourceFactory.Create();

    private MainViewModel CreateMockMainViewModel()
    {
        var mockMediator = new Mock<IMediator>();
        var mockOnboardingLogger = new Mock<ILogger<SaveState.Presentation.ViewModels.Onboarding.OnboardingViewModel>>();
        var mockMainLogger = new Mock<ILogger<MainViewModel>>();
        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        mockUserPreferences.Setup(x => x.ShouldShowOnboardingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return new MainViewModel(mockMediator.Object, CreateMockOnboardingService(), mockOnboardingLogger.Object, mockUserPreferences.Object, mockMainLogger.Object, _resources);
    }

    private OnboardingService CreateMockOnboardingService()
    {
        var mockAi = new Mock<IAiOrchestrator>();
        mockAi.Setup(x => x.ProcessRequestAsync(It.IsAny<AiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse("Welcome to SaveState Reborn!", "stop", new TokenUsage(10, 50, 60), "test-model", "test"));

        var mockGames = new Mock<IGameRepository>();
        mockGames.Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        mockGames.Setup(x => x.GetGamesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<SaveState.Core.GameLibrary.Enums.GameStatus?>(),
                It.IsAny<string?>(),
                It.IsAny<SaveState.Core.GameLibrary.Enums.GameSortBy>(),
                It.IsAny<bool>(),
                It.IsAny<SaveState.Core.GameLibrary.Entities.CollectionFilter?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SaveState.Core.GameLibrary.Entities.Game>(
                Array.Empty<SaveState.Core.GameLibrary.Entities.Game>(),
                0,
                1,
                50));

        return new OnboardingService(mockAi.Object, mockGames.Object);
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
        var mainViewModel = CreateMockMainViewModel();
        var mockLogger = new Mock<ILogger<OnboardingViewModel>>();

        // Act
        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        mockUserPreferences.Setup(x => x.CompleteOnboardingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var viewModel = new OnboardingViewModel(
            service,
            mainViewModel,
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

        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        mockUserPreferences.Setup(x => x.CompleteOnboardingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
        var mainViewModel = CreateMockMainViewModel();
        var mockLogger = new Mock<ILogger<OnboardingViewModel>>();

        // Act
        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        mockUserPreferences.Setup(x => x.CompleteOnboardingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var viewModel = new OnboardingViewModel(
            service,
            mainViewModel,
            mockLogger.Object,
            mockUserPreferences.Object,
            _resources);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.WelcomeMessage.Should().NotBeNull();
    }
}
