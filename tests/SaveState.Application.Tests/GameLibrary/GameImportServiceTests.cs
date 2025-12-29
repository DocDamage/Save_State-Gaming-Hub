namespace SaveState.Application.Tests.GameLibrary;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SaveState.Application.GameLibrary.Services;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Application.Common.Events;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Events;
using SaveState.Core.Common.ValueObjects;

public class GameImportServiceTests
{
    private readonly Mock<IEnumerable<IGameProvider>> _mockProviders = new();
    private readonly Mock<IMetadataService> _mockMetadataService = new();
    private readonly Mock<IGameRepository> _mockGameRepository = new();
    private readonly Mock<IEventPublisher> _mockEventPublisher = new();
    private readonly Mock<ILogger<GameImportService>> _mockLogger = new();
    private readonly GameImportService _sut;

    public GameImportServiceTests()
    {
        _sut = new GameImportService(
            _mockProviders.Object,
            _mockMetadataService.Object,
            _mockGameRepository.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ImportAllLibrariesAsync_WithNoProviders_ReturnsEmptyResult()
    {
        // Arrange
        _mockProviders.Setup(p => p.GetEnumerator()).Returns(Enumerable.Empty<IGameProvider>().GetEnumerator());
        var options = new ImportOptions();

        // Act
        var result = await _sut.ImportAllLibrariesAsync(options, default);

        // Assert
        result.Should().NotBeNull();
        result.GamesImported.Should().Be(0);
        result.GamesFailed.Should().Be(0);
        result.GamesSkipped.Should().Be(0);
        result.ProviderResults.Should().BeEmpty();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ImportAllLibrariesAsync_WithSingleProvider_Succeeds()
    {
        // Arrange
        var mockProvider = new Mock<IGameProvider>();
        mockProvider.Setup(p => p.Name).Returns("Steam");
        mockProvider.Setup(p => p.GetInstalledGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameInfo>
            {
                new GameInfo { Title = "Half-Life 2", Source = "Steam", SourceId = "220" }
            });

        _mockProviders.Setup(p => p.GetEnumerator()).Returns(new[] { mockProvider.Object }.AsEnumerable().GetEnumerator());

        _mockGameRepository.Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<GameImportedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new ImportOptions();

        // Act
        var result = await _sut.ImportAllLibrariesAsync(options, default);

        // Assert
        result.GamesImported.Should().Be(1);
        result.GamesFailed.Should().Be(0);
        result.ProviderResults.Should().ContainKey("Steam");
        result.ProviderResults["Steam"].Success.Should().BeTrue();
        result.ProviderResults["Steam"].GamesFound.Should().Be(1);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ImportAllLibrariesAsync_WithProviderFailure_ContinuesImport()
    {
        // Arrange
        var failingProvider = new Mock<IGameProvider>();
        failingProvider.Setup(p => p.Name).Returns("FailingProvider");
        failingProvider.Setup(p => p.GetInstalledGamesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider error"));

        var successfulProvider = new Mock<IGameProvider>();
        successfulProvider.Setup(p => p.Name).Returns("Steam");
        successfulProvider.Setup(p => p.GetInstalledGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameInfo>
            {
                new GameInfo { Title = "Half-Life 2", Source = "Steam", SourceId = "220" }
            });

        _mockProviders.Setup(p => p.GetEnumerator())
            .Returns(new[] { failingProvider.Object, successfulProvider.Object }.AsEnumerable().GetEnumerator());

        _mockGameRepository.Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<GameImportedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new ImportOptions();

        // Act
        var result = await _sut.ImportAllLibrariesAsync(options, default);

        // Assert
        result.GamesImported.Should().Be(1);
        result.GamesFailed.Should().Be(0);
        result.ProviderResults.Should().HaveCount(2);
        result.ProviderResults["FailingProvider"].Success.Should().BeFalse();
        result.ProviderResults["FailingProvider"].Error.Should().Be("Provider error");
        result.ProviderResults["Steam"].Success.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }


    [Fact]
    public async Task ImportAllLibrariesAsync_WithMetadataEnrichment_EnrichesGame()
    {
        // Arrange
        var provider = new Mock<IGameProvider>();
        provider.Setup(p => p.Name).Returns("Steam");
        provider.Setup(p => p.GetInstalledGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameInfo>
            {
                new GameInfo { Title = "Half-Life 2", Source = "Steam", SourceId = "220" }
            });

        _mockProviders.Setup(p => p.GetEnumerator()).Returns(new[] { provider.Object }.AsEnumerable().GetEnumerator());

        var metadata = new GameMetadata
        {
            Title = "Half-Life 2",
            Description = "A great FPS game",
            Genres = new[] { "FPS", "Action" },
            Developer = "Valve",
            Publisher = "Valve"
        };

        _mockMetadataService.Setup(m => m.GetGameMetadataAsync("Half-Life 2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        _mockGameRepository.Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<GameImportedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new ImportOptions { SkipMetadata = false };

        // Act
        var result = await _sut.ImportAllLibrariesAsync(options, default);

        // Assert
        result.GamesImported.Should().Be(1);
        _mockMetadataService.Verify(m => m.GetGameMetadataAsync("Half-Life 2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAllLibrariesAsync_WithSkipMetadata_SkipsEnrichment()
    {
        // Arrange
        var provider = new Mock<IGameProvider>();
        provider.Setup(p => p.Name).Returns("Steam");
        provider.Setup(p => p.GetInstalledGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameInfo>
            {
                new GameInfo { Title = "Half-Life 2", Source = "Steam", SourceId = "220" }
            });

        _mockProviders.Setup(p => p.GetEnumerator()).Returns(new[] { provider.Object }.AsEnumerable().GetEnumerator());

        _mockGameRepository.Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<GameImportedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new ImportOptions { SkipMetadata = true };

        // Act
        var result = await _sut.ImportAllLibrariesAsync(options, default);

        // Assert
        result.GamesImported.Should().Be(1);
        _mockMetadataService.Verify(m => m.GetGameMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportAllLibrariesAsync_ReportsProgress()
    {
        // Arrange
        var progressReports = new List<ImportProgress>();
        var progress = new Progress<ImportProgress>(p => progressReports.Add(p));

        var provider = new Mock<IGameProvider>();
        provider.Setup(p => p.Name).Returns("Steam");
        provider.Setup(p => p.GetInstalledGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameInfo>
            {
                new GameInfo { Title = "Half-Life 2", Source = "Steam", SourceId = "220" }
            });

        _mockProviders.Setup(p => p.GetEnumerator()).Returns(new[] { provider.Object }.AsEnumerable().GetEnumerator());

        _mockGameRepository.Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(e => e.PublishAsync(It.IsAny<GameImportedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new ImportOptions();

        // Act
        await _sut.ImportAllLibrariesAsync(options, progress, default);

        // Assert
        progressReports.Should().NotBeEmpty();
        progressReports.Should().Contain(p => p.Stage == ImportStage.Discovery);
        progressReports.Should().Contain(p => p.Stage == ImportStage.Import);
        progressReports.Should().Contain(p => p.Stage == ImportStage.Complete);
    }
}
