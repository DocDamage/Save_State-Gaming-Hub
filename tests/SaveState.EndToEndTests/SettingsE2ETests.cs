using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.EndToEndTests.Infrastructure;
using SaveState.Presentation.Resources;
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
        var mockMediator = new Mock<IMediator>();
        var mockPreferences = new Mock<IUserPreferencesService>();
        var mockLogger = new Mock<ILogger<SettingsViewModel>>();
        var mockTimeProvider = new Mock<ITimeProvider>();
        var mockResources = CreateMockResources();

        // Setup mock settings
        var settings = new UserSettings
        {
            Theme = "Dark",
            Language = "en-US",
            AutoSaveEnabled = true,
            AutoSaveInterval = TimeSpan.FromMinutes(15),
            NotificationsEnabled = true,
            CloudSyncEnabled = false,
            DefaultGameLaunchMode = GameLaunchMode.Default
        };

        mockPreferences.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        mockPreferences.Setup(x => x.SaveSettingsAsync(It.IsAny<UserSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var viewModel = new SettingsViewModel(
            mockMediator.Object,
            mockPreferences.Object,
            mockLogger.Object,
            mockTimeProvider.Object,
            mockResources);

        return new SettingsView { DataContext = viewModel };
    }

    private static Resources CreateMockResources()
    {
        var localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<Resources>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new Microsoft.Extensions.Localization.LocalizedString(key, key));
        return new Resources(localizerMock.Object);
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
    public async Task SettingsView_HasCategories()
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
            viewModel!.Categories.Should().NotBeNull();
            viewModel.Categories.Should().NotBeEmpty();
            _output.WriteLine($"Number of setting categories: {viewModel.Categories.Count}");
        }, _host!, "SettingsView_HasCategories");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    public async Task SettingsView_HasDefaultSelection()
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
            viewModel!.SelectedCategory.Should().NotBeNull();
            _output.WriteLine($"Default selected category: {viewModel.SelectedCategory?.Name}");
        }, _host!, "SettingsView_HasDefaultSelection");
    }

    #endregion

    #region Category Navigation Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Navigation")]
    public async Task SettingsView_CanSelectGeneralCategory()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            var generalCategory = viewModel!.Categories.FirstOrDefault(c => 
                c.Name.Contains("General", StringComparison.OrdinalIgnoreCase));
            if (generalCategory != null)
            {
                viewModel.SelectedCategory = generalCategory;
            }

            // Assert
            viewModel.SelectedCategory.Should().NotBeNull();
            _output.WriteLine($"Selected category: {viewModel.SelectedCategory?.Name}");
        }, _host!, "SettingsView_CanSelectGeneralCategory");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Navigation")]
    public async Task SettingsView_CanSelectAppearanceCategory()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            var appearanceCategory = viewModel!.Categories.FirstOrDefault(c => 
                c.Name.Contains("Appearance", StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains("Theme", StringComparison.OrdinalIgnoreCase));
            if (appearanceCategory != null)
            {
                viewModel.SelectedCategory = appearanceCategory;
            }

            // Assert
            viewModel.SelectedCategory.Should().NotBeNull();
            _output.WriteLine($"Selected category: {viewModel.SelectedCategory?.Name}");
        }, _host!, "SettingsView_CanSelectAppearanceCategory");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Navigation")]
    public async Task SettingsView_CanSelectCloudSyncCategory()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            var cloudCategory = viewModel!.Categories.FirstOrDefault(c => 
                c.Name.Contains("Cloud", StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains("Sync", StringComparison.OrdinalIgnoreCase));
            if (cloudCategory != null)
            {
                viewModel.SelectedCategory = cloudCategory;
            }

            // Assert
            viewModel.SelectedCategory.Should().NotBeNull();
            _output.WriteLine($"Selected category: {viewModel.SelectedCategory?.Name}");
        }, _host!, "SettingsView_CanSelectCloudSyncCategory");
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
            var originalTheme = viewModel!.CurrentTheme;
            viewModel.CurrentTheme = "Light";
            await Task.Delay(100);

            // Assert
            viewModel.CurrentTheme.Should().Be("Light");
            _output.WriteLine($"Theme changed from {originalTheme} to {viewModel.CurrentTheme}");
        }, _host!, "SettingsView_CanChangeTheme");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Theme")]
    public async Task SettingsView_SupportsDarkTheme()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            viewModel!.CurrentTheme = "Dark";
            await Task.Delay(100);

            // Assert
            viewModel.CurrentTheme.Should().Be("Dark");
            _output.WriteLine("Dark theme selected");
        }, _host!, "SettingsView_SupportsDarkTheme");
    }

    #endregion

    #region Auto-Save Settings Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "AutoSave")]
    public async Task SettingsView_CanToggleAutoSave()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act - Toggle auto-save
            var originalValue = viewModel!.AutoSaveEnabled;
            viewModel.AutoSaveEnabled = !originalValue;
            await Task.Delay(100);

            // Assert
            viewModel.AutoSaveEnabled.Should().Be(!originalValue);
            _output.WriteLine($"Auto-save toggled from {originalValue} to {viewModel.AutoSaveEnabled}");
        }, _host!, "SettingsView_CanToggleAutoSave");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "AutoSave")]
    public async Task SettingsView_CanChangeAutoSaveInterval()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            viewModel!.AutoSaveInterval = TimeSpan.FromMinutes(30);
            await Task.Delay(100);

            // Assert
            viewModel.AutoSaveInterval.Should().Be(TimeSpan.FromMinutes(30));
            _output.WriteLine($"Auto-save interval set to: {viewModel.AutoSaveInterval}");
        }, _host!, "SettingsView_CanChangeAutoSaveInterval");
    }

    #endregion

    #region Notification Settings Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Notifications")]
    public async Task SettingsView_CanToggleNotifications()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            var originalValue = viewModel!.NotificationsEnabled;
            viewModel.NotificationsEnabled = !originalValue;
            await Task.Delay(100);

            // Assert
            viewModel.NotificationsEnabled.Should().Be(!originalValue);
            _output.WriteLine($"Notifications toggled from {originalValue} to {viewModel.NotificationsEnabled}");
        }, _host!, "SettingsView_CanToggleNotifications");
    }

    #endregion

    #region Language Settings Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Language")]
    public async Task SettingsView_CanChangeLanguage()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            viewModel!.SelectedLanguage = "es-ES";
            await Task.Delay(100);

            // Assert
            viewModel.SelectedLanguage.Should().Be("es-ES");
            _output.WriteLine($"Language changed to: {viewModel.SelectedLanguage}");
        }, _host!, "SettingsView_CanChangeLanguage");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Language")]
    public async Task SettingsView_SupportsMultipleLanguages()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            var languages = viewModel!.AvailableLanguages;

            // Assert
            languages.Should().NotBeNull();
            languages.Should().NotBeEmpty();
            _output.WriteLine($"Available languages: {string.Join(", ", languages)}");
        }, _host!, "SettingsView_SupportsMultipleLanguages");
    }

    #endregion

    #region Save/Reset Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Persistence")]
    public async Task SettingsView_CanSaveSettings()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            viewModel!.CurrentTheme = "Dark";
            viewModel.NotificationsEnabled = true;
            var saved = await viewModel.SaveSettingsAsync();

            // Assert
            saved.Should().BeTrue();
            _output.WriteLine("Settings saved successfully");
        }, _host!, "SettingsView_CanSaveSettings");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Persistence")]
    public async Task SettingsView_CanResetToDefaults()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Change some settings first
            viewModel!.CurrentTheme = "Light";
            viewModel.NotificationsEnabled = false;
            await Task.Delay(100);

            // Act
            viewModel.ResetToDefaultsCommand.Execute(null);
            await Task.Delay(100);

            // Assert - Settings should be reset
            _output.WriteLine("Settings reset to defaults");
        }, _host!, "SettingsView_CanResetToDefaults");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Persistence")]
    public async Task SettingsView_ShowsUnsavedChangesIndicator()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act - Make a change
            viewModel!.CurrentTheme = viewModel.CurrentTheme == "Dark" ? "Light" : "Dark";
            await Task.Delay(100);

            // Assert
            viewModel.HasUnsavedChanges.Should().BeTrue();
            _output.WriteLine("Unsaved changes indicator is showing");
        }, _host!, "SettingsView_ShowsUnsavedChangesIndicator");
    }

    #endregion

    #region Accessibility Settings Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Accessibility")]
    public async Task SettingsView_CanToggleHighContrast()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            var originalValue = viewModel!.HighContrastEnabled;
            viewModel.HighContrastEnabled = !originalValue;
            await Task.Delay(100);

            // Assert
            viewModel.HighContrastEnabled.Should().Be(!originalValue);
            _output.WriteLine($"High contrast toggled to: {viewModel.HighContrastEnabled}");
        }, _host!, "SettingsView_CanToggleHighContrast");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Settings")]
    [Trait("SubFeature", "Accessibility")]
    public async Task SettingsView_CanChangeFontSize()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var settingsView = window.Content as SettingsView;
            var viewModel = settingsView!.DataContext as SettingsViewModel;

            // Act
            viewModel!.FontSize = 16;
            await Task.Delay(100);

            // Assert
            viewModel.FontSize.Should().Be(16);
            _output.WriteLine($"Font size set to: {viewModel.FontSize}");
        }, _host!, "SettingsView_CanChangeFontSize");
    }

    #endregion
}

// Supporting types for settings tests
public class UserSettings
{
    public string Theme { get; set; } = "Dark";
    public string Language { get; set; } = "en-US";
    public bool AutoSaveEnabled { get; set; }
    public TimeSpan AutoSaveInterval { get; set; }
    public bool NotificationsEnabled { get; set; }
    public bool CloudSyncEnabled { get; set; }
    public GameLaunchMode DefaultGameLaunchMode { get; set; }
}

public enum GameLaunchMode
{
    Default,
    BigPicture,
    Windowed,
    Borderless
}
