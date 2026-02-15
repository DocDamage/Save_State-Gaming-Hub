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
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Onboarding;
using SaveState.Presentation.Views;
using Xunit;

namespace SaveState.Presentation.UITests;

/// <summary>
/// UI integration tests for the main window and navigation.
/// Tests window initialization, view switching, and user interaction flows.
/// </summary>
public class MainWindowTests : HeadlessTestBase
{
    static MainWindowTests()
    {
        UiTestLocator.EnsureInitialized();
    }

    private readonly SaveState.Presentation.Resources.Resources _resources = UiTestResourceFactory.Create();

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
    public void MainWindow_Initializes_WithoutErrors()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert
        window.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void MainWindow_Title_IsCorrect()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert
        window.Title.Should().NotBeNullOrWhiteSpace();
    }

    [AvaloniaFact]
    public void MainWindow_Dimensions_ArePositive()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert
        window.MinWidth.Should().BeGreaterThan(0);
        window.MinHeight.Should().BeGreaterThan(0);
    }

    [AvaloniaFact]
    public void MainWindow_CanResize_IsTrue()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert
        window.CanResize.Should().BeTrue();
    }

    [AvaloniaFact]
    public void MainViewModel_Navigation_ChangesCurrentViewModel()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<OnboardingViewModel>>();
        var service = CreateMockOnboardingService();
        var mockMainViewModelLogger = new Mock<ILogger<MainViewModel>>();

        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        mockUserPreferences.Setup(x => x.ShouldShowOnboardingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var mainViewModel = new MainViewModel(mockMediator.Object, service, mockLogger.Object, mockUserPreferences.Object, mockMainViewModelLogger.Object, _resources);
        var initialViewModel = mainViewModel.CurrentViewModel;

        // Act
        mainViewModel.NavigateToGameLibrary();

        // Assert
        mainViewModel.CurrentViewModel.Should().NotBe(initialViewModel);
        mainViewModel.CurrentViewModel.Should().BeOfType<GameLibraryViewModel>();
    }

    [AvaloniaFact]
    public void MainViewModel_NavigateToOnboarding_SetsOnboardingViewModel()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<OnboardingViewModel>>();
        var service = CreateMockOnboardingService();
        var mockMainViewModelLogger = new Mock<ILogger<MainViewModel>>();

        var mockUserPreferences = new Mock<SaveState.Core.Common.Services.IUserPreferencesService>();
        mockUserPreferences.Setup(x => x.ShouldShowOnboardingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var mainViewModel = new MainViewModel(mockMediator.Object, service, mockLogger.Object, mockUserPreferences.Object, mockMainViewModelLogger.Object, _resources);
        mainViewModel.NavigateToGameLibrary(); // Change away from onboarding first

        // Act
        mainViewModel.NavigateToOnboarding();

        // Assert
        mainViewModel.CurrentViewModel.Should().BeOfType<OnboardingViewModel>();
    }

    [AvaloniaFact]
    public void MainWindow_Closing_FiresEvent()
    {
        // Arrange
        var window = new MainWindow();
        var closingFired = false;

        window.Closing += (sender, args) =>
        {
            closingFired = true;
        };

        // Act
        window.Close();

        // Assert
        closingFired.Should().BeTrue();
    }
}
