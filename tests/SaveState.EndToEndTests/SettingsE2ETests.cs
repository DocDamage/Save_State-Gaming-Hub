using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.EndToEndTests.Infrastructure;
using SaveState.Presentation.Resources;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Settings;
using SaveState.Presentation.Views;
using Xunit;
using Xunit.Abstractions;

namespace SaveState.EndToEndTests;

/// <summary>
/// End-to-end browser automation tests for Settings feature.
/// Tests settings navigation and changes.
/// </summary>
public class SettingsE2ETests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private AvaloniaTestHost? _host;
    private readonly IServiceProvider _serviceProvider;

    public SettingsE2ETests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _serviceProvider = fixture.Services;
    }

    public async Task InitializeAsync()
    {
        _host = new AvaloniaTestHost(_serviceProvider);
        await _host.StartAsync(sp => CreateSettingsWindow(sp));
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }

    private static Window CreateSettingsWindow(IServiceProvider services)
    {
        var window = new Window
        {
            Title = "Settings E2E Test",
            Width = 1000,
            Height = 700,
            Content = CreateSettingsView(services)
        };
        return window;
    }

    private static SettingsView CreateSettingsView(IServiceProvider services)
    {
        var mockCultureManager = new Mock<ICultureManager>();
        var localizerMock = new Mock<IStringLocalizer<Resources>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var mockResources = new Resources(localizerMock.Object);
        var mockThemeService = new Mock<IThemeService>();
        var mockAiOrchestrator = new Mock<Core.Ai.Services.IAiOrchestrator>();
        var mockPreferences = new Mock<IUserPreferencesService>();
        var mockTimeProvider = new Mock<ITimeProvider>();
        var mockDialogService = new Mock<IDialogService>();
        var mockNavigationService = new Mock<INavigationService>();
        var mockNotificationService = new Mock<INotificationService>();

        mockCultureManager.Setup(x => x.CurrentCulture).Returns(System.Globalization.CultureInfo.CurrentCulture);
        mockCultureManager.Setup(x => x.SupportedCultures).Returns(new[] { System.Globalization.CultureInfo.CurrentCulture });
        mockThemeService.Setup(x => x.CurrentTheme).Returns(SaveState.Presentation.Services.ThemeType.Dark);
        mockThemeService.Setup(x => x.AvailableThemes).Returns(new[] { SaveState.Presentation.Services.ThemeType.Light, SaveState.Presentation.Services.ThemeType.Dark, SaveState.Presentation.Services.ThemeType.System });

        // Create required sub-viewmodels
        var systemHealthVm = new SystemHealthViewModel(
            mockTimeProvider.Object,
            null,
            mockDialogService.Object,
            mockNotificationService.Object);

        var connectedAccountsVm = new ConnectedAccountsViewModel(
            mockNotificationService.Object,
            null,
            null,
            mockDialogService.Object);

        var dataManagementVm = new DataManagementViewModel(
            mockNotificationService.Object,
            mockDialogService.Object,
            mockTimeProvider.Object,
            null);

        // Use default constructors for sub-viewmodels that support them
        var aiAdminVm = new AiAdministrationViewModel();
        var userManagementVm = new UserManagementViewModel();
        var apiKeyManagerVm = new ApiKeyManagerViewModel();
        var roleManagementVm = new RoleManagementViewModel();

        // AudioOptimizationViewModel and VoiceControlViewModel require complex dependencies - pass null
        var viewModel = new SettingsViewModel(
            mockCultureManager.Object,
            mockResources,
            mockThemeService.Object,
            mockAiOrchestrator.Object,
            mockPreferences.Object,
            mockDialogService.Object,
            null!, // voiceSettings - requires complex dependencies
            null!, // audioOptimizationViewModel - requires complex dependencies
            systemHealthVm,
            connectedAccountsVm,
            dataManagementVm,
            aiAdminVm,
            userManagementVm,
            apiKeyManagerVm,
            roleManagementVm,
            mockNavigationService.Object);

        return new SettingsView { DataContext = viewModel };
    }

    #region Settings View Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    public async Task SettingsView_Loads_Successfully()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange & Act
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;

            // Assert
            settingsView.Should().NotBeNull();
            settingsView!.DataContext.Should().BeOfType<SettingsViewModel>();
            _output.WriteLine("Settings view loaded successfully");
        }, _host!, "SettingsView_Loads_Successfully");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    public async Task SettingsView_HasSubViewModels()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            await Task.Delay(100);

            // Assert
            viewModel!.Should().NotBeNull();
            viewModel.SystemHealthViewModel.Should().NotBeNull();
            viewModel.ConnectedAccountsViewModel.Should().NotBeNull();
            viewModel.DataManagementViewModel.Should().NotBeNull();
            _output.WriteLine("Settings sub-viewmodels loaded successfully");
        }, _host!, "SettingsView_HasSubViewModels");
    }

    #endregion

    #region Theme Settings Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Theme")]
    public async Task SettingsView_CanChangeTheme()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            viewModel!.SelectedTheme = SaveState.Presentation.Services.ThemeType.Light;
            await Task.Delay(100);

            // Assert
            viewModel.SelectedTheme.Should().Be(SaveState.Presentation.Services.ThemeType.Light);
            _output.WriteLine("Theme changed to Light");
        }, _host!, "SettingsView_CanChangeTheme");
    }

    #endregion
}
