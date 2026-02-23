using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Moq;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;
using ITournamentServiceCore = SaveState.Core.Esports.Services.ITournamentService;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Esports;
using SaveState.Presentation.Services;
using Xunit;

namespace SaveState.Presentation.Tests;

/// <summary>
/// Tests for Phase 4: Tournament Management UI surfacing
/// </summary>
public class UiSurfacingPhase4Tests
{
    private readonly Mock<ITournamentServiceCore> _tournamentServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<INavigationService> _navigationServiceMock;

    public UiSurfacingPhase4Tests()
    {
        _tournamentServiceMock = new Mock<ITournamentServiceCore>();
        _dialogServiceMock = new Mock<IDialogService>();
        _navigationServiceMock = new Mock<INavigationService>();
    }

    #region TournamentListViewModel Tests

    [Fact]
    public async Task TournamentListViewModel_LoadTournamentsAsync_ShouldPopulateTournaments()
    {
        // Arrange
        var tournaments = new List<Tournament>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Tournament 1", Status = TournamentStatus.RegistrationOpen },
            new() { Id = Guid.NewGuid(), Name = "Test Tournament 2", Status = TournamentStatus.InProgress }
        };

        _tournamentServiceMock
            .Setup(s => s.GetTournamentsAsync(It.IsAny<TournamentFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<IReadOnlyList<Tournament>>.Success(tournaments));

        var viewModel = new TournamentListViewModel(
            _tournamentServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object);

        // Act
        await viewModel.LoadTournamentsAsync();

        // Assert
        viewModel.Tournaments.Should().HaveCount(2);
        viewModel.Tournaments[0].Name.Should().Be("Test Tournament 1");
        viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task TournamentListViewModel_CreateTournamentAsync_ShouldShowDialog()
    {
        // Arrange
        var dialogResult = new CreateTournamentResult
        {
            Success = true,
            TournamentId = Guid.NewGuid()
        };

        _dialogServiceMock
            .Setup(d => d.ShowCreateTournamentDialogAsync())
            .ReturnsAsync(dialogResult);

        var viewModel = new TournamentListViewModel(
            _tournamentServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object);

        // Act
        await viewModel.CreateTournamentAsync();

        // Assert
        _dialogServiceMock.Verify(d => d.ShowCreateTournamentDialogAsync(), Times.Once);
    }

    [Fact]
    public async Task TournamentListViewModel_RegisterAsync_ShouldCallService()
    {
        // Arrange
        var tournamentId = Guid.NewGuid();
        var tournamentItem = new TournamentListItem
        {
            Id = tournamentId,
            Name = "Test Tournament",
            IsRegistered = false
        };

        _tournamentServiceMock
            .Setup(s => s.RegisterParticipantAsync(
                tournamentId,
                It.IsAny<RegisterParticipantRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<Participant>.Success(new Participant()));

        var viewModel = new TournamentListViewModel(
            _tournamentServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object);

        // Act
        await viewModel.RegisterAsync(tournamentItem);

        // Assert
        _tournamentServiceMock.Verify(
            s => s.RegisterParticipantAsync(
                tournamentId,
                It.IsAny<RegisterParticipantRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TournamentDetailViewModel Tests

    [Fact]
    public async Task TournamentDetailViewModel_LoadTournamentAsync_ShouldLoadAllData()
    {
        // Arrange
        var tournamentId = Guid.NewGuid();
        var tournament = new Tournament
        {
            Id = tournamentId,
            Name = "Test Tournament",
            Participants = new List<Participant>
            {
                new() { Id = Guid.NewGuid(), DisplayName = "Player 1" },
                new() { Id = Guid.NewGuid(), DisplayName = "Player 2" }
            },
            Matches = new List<Match>
            {
                new() { Id = Guid.NewGuid(), Round = 1 }
            }
        };

        _tournamentServiceMock
            .Setup(s => s.GetTournamentAsync(tournamentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<Tournament>.Success(tournament));

        var viewModel = new TournamentDetailViewModel(
            _tournamentServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object);

        // Act
        await viewModel.LoadTournamentAsync(tournamentId);

        // Assert
        viewModel.Tournament.Should().NotBeNull();
        viewModel.Tournament!.Name.Should().Be("Test Tournament");
        viewModel.Participants.Should().HaveCount(2);
        viewModel.Matches.Should().HaveCount(1);
    }

    [Fact]
    public void TournamentDetailViewModel_CanRegister_ShouldBeTrue_WhenOpenAndNotRegistered()
    {
        // Arrange
        var viewModel = new TournamentDetailViewModel(
            _tournamentServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object)
        {
            Tournament = new Tournament { Status = TournamentStatus.RegistrationOpen },
            IsRegistered = false
        };

        // Act & Assert
        viewModel.CanRegister.Should().BeTrue();
    }

    [Fact]
    public void TournamentDetailViewModel_CanStart_ShouldBeTrue_WhenOrganizerAndNotStarted()
    {
        // Arrange
        var viewModel = new TournamentDetailViewModel(
            _tournamentServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object)
        {
            Tournament = new Tournament { Status = TournamentStatus.RegistrationClosed },
            IsOrganizer = true
        };

        // Act & Assert
        viewModel.CanStart.Should().BeTrue();
    }

    #endregion

    #region CreateTournamentDialogViewModel Tests

    [Fact]
    public void CreateTournamentDialogViewModel_Validate_ShouldFail_WhenNameEmpty()
    {
        // Arrange
        var viewModel = new CreateTournamentDialogViewModel();
        viewModel.Name = "";
        viewModel.SelectedGame = new GameInfoViewModel { GameId = Guid.NewGuid(), Name = "Test Game" };

        // Act
        var isValid = viewModel.Validate();

        // Assert
        isValid.Should().BeFalse();
        viewModel.Errors.Should().Contain(e => e.Contains("name"));
    }

    [Fact]
    public void CreateTournamentDialogViewModel_Validate_ShouldFail_WhenStartDateBeforeRegistration()
    {
        // Arrange
        var viewModel = new CreateTournamentDialogViewModel
        {
            Name = "Test Tournament",
            SelectedGame = new GameInfoViewModel { GameId = Guid.NewGuid(), Name = "Test Game" },
            StartDate = DateTime.Now.AddDays(1),
            RegistrationDeadline = DateTime.Now.AddDays(2) // After start date
        };

        // Act
        var isValid = viewModel.Validate();

        // Assert
        isValid.Should().BeFalse();
        viewModel.Errors.Should().Contain(e => e.Contains("deadline"));
    }

    [Fact]
    public void CreateTournamentDialogViewModel_Validate_ShouldPass_WhenValid()
    {
        // Arrange
        var viewModel = new CreateTournamentDialogViewModel
        {
            Name = "Test Tournament",
            SelectedGame = new GameInfoViewModel { GameId = Guid.NewGuid(), Name = "Test Game" },
            StartDate = DateTime.Now.AddDays(2),
            RegistrationDeadline = DateTime.Now.AddDays(1),
            MaxParticipants = 16
        };

        // Act
        var isValid = viewModel.Validate();

        // Assert
        isValid.Should().BeTrue();
        viewModel.Errors.Should().BeEmpty();
    }

    #endregion

    #region MatchDetailViewModel Tests

    [Fact]
    public void MatchDetailViewModel_Player1Wins_ShouldSetCorrectWinner()
    {
        // Arrange
        var player1 = new Participant { Id = Guid.NewGuid(), DisplayName = "Player 1" };
        var player2 = new Participant { Id = Guid.NewGuid(), DisplayName = "Player 2" };

        var viewModel = new MatchDetailViewModel(
            _tournamentServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object)
        {
            Match = new Match { Player1 = player1, Player2 = player2 },
            Player1Score = 2,
            Player2Score = 1
        };

        // Act
        var winner = viewModel.DetermineWinner();

        // Assert
        winner.Should().Be(player1);
    }

    [Fact]
    public void MatchDetailViewModel_CanReportResult_ShouldBeTrue_WhenPlayerInMatch()
    {
        // Arrange
        var playerId = "current-user-id";
        var player1 = new Participant { Id = Guid.NewGuid(), UserId = playerId, DisplayName = "Player 1" };
        var player2 = new Participant { Id = Guid.NewGuid(), UserId = "other-id", DisplayName = "Player 2" };

        var viewModel = new MatchDetailViewModel(
            _tournamentServiceMock.Object,
            _dialogServiceMock.Object,
            _navigationServiceMock.Object)
        {
            Match = new Match
            {
                Player1 = player1,
                Player2 = player2,
                Status = MatchStatus.Scheduled
            },
            CurrentUserId = playerId
        };

        // Act & Assert
        viewModel.CanReportResult.Should().BeTrue();
    }

    #endregion

    #region LiveTournamentTrackerViewModel Tests

    [Fact]
    public async Task LiveTournamentTrackerViewModel_ConnectAsync_ShouldSetIsConnected()
    {
        // Arrange
        var viewModel = new LiveTournamentTrackerViewModel(
            _tournamentServiceMock.Object,
            Mock.Of<ILiveTournamentHub>());

        // Act
        await viewModel.ConnectAsync();

        // Assert
        viewModel.IsConnected.Should().BeTrue();
        viewModel.ConnectionStatus.Should().Be("Connected");
    }

    [Fact]
    public void LiveTournamentTrackerViewModel_OnMatchUpdate_ShouldUpdateLiveMatches()
    {
        // Arrange
        var viewModel = new LiveTournamentTrackerViewModel(
            _tournamentServiceMock.Object,
            Mock.Of<ILiveTournamentHub>());

        var liveMatch = new LiveMatch
        {
            MatchId = Guid.NewGuid(),
            Player1Name = "Player 1",
            Player2Name = "Player 2",
            Player1Score = 1,
            Player2Score = 0
        };

        // Act
        viewModel.OnMatchUpdate(liveMatch);

        // Assert
        viewModel.LiveMatches.Should().ContainSingle();
        viewModel.LiveMatches[0].Player1Score.Should().Be(1);
    }

    #endregion

    #region TournamentStandingsViewModel Tests

    [Fact]
    public void TournamentStandingsViewModel_CalculateTiebreakers_ShouldSortCorrectly()
    {
        // Arrange
        var viewModel = new TournamentStandingsViewModel(
            _tournamentServiceMock.Object,
            _navigationServiceMock.Object);

        var standings = new List<SwissStanding>
        {
            new()
            {
                Participant = new Participant { DisplayName = "Player A" },
                MatchPoints = 9,
                OpponentMatchWinPct = 0.65m
            },
            new()
            {
                Participant = new Participant { DisplayName = "Player B" },
                MatchPoints = 9,
                OpponentMatchWinPct = 0.70m
            },
            new()
            {
                Participant = new Participant { DisplayName = "Player C" },
                MatchPoints = 6,
                OpponentMatchWinPct = 0.50m
            }
        };

        // Act
        var sorted = viewModel.CalculateTiebreakers(standings);

        // Assert
        sorted[0].Participant.DisplayName.Should().Be("Player B"); // Higher OMW%
        sorted[1].Participant.DisplayName.Should().Be("Player A");
        sorted[2].Participant.DisplayName.Should().Be("Player C");
    }

    [Fact]
    public void TournamentStandingsViewModel_GetTopCutQualifier_ShouldReturnCorrectCutoff()
    {
        // Arrange
        var viewModel = new TournamentStandingsViewModel(
            _tournamentServiceMock.Object,
            _navigationServiceMock.Object);

        var standings = new List<SwissStanding>();
        for (int i = 0; i < 16; i++)
        {
            standings.Add(new SwissStanding
            {
                Participant = new Participant { DisplayName = $"Player {i + 1}" },
                Rank = i + 1
            });
        }

        // Act
        var cutoff = viewModel.GetTopCutQualifier(standings, 8);

        // Assert
        cutoff.Should().Be(8);
    }

    #endregion

    #region Bracket Generation Tests

    [Theory]
    [InlineData(4, 2)]   // 4 players = 2 rounds
    [InlineData(8, 3)]   // 8 players = 3 rounds
    [InlineData(16, 4)]  // 16 players = 4 rounds
    [InlineData(32, 5)]  // 32 players = 5 rounds
    public void SingleEliminationBracket_ShouldHaveCorrectRounds(int players, int expectedRounds)
    {
        // Arrange
        var participants = new List<Participant>();
        for (int i = 0; i < players; i++)
        {
            participants.Add(new Participant { Id = Guid.NewGuid(), DisplayName = $"Player {i + 1}", Seed = i + 1 });
        }

        var generator = new BracketGenerator();

        // Act
        var bracket = generator.GenerateSingleElimination(participants);

        // Assert
        bracket.TotalRounds.Should().Be(expectedRounds);
        bracket.Matches.Count.Should().Be(players - 1);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void DoubleEliminationBracket_ShouldHaveWinnersAndLosers(int players)
    {
        // Arrange
        var participants = new List<Participant>();
        for (int i = 0; i < players; i++)
        {
            participants.Add(new Participant { Id = Guid.NewGuid(), DisplayName = $"Player {i + 1}", Seed = i + 1 });
        }

        var generator = new BracketGenerator();

        // Act
        var bracket = generator.GenerateDoubleElimination(participants);

        // Assert
        bracket.Rounds.Should().Contain(r => r.Type == BracketType.Winners);
        bracket.Rounds.Should().Contain(r => r.Type == BracketType.Losers);
        bracket.Rounds.Should().Contain(r => r.Type == BracketType.GrandFinals);
    }

    [Theory]
    [InlineData(4, 6)]   // 4 players = 6 matches (everyone plays everyone)
    [InlineData(5, 10)]  // 5 players = 10 matches
    public void RoundRobinBracket_ShouldHaveCorrectMatches(int players, int expectedMatches)
    {
        // Arrange
        var participants = new List<Participant>();
        for (int i = 0; i < players; i++)
        {
            participants.Add(new Participant { Id = Guid.NewGuid(), DisplayName = $"Player {i + 1}" });
        }

        var generator = new BracketGenerator();

        // Act
        var bracket = generator.GenerateRoundRobin(participants);

        // Assert
        bracket.Matches.Count.Should().Be(expectedMatches);
    }

    #endregion
}
