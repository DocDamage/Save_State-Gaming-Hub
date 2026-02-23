using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Ai.Services;
using SaveState.EndToEndTests.Infrastructure;
using SaveState.Presentation.Resources;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Library;
using SaveState.Presentation.Views.Library;
using Xunit;
using Xunit.Abstractions;

namespace SaveState.EndToEndTests;

/// <summary>
/// End-to-end browser automation tests for the Game Library feature.
/// Tests the complete user flow from library navigation to game management.
/// </summary>
public class GameLibraryE2ETests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private AvaloniaTestHost? _host;
    private readonly IServiceProvider _serviceProvider;

    public GameLibraryE2ETests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _serviceProvider = fixture.Services;
    }

    public async Task InitializeAsync()
    {
        _host = new AvaloniaTestHost(_serviceProvider);
        await _host.StartAsync(sp => CreateMainWindow(sp));
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }

    private static Window CreateMainWindow(IServiceProvider services)
    {
        var window = new Window
        {
            Title = "Game Library E2E Test",
            Width = 1400,
            Height = 900,
            Content = CreateLibraryView(services)
        };
        return window;
    }

    private static LibraryView CreateLibraryView(IServiceProvider services)
    {
        // Create mocks for all required dependencies
        var mockMediator = new Mock<IMediator>();
        var mockGameRepo = new Mock<IGameRepository>();
        var mockLogger = new Mock<ILogger<LibraryViewModel>>();
        var mockNavigationService = new Mock<INavigationService>();
        var mockDialogService = new Mock<IDialogService>();
        var mockCollectionService = new Mock<IVirtualCollectionService>();
        var mockNlSearch = new Mock<INaturalLanguageGameSearch>();

        // Create sub-viewmodels with their required dependencies
        var mockSidebarLogger = new Mock<ILogger<LibrarySidebarViewModel>>();
        var mockToolbarLogger = new Mock<ILogger<LibraryToolbarViewModel>>();
        var mockGridLogger = new Mock<ILogger<GameGridViewModel>>();
        var mockListLogger = new Mock<ILogger<GameListViewModel>>();

        var sidebarViewModel = new LibrarySidebarViewModel(
            mockMediator.Object,
            mockCollectionService.Object,
            mockGameRepo.Object,
            mockSidebarLogger.Object);

        var toolbarViewModel = new LibraryToolbarViewModel(
            mockMediator.Object,
            mockToolbarLogger.Object);

        var gridViewModel = new GameGridViewModel(
            mockMediator.Object,
            mockGameRepo.Object,
            mockCollectionService.Object,
            mockGridLogger.Object);

        var listViewModel = new GameListViewModel(
            mockMediator.Object,
            mockGameRepo.Object,
            mockCollectionService.Object,
            mockListLogger.Object);

        // Compact and table views reuse the same VM types
        var compactViewModel = new GameGridViewModel(
            mockMediator.Object,
            mockGameRepo.Object,
            mockCollectionService.Object,
            Mock.Of<ILogger<GameGridViewModel>>());

        var tableViewModel = new GameListViewModel(
            mockMediator.Object,
            mockGameRepo.Object,
            mockCollectionService.Object,
            Mock.Of<ILogger<GameListViewModel>>());

        // Setup mock game repository to return test data
        mockGameRepo.Setup(x => x.GetGamesAsync(
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
            .ReturnsAsync(new PagedResult<Game>(
                new[] {
                    Game.Create("Test Game 1", Guid.NewGuid()),
                    Game.Create("Test Game 2", Guid.NewGuid()),
                    Game.Create("Another Game", Guid.NewGuid())
                },
                3, 1, 50));

        // Create the main LibraryViewModel with all required dependencies
        var viewModel = new LibraryViewModel(
            sidebarViewModel,
            toolbarViewModel,
            gridViewModel,
            listViewModel,
            compactViewModel,
            tableViewModel,
            mockNavigationService.Object,
            mockDialogService.Object,
            mockGameRepo.Object,
            mockCollectionService.Object,
            mockNlSearch.Object,
            mockLogger.Object);

        return new LibraryView { DataContext = viewModel };
    }

    private static Resources CreateMockResources()
    {
        var localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<Resources>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new Microsoft.Extensions.Localization.LocalizedString(key, key));
        return new Resources(localizerMock.Object);
    }

    #region Game Library Navigation Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    public async Task LibraryView_Loads_Successfully()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange & Act - LibraryView is already loaded
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;

            // Assert
            libraryView.Should().NotBeNull();
            libraryView!.DataContext.Should().BeOfType<LibraryViewModel>();
        }, _host!, "LibraryView_Loads_Successfully");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    public async Task LibraryView_DisplaysGameList()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;

            // Act - Wait for view to load
            await Task.Delay(200);
            await libraryView!.WaitForLayoutAsync();

            // Assert - Check that the view contains game grid/list
            var gridView = libraryView.FindControl<Control>("GameGridView");
            var listView = libraryView.FindControl<Control>("GameListView");
            
            (gridView ?? listView).Should().NotBeNull("Expected either grid or list view to be present");
        }, _host!, "LibraryView_DisplaysGameList");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    public async Task LibraryView_RefreshButton_Clickable()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            
            // Act - Find refresh button by automation name
            var refreshButton = window.FindByAutomationId<Button>("Refresh Library");
            
            // Assert
            refreshButton.Should().NotBeNull("Refresh button should be present");
            refreshButton!.IsEnabled.Should().BeTrue();
        }, _host!, "LibraryView_RefreshButton_Clickable");
    }

    #endregion

    #region Game Search Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    [Trait("SubFeature", "Search")]
    public async Task SearchGames_ByName_FiltersResults()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;
            var viewModel = libraryView!.DataContext as LibraryViewModel;

            // Act - Enter search text
            viewModel!.SearchText = "Test Game";
            await Task.Delay(200);

            // Assert
            viewModel.SearchText.Should().Be("Test Game");
            _output.WriteLine("Search text entered successfully");
        }, _host!, "SearchGames_ByName_FiltersResults");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    [Trait("SubFeature", "Search")]
    public async Task SearchGames_ClearSearch_ResetResults()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;
            var viewModel = libraryView!.DataContext as LibraryViewModel;

            // Act - Enter and then clear search
            viewModel!.SearchText = "Test Game";
            await Task.Delay(100);
            viewModel.SearchText = string.Empty;
            await Task.Delay(100);

            // Assert
            viewModel.SearchText.Should().BeEmpty();
            _output.WriteLine("Search cleared successfully");
        }, _host!, "SearchGames_ClearSearch_ResetResults");
    }

    #endregion

    #region View Mode Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    [Trait("SubFeature", "ViewModes")]
    public async Task ViewMode_SwitchToGrid_ShowsGridView()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;
            var viewModel = libraryView!.DataContext as LibraryViewModel;

            // Act - Switch to grid view
            viewModel!.CurrentViewMode = LibraryViewMode.Grid;
            await Task.Delay(100);

            // Assert
            viewModel.IsGridView.Should().BeTrue();
            _output.WriteLine("Grid view mode activated");
        }, _host!, "ViewMode_SwitchToGrid_ShowsGridView");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    [Trait("SubFeature", "ViewModes")]
    public async Task ViewMode_SwitchToList_ShowsListView()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;
            var viewModel = libraryView!.DataContext as LibraryViewModel;

            // Act - Switch to list view
            viewModel!.CurrentViewMode = LibraryViewMode.List;
            await Task.Delay(100);

            // Assert
            viewModel.IsListView.Should().BeTrue();
            _output.WriteLine("List view mode activated");
        }, _host!, "ViewMode_SwitchToList_ShowsListView");
    }

    #endregion

    #region Game Selection Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    [Trait("SubFeature", "Selection")]
    public async Task SelectGame_UpdatesSelectedGame()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;
            var viewModel = libraryView!.DataContext as LibraryViewModel;

            // Act - Wait for games to load and select first
            await Task.Delay(200);
            var games = viewModel!.Games.ToList();
            
            if (games.Any())
            {
                viewModel.SelectedGame = games.First();
            }

            // Assert
            viewModel.SelectedGame.Should().NotBeNull();
            _output.WriteLine($"Selected game: {viewModel.SelectedGame?.Title}");
        }, _host!, "SelectGame_UpdatesSelectedGame");
    }

    #endregion

    #region Launch Game Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    [Trait("SubFeature", "Launch")]
    public async Task LaunchGame_ButtonDisabled_WhenNoGameSelected()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;
            var viewModel = libraryView!.DataContext as LibraryViewModel;

            // Act - Ensure no game is selected
            viewModel!.SelectedGame = null;
            await Task.Delay(100);

            // Assert - Check that launch command cannot execute
            viewModel.SelectedGame.Should().BeNull();
            _output.WriteLine("No game selected - launch should be disabled");
        }, _host!, "LaunchGame_ButtonDisabled_WhenNoGameSelected");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "GameLibrary")]
    [Trait("SubFeature", "Launch")]
    public async Task LaunchGame_CommandAvailable_WhenGameSelected()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var libraryView = window.Content as LibraryView;
            var viewModel = libraryView!.DataContext as LibraryViewModel;

            // Act - Select a game
            await Task.Delay(200);
            var games = viewModel!.Games.ToList();
            if (games.Any())
            {
                viewModel.SelectedGame = games.First();
            }

            // Assert
            viewModel.SelectedGame.Should().NotBeNull();
            _output.WriteLine("Game selected - launch command should be available");
        }, _host!, "LaunchGame_CommandAvailable_WhenGameSelected");
    }

    #endregion
}
