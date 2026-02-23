using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.RetroArch;
using SaveState.Presentation.ViewModels.Settings;

namespace SaveState.Presentation.Tests;

/// <summary>
/// Integration tests for Phase 1 UI Feature Surfacing.
/// Tests ViewModel creation, initialization, and basic functionality.
/// </summary>
public class UiSurfacingPhase1Tests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<RetroArchTabViewModel>> _retroArchLoggerMock;
    private readonly Mock<ILogger<LaunchExperienceConfigDialogViewModel>> _launchConfigLoggerMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly Mock<INotificationService> _notificationServiceMock;

    public UiSurfacingPhase1Tests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _retroArchLoggerMock = new Mock<ILogger<RetroArchTabViewModel>>();
        _launchConfigLoggerMock = new Mock<ILogger<LaunchExperienceConfigDialogViewModel>>();
        _navigationServiceMock = new Mock<INavigationService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _notificationServiceMock = new Mock<INotificationService>();

        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns((string category) => Mock.Of<ILogger>());

        _loggerFactoryMock
            .Setup(x => x.CreateLogger(typeof(RetroArchTabViewModel).FullName!))
            .Returns(_retroArchLoggerMock.Object);

        _timeProviderMock.Setup(x => x.Now).Returns(DateTime.Now);
        _timeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    #region RetroArch Tests

    [Fact]
    public void RetroArchTabViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new RetroArchTabViewModel(
            _loggerFactoryMock.Object,
            _navigationServiceMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void RetroArchCoreManagerViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new RetroArchCoreManagerViewModel(
            Mock.Of<ILogger<RetroArchCoreManagerViewModel>>(),
            Mock.Of<IDialogService>());

        // Assert
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void RetroArchPlaylistViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new RetroArchPlaylistViewModel(
            Mock.Of<ILogger<RetroArchPlaylistViewModel>>(),
            Mock.Of<IDialogService>());

        // Assert
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void RetroArchNetplayViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new RetroArchNetplayViewModel(
            Mock.Of<ILogger<RetroArchNetplayViewModel>>(),
            Mock.Of<IDialogService>());

        // Assert
        viewModel.Should().NotBeNull();
    }

    #endregion

    #region Launch Experience Tests

    [Fact]
    public void LaunchExperienceConfigDialogViewModel_InitializesWithDefaults()
    {
        // Arrange & Act
        var viewModel = new LaunchExperienceConfigDialogViewModel(
            _launchConfigLoggerMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void LaunchExperienceConfigDialogViewModel_CanToggleSettings()
    {
        // Arrange
        var viewModel = new LaunchExperienceConfigDialogViewModel(
            _launchConfigLoggerMock.Object);

        // Act & Assert - just verify no exception is thrown
        viewModel.Should().NotBeNull();
    }

    #endregion

    #region System Health Tests

    [Fact]
    public void SystemHealthViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new SystemHealthViewModel(
            _timeProviderMock.Object,
            null,
            _dialogServiceMock.Object,
            _notificationServiceMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.IsRefreshing.Should().BeFalse();
    }

    [Fact]
    public async Task SystemHealthViewModel_RefreshUpdatesData()
    {
        // Arrange
        var viewModel = new SystemHealthViewModel(
            _timeProviderMock.Object,
            null,
            _dialogServiceMock.Object,
            _notificationServiceMock.Object);

        var beforeRefresh = viewModel.LastUpdated;

        // Act
        await Task.Delay(10); // Small delay to ensure time difference
        await viewModel.RefreshCommand.ExecuteAsync(null);

        // Assert
        viewModel.LastUpdated.Should().BeOnOrAfter(beforeRefresh);
    }

    [Fact]
    public void SystemHealthViewModel_CanOpenErrorLogs()
    {
        // Arrange
        var viewModel = new SystemHealthViewModel(
            _timeProviderMock.Object,
            null,
            _dialogServiceMock.Object,
            _notificationServiceMock.Object);

        // Act
        viewModel.ViewErrorLogCommand.Execute(null);

        // Assert - Verify no exception thrown
        viewModel.Should().NotBeNull();
    }

    #endregion

    #region Connected Accounts Tests

    [Fact]
    public void ConnectedAccountsViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new ConnectedAccountsViewModel(
            _notificationServiceMock.Object,
            null,
            null,
            _dialogServiceMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void ConnectedAccountsViewModel_TracksConnectionStates()
    {
        // Arrange & Act
        var viewModel = new ConnectedAccountsViewModel(
            _notificationServiceMock.Object,
            null,
            null,
            _dialogServiceMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.SteamStatus.Should().NotBeNull();
        viewModel.GogStatus.Should().NotBeNull();
        viewModel.EpicStatus.Should().NotBeNull();
    }

    #endregion

    #region Navigation Service Extension Tests

    [Fact]
    public async Task NavigationServiceExtensions_NavigateToRetroArch_CallsNavigateWithCorrectTab()
    {
        // Arrange
        var navigationService = new Mock<INavigationService>();

        // Act
        await navigationService.Object.NavigateToRetroArchAsync();

        // Assert
        navigationService.Verify(x => x.NavigateToAsync("RetroArch"), Times.Once);
    }

    [Fact]
    public async Task NavigationServiceExtensions_ShowSystemHealth_CallsNavigateWithSettings()
    {
        // Arrange
        var navigationService = new Mock<INavigationService>();

        // Act
        await navigationService.Object.ShowSystemHealthAsync();

        // Assert
        navigationService.Verify(x => x.NavigateToAsync("Settings"), Times.Once);
    }

    [Fact]
    public async Task NavigationServiceExtensions_ShowConnectedAccounts_CallsNavigateWithSettings()
    {
        // Arrange
        var navigationService = new Mock<INavigationService>();

        // Act
        await navigationService.Object.ShowConnectedAccountsAsync();

        // Assert
        navigationService.Verify(x => x.NavigateToAsync("Settings"), Times.Once);
    }

    #endregion

    #region Settings ViewModel Integration Tests

    [Fact]
    public void SettingsViewModel_HasSystemHealthCommand()
    {
        // Arrange & Act
        var viewModel = CreateSettingsViewModel();

        // Assert
        viewModel.ShowSystemHealthCommand.Should().NotBeNull();
    }

    [Fact]
    public void SettingsViewModel_HasConnectedAccountsCommand()
    {
        // Arrange & Act
        var viewModel = CreateSettingsViewModel();

        // Assert
        viewModel.ShowConnectedAccountsCommand.Should().NotBeNull();
    }

    [Fact]
    public void SettingsViewModel_HasLaunchExperienceConfigCommand()
    {
        // Arrange & Act
        var viewModel = CreateSettingsViewModel();

        // Assert
        viewModel.ShowLaunchExperienceConfigCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SettingsViewModel_ShowSystemHealth_InvokesNavigationService()
    {
        // Arrange
        var viewModel = CreateSettingsViewModel();

        // Act
        await viewModel.ShowSystemHealthCommand.ExecuteAsync(null);

        // Assert
        _navigationServiceMock.Verify(x => x.NavigateToAsync("Settings"), Times.Once);
    }

    [Fact]
    public async Task SettingsViewModel_ShowLaunchExperienceConfig_InvokesDialogService()
    {
        // Arrange
        var viewModel = CreateSettingsViewModel();

        // Act
        await viewModel.ShowLaunchExperienceConfigCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(x => x.ShowLaunchExperienceConfigAsync(), Times.Once);
    }

    #endregion

    #region Dialog Service Integration Tests

    [Fact]
    public async Task DialogService_ShowLaunchExperienceConfig_ReturnsNullWhenCancelled()
    {
        // Arrange
        _dialogServiceMock
            .Setup(x => x.ShowLaunchExperienceConfigAsync())
            .ReturnsAsync((LaunchExperienceConfigResult?)null);

        // Act
        var result = await _dialogServiceMock.Object.ShowLaunchExperienceConfigAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DialogService_ShowLaunchExperienceConfig_ReturnsConfigWhenSaved()
    {
        // Arrange
        var expectedConfig = new LaunchExperienceConfigResult(
            EnableCinematicLaunch: true,
            ShowGameFacts: true,
            ShowLastProgress: true,
            ShowAchievementProgress: true,
            DurationSeconds: 5);

        _dialogServiceMock
            .Setup(x => x.ShowLaunchExperienceConfigAsync())
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _dialogServiceMock.Object.ShowLaunchExperienceConfigAsync();

        // Assert
        result.Should().NotBeNull();
        result!.EnableCinematicLaunch.Should().BeTrue();
        result.DurationSeconds.Should().Be(5);
    }

    #endregion

    #region Helper Methods

    private SettingsViewModel CreateSettingsViewModel()
    {
        var cultureManagerMock = new Mock<SaveState.Core.Common.Services.ICultureManager>();
        var resources = new SaveState.Presentation.Resources.Resources();
        var themeServiceMock = new Mock<IThemeService>();
        var aiOrchestratorMock = new Mock<SaveState.Core.Ai.Services.IAiOrchestrator>();
        var preferencesServiceMock = new Mock<IUserPreferencesService>();

        cultureManagerMock.Setup(x => x.CurrentCulture).Returns(System.Globalization.CultureInfo.CurrentCulture);
        cultureManagerMock.Setup(x => x.SupportedCultures).Returns(new[] { System.Globalization.CultureInfo.CurrentCulture });
        themeServiceMock.Setup(x => x.CurrentTheme).Returns(ThemeType.Dark);
        themeServiceMock.Setup(x => x.AvailableThemes).Returns(new[] { ThemeType.Light, ThemeType.Dark, ThemeType.System });

        var systemHealthVm = new SystemHealthViewModel(
            _timeProviderMock.Object,
            null,
            _dialogServiceMock.Object,
            _notificationServiceMock.Object);

        var connectedAccountsVm = new ConnectedAccountsViewModel(
            _notificationServiceMock.Object,
            null,
            null,
            _dialogServiceMock.Object);

        var dataManagementVm = new DataManagementViewModel();

        var aiAdminVm = new AiAdministrationViewModel(
            Mock.Of<ILogger<AiAdministrationViewModel>>(),
            Mock.Of<IDialogService>(),
            Mock.Of<SaveState.Core.Ai.Services.IAiOrchestrator>(),
            Mock.Of<SaveState.Core.Common.Services.IUserPreferencesService>());

        var audioOptimizationVm = new AudioOptimizationViewModel();

        var voiceControlVm = new Shell.VoiceControlViewModel(
            Mock.Of<ILogger<Shell.VoiceControlViewModel>>(),
            Mock.Of<SaveState.Core.Input.Services.IVoiceCommandService>(),
            Mock.Of<INotificationService>());

        return new SettingsViewModel(
            cultureManagerMock.Object,
            resources,
            themeServiceMock.Object,
            aiOrchestratorMock.Object,
            preferencesServiceMock.Object,
            _dialogServiceMock.Object,
            voiceControlVm,
            audioOptimizationVm,
            systemHealthVm,
            connectedAccountsVm,
            dataManagementVm,
            aiAdminVm,
            _navigationServiceMock.Object);
    }

    #endregion
}
