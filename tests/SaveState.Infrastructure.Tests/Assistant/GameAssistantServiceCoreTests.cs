using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SaveState.Core.Ai.Services;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.Assistant;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.Assistant;

public class GameAssistantServiceCoreTests
{
    private readonly Mock<IAiOrchestrator> _aiOrchestratorMock = new();
    private readonly Mock<IGameRepository> _gameRepositoryMock = new();
    private readonly Mock<ISmartCategorizationService> _categorizationServiceMock = new();
    private readonly Mock<IEyeTrackingMonitor> _eyeTrackingMonitorMock = new();
    private readonly TestTimeProvider _timeProvider = new(new DateTime(2026, 2, 13, 20, 0, 0, DateTimeKind.Utc));

    public GameAssistantServiceCoreTests()
    {
        _eyeTrackingMonitorMock.SetupGet(monitor => monitor.IsAvailable).Returns(true);
        _eyeTrackingMonitorMock.SetupGet(monitor => monitor.IsMonitoring).Returns(false);
        _eyeTrackingMonitorMock
            .Setup(monitor => monitor.StartMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _eyeTrackingMonitorMock
            .Setup(monitor => monitor.StopMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    [Fact]
    public async Task AnalyzeDifficultyAsync_WhenGameMissing_ReturnsNotFound()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        _gameRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                It.Is<GameId>(value => value.Value == gameId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);
        var sut = CreateSut();

        var metrics = new GameplayMetrics(
            DeathCount: 3,
            TimeInCurrentSection: TimeSpan.FromMinutes(8),
            RetryCount: 2,
            InputPattern: new InputPattern(40, 0.12f, false, false),
            SessionStartTimeUtc: _timeProvider.UtcNow.AddMinutes(-20));

        // Act
        var result = await sut.AnalyzeDifficultyAsync(gameId, metrics);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task AnalyzeDifficultyAsync_WhenFrustrationHigh_SuggestsDecrease()
    {
        // Arrange
        var game = Game.Create("Dark Souls");
        SetupGame(game);
        var sut = CreateSut();

        var metrics = new GameplayMetrics(
            DeathCount: 12,
            TimeInCurrentSection: TimeSpan.FromMinutes(28),
            RetryCount: 9,
            InputPattern: new InputPattern(88, 0.41f, true, true),
            SessionStartTimeUtc: _timeProvider.UtcNow.AddHours(-2));

        // Act
        var result = await sut.AnalyzeDifficultyAsync(game.Id, metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Difficulty.Should().Be(SuggestedDifficulty.Decrease);
        result.Value.Confidence.Should().BeGreaterThan(0.8f);
        result.Value.SupportingMetrics.Should().Contain(metric => metric.Contains("deaths", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeDifficultyAsync_WhenPerformanceStrong_SuggestsIncrease()
    {
        // Arrange
        var game = Game.Create("Celeste");
        SetupGame(game);
        var sut = CreateSut();

        var metrics = new GameplayMetrics(
            DeathCount: 1,
            TimeInCurrentSection: TimeSpan.FromMinutes(4),
            RetryCount: 1,
            InputPattern: new InputPattern(62, 0.05f, false, false),
            SessionStartTimeUtc: _timeProvider.UtcNow.AddMinutes(-35));

        // Act
        var result = await sut.AnalyzeDifficultyAsync(game.Id, metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Difficulty.Should().Be(SuggestedDifficulty.Increase);
        result.Value.Confidence.Should().BeGreaterThan(0.7f);
    }

    [Fact]
    public async Task EnableSmartPauseAsync_WhenThresholdInvalid_ReturnsValidationFailure()
    {
        // Arrange
        var sut = CreateSut();
        var options = new SmartPauseOptions(
            Enabled: true,
            LookAwayThresholdSeconds: 1,
            ResumeOnGazeReturn: true,
            RequireEyeTracking: true);

        // Act
        var result = await sut.EnableSmartPauseAsync(options);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task EnableSmartPauseAsync_WhenEyeTrackingRequiredAndUnavailable_ReturnsNotImplemented()
    {
        // Arrange
        _eyeTrackingMonitorMock.SetupGet(monitor => monitor.IsAvailable).Returns(false);
        var sut = CreateSut();
        var options = new SmartPauseOptions(
            Enabled: true,
            LookAwayThresholdSeconds: 5,
            ResumeOnGazeReturn: true,
            RequireEyeTracking: true);

        // Act
        var result = await sut.EnableSmartPauseAsync(options);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotImplemented);
        _eyeTrackingMonitorMock.Verify(
            monitor => monitor.StartMonitoringAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AnalyzeSessionAsync_WhenLookAwayExceedsThreshold_ReturnsSmartPauseRecommendation()
    {
        // Arrange
        var game = Game.Create("Metroid Prime");
        SetupGame(game);
        var sut = CreateSut();
        await sut.EnableSmartPauseAsync(new SmartPauseOptions(
            Enabled: true,
            LookAwayThresholdSeconds: 5,
            ResumeOnGazeReturn: true,
            RequireEyeTracking: false));

        var context = new SessionContext(
            GameId: game.Id,
            SessionStartTimeUtc: _timeProvider.UtcNow.AddMinutes(-30),
            RecentDeaths: 0,
            RecentRetries: 0,
            BreaksTaken: 1,
            InputPattern: new InputPattern(32, 0.08f, false, false),
            LookAwayDurationSeconds: 9);

        // Act
        var result = await sut.AnalyzeSessionAsync(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(AssistantRecommendationType.SmartPause);
        result.Value.ShouldInterruptGameplay.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeSessionAsync_WhenLookAwayNotProvided_UsesEyeTrackingSnapshot()
    {
        // Arrange
        var game = Game.Create("Resident Evil 4");
        SetupGame(game);
        _eyeTrackingMonitorMock.SetupGet(monitor => monitor.IsMonitoring).Returns(true);
        _eyeTrackingMonitorMock
            .Setup(monitor => monitor.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EyeTrackingSnapshot(
                CapturedAtUtc: _timeProvider.UtcNow,
                IsLookingAtScreen: false,
                LookAwayDurationSeconds: 7,
                Confidence: 0.91f,
                Source: "TestEyeTracker")));

        var sut = CreateSut();
        await sut.EnableSmartPauseAsync(new SmartPauseOptions(
            Enabled: true,
            LookAwayThresholdSeconds: 5,
            ResumeOnGazeReturn: true,
            RequireEyeTracking: false));

        var context = new SessionContext(
            GameId: game.Id,
            SessionStartTimeUtc: _timeProvider.UtcNow.AddMinutes(-10),
            RecentDeaths: 0,
            RecentRetries: 0,
            BreaksTaken: 0,
            InputPattern: new InputPattern(30, 0.08f, false, false),
            LookAwayDurationSeconds: null);

        // Act
        var result = await sut.AnalyzeSessionAsync(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(AssistantRecommendationType.SmartPause);
        _eyeTrackingMonitorMock.Verify(
            monitor => monitor.GetSnapshotAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeSessionAsync_WhenSessionTooLongWithoutBreak_ReturnsBreakReminder()
    {
        // Arrange
        var game = Game.Create("Stardew Valley");
        SetupGame(game);
        var sut = CreateSut();

        var context = new SessionContext(
            GameId: game.Id,
            SessionStartTimeUtc: _timeProvider.UtcNow.AddMinutes(-110),
            RecentDeaths: 1,
            RecentRetries: 1,
            BreaksTaken: 0,
            InputPattern: new InputPattern(20, 0.05f, false, false),
            LookAwayDurationSeconds: null);

        // Act
        var result = await sut.AnalyzeSessionAsync(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(AssistantRecommendationType.BreakReminder);
        result.Value.ShouldInterruptGameplay.Should().BeFalse();
    }

    private void SetupGame(Game game)
    {
        _gameRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                It.Is<GameId>(value => value.Value == game.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
    }

    private GameAssistantService CreateSut()
    {
        return new GameAssistantService(
            _aiOrchestratorMock.Object,
            _gameRepositoryMock.Object,
            _categorizationServiceMock.Object,
            _eyeTrackingMonitorMock.Object,
            _timeProvider,
            NullLogger<GameAssistantService>.Instance);
    }
}
