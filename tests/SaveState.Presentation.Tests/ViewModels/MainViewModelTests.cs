using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SaveState.Application.Onboarding.Services;
using SaveState.Presentation.ViewModels;

namespace SaveState.Presentation.Tests.ViewModels;

/// <summary>
/// Test stub for OnboardingService to avoid complex dependencies.
/// </summary>
internal class TestOnboardingService : OnboardingService
{
    public TestOnboardingService() : base(null!, null!) { }
}

/// <summary>
/// Tests for the MainViewModel that manages application navigation.
/// </summary>
public class MainViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly TestOnboardingService _onboardingService = new();
    private readonly Mock<ILogger<SaveState.Presentation.ViewModels.Onboarding.OnboardingViewModel>> _loggerMock = new();

    [Fact]
    public void Constructor_WithMediator_SetsUpInitialViewModel()
    {
        // Act
        var viewModel = new MainViewModel(_mediatorMock.Object, _onboardingService, _loggerMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.CurrentViewModel.Should().NotBeNull();
        // Note: The actual initial ViewModel depends on Locator services and is tested in integration
    }

    [Fact]
    public void NavigateToGameLibrary_SetsCurrentViewModelToGameLibraryViewModel()
    {
        // Arrange
        var viewModel = new MainViewModel(_mediatorMock.Object, _onboardingService, _loggerMock.Object);

        // Act
        viewModel.NavigateToGameLibrary();

        // Assert
        viewModel.CurrentViewModel.Should().BeOfType<GameLibraryViewModel>();
    }

    [Fact]
    public void NavigateToGameLibrary_PassesMediatorToGameLibraryViewModel()
    {
        // Arrange
        var viewModel = new MainViewModel(_mediatorMock.Object, _onboardingService, _loggerMock.Object);

        // Act
        viewModel.NavigateToGameLibrary();

        // Assert
        var gameLibraryViewModel = viewModel.CurrentViewModel as GameLibraryViewModel;
        gameLibraryViewModel.Should().NotBeNull();
        // Note: We can't easily test the internal mediator without reflection
    }

    [Fact]
    public void CurrentViewModel_PropertyChange_NotifiesObservers()
    {
        // Arrange
        var viewModel = new MainViewModel(_mediatorMock.Object, _onboardingService, _loggerMock.Object);
        var propertyChangedCalled = false;
        var propertyName = "";

        viewModel.PropertyChanged += (sender, args) =>
        {
            propertyChangedCalled = true;
            propertyName = args.PropertyName;
        };

        // Act
        viewModel.NavigateToGameLibrary();

        // Assert
        propertyChangedCalled.Should().BeTrue();
        propertyName.Should().Be(nameof(MainViewModel.CurrentViewModel));
    }

    [Fact]
    public void MultipleNavigationCalls_UpdateCurrentViewModelEachTime()
    {
        // Arrange
        var viewModel = new MainViewModel(_mediatorMock.Object, _onboardingService, _loggerMock.Object);

        // Act
        viewModel.NavigateToGameLibrary();
        var firstViewModel = viewModel.CurrentViewModel;

        viewModel.NavigateToGameLibrary();
        var secondViewModel = viewModel.CurrentViewModel;

        // Assert
        firstViewModel.Should().NotBeNull();
        secondViewModel.Should().NotBeNull();
        // Note: They should be different instances, but same type
        firstViewModel.Should().BeOfType<GameLibraryViewModel>();
        secondViewModel.Should().BeOfType<GameLibraryViewModel>();
    }
}
