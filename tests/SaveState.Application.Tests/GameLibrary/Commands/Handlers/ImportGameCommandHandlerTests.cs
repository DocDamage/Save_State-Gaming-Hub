using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.Common;
using SaveState.Application.Common.Events;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using Xunit;

namespace SaveState.Application.Tests.GameLibrary.Commands.Handlers;

public class ImportGameCommandHandlerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly Mock<IGameValidationService> _validationServiceMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler>> _loggerMock;
    private readonly SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler _handler;

    public ImportGameCommandHandlerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _validationServiceMock = new Mock<IGameValidationService>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler>>();

        _handler = new SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler(
            _gameRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _validationServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NewGame_ImportsSuccessfully()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        var command = new SaveState.Application.GameLibrary.Commands.ImportGameCommand
        {
            Title = "New Game",
            PlatformName = "PC",
            Description = "A great game",
            CoverImageUrl = "/images/cover.png",
            InstallPath = "C:\\Games\\NewGame"
        };

        _platformRepositoryMock.Setup(p => p.GetByNameAsync(PlatformName.From("PC"), default))
            .ReturnsAsync(platform);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<SaveState.Core.Common.ValueObjects.GameTitle>(), platform.Id, default))
            .ReturnsAsync((Game?)null);
        _validationServiceMock.Setup(v => v.IsValidGameAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(true);
        _gameRepositoryMock.Setup(g => g.AddAsync(It.IsAny<Game>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Result<GameId>>();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ExistingGame_ReturnsFailure()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        var existingGame = Game.Create("Existing Game", platform.Id);
        var command = new SaveState.Application.GameLibrary.Commands.ImportGameCommand
        {
            Title = "Existing Game",
            PlatformName = "PC"
        };

        _platformRepositoryMock.Setup(p => p.GetByNameAsync(PlatformName.From("PC"), default))
            .ReturnsAsync(platform);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<SaveState.Core.Common.ValueObjects.GameTitle>(), platform.Id, default))
            .ReturnsAsync(existingGame);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Result<GameId>>();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_PlatformNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new SaveState.Application.GameLibrary.Commands.ImportGameCommand
        {
            Title = "New Game",
            PlatformName = "NonExistent"
        };

        _platformRepositoryMock.Setup(p => p.GetByNameAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Platform?)null);
        _platformRepositoryMock.Setup(p => p.AddAsync(It.IsAny<Platform>(), default))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Result<GameId>>();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().ContainEquivalentOf("platform");
    }

    [Fact]
    public async Task Handle_InvalidGameData_ReturnsFailure()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        var command = new SaveState.Application.GameLibrary.Commands.ImportGameCommand
        {
            Title = "", // Invalid empty title
            PlatformName = "PC"
        };

        _platformRepositoryMock.Setup(p => p.GetByNameAsync(PlatformName.From("PC"), default))
            .ReturnsAsync(platform);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<SaveState.Core.Common.ValueObjects.GameTitle>(), platform.Id, default))
            .ReturnsAsync((Game?)null);
        _validationServiceMock.Setup(v => v.IsValidGameAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(false);
        _validationServiceMock.Setup(v => v.GetValidationErrorsAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(new[] { "Title is required" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Result<GameId>>();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().ContainEquivalentOf("validation");
    }

    [Fact]
    public async Task Handle_NewPlatform_CreatesPlatform()
    {
        // Arrange
        var command = new SaveState.Application.GameLibrary.Commands.ImportGameCommand
        {
            Title = "New Game",
            PlatformName = "NewPlatform"
        };

        _platformRepositoryMock.Setup(p => p.GetByNameAsync(PlatformName.From("NewPlatform"), default))
            .ReturnsAsync((Platform?)null);
        _platformRepositoryMock.Setup(p => p.AddAsync(It.IsAny<Platform>(), default))
            .Returns(Task.CompletedTask);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<SaveState.Core.Common.ValueObjects.GameTitle>(), It.IsAny<Guid>(), default))
            .ReturnsAsync((Game?)null);
        _validationServiceMock.Setup(v => v.IsValidGameAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(true);
        _gameRepositoryMock.Setup(g => g.AddAsync(It.IsAny<Game>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _platformRepositoryMock.Verify(p => p.AddAsync(It.IsAny<Platform>(), default), Times.Once);
    }
}
