using FluentAssertions;
using Moq;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Commands.Handlers;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.Tests.GameLibrary.Commands.Handlers;

public class CreateAchievementCommandHandlerTests
{
    private readonly Mock<IAchievementRepository> _achievementRepositoryMock;
    private readonly CreateAchievementCommandHandler _handler;

    public CreateAchievementCommandHandlerTests()
    {
        _achievementRepositoryMock = new Mock<IAchievementRepository>();
        _handler = new CreateAchievementCommandHandler(_achievementRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesAchievementAndReturnsId()
    {
        // Arrange
        var command = new CreateAchievementCommand(
            Name: "First Victory",
            Description: "Win your first game",
            IconPath: "/icons/victory.png",
            Points: 10,
            Type: AchievementType.GameCompletion);

        Achievement? capturedAchievement = null;
        _achievementRepositoryMock
            .Setup(r => r.AddAchievementAsync(It.IsAny<Achievement>(), It.IsAny<CancellationToken>()))
            .Callback<Achievement, CancellationToken>((achievement, _) => capturedAchievement = achievement)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        capturedAchievement.Should().NotBeNull();
        capturedAchievement!.Name.Should().Be(command.Name);
        capturedAchievement.Description.Should().Be(command.Description);
        capturedAchievement.IconPath.Should().Be(command.IconPath);
        capturedAchievement.Points.Should().Be(command.Points);
        capturedAchievement.Type.Should().Be(command.Type);
        capturedAchievement.Criteria.Should().BeNull();

        _achievementRepositoryMock.Verify(
            r => r.AddAchievementAsync(It.IsAny<Achievement>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCriteria_SetsCriteriaOnAchievement()
    {
        // Arrange
        var command = new CreateAchievementCommand(
            Name: "Speed Runner",
            Description: "Complete a game in under 30 minutes",
            IconPath: "/icons/speed.png",
            Points: 25,
            Type: AchievementType.PlayTime,
            Criteria: "CompleteTime < 30");

        Achievement? capturedAchievement = null;
        _achievementRepositoryMock
            .Setup(r => r.AddAchievementAsync(It.IsAny<Achievement>(), It.IsAny<CancellationToken>()))
            .Callback<Achievement, CancellationToken>((achievement, _) => capturedAchievement = achievement)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedAchievement.Should().NotBeNull();
        capturedAchievement!.Criteria.Should().Be(command.Criteria);
    }

    [Fact]
    public async Task Handle_WithNullCriteria_DoesNotSetCriteria()
    {
        // Arrange
        var command = new CreateAchievementCommand(
            Name: "Explorer",
            Description: "Discover 10 hidden areas",
            IconPath: "/icons/explorer.png",
            Points: 15,
            Type: AchievementType.Special,
            Criteria: null);

        Achievement? capturedAchievement = null;
        _achievementRepositoryMock
            .Setup(r => r.AddAchievementAsync(It.IsAny<Achievement>(), It.IsAny<CancellationToken>()))
            .Callback<Achievement, CancellationToken>((achievement, _) => capturedAchievement = achievement)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedAchievement.Should().NotBeNull();
        capturedAchievement!.Criteria.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyCriteria_DoesNotSetCriteria()
    {
        // Arrange
        var command = new CreateAchievementCommand(
            Name: "Collector",
            Description: "Collect 100 items",
            IconPath: "/icons/collector.png",
            Points: 20,
            Type: AchievementType.Collection,
            Criteria: string.Empty);

        Achievement? capturedAchievement = null;
        _achievementRepositoryMock
            .Setup(r => r.AddAchievementAsync(It.IsAny<Achievement>(), It.IsAny<CancellationToken>()))
            .Callback<Achievement, CancellationToken>((achievement, _) => capturedAchievement = achievement)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedAchievement.Should().NotBeNull();
        capturedAchievement!.Criteria.Should().BeNull();
    }

    [Theory]
    [InlineData(AchievementType.GameCompletion)]
    [InlineData(AchievementType.PlayTime)]
    [InlineData(AchievementType.Collection)]
    [InlineData(AchievementType.Social)]
    [InlineData(AchievementType.Special)]
    public async Task Handle_WithDifferentAchievementTypes_CreatesCorrectly(AchievementType achievementType)
    {
        // Arrange
        var command = new CreateAchievementCommand(
            Name: "Test Achievement",
            Description: "Test Description",
            IconPath: "/icons/test.png",
            Points: 5,
            Type: achievementType);

        Achievement? capturedAchievement = null;
        _achievementRepositoryMock
            .Setup(r => r.AddAchievementAsync(It.IsAny<Achievement>(), It.IsAny<CancellationToken>()))
            .Callback<Achievement, CancellationToken>((achievement, _) => capturedAchievement = achievement)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedAchievement.Should().NotBeNull();
        capturedAchievement!.Type.Should().Be(achievementType);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var command = new CreateAchievementCommand(
            Name: "Test",
            Description: "Test",
            IconPath: "/test.png",
            Points: 1,
            Type: AchievementType.GameCompletion);

        var cts = new CancellationTokenSource();

        _achievementRepositoryMock
            .Setup(r => r.AddAchievementAsync(It.IsAny<Achievement>(), cts.Token))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _achievementRepositoryMock.Verify(
            r => r.AddAchievementAsync(It.IsAny<Achievement>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AlwaysReturnsGuid()
    {
        // Arrange
        var command = new CreateAchievementCommand(
            Name: "Test",
            Description: "Test",
            IconPath: "/test.png",
            Points: 1,
            Type: AchievementType.GameCompletion);

        _achievementRepositoryMock
            .Setup(r => r.AddAchievementAsync(It.IsAny<Achievement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().NotBe(Guid.Empty);
    }
}
