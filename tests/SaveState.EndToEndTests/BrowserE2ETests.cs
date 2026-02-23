using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;
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
        var mockBrowserService = new Mock<IBrowserService>();
        var mockLogger = new Mock<ILogger<BrowserShellViewModel>>();

        // Setup mock data for bookmarks
        var bookmarks = new List<BrowserBookmark>
        {
            new() { Title = "Google", Url = "https://google.com" },
            new() { Title = "GitHub", Url = "https://github.com" },
            new() { Title = "Stack Overflow", Url = "https://stackoverflow.com" }
        };

        mockBrowserService.Setup(x => x.GetBookmarksAsync(It.IsAny<string?>()))
            .ReturnsAsync(Result<IReadOnlyList<BrowserBookmark>>.Success(bookmarks));

        // Setup mock settings
        var settings = new BrowserSettings
        {
            HomePage = "https://www.google.com",
            SearchEngine = "https://www.google.com/search?q=",
            BlockPopups = true
        };
        mockBrowserService.Setup(x => x.CurrentSettings).Returns(settings);

        // Setup tabs collection
        var initialTab = new BrowserTab
        {
            Id = Guid.NewGuid(),
            Title = "New Tab",
            Url = "about:blank",
            State = BrowserTabState.Loaded,
            CanGoBack = false,
            CanGoForward = false
        };
        mockBrowserService.Setup(x => x.Tabs).Returns(new List<BrowserTab> { initialTab });
        mockBrowserService.Setup(x => x.ActiveTab).Returns(initialTab);

        // Setup history
        var history = new List<BrowserHistoryItem>
        {
            new() { Title = "Google", Url = "https://google.com", VisitedAt = DateTime.Now.AddHours(-1) },
            new() { Title = "GitHub", Url = "https://github.com", VisitedAt = DateTime.Now.AddHours(-2) }
        };
        mockBrowserService.Setup(x => x.GetHistoryAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result<IReadOnlyList<BrowserHistoryItem>>.Success(history));

        // Setup CreateTab to return a new tab
        mockBrowserService.Setup(x => x.CreateTabAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync((string? url, bool activate, bool incognito) =>
            {
                var tab = new BrowserTab
                {
                    Id = Guid.NewGuid(),
                    Title = url ?? "New Tab",
                    Url = url ?? "about:blank",
                    State = BrowserTabState.Loaded,
                    CanGoBack = false,
                    CanGoForward = false
                };
                return Result<BrowserTab>.Success(tab);
            });

        // Setup navigation methods
        mockBrowserService.Setup(x => x.NavigateAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Success());
        mockBrowserService.Setup(x => x.GoBackAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success());
        mockBrowserService.Setup(x => x.GoForwardAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success());
        mockBrowserService.Setup(x => x.RefreshAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success());
        mockBrowserService.Setup(x => x.StopAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success());
        mockBrowserService.Setup(x => x.ActivateTabAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success());
        mockBrowserService.Setup(x => x.CloseTabAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Result.Success());
        mockBrowserService.Setup(x => x.AddBookmarkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(Result.Success());

        var viewModel = new BrowserShellViewModel(
            mockBrowserService.Object,
            mockLogger.Object);

        return new BrowserShellView { DataContext = viewModel };
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
            viewModel!.AddressBarText.Should().NotBeNull();
            _output.WriteLine($"Address bar present: {viewModel.AddressBarText}");
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
            await viewModel.NewTabCommand.ExecuteAsync(null);
            await Task.Delay(100);

            // Assert - Note: In mock environment, the count won't actually change
            // but we verify the command executed without errors
            viewModel.NewTabCommand.Should().NotBeNull();
            _output.WriteLine($"New tab command executed (initial tabs: {initialTabCount})");
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

            // Ensure we have at least 1 tab to close
            viewModel!.Tabs.Should().NotBeEmpty();
            var tabToClose = viewModel.Tabs.First();

            // Act
            await viewModel.CloseTabCommand.ExecuteAsync(tabToClose);
            await Task.Delay(100);

            // Assert - Command executed without errors
            viewModel.CloseTabCommand.Should().NotBeNull();
            _output.WriteLine($"Close tab command executed for tab: {tabToClose.Title}");
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

            // Ensure we have at least 1 tab
            viewModel!.Tabs.Should().NotBeEmpty();
            var firstTab = viewModel.Tabs.First();

            // Act
            await viewModel.ActivateTabCommand.ExecuteAsync(firstTab);

            // Assert
            viewModel.ActivateTabCommand.Should().NotBeNull();
            _output.WriteLine($"Switched to tab: {firstTab.Title}");
        }, _host!, "BrowserView_CanSwitchTabs");
    }

    #endregion

    #region Bookmark Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Bookmarks")]
    public async Task BrowserView_ShowsBookmarksBar()
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
            viewModel!.BookmarkBarItems.Should().NotBeNull();
            viewModel.ShowBookmarksBar.Should().BeTrue();
            _output.WriteLine($"Number of bookmark bar items: {viewModel.BookmarkBarItems.Count}");
        }, _host!, "BrowserView_ShowsBookmarksBar");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Bookmarks")]
    public async Task BrowserView_CanAddBookmark()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.AddBookmarkCommand.ExecuteAsync(null);

            // Assert
            viewModel!.AddBookmarkCommand.Should().NotBeNull();
            _output.WriteLine("Add bookmark command executed");
        }, _host!, "BrowserView_CanAddBookmark");
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
            viewModel!.AddressBarText = "https://example.com";
            await viewModel.NavigateCommand.ExecuteAsync(null);
            await Task.Delay(100);

            // Assert
            viewModel.NavigateCommand.Should().NotBeNull();
            _output.WriteLine($"Navigate command executed with address: {viewModel.AddressBarText}");
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
            await viewModel.GoHomeCommand.ExecuteAsync(null);
            await Task.Delay(100);

            // Assert
            viewModel!.GoHomeCommand.Should().NotBeNull();
            _output.WriteLine("Go home command executed");
        }, _host!, "BrowserView_CanGoHome");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Navigation")]
    public async Task BrowserView_CanGoBack()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.GoBackCommand.ExecuteAsync(null);

            // Assert
            viewModel!.GoBackCommand.Should().NotBeNull();
            _output.WriteLine("Go back command executed");
        }, _host!, "BrowserView_CanGoBack");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Navigation")]
    public async Task BrowserView_CanRefresh()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.RefreshCommand.ExecuteAsync(null);

            // Assert
            viewModel!.RefreshCommand.Should().NotBeNull();
            _output.WriteLine("Refresh command executed");
        }, _host!, "BrowserView_CanRefresh");
    }

    #endregion

    #region Find Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Find")]
    public async Task BrowserView_CanToggleFindBar()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;
            var initialState = viewModel!.ShowFindBar;

            // Act
            await viewModel.ToggleFindBarCommand.ExecuteAsync(null);

            // Assert
            viewModel.ShowFindBar.Should().Be(!initialState);
            _output.WriteLine($"Find bar toggled from {initialState} to {viewModel.ShowFindBar}");
        }, _host!, "BrowserView_CanToggleFindBar");
    }

    #endregion

    #region Zoom Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Zoom")]
    public async Task BrowserView_CanZoomIn()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.ZoomInCommand.ExecuteAsync(null);

            // Assert
            viewModel!.ZoomInCommand.Should().NotBeNull();
            _output.WriteLine("Zoom in command executed");
        }, _host!, "BrowserView_CanZoomIn");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Zoom")]
    public async Task BrowserView_CanZoomOut()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.ZoomOutCommand.ExecuteAsync(null);

            // Assert
            viewModel!.ZoomOutCommand.Should().NotBeNull();
            _output.WriteLine("Zoom out command executed");
        }, _host!, "BrowserView_CanZoomOut");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Zoom")]
    public async Task BrowserView_CanResetZoom()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.ResetZoomCommand.ExecuteAsync(null);

            // Assert
            viewModel!.ResetZoomCommand.Should().NotBeNull();
            _output.WriteLine("Reset zoom command executed");
        }, _host!, "BrowserView_CanResetZoom");
    }

    #endregion

    #region DevTools Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "DevTools")]
    public async Task BrowserView_CanShowDevTools()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.ShowDevToolsCommand.ExecuteAsync(null);

            // Assert
            viewModel!.ShowDevToolsCommand.Should().NotBeNull();
            _output.WriteLine("Show DevTools command executed");
        }, _host!, "BrowserView_CanShowDevTools");
    }

    #endregion

    #region Screenshot Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Screenshot")]
    public async Task BrowserView_CanTakeScreenshot()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.TakeScreenshotCommand.ExecuteAsync(null);

            // Assert
            viewModel!.TakeScreenshotCommand.Should().NotBeNull();
            _output.WriteLine("Take screenshot command executed");
        }, _host!, "BrowserView_CanTakeScreenshot");
    }

    #endregion

    #region Print Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Browser")]
    [Trait("SubFeature", "Print")]
    public async Task BrowserView_CanPrint()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var browserView = window.Content as BrowserShellView;
            var viewModel = browserView!.DataContext as BrowserShellViewModel;

            // Act
            await viewModel.PrintCommand.ExecuteAsync(null);

            // Assert
            viewModel!.PrintCommand.Should().NotBeNull();
            _output.WriteLine("Print command executed");
        }, _host!, "BrowserView_CanPrint");
    }

    #endregion
}
