using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Application.Common.Events;
using SaveState.Core.Common.ValueObjects;
using Xunit;

namespace SaveState.Application.Tests.GameLibrary;

/// <summary>
/// Tests for edge cases and error scenarios in game library operations.
/// </summary>
public class EdgeCaseTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock = new();
    private readonly Mock<IPlatformRepository> _platformRepositoryMock = new();
    private readonly Mock<IGameValidationService> _validationServiceMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();
    private readonly Mock<ILogger<SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler>> _loggerMock = new();

    [Fact]
    public async Task ImportGame_WithExtremelyLongTitle_HandlesGracefully()
    {
        // Arrange
        var veryLongTitle = new string('A', 1000); // 1000 character title
        var platformId = Guid.NewGuid();

        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);

        _platformRepositoryMock.Setup(p => p.GetByNameAsync("PC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<GameTitle>(), platformId, default))
            .ReturnsAsync((Game?)null);
        _validationServiceMock.Setup(v => v.IsValidGameAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(true);

        var handler = new SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler(
            _gameRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _validationServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);

        var command = new ImportGameCommand
        {
            Title = veryLongTitle,
            PlatformName = "PC",
            Description = "description",
            Source = "test-url",
            Tags = new[] { "Action" },
            CoverImageUrl = null
        };

        // Act
        var result = await handler.Handle(command, default);

        // Assert - Should handle gracefully by returning failure for invalid domain object
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Game title must be 1-200 characters");
    }

    [Fact]
    public async Task ImportGame_WithEmptyGenresArray_HandlesGracefully()
    {
        // Arrange
        var platformId = Guid.NewGuid();

        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);

        _platformRepositoryMock.Setup(p => p.GetByNameAsync("PC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<GameTitle>(), platformId, default))
            .ReturnsAsync((Game?)null);
        _validationServiceMock.Setup(v => v.IsValidGameAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(true);

        var handler = new SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler(
            _gameRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _validationServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);

        var command = new ImportGameCommand
        {
            Title = "Test Game",
            PlatformName = "PC",
            Description = "description",
            Source = "test-url",
            Tags = Array.Empty<string>(),
            CoverImageUrl = null
        };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ImportGame_WithUnicodeCharactersInTitle_HandlesCorrectly()
    {
        // Arrange
        var unicodeTitle = "游戏测试 🎮"; // Chinese characters and emoji
        var platformId = Guid.NewGuid();

        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);

        _platformRepositoryMock.Setup(p => p.GetByNameAsync("PC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<GameTitle>(), platformId, default))
            .ReturnsAsync((Game?)null);
        _validationServiceMock.Setup(v => v.IsValidGameAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(true);

        var handler = new SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler(
            _gameRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _validationServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);

        var command = new ImportGameCommand
        {
            Title = unicodeTitle,
            PlatformName = "PC",
            Description = "description",
            Source = "test-url",
            Tags = new[] { "Action" },
            CoverImageUrl = null
        };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _gameRepositoryMock.Verify(g => g.AddAsync(It.Is<Game>(game => game.Title.Contains("游戏")), default), Times.Once);
    }

    [Fact]
    public async Task GetGameDetails_WithNonExistentGame_ReturnsNull()
    {
        // Arrange
        var gameId = GameId.NewId();
        _gameRepositoryMock.Setup(g => g.GetByIdAsync(gameId, default))
            .ReturnsAsync((Game?)null);

        var handler = new SaveState.Application.GameLibrary.Queries.Handlers.GetGameDetailsQueryHandler(
            _gameRepositoryMock.Object);

        var query = new GetGameDetailsQuery { GameId = gameId };

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Game not found");
    }

    [Fact]
    public async Task ImportGame_WithDuplicateSourceId_FailsGracefully()
    {
        // Arrange
        var platformId = Guid.NewGuid();
        var sourceId = "duplicate-id";

        var platform = new Platform(PlatformName.From("Steam"), PlatformShortName.From("STM"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);

        _platformRepositoryMock.Setup(p => p.GetByNameAsync("Steam", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);

        var existingGame = Game.Create("Existing Game", platformId, source: "Steam", sourceId: sourceId);

        _platformRepositoryMock.Setup(p => p.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<GameTitle>(), platformId, default))
            .ReturnsAsync((Game?)null);
        _gameRepositoryMock.Setup(g => g.GetBySourceAndSourceIdAsync("Steam", sourceId, default))
            .ReturnsAsync(existingGame);

        var handler = new SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler(
            _gameRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _validationServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);

        var command = new ImportGameCommand
        {
            Title = "Different Title",
            PlatformName = "Steam",
            Description = "description",
            Source = "Steam",
            SourceId = sourceId,
            Tags = new[] { "Action" },
            CoverImageUrl = null
        };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task ImportGame_WithValidationFailure_ReturnsDetailedErrors()
    {
        // Arrange
        var platformId = Guid.NewGuid();

        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), Core.GameLibrary.Enums.PlatformType.Computer);
        typeof(Platform).GetProperty("Id")?.SetValue(platform, platformId);

        _platformRepositoryMock.Setup(p => p.GetByNameAsync("PC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _gameRepositoryMock.Setup(g => g.GetByTitleAndPlatformAsync(It.IsAny<GameTitle>(), platformId, default))
            .ReturnsAsync((Game?)null);
        _validationServiceMock.Setup(v => v.IsValidGameAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(false);
        _validationServiceMock.Setup(v => v.GetValidationErrorsAsync(It.IsAny<Game>(), default))
            .ReturnsAsync(new[] { "Invalid game title", "Missing required metadata" });

        var handler = new SaveState.Application.GameLibrary.Commands.Handlers.ImportGameCommandHandler(
            _gameRepositoryMock.Object,
            _platformRepositoryMock.Object,
            _validationServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);

        var command = new ImportGameCommand
        {
            Title = "Valid Title", // Passes GameTitle validation
            PlatformName = "PC",
            Description = "description",
            Source = "test-url",
            Tags = new[] { "Action" },
            CoverImageUrl = null
        };

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Validation failed");
        result.Error.Should().Contain("Invalid game title");
        result.Error.Should().Contain("Missing required metadata");
    }
}
