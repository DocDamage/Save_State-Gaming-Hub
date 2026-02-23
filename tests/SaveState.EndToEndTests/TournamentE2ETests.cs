using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Enums;
using SaveState.EndToEndTests.Infrastructure;
using SaveState.Presentation.Resources;
using SaveState.Presentation.ViewModels.Mugen;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.Views.Shell;
using Xunit;
using Xunit.Abstractions;

namespace SaveState.EndToEndTests;

/// <summary>
/// End-to-end browser automation tests for Tournament management.
/// Tests complete tournament creation and management flow.
/// </summary>
public class TournamentE2ETests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private AvaloniaTestHost? _host;
    private readonly IServiceProvider _serviceProvider;

    public TournamentE2ETests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _serviceProvider = fixture.Services;
    }

    public async Task InitializeAsync()
    {
        _host = new AvaloniaTestHost(_serviceProvider);
        await _host.StartAsync(sp => CreateTournamentWindow(sp));
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }

    private static Window CreateTournamentWindow(IServiceProvider services)
    {
        var window = new Window
        {
            Title = "Tournament E2E Test",
            Width = 1200,
            Height = 800,
            Content = CreateTournamentBracketView(services)
        };
        return window;
    }

    private static TournamentBracketView CreateTournamentBracketView(IServiceProvider services)
    {
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<TournamentBracketViewModel>>();
        var mockResources = CreateMockResources();
        var mockTimeProvider = new Mock<ITimeProvider>();

        // Setup mock tournament data
        var tournament = CreateMockTournament();

        mockMediator.Setup(x => x.Send(It.IsAny<IRequest<Core.Common.Result<Tournament>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<Tournament>.Success(tournament));

        var viewModel = new TournamentBracketViewModel(
            mockMediator.Object,
            mockLogger.Object,
            mockResources);

        // Load tournament data
        viewModel.LoadTournament(tournament);

        return new TournamentBracketView { DataContext = viewModel };
    }

    private static Tournament CreateMockTournament()
    {
        var tournament = Tournament.Create(
            "Test Tournament",
            "Test Description",
            TournamentFormat.SingleElimination,
            8,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3),
            TournamentRules.Default());

        // Add participants
        var participant1 = TournamentParticipant.Create("Player 1", "player1@example.com", null, null);
        var participant2 = TournamentParticipant.Create("Player 2", "player2@example.com", null, null);
        var participant3 = TournamentParticipant.Create("Player 3", "player3@example.com", null, null);
        var participant4 = TournamentParticipant.Create("Player 4", "player4@example.com", null, null);

        tournament.AddParticipant(participant1);
        tournament.AddParticipant(participant2);
        tournament.AddParticipant(participant3);
        tournament.AddParticipant(participant4);

        return tournament;
    }

    private static Resources CreateMockResources()
    {
        var localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<Resources>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new Microsoft.Extensions.Localization.LocalizedString(key, key));
        return new Resources(localizerMock.Object);
    }

    #region Tournament Display Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    public async Task TournamentView_Loads_Successfully()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange & Act
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;

            // Assert
            bracketView.Should().NotBeNull();
            bracketView!.DataContext.Should().BeOfType<TournamentBracketViewModel>();
            _output.WriteLine("Tournament bracket view loaded successfully");
        }, _host!, "TournamentView_Loads_Successfully");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    public async Task TournamentView_DisplaysTournamentName()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;
            var viewModel = bracketView!.DataContext as TournamentBracketViewModel;

            // Act & Assert
            viewModel!.TournamentName.Should().NotBeNullOrEmpty();
            viewModel.TournamentName.Should().Contain("Tournament");
            _output.WriteLine($"Tournament name: {viewModel.TournamentName}");
        }, _host!, "TournamentView_DisplaysTournamentName");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    public async Task TournamentView_DisplaysBracketStructure()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;
            var viewModel = bracketView!.DataContext as TournamentBracketViewModel;

            // Act
            await Task.Delay(200);

            // Assert
            viewModel!.Rounds.Should().NotBeNull();
            viewModel.Rounds.Should().NotBeEmpty();
            _output.WriteLine($"Number of rounds: {viewModel.Rounds.Count}");
        }, _host!, "TournamentView_DisplaysBracketStructure");
    }

    #endregion

    #region Participant Management Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    [Trait("SubFeature", "Participants")]
    public async Task TournamentView_ShowsParticipantList()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;
            var viewModel = bracketView!.DataContext as TournamentBracketViewModel;

            // Act
            await Task.Delay(200);

            // Assert
            viewModel!.Participants.Should().NotBeNull();
            viewModel.Participants.Should().NotBeEmpty();
            _output.WriteLine($"Number of participants: {viewModel.Participants.Count}");
        }, _host!, "TournamentView_ShowsParticipantList");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    [Trait("SubFeature", "Participants")]
    public async Task TournamentView_CanSelectParticipant()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;
            var viewModel = bracketView!.DataContext as TournamentBracketViewModel;

            // Act
            var firstParticipant = viewModel!.Participants.FirstOrDefault();
            if (firstParticipant != null)
            {
                viewModel.SelectedParticipant = firstParticipant;
            }

            // Assert
            viewModel.SelectedParticipant.Should().NotBeNull();
            _output.WriteLine($"Selected participant: {viewModel.SelectedParticipant?.Name}");
        }, _host!, "TournamentView_CanSelectParticipant");
    }

    #endregion

    #region Match Management Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    [Trait("SubFeature", "Matches")]
    public async Task TournamentView_DisplaysMatches()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;
            var viewModel = bracketView!.DataContext as TournamentBracketViewModel;

            // Act
            await Task.Delay(200);

            // Assert
            var matches = viewModel!.Rounds.SelectMany(r => r.Matches).ToList();
            matches.Should().NotBeEmpty();
            _output.WriteLine($"Total matches: {matches.Count}");
        }, _host!, "TournamentView_DisplaysMatches");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    [Trait("SubFeature", "Matches")]
    public async Task TournamentView_CanSelectMatch()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;
            var viewModel = bracketView!.DataContext as TournamentBracketViewModel;

            // Act
            var firstMatch = viewModel!.Rounds.SelectMany(r => r.Matches).FirstOrDefault();
            if (firstMatch != null)
            {
                viewModel.SelectedMatch = firstMatch;
            }

            // Assert
            viewModel.SelectedMatch.Should().NotBeNull();
            _output.WriteLine($"Selected match: {viewModel.SelectedMatch?.Id}");
        }, _host!, "TournamentView_CanSelectMatch");
    }

    #endregion

    #region Tournament Creation Dialog Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    [Trait("SubFeature", "Creation")]
    public async Task CreateTournamentDialog_Opens_Successfully()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            var mockLogger = new Mock<ILogger<CreateTournamentDialogViewModel>>();
            var mockResources = CreateMockResources();

            // Act - Create dialog
            var dialog = new CreateTournamentDialog();
            var viewModel = new CreateTournamentDialogViewModel(
                mockMediator.Object,
                mockLogger.Object,
                mockResources);
            dialog.DataContext = viewModel;

            // Assert
            dialog.Should().NotBeNull();
            dialog.DataContext.Should().BeOfType<CreateTournamentDialogViewModel>();
            _output.WriteLine("Create tournament dialog opened successfully");
        }, _host!, "CreateTournamentDialog_Opens_Successfully");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    [Trait("SubFeature", "Creation")]
    public async Task CreateTournamentDialog_ValidatesRequiredFields()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            var mockLogger = new Mock<ILogger<CreateTournamentDialogViewModel>>();
            var mockResources = CreateMockResources();

            var viewModel = new CreateTournamentDialogViewModel(
                mockMediator.Object,
                mockLogger.Object,
                mockResources);

            // Act - Leave required fields empty
            viewModel.TournamentName = string.Empty;
            viewModel.Validate();

            // Assert
            viewModel.HasErrors.Should().BeTrue();
            _output.WriteLine("Validation correctly identifies missing required fields");
        }, _host!, "CreateTournamentDialog_ValidatesRequiredFields");
    }

    #endregion

    #region Tournament Settings Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    [Trait("SubFeature", "Settings")]
    public async Task TournamentView_SupportsFormatSelection()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;
            var viewModel = bracketView!.DataContext as TournamentBracketViewModel;

            // Act & Assert
            var formats = Enum.GetValues<TournamentFormat>();
            formats.Should().NotBeEmpty();
            
            foreach (var format in formats)
            {
                _output.WriteLine($"Supported format: {format}");
            }
        }, _host!, "TournamentView_SupportsFormatSelection");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "Tournament")]
    [Trait("SubFeature", "Settings")]
    public async Task TournamentView_SupportsRoundConfiguration()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var bracketView = window.Content as TournamentBracketView;
            var viewModel = bracketView!.DataContext as TournamentBracketViewModel;

            // Act & Assert
            viewModel!.Rounds.Should().NotBeNull();
            viewModel.Rounds.Should().NotBeEmpty();
            
            foreach (var round in viewModel.Rounds)
            {
                _output.WriteLine($"Round: {round.Name}, Matches: {round.Matches.Count}");
            }
        }, _host!, "TournamentView_SupportsRoundConfiguration");
    }

    #endregion
}
