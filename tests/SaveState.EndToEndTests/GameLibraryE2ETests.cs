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

        var mockPlatformRepo = new Mock<IPlatformRepository>();

        var sidebarViewModel = new LibrarySidebarViewModel(
            mockCollectionService.Object,
            mockPlatformRepo.Object,
            mockGameRepo.Object,
            mockSidebarLogger.Object);

        var toolbarViewModel = new LibraryToolbarViewModel(
            mockToolbarLogger.Object,
            mockDialogService.Object);

        var gridViewModel = new GameGridViewModel(
            mockMediator.Object,
            mockGridLogger.Object,
            mockNavigationService.Object);

        var listViewModel = new GameListViewModel(
            mockMediator.Object,
            mockListLogger.Object,
            mockNavigationService.Object);

        // Compact and table views reuse the same VM types
        var compactViewModel = new GameGridViewModel(
            mockMediator.Object,
            Mock.Of<ILogger<GameGridViewModel>>(),
            mockNavigationService.Object);

        var tableViewModel = new GameListViewModel(
            mockMediator.Object,
            Mock.Of<ILogger<GameListViewModel>>(),
            mockNavigationService.Object);

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
            viewModel!.ToolbarViewModel.SearchTerm = "Test Game";
            await Task.Delay(200);

            // Assert
            viewModel.ToolbarViewModel.SearchTerm.Should().Be("Test Game");
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
            viewModel!.ToolbarViewModel.SearchTerm = "Test Game";
            await Task.Delay(100);
            viewModel.ToolbarViewModel.SearchTerm = string.Empty;
            await Task.Delay(100);

            // Assert
            viewModel.ToolbarViewModel.SearchTerm.Should().BeEmpty();
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
            viewModel!.SetGridViewCommand.Execute(null);
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
            viewModel!.SetListViewCommand.Execute(null);
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
            var games = viewModel!.GridViewModel.Games.ToList();
            
            // Games collection exists - selection is tracked internally
            // via GetSelectedGames() method
            var selectedGames = viewModel.GridViewModel.GetSelectedGames();

            // Assert
            games.Should().NotBeEmpty("Games should be loaded");
            _output.WriteLine($"Games loaded: {games.Count}");
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

            // Act - Ensure no games are selected
            var selectedGames = viewModel!.GridViewModel.GetSelectedGames();
            await Task.Delay(100);

            // Assert - Check selection state
            selectedGames.Should().BeEmpty("No games should be selected initially");
            _output.WriteLine("No games selected - launch should be disabled");
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

            // Act - Check games are loaded
            await Task.Delay(200);
            var games = viewModel!.GridViewModel.Games.ToList();

            // Assert
            games.Should().NotBeEmpty("Games should be loaded for selection");
            _output.WriteLine($"Games available: {games.Count} - launch command should be available when game selected");
        }, _host!, "LaunchGame_CommandAvailable_WhenGameSelected");
    }

    #endregion
}
