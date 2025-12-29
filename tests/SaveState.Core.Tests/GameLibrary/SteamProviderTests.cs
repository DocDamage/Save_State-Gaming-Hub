namespace SaveState.Core.Tests.GameLibrary;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Infrastructure.External;

public class SteamProviderTests
{
    private readonly Mock<ISteamApiClient> _mockClient = new();
    private readonly Mock<ILogger<SteamProvider>> _mockLogger = new();
    private readonly SteamProvider _sut;

    public SteamProviderTests()
    {
        _sut = new SteamProvider(_mockClient.Object, _mockLogger.Object);
    }

    [Fact]
    public void Name_ReturnsSteam()
    {
        _sut.Name.Should().Be("Steam");
    }

    [Fact]
    public void Capabilities_ReturnsAll()
    {
        _sut.Capabilities.Should().Be(ProviderCapabilities.All);
    }

    [Fact]
    public async Task GetInstalledGamesAsync_ReturnsGames_WhenApiSucceeds()
    {
        // Arrange
        var steamGames = new List<SteamGame>
        {
            new() { AppId = 220, Name = "Half-Life 2", InstallPath = @"C:\Games\HL2", PlayTimeMinutes = 240 }
        };
        _mockClient.Setup(x => x.GetOwnedGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(steamGames);

        // Act
        var result = await _sut.GetInstalledGamesAsync(default);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Half-Life 2");
        result[0].Source.Should().Be("Steam");
        result[0].SourceId.Should().Be("220");
        result[0].InstallPath.Should().Be(@"C:\Games\HL2");
        result[0].PlayTimeMinutes.Should().Be(240);
        result[0].Platform.Should().Be("PC");
    }

    [Fact]
    public async Task GetInstalledGamesAsync_ReturnsEmpty_WhenApiFails()
    {
        // Arrange
        _mockClient.Setup(x => x.GetOwnedGamesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SteamApiException("API unavailable"));

        // Act
        var result = await _sut.GetInstalledGamesAsync(default);

        // Assert
        result.Should().BeEmpty();
        _mockLogger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<SteamApiException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task GetGameMetadataAsync_DelegatesToApiClient()
    {
        // Arrange
        var expectedMetadata = new GameMetadata { Title = "Test Game" };
        _mockClient.Setup(x => x.GetGameDetailsAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        // Act
        var result = await _sut.GetGameMetadataAsync("123", default);

        // Assert
        result.Should().Be(expectedMetadata);
    }

    [Fact]
    public async Task LaunchGameAsync_DelegatesToApiClient()
    {
        // Arrange
        _mockClient.Setup(x => x.LaunchGameAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.LaunchGameAsync("123", default);

        // Assert
        result.Should().BeTrue();
    }
}
