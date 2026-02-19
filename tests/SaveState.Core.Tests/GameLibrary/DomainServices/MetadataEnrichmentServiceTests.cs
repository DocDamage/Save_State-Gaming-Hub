using FluentAssertions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DomainServices;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.RomManagement;

namespace SaveState.Core.Tests.GameLibrary.DomainServices;

public class MetadataEnrichmentServiceTests
{
    private readonly Mock<IMetadataService> _metadataServiceMock;
    private readonly Mock<IPlatformRepository> _platformRepositoryMock;
    private readonly Mock<IPlatformExtensionRegistry> _extensionRegistryMock;
    private readonly MetadataEnrichmentService _service;
    private readonly Game _testGame;

    public MetadataEnrichmentServiceTests()
    {
        _metadataServiceMock = new Mock<IMetadataService>();
        _platformRepositoryMock = new Mock<IPlatformRepository>();
        _extensionRegistryMock = new Mock<IPlatformExtensionRegistry>();

        _service = new MetadataEnrichmentService(
            _metadataServiceMock.Object,
            _platformRepositoryMock.Object,
            _extensionRegistryMock.Object);

        _testGame = Game.Create("Test Game", null, "Original Description", "original-cover.jpg");
    }

    [Fact]
    public async Task EnrichGameMetadataAsync_WithValidMetadata_UpdatesGame()
    {
        // Arrange
        var enrichedMetadata = new GameMetadata
        {
            Title = "Enriched Game Title",
            Description = "Enriched description",
            CoverImageUrl = "https://example.com/enriched-cover.jpg",
            Genres = new[] { "Action", "Adventure" }
        };

        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrichedMetadata);

        // Act
        await _service.EnrichGameMetadataAsync(_testGame);

