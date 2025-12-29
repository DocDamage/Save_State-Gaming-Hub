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
using SaveState.Presentation.Views;
using Xunit;

namespace SaveState.Presentation.UITests;

/// <summary>
/// UI integration tests for the main window and navigation.
/// Tests window initialization, view switching, and user interaction flows.
/// </summary>
public class MainWindowTests : HeadlessTestBase
{
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
        window.Title.Should().Be("SaveState Reborn");
    }

    [AvaloniaFact]
    public void MainWindow_Dimensions_ArePositive()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert
        window.Width.Should().BeGreaterThan(0);
        window.Height.Should().BeGreaterThan(0);
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

        var mainViewModel = new MainViewModel(mockMediator.Object, service, mockLogger.Object);
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

        var mainViewModel = new MainViewModel(mockMediator.Object, service, mockLogger.Object);
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
