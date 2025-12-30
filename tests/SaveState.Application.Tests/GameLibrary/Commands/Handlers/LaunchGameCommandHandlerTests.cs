using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.Common;
using SaveState.Application.Common.Options;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Commands.Handlers;
using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.Tests.GameLibrary.Commands.Handlers;

public class LaunchGameCommandHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameValidationService> _validationServiceMock;
    private readonly Mock<IProcessLauncher> _processLauncherMock;
    private readonly Mock<ILogger<LaunchGameCommandHandler>> _loggerMock;
    private readonly LaunchGameCommandHandler _handler;
    private readonly Game _testGame;

    public LaunchGameCommandHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _validationServiceMock = new Mock<IGameValidationService>();
        _processLauncherMock = new Mock<IProcessLauncher>();
        _loggerMock = new Mock<ILogger<LaunchGameCommandHandler>>();

        _handler = new LaunchGameCommandHandler(
            _gameRepositoryMock.Object,
            _validationServiceMock.Object,
            _processLauncherMock.Object,
            _loggerMock.Object);

        _testGame = Game.Create("Test Game", null, "Test Description", "test-cover.jpg");
    }

    [Fact]
    public async Task Handle_WithNonExistentGame_ReturnsFailure()
    {
        // Arrange
        var command = new LaunchGameCommand { GameId = GameId.From(Guid.NewGuid()) };

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(command.GameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Game not found");
    }

    [Fact]
    public async Task Handle_WithGameThatCannotBeLaunched_ReturnsFailure()
    {
        // Arrange
        var command = new LaunchGameCommand { GameId = GameId.From(_testGame.Id) };

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(command.GameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testGame);

        _validationServiceMock
            .Setup(v => v.CanLaunchGameAsync(_testGame, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Game cannot be launched");
    }

    [Fact]
    public async Task Handle_WithValidGame_LaunchesSuccessfully()
    {
        // Arrange
        var command = new LaunchGameCommand { GameId = GameId.From(_testGame.Id) };
        var expectedProcessInfo = new ProcessInfo { ProcessId = 1234, ProcessName = "test.exe" };

        // Set up game with install path
        _testGame.SetInstallPath(@"C:\Games\TestGame");
        CreateTestExecutable(@"C:\Games\TestGame\game.exe");

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(command.GameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testGame);

        _validationServiceMock
            .Setup(v => v.CanLaunchGameAsync(_testGame, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _processLauncherMock
            .Setup(p => p.LaunchAsync(It.IsAny<LaunchConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProcessInfo);

        _gameRepositoryMock
            .Setup(r => r.UpdateAsync(_testGame, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedProcessInfo);

        // Verify game was marked as running and updated
        _testGame.Status.Should().Be(GameStatus.Running);

        _gameRepositoryMock.Verify(r => r.UpdateAsync(_testGame, It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithLaunchOptions_PassesOptionsToLauncher()
    {
        // Arrange
        var launchOptions = new LaunchOptions
        {
            Arguments = "--fullscreen",
            WorkingDirectory = @"C:\Custom\Dir",
            WaitForExit = true,
            Timeout = TimeSpan.FromMinutes(5)
        };

        var command = new LaunchGameCommand
        {
            GameId = GameId.From(_testGame.Id),
            Options = launchOptions
        };

        _testGame.SetInstallPath(@"C:\Games\TestGame");
        CreateTestExecutable(@"C:\Games\TestGame\game.exe");

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(command.GameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testGame);

        _validationServiceMock
            .Setup(v => v.CanLaunchGameAsync(_testGame, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _processLauncherMock
            .Setup(p => p.LaunchAsync(It.IsAny<LaunchConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessInfo { ProcessId = 1234, ProcessName = "game.exe" })
            .Callback<LaunchConfiguration, CancellationToken>((config, _) =>
            {
                config.Arguments.Should().Be(launchOptions.Arguments);
                config.WorkingDirectory.Should().Be(launchOptions.WorkingDirectory);
                config.WaitForExit.Should().Be(launchOptions.WaitForExit);
                config.Timeout.Should().Be(launchOptions.Timeout);
            });

        _gameRepositoryMock
            .Setup(r => r.UpdateAsync(_testGame, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _processLauncherMock.Verify(p => p.LaunchAsync(It.IsAny<LaunchConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLaunchFails_ReturnsFailureAndLogsError()
    {
        // Arrange
        var command = new LaunchGameCommand { GameId = GameId.From(_testGame.Id) };
        var launchException = new InvalidOperationException("Launch failed");

        _testGame.SetInstallPath(@"C:\Games\TestGame");
        CreateTestExecutable(@"C:\Games\TestGame\game.exe");

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(command.GameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testGame);

        _validationServiceMock
            .Setup(v => v.CanLaunchGameAsync(_testGame, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _processLauncherMock
            .Setup(p => p.LaunchAsync(It.IsAny<LaunchConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(launchException);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to launch game");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                launchException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToDependencies()
    {
        // Arrange
        var command = new LaunchGameCommand { GameId = GameId.From(_testGame.Id) };
        var cts = new CancellationTokenSource();

        _testGame.SetInstallPath(@"C:\Games\TestGame");
        CreateTestExecutable(@"C:\Games\TestGame\game.exe");

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(command.GameId, cts.Token))
            .ReturnsAsync(_testGame);

        _validationServiceMock
            .Setup(v => v.CanLaunchGameAsync(_testGame, cts.Token))
            .ReturnsAsync(true);

        _processLauncherMock
            .Setup(p => p.LaunchAsync(It.IsAny<LaunchConfiguration>(), cts.Token))
            .ReturnsAsync(new ProcessInfo { ProcessId = 1234, ProcessName = "game.exe" });

        _gameRepositoryMock
            .Setup(r => r.UpdateAsync(_testGame, cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _gameRepositoryMock.Verify(r => r.GetByIdAsync(command.GameId, cts.Token), Times.Once);
        _validationServiceMock.Verify(v => v.CanLaunchGameAsync(_testGame, cts.Token), Times.Once);
        _processLauncherMock.Verify(p => p.LaunchAsync(It.IsAny<LaunchConfiguration>(), cts.Token), Times.Once);
        _gameRepositoryMock.Verify(r => r.UpdateAsync(_testGame, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutInstallPath_ReturnsFailure()
    {
        // Arrange
        var command = new LaunchGameCommand { GameId = GameId.From(_testGame.Id) };

        // Game without install path
        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(command.GameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testGame);

        _validationServiceMock
            .Setup(v => v.CanLaunchGameAsync(_testGame, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to launch game");
    }

    private static void CreateTestExecutable(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create a dummy file to represent the executable
        File.WriteAllText(path, "dummy executable");
    }
}