        // Assert
        _testGame.Title.Should().Be(enrichedMetadata.Title);
        _testGame.Description.Should().Be(enrichedMetadata.Description);
        _testGame.CoverImagePath.Should().Be(enrichedMetadata.CoverImageUrl);
    }

    [Fact]
    public async Task EnrichGameMetadataAsync_WithNullMetadata_DoesNotUpdateGame()
    {
        // Arrange
        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMetadata?)null);

        var originalTitle = _testGame.Title;
        var originalDescription = _testGame.Description;
        var originalCover = _testGame.CoverImagePath;

        // Act
        await _service.EnrichGameMetadataAsync(_testGame);

        // Assert - Game should remain unchanged
        _testGame.Title.Should().Be(originalTitle);
        _testGame.Description.Should().Be(originalDescription);
        _testGame.CoverImagePath.Should().Be(originalCover);
    }

    [Fact]
    public async Task EnrichGameMetadataAsync_WithEmptyMetadata_DoesNotUpdateGame()
    {
        // Arrange
        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GameMetadata.Empty);

        var originalTitle = _testGame.Title;
        var originalDescription = _testGame.Description;
        var originalCover = _testGame.CoverImagePath;

        // Act
        await _service.EnrichGameMetadataAsync(_testGame);

        // Assert - Game should remain unchanged
        _testGame.Title.Should().Be(originalTitle);
        _testGame.Description.Should().Be(originalDescription);
        _testGame.CoverImagePath.Should().Be(originalCover);
    }

    [Fact]
    public async Task GetCoverImageUrlAsync_WithValidMetadata_ReturnsUrl()
    {
        // Arrange
        const string expectedUrl = "https://example.com/cover.jpg";
        var metadata = new GameMetadata { CoverImageUrl = expectedUrl };

        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        // Act
        var result = await _service.GetCoverImageUrlAsync(_testGame);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task GetCoverImageUrlAsync_WithNullMetadata_ReturnsNull()
    {
        // Arrange
        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMetadata?)null);

        // Act
        var result = await _service.GetCoverImageUrlAsync(_testGame);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetTagsAsync_WithValidMetadata_ReturnsGenres()
    {
        // Arrange
        var expectedGenres = new[] { "Action", "RPG", "Adventure" };
        var metadata = new GameMetadata { Genres = expectedGenres };

        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        // Act
        var result = await _service.GetTagsAsync(_testGame);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedGenres);
    }

    [Fact]
    public async Task GetTagsAsync_WithNullMetadata_ReturnsEmptyCollection()
    {
        // Arrange
        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMetadata?)null);

        // Act
        var result = await _service.GetTagsAsync(_testGame);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDescriptionAsync_WithValidMetadata_ReturnsDescription()
    {
        // Arrange
        const string expectedDescription = "A fantastic game description";
        var metadata = new GameMetadata { Description = expectedDescription };

        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        // Act
        var result = await _service.GetDescriptionAsync(_testGame);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedDescription);
    }

    [Fact]
    public async Task GetDescriptionAsync_WithNullMetadata_ReturnsNull()
    {
        // Arrange
        _metadataServiceMock
            .Setup(s => s.GetGameMetadataAsync(_testGame.Title, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameMetadata?)null);

        // Act
        var result = await _service.GetDescriptionAsync(_testGame);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task DetectPlatformAsync_WithValidPathAndPlatform_ReturnsSuccess()
    {
        // Arrange
        const string gamePath = @"C:\Games\game.exe";
        const string detectedPlatformName = "PC";
        var platformName = PlatformName.From("PC");
        var platformShortName = PlatformShortName.From("PC");
        var expectedPlatform = new Platform(platformName, platformShortName, PlatformType.Computer);

        _extensionRegistryMock
            .Setup(r => r.DetectPlatformName(gamePath))
            .Returns(Result.Success<string>(detectedPlatformName));

        _platformRepositoryMock
            .Setup(r => r.GetByNameAsync(detectedPlatformName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPlatform);

        // Act
        var result = await _service.DetectPlatformAsync(gamePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedPlatform);
    }

    [Fact]
    public async Task DetectPlatformAsync_WithPlatformDetectionFailure_ReturnsFailure()
    {
        // Arrange
        const string gamePath = @"C:\Games\game.unknown";
        const string errorMessage = "Unknown file extension";

        _extensionRegistryMock
            .Setup(r => r.DetectPlatformName(gamePath))
            .Returns(Result.Failure<string>(errorMessage, ErrorType.Validation));

        // Act
        var result = await _service.DetectPlatformAsync(gamePath);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(errorMessage);
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task DetectPlatformAsync_WithPlatformNotInRepository_ReturnsFailure()
    {
        // Arrange
        const string gamePath = @"C:\Games\game.exe";
        const string detectedPlatformName = "UnknownPlatform";

        _extensionRegistryMock
            .Setup(r => r.DetectPlatformName(gamePath))
            .Returns(Result.Success<string>(detectedPlatformName));

        _platformRepositoryMock
            .Setup(r => r.GetByNameAsync(detectedPlatformName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        // Act
        var result = await _service.DetectPlatformAsync(gamePath);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Platform '{detectedPlatformName}' not found in repository");
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task AllMethods_AcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Set up mocks for the methods that need them
        var metadata = new GameMetadata { Title = "Test", Description = "Test game", CoverImageUrl = "test.jpg", Genres = new[] { "Action" } };
        _metadataServiceMock.Setup(s => s.GetGameMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(metadata);

        _extensionRegistryMock.Setup(r => r.DetectPlatformName(It.IsAny<string>())).Returns(Result.Success<string>("PC"));
        var platform = new Platform(PlatformName.From("PC"), PlatformShortName.From("PC"), PlatformType.Computer);
        _platformRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(platform);

        // Act & Assert - All methods should accept cancellation token parameter
        var enrichTask = _service.EnrichGameMetadataAsync(_testGame, cts.Token);
        var coverTask = _service.GetCoverImageUrlAsync(_testGame, cts.Token);
        var tagsTask = _service.GetTagsAsync(_testGame, cts.Token);
        var descTask = _service.GetDescriptionAsync(_testGame, cts.Token);
        var platformTask = _service.DetectPlatformAsync(@"C:\Games\test.exe", cts.Token);

        // Wait for all to complete
        await Task.WhenAll(enrichTask, coverTask, tagsTask, descTask, platformTask);

        // If we get here without exceptions, the test passes
        Assert.True(true);
    }
}

