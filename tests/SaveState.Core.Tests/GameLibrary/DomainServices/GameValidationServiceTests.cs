using FluentAssertions;
using Moq;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.ValueObjects;
using Xunit;

namespace SaveState.Core.Tests.GameLibrary.DomainServices;

public class GameValidationServiceTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly GameValidationService _service;

    public GameValidationServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _service = new GameValidationService(_fileSystemMock.Object);
    }

    [Fact]
    public async Task IsValidGame_ValidGame_ReturnsTrue()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), PlatformType.Computer);
        var game = Game.Create("Valid Game", platform.Id, "A valid game", "/images/cover.png");

        _fileSystemMock.Setup(fs => fs.DirectoryExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsValidGameAsync(game, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidGame_InvalidTitle_ThrowsImmediately()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), PlatformType.Computer);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Game.Create("", platform.Id));
    }

    [Fact]
    public async Task CanLaunchGame_InstalledGameWithValidPath_ReturnsTrue()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), PlatformType.Computer);
        var game = Game.Create("Test Game", platform.Id);
        game.SetInstallPath("C:\\Games\\TestGame");

        _fileSystemMock.Setup(fs => fs.DirectoryExistsAsync("C:\\Games\\TestGame", default))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(fs => fs.GetFilesAsync("C:\\Games\\TestGame", "*.exe", SearchOption.TopDirectoryOnly, default))
            .ReturnsAsync(new[] { "C:\\Games\\TestGame\\TestGame.exe" });
        _fileSystemMock.Setup(fs => fs.FileExistsAsync("C:\\Games\\TestGame\\TestGame.exe", default))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CanLaunchGameAsync(game, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanLaunchGame_NotInstalled_ReturnsFalse()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), PlatformType.Computer);
        var game = Game.Create("Test Game", platform.Id);

        // Act
        var result = await _service.CanLaunchGameAsync(game, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetValidationErrors_ValidGame_ReturnsEmptyList()
    {
        // Arrange
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), PlatformType.Computer);
        var game = Game.Create("Valid Game", platform.Id, "A valid game", "/images/cover.png");

        _fileSystemMock.Setup(fs => fs.DirectoryExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        // Act
        var errorsResult = await _service.GetValidationErrorsAsync(game, CancellationToken.None);

        // Assert
        errorsResult.IsSuccess.Should().BeTrue();
        errorsResult.Value.Should().BeEmpty();
    }
}
