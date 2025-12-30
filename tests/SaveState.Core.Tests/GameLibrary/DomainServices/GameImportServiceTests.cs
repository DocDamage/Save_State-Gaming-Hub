using FluentAssertions;
using Moq;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Core.Tests.GameLibrary.DomainServices;

public class GameImportServiceTests
{
    private readonly Mock<IGameProvider> _steamProviderMock;
    private readonly Mock<IGameProvider> _gogProviderMock;
    private readonly Mock<IGameProvider> _epicProviderMock;
    private readonly GameImportService _service;

    public GameImportServiceTests()
    {
        _steamProviderMock = new Mock<IGameProvider>();
        _steamProviderMock.Setup(p => p.Name).Returns("Steam");

        _gogProviderMock = new Mock<IGameProvider>();
        _gogProviderMock.Setup(p => p.Name).Returns("GOG");

        _epicProviderMock = new Mock<IGameProvider>();
        _epicProviderMock.Setup(p => p.Name).Returns("Epic");

        var providers = new[] { _steamProviderMock.Object, _gogProviderMock.Object, _epicProviderMock.Object };
        _service = new GameImportService(providers);
    }

    [Fact]
    public async Task ImportGameFromSteamAsync_WithValidSteamId_ReturnsGame()
    {
        // Arrange
        const string steamAppId = "12345";
        var expectedMetadata = new GameMetadata
        {
            Title = "Test Game",
            Description = "A test game",
            CoverImageUrl = "https://example.com/cover.jpg"
        };

        _steamProviderMock
            .Setup(p => p.GetGameMetadataAsync(steamAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        // Act
        var game = await _service.ImportGameFromSteamAsync(steamAppId);

        // Assert
        game.Title.Should().Be(expectedMetadata.Title);
        game.Description.Should().Be(expectedMetadata.Description);
        game.CoverImagePath.Should().Be(expectedMetadata.CoverImageUrl);
        game.Source.Should().Be("Steam");
        game.SourceId.Should().Be(steamAppId);
        game.PlatformId.Should().BeNull();
    }

    [Fact]
    public async Task ImportGameFromSteamAsync_WithSteamProviderNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new GameImportService(Enumerable.Empty<IGameProvider>());
        const string steamAppId = "12345";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ImportGameFromSteamAsync(steamAppId));
    }

    [Fact]
    public async Task ImportGameFromGogAsync_WithValidGogId_ReturnsGame()
    {
        // Arrange
        const string gogId = "54321";
        var expectedMetadata = new GameMetadata
        {
            Title = "GOG Game",
            Description = "A GOG game",
            CoverImageUrl = "https://gog.com/cover.jpg"
        };

        _gogProviderMock
            .Setup(p => p.GetGameMetadataAsync(gogId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        // Act
        var game = await _service.ImportGameFromGogAsync(gogId);

        // Assert
        game.Title.Should().Be(expectedMetadata.Title);
        game.Description.Should().Be(expectedMetadata.Description);
        game.CoverImagePath.Should().Be(expectedMetadata.CoverImageUrl);
        game.Source.Should().Be("GOG");
        game.SourceId.Should().Be(gogId);
    }

    [Fact]
    public async Task ImportGameFromGogAsync_WithGogProviderNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new GameImportService(Enumerable.Empty<IGameProvider>());
        const string gogId = "54321";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ImportGameFromGogAsync(gogId));
    }

    [Fact]
    public async Task ImportGameFromEpicAsync_WithValidEpicId_ReturnsGame()
    {
        // Arrange
        const string epicId = "epic123";
        var expectedMetadata = new GameMetadata
        {
            Title = "Epic Game",
            Description = "An Epic game",
            CoverImageUrl = "https://epic.com/cover.jpg"
        };

        _epicProviderMock
            .Setup(p => p.GetGameMetadataAsync(epicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        // Act
        var game = await _service.ImportGameFromEpicAsync(epicId);

        // Assert
        game.Title.Should().Be(expectedMetadata.Title);
        game.Description.Should().Be(expectedMetadata.Description);
        game.CoverImagePath.Should().Be(expectedMetadata.CoverImageUrl);
        game.Source.Should().Be("Epic");
        game.SourceId.Should().Be(epicId);
    }

    [Fact]
    public async Task ImportGameFromEpicAsync_WithEpicProviderNotAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = new GameImportService(Enumerable.Empty<IGameProvider>());
        const string epicId = "epic123";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ImportGameFromEpicAsync(epicId));
    }

    [Fact]
    public async Task ImportGameFromDirectoryAsync_WithValidPath_ReturnsGame()
    {
        // Arrange
        const string gamePath = @"C:\Games\TestGame";
        const string expectedTitle = "TestGame";

        // Act
        var game = await _service.ImportGameFromDirectoryAsync(gamePath);

        // Assert
        game.Title.Should().Be(expectedTitle);
        game.InstallPath.Should().Be(gamePath);
        game.Source.Should().BeNull();
        game.SourceId.Should().BeNull();
        game.PlatformId.Should().BeNull();
        game.Description.Should().BeNull();
        game.CoverImagePath.Should().BeNull();
    }

    [Theory]
    [InlineData(@"C:\Games\My Game", "My Game")]
    [InlineData(@"C:\Games\AnotherGame\", "AnotherGame")]
    [InlineData(@"/games/linux-game", "linux-game")]
    public async Task ImportGameFromDirectoryAsync_WithVariousPaths_ExtractsCorrectTitle(string gamePath, string expectedTitle)
    {
        // Act
        var game = await _service.ImportGameFromDirectoryAsync(gamePath);

        // Assert
        game.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public async Task ImportGameManuallyAsync_WithValidTitle_ReturnsGame()
    {
        // Arrange
        const string title = "Manual Game";
        const string description = "Manually imported game";

        // Act
        var game = await _service.ImportGameManuallyAsync(title, description);

        // Assert
        game.Title.Should().Be(title);
        game.Description.Should().Be(description);
        game.Source.Should().BeNull();
        game.SourceId.Should().BeNull();
        game.PlatformId.Should().BeNull();
        game.InstallPath.Should().BeNull();
        game.CoverImagePath.Should().BeNull();
    }

    [Fact]
    public async Task ImportGameManuallyAsync_WithTitleOnly_ReturnsGame()
    {
        // Arrange
        const string title = "Manual Game";

        // Act
        var game = await _service.ImportGameManuallyAsync(title);

        // Assert
        game.Title.Should().Be(title);
        game.Description.Should().BeNull();
    }

    [Fact]
    public async Task ImportMethods_AcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Set up mocks to return valid metadata so the methods don't fail due to missing providers
        var metadata = new GameMetadata { Title = "Test", Description = "Test game" };
        _steamProviderMock.Setup(p => p.GetGameMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(metadata);
        _gogProviderMock.Setup(p => p.GetGameMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(metadata);
        _epicProviderMock.Setup(p => p.GetGameMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(metadata);

        // Act & Assert - All methods should accept cancellation token parameter
        var steamTask = _service.ImportGameFromSteamAsync("123", cts.Token);
        var gogTask = _service.ImportGameFromGogAsync("456", cts.Token);
        var epicTask = _service.ImportGameFromEpicAsync("789", cts.Token);
        var directoryTask = _service.ImportGameFromDirectoryAsync(@"C:\Games\Test", cts.Token);
        var manualTask = _service.ImportGameManuallyAsync("Test Game", "Description", cts.Token);

        // Wait for all to complete
        await Task.WhenAll(steamTask, gogTask, epicTask, directoryTask, manualTask);

        // If we get here without exceptions, the test passes
        Assert.True(true);
    }
}
