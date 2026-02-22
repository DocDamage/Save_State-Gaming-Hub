using FluentAssertions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.ValueObjects;
using SaveState.Core.Social.Netplay;
using SaveState.Infrastructure.Social.Netplay;
using Xunit;

namespace SaveState.Core.Tests.Social.Netplay;

public class NetplayServiceTests
{
    private readonly Mock<IMatchmakingEngine> _matchmakingEngineMock;
    private readonly Mock<IRollbackNetcodeService> _rollbackServiceMock;
    private readonly Mock<ISpectatorRelayService> _spectatorServiceMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly RetroNetplayService _service;

    public NetplayServiceTests()
    {
        _matchmakingEngineMock = new Mock<IMatchmakingEngine>();
        _rollbackServiceMock = new Mock<IRollbackNetcodeService>();
        _spectatorServiceMock = new Mock<ISpectatorRelayService>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);

        _service = new RetroNetplayService(
            _matchmakingEngineMock.Object,
            _rollbackServiceMock.Object,
            _spectatorServiceMock.Object,
            _timeProviderMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<RetroNetplayService>>());
    }

    [Fact]
    public async Task GetCurrentSessionAsync_WhenNoSession_ReturnsNull()
    {
        // Act
        var result = await _service.GetCurrentSessionAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task JoinQueueAsync_WithValidRom_ReturnsTicket()
    {
        // Arrange
        var rom = CreateRomFile("abc123");
        var preferences = new MatchmakingPreferences("US-East", 1500, 300, 300, true);
        var expectedTicket = new MatchmakingTicket("ticket1", "abc123", "US-East", MatchmakingStatus.Queued, DateTime.UtcNow, 45);

        _matchmakingEngineMock.Setup(m => m.EnqueueAsync(It.IsAny<MatchmakingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchmakingTicket>.Success(expectedTicket));

        // Act
        var result = await _service.JoinQueueAsync(rom, preferences);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be("ticket1");
    }

    [Fact]
    public async Task JoinQueueAsync_WithNullRom_ReturnsFailure()
    {
        // Arrange
        var preferences = new MatchmakingPreferences("US-East");

        // Act
        var result = await _service.JoinQueueAsync(null!, preferences);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task LeaveQueueAsync_WithValidTicket_ReturnsSuccess()
    {
        // Arrange
        var rom = CreateRomFile("abc123");
        var preferences = new MatchmakingPreferences("US-East");
        var ticket = new MatchmakingTicket("ticket1", "abc123", "US-East", MatchmakingStatus.Queued, DateTime.UtcNow, 45);

        _matchmakingEngineMock.Setup(m => m.EnqueueAsync(It.IsAny<MatchmakingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MatchmakingTicket>.Success(ticket));
        _matchmakingEngineMock.Setup(m => m.DequeueAsync("ticket1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await _service.JoinQueueAsync(rom, preferences);

        // Act
        var result = await _service.LeaveQueueAsync("ticket1");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectToSessionAsync_WithValidSession_ReturnsConnection()
    {
        // Arrange
        var config = new RollbackConfiguration(8, 2, 1, true, 60, 16);
        var rollbackState = new RollbackState(true, 0, 0, 0, 2, DateTime.UtcNow);

        _rollbackServiceMock.Setup(r => r.InitializeAsync(It.IsAny<RollbackConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RollbackState>.Success(rollbackState));

        // Act
        var result = await _service.ConnectToSessionAsync("session1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SessionId.Should().Be("session1");
    }

    [Fact]
    public async Task GetConnectionQualityAsync_WhenConnected_ReturnsQuality()
    {
        // Arrange
        var config = new RollbackConfiguration(8, 2, 1, true, 60, 16);
        var rollbackState = new RollbackState(true, 0, 0, 0, 2, DateTime.UtcNow);

        _rollbackServiceMock.Setup(r => r.InitializeAsync(It.IsAny<RollbackConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RollbackState>.Success(rollbackState));

        await _service.ConnectToSessionAsync("session1");

        // Act
        var result = await _service.GetConnectionQualityAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Rating.Should().Be(ConnectionQualityRating.Excellent);
    }

    [Fact]
    public async Task GetConnectionQualityAsync_WhenNotConnected_ReturnsFailure()
    {
        // Act
        var result = await _service.GetConnectionQualityAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    private RomFile CreateRomFile(string checksum)
    {
        var rom = new RomFile(
            title: "Test ROM",
            platformId: Guid.NewGuid(),
            filePath: new FilePath(@"C:\roms\test.nes"),
            fileSize: 1024,
            timeProvider: _timeProviderMock.Object);
        rom.SetChecksum(checksum);
        return rom;
    }
}

public class MatchmakingEngineTests
{
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly MatchmakingEngine _engine;

    public MatchmakingEngineTests()
    {
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);

        _engine = new MatchmakingEngine(
            _timeProviderMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<MatchmakingEngine>>());
    }

    [Fact]
    public async Task EnqueueAsync_WithValidRequest_ReturnsTicket()
    {
        // Arrange
        var request = new MatchmakingRequest(
            "player1", "TestUser", "rom123", "US-East", 1500,
            new MatchmakingCriteria(300, 300, false, new[] { "US-East" }, true),
            DateTime.UtcNow);

        // Act
        var result = await _engine.EnqueueAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RomHash.Should().Be("rom123");
    }

    [Fact]
    public async Task ValidateRomCompatibilityAsync_WithIdenticalHashes_ReturnsCompatible()
    {
        // Arrange
        var hash1 = "abc123";
        var hash2 = "abc123";

        // Act
        var result = await _engine.ValidateRomCompatibilityAsync(hash1, hash2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCompatible.Should().BeTrue();
        result.Value.CompatibilityLevel.Should().Be(RomCompatibilityLevel.Identical);
    }

    [Fact]
    public async Task ValidateRomCompatibilityAsync_WithDifferentHashes_ReturnsIncompatible()
    {
        // Arrange
        var hash1 = "abc123";
        var hash2 = "def456";

        // Act
        var result = await _engine.ValidateRomCompatibilityAsync(hash1, hash2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCompatible.Should().BeFalse();
        result.Value.CompatibilityLevel.Should().Be(RomCompatibilityLevel.Incompatible);
    }

    [Fact]
    public async Task CalculateSkillMatchAsync_WithAcceptableDifference_ReturnsAcceptable()
    {
        // Arrange
        var player1Rating = 1500;
        var player2Rating = 1600;
        var maxDifference = 300;

        // Act
        var result = await _engine.CalculateSkillMatchAsync(player1Rating, player2Rating, maxDifference);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAcceptable.Should().BeTrue();
        result.Value.Difference.Should().Be(100);
    }

    [Fact]
    public async Task CalculateSkillMatchAsync_WithLargeDifference_ReturnsNotAcceptable()
    {
        // Arrange
        var player1Rating = 1500;
        var player2Rating = 2000;
        var maxDifference = 300;

        // Act
        var result = await _engine.CalculateSkillMatchAsync(player1Rating, player2Rating, maxDifference);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAcceptable.Should().BeFalse();
        result.Value.Difference.Should().Be(500);
    }

    [Fact]
    public async Task GetQueueStatisticsAsync_ReturnsStatistics()
    {
        // Act
        var result = await _engine.GetQueueStatisticsAsync("US-East");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Region.Should().Be("US-East");
    }

}
