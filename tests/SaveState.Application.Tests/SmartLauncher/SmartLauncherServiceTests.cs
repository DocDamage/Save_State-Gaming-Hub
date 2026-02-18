// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.SmartLauncher;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.SmartLauncher;

namespace SaveState.Application.Tests.SmartLauncher;

public sealed class SmartLauncherServiceTests
{
    private readonly Mock<ILogger<SmartLauncherService>> _loggerMock;
    private readonly Mock<ILaunchProfileRepository> _profileRepositoryMock;
    private readonly Mock<ILaunchSessionRepository> _sessionRepositoryMock;
    private readonly Mock<ISystemOptimizerService> _optimizerMock;
    private readonly Mock<IGameProcessMonitor> _processMonitorMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly SmartLauncherService _service;

    public SmartLauncherServiceTests()
    {
        _loggerMock = new Mock<ILogger<SmartLauncherService>>();
        _profileRepositoryMock = new Mock<ILaunchProfileRepository>();
        _sessionRepositoryMock = new Mock<ILaunchSessionRepository>();
        _optimizerMock = new Mock<ISystemOptimizerService>();
        _processMonitorMock = new Mock<IGameProcessMonitor>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _timeProviderMock = new Mock<ITimeProvider>();

        _timeProviderMock.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);

        _service = new SmartLauncherService(
            _loggerMock.Object,
            _profileRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _optimizerMock.Object,
            _processMonitorMock.Object,
            _gameRepositoryMock.Object,
            _timeProviderMock.Object);
    }

    [Fact]
    public async Task LaunchGameAsync_WithActiveSession_ReturnsFailure()
    {
        // Arrange
        var activeSession = new LaunchSession { Id = Guid.NewGuid(), GameName = "Test Game" };
        _sessionRepositoryMock.Setup(r => r.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(activeSession));

        // Act
        var result = await _service.LaunchGameAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already running", result.ErrorMessage);
    }

    [Fact]
    public async Task LaunchGameAsync_GameNotFound_ReturnsFailure()
    {
        // Arrange
        _sessionRepositoryMock.Setup(r => r.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LaunchSession>("No active session", ErrorType.NotFound));
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<GameId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        // Act
        var result = await _service.LaunchGameAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task LaunchGameAsync_NoExecutable_ReturnsFailure()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var game = CreateTestGame(gameId, "Test Game");
        // ExecutablePath is null by default

        _sessionRepositoryMock.Setup(r => r.GetActiveSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LaunchSession>("No active session", ErrorType.NotFound));
        _gameRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<GameId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        // Act
        var result = await _service.LaunchGameAsync(gameId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("executable not configured", result.ErrorMessage);
    }

    [Fact]
    public async Task GetProfilesAsync_ReturnsProfilesFromRepository()
    {
        // Arrange
        var profiles = new List<LaunchProfile>
        {
            LaunchProfile.CreateBalancedProfile(),
            LaunchProfile.CreatePerformanceProfile()
        };
        _profileRepositoryMock.Setup(r => r.GetProfilesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);

        // Act
        var result = await _service.GetProfilesAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateProfileAsync_SavesProfileAndReturnsSuccess()
    {
        // Arrange
        var profile = LaunchProfile.CreateBalancedProfile();
        profile.Name = "Test Profile";

        // Act
        var result = await _service.CreateProfileAsync(profile);

        // Assert
        Assert.True(result.IsSuccess);
        _profileRepositoryMock.Verify(r => r.SaveProfileAsync(
            It.Is<LaunchProfile>(p => p.Name == "Test Profile"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProfileAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        // Arrange
        var profile = LaunchProfile.CreateBalancedProfile();
        _profileRepositoryMock.Setup(r => r.SaveProfileAsync(It.IsAny<LaunchProfile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.CreateProfileAsync(profile);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task EndSessionAsync_SessionNotFound_ReturnsFailure()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _sessionRepositoryMock.Setup(r => r.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LaunchSession>("Session not found", ErrorType.NotFound));

        // Act
        var result = await _service.EndSessionAsync(sessionId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task EndSessionAsync_ValidSession_StopsMonitoringAndRestoresState()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new LaunchSession
        {
            Id = sessionId,
            GameName = "Test Game",
            InitialSystemState = new SystemState()
        };

        _sessionRepositoryMock.Setup(r => r.GetSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(session));
        _processMonitorMock.Setup(r => r.StopMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionPerformanceMetrics());

        // Act
        var result = await _service.EndSessionAsync(sessionId);

        // Assert
        Assert.True(result.IsSuccess);
        _processMonitorMock.Verify(r => r.StopMonitoringAsync(It.IsAny<CancellationToken>()), Times.Once);
        _optimizerMock.Verify(r => r.RestoreSystemStateAsync(
            session.InitialSystemState,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PreviewOptimizationsAsync_WithProfile_ReturnsOptimizationList()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = LaunchProfile.CreatePerformanceProfile();
        _profileRepositoryMock.Setup(r => r.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(profile));

        // Act
        var result = await _service.PreviewOptimizationsAsync(profileId);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, r => r.Contains("Process priority"));
    }

    [Fact]
    public async Task GetLaunchHistoryAsync_ReturnsSessionsFromRepository()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var sessions = new List<LaunchSession>
        {
            new() { Id = Guid.NewGuid(), GameId = gameId, GameName = "Session 1" },
            new() { Id = Guid.NewGuid(), GameId = gameId, GameName = "Session 2" }
        };
        _sessionRepositoryMock.Setup(r => r.GetLaunchHistoryAsync(gameId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.GetLaunchHistoryAsync(gameId, 10);

        // Assert
        Assert.Equal(2, result.Count);
    }

    private static Game CreateTestGame(Guid id, string title)
    {
        var game = Game.Create(title);
        typeof(Game).GetProperty("Id")?.SetValue(game, id);
        return game;
    }
}
