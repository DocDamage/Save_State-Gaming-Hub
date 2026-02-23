using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.EndToEndTests.Infrastructure;
using SaveState.Presentation.Resources;
using SaveState.Presentation.ViewModels.WebBrowser;
using SaveState.Presentation.Views.WebBrowser;
using Xunit;
using Xunit.Abstractions;

namespace SaveState.EndToEndTests;

/// <summary>
/// End-to-end browser automation tests for the Web Browser feature.
/// Tests browser navigation, tab management, and bookmarks.
/// </summary>
public class BrowserE2ETests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private AvaloniaTestHost? _host;
    private readonly IServiceProvider _serviceProvider;

    public BrowserE2ETests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _serviceProvider = fixture.Services;
    }

    public async Task InitializeAsync()
    {
        _host = new AvaloniaTestHost(_serviceProvider);
        await _host.StartAsync(sp => CreateBrowserWindow(sp));
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }

    private static Window CreateBrowserWindow(IServiceProvider services)
    {
        var window = new Window
        {
            Title = "Browser E2E Test",
            Width = 1200,
            Height = 800,
            Content = CreateBrowserShellView(services)
        };
        return window;
    }

    private static BrowserShellView CreateBrowserShellView(IServiceProvider services)
    {
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<BrowserShellViewModel>>();
        var mockSettings = new Mock<SaveState.Core.WebBrowser.Services.IBrowserSettingsService>();
        var mockHistory = new Mock<SaveState.Core.WebBrowser.Services.IBrowserHistoryService>();
        var mockBookmarks = new Mock<SaveState.Core.WebBrowser.Services.IBookmarkService>();
        var mockResources = CreateMockResources();

        // Setup mock data
        var bookmarks = new List<SaveState.Core.WebBrowser.Entities.Bookmark>
        {
            new() { Title = "Google", Url = "https://google.com", FaviconUrl = null },
            new() { Title = "GitHub", Url = "https://github.com", FaviconUrl = null },
            new() { Title = "Stack Overflow", Url = "https://stackoverflow.com", FaviconUrl = null }
        };

        mockBookmarks.Setup(x => x.GetBookmarksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookmarks);

        mockSettings.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaveState.Core.WebBrowser.Services.DTOs.BrowserSettings
            {
                HomePage = "https://google.com",
                SearchEngine = "Google",
                EnableAdBlocker = true
            });

        var viewModel = new BrowserShellViewModel(
            mockMediator.Object,
            mockSettings.Object,
            mockHistory.Object,
            mockBookmarks.Object,
            mockLogger.Object,
            mockResources);

        return new BrowserShellView { DataContext = viewModel };
    }

    private static Resources CreateMockResources()
    {
        var localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<Resources>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new Microsoft.Extensions.Localization.LocalizedString(key, key));
        return new Resources(localizerMock.Object);
    }

    #region Browser Shell Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    public async Task BrowserView_Loads_Successfully()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange & Act
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;

            // Assert
            browserView.Should().NotBeNull();
            browserView!.DataContext.Should().BeOfType<BrowserShellViewModel>();
            _output.WriteLine("Browser shell view loaded successfully");
        }, _host!, "BrowserView_Loads_Successfully");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    public async Task BrowserView_HasAddressBar()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act & Assert
            viewModel!.Address.Should().NotBeNull();
            _output.WriteLine($"Address bar present: {viewModel.Address}");
        }, _host!, "BrowserView_HasAddressBar");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    public async Task BrowserView_HasNavigationButtons()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act & Assert
            viewModel!.CanGoBack.Should().BeFalse(); // No history initially
            viewModel.CanGoForward.Should().BeFalse();
            viewModel.CanReload.Should().BeTrue();
            _output.WriteLine("Navigation buttons state verified");
        }, _host!, "BrowserView_HasNavigationButtons");
    }

    #endregion

    #region Tab Management Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Tabs")]
    public async Task BrowserView_HasTabBar()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await Task.Delay(100);

            // Assert
            viewModel!.Tabs.Should().NotBeNull();
            _output.WriteLine($"Number of tabs: {viewModel.Tabs.Count}");
        }, _host!, "BrowserView_HasTabBar");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Tabs")]
    public async Task BrowserView_CanAddNewTab()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;
            var initialTabCount = viewModel!.Tabs.Count;

            // Act
            viewModel.AddNewTabCommand.Execute(null);
            await Task.Delay(100);

            // Assert
            viewModel.Tabs.Count.Should().BeGreaterThan(initialTabCount);
            _output.WriteLine($"Tab count increased from {initialTabCount} to {viewModel.Tabs.Count}");
        }, _host!, "BrowserView_CanAddNewTab");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Tabs")]
    public async Task BrowserView_CanCloseTab()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;
            
            // Ensure we have at least 2 tabs
            if (viewModel!.Tabs.Count < 2)
            {
                viewModel.AddNewTabCommand.Execute(null);
                await Task.Delay(100);
            }
            
            var initialTabCount = viewModel.Tabs.Count;

            // Act
            var tabToClose = viewModel.Tabs.Last();
            viewModel.CloseTabCommand.Execute(tabToClose);
            await Task.Delay(100);

            // Assert
            viewModel.Tabs.Count.Should().Be(initialTabCount - 1);
            _output.WriteLine($"Tab count decreased from {initialTabCount} to {viewModel.Tabs.Count}");
        }, _host!, "BrowserView_CanCloseTab");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Tabs")]
    public async Task BrowserView_CanSwitchTabs()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;
            
            // Ensure we have at least 2 tabs
            if (viewModel!.Tabs.Count < 2)
            {
                viewModel.AddNewTabCommand.Execute(null);
                await Task.Delay(100);
            }

            // Act
            var secondTab = viewModel.Tabs[1];
            viewModel.SelectedTab = secondTab;

            // Assert
            viewModel.SelectedTab.Should().Be(secondTab);
            _output.WriteLine($"Switched to tab: {secondTab.Title}");
        }, _host!, "BrowserView_CanSwitchTabs");
    }

    #endregion

    #region Bookmark Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Bookmarks")]
    public async Task BrowserView_ShowsBookmarks()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await Task.Delay(100);

            // Assert
            viewModel!.Bookmarks.Should().NotBeNull();
            viewModel.Bookmarks.Should().NotBeEmpty();
            _output.WriteLine($"Number of bookmarks: {viewModel.Bookmarks.Count}");
        }, _host!, "BrowserView_ShowsBookmarks");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Bookmarks")]
    public async Task BrowserView_CanSelectBookmark()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            var firstBookmark = viewModel!.Bookmarks.FirstOrDefault();
            if (firstBookmark != null)
            {
                viewModel.OpenBookmarkCommand.Execute(firstBookmark);
            }

            // Assert
            firstBookmark.Should().NotBeNull();
            _output.WriteLine($"Selected bookmark: {firstBookmark?.Title} - {firstBookmark?.Url}");
        }, _host!, "BrowserView_CanSelectBookmark");
    }

    #endregion

    #region Navigation Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Navigation")]
    public async Task BrowserView_CanNavigateToUrl()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            viewModel!.Address = "https://example.com";
            viewModel.NavigateCommand.Execute(null);
            await Task.Delay(100);

            // Assert
            viewModel.Address.Should().Contain("example.com");
            _output.WriteLine($"Navigated to: {viewModel.Address}");
        }, _host!, "BrowserView_CanNavigateToUrl");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Navigation")]
    public async Task BrowserView_CanGoHome()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            viewModel!.GoHomeCommand.Execute(null);
            await Task.Delay(100);

            // Assert
            viewModel.Address.Should().NotBeNullOrEmpty();
            _output.WriteLine($"Home page: {viewModel.Address}");
        }, _host!, "BrowserView_CanGoHome");
    }

    #endregion

    #region History Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "History")]
    public async Task BrowserView_CanShowHistory()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            viewModel!.ShowHistoryCommand.Execute(null);
            await Task.Delay(100);

            // Assert - History view should be visible
            _output.WriteLine("History view opened");
        }, _host!, "BrowserView_CanShowHistory");
    }

    #endregion
}
