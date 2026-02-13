using FluentAssertions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Social.Streaming;
using SaveState.Infrastructure.Social.Streaming;
using Xunit;

namespace SaveState.Infrastructure.Tests.Social;

public class StreamingStudioServiceTests
{
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly StreamingStudioService _service;

    public StreamingStudioServiceTests()
    {
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);

        _service = new StreamingStudioService(
            _timeProviderMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<StreamingStudioService>>());
    }

    [Fact]
    public async Task GetAvailablePlatformsAsync_ReturnsPlatforms()
    {
        // Act
        var result = await _service.GetAvailablePlatformsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain(p => p.Type == StreamingPlatformType.Twitch);
        result.Value.Should().Contain(p => p.Type == StreamingPlatformType.YouTube);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCode_ReturnsAuthResult()
    {
        // Act
        var result = await _service.AuthenticateAsync(StreamingPlatformType.Twitch, "auth_code_123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Platform.Should().Be(StreamingPlatformType.Twitch);
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StartStreamAsync_WithAuthenticatedPlatforms_ReturnsSession()
    {
        // Arrange
        await _service.AuthenticateAsync(StreamingPlatformType.Twitch, "auth_code");

        var config = new StreamConfiguration(
            "Test Stream",
            "Test Game",
            new List<StreamingPlatformType> { StreamingPlatformType.Twitch },
            new StreamQuality("1080p60", 1920, 1080, 60, 6000, 160),
            true,
            true,
            false);

        // Act
        var result = await _service.StartStreamAsync(config);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(StreamStatus.Live);
    }

    [Fact]
    public async Task StartStreamAsync_WithoutAuthentication_ReturnsFailure()
    {
        // Arrange
        var config = new StreamConfiguration(
            "Test Stream",
            "Test Game",
            new List<StreamingPlatformType> { StreamingPlatformType.Twitch },
            new StreamQuality("1080p60", 1920, 1080, 60, 6000, 160),
            true,
            true,
            false);

        // Act
        var result = await _service.StartStreamAsync(config);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task StopStreamAsync_WithActiveStream_ReturnsSuccess()
    {
        // Arrange
        await _service.AuthenticateAsync(StreamingPlatformType.Twitch, "auth_code");
        var config = new StreamConfiguration(
            "Test Stream",
            "Test Game",
            new List<StreamingPlatformType> { StreamingPlatformType.Twitch },
            new StreamQuality("1080p60", 1920, 1080, 60, 6000, 160),
            true,
            true,
            false);

        var session = await _service.StartStreamAsync(config);

        // Act
        var result = await _service.StopStreamAsync(session.Value!.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
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
    public async Task GetStreamHealthAsync_WithActiveStream_ReturnsHealth()
    {
        // Arrange
        await _service.AuthenticateAsync(StreamingPlatformType.Twitch, "auth_code");
        var config = new StreamConfiguration(
            "Test Stream",
            "Test Game",
            new List<StreamingPlatformType> { StreamingPlatformType.Twitch },
            new StreamQuality("1080p60", 1920, 1080, 60, 6000, 160),
            true,
            true,
            false);

        var session = await _service.StartStreamAsync(config);

        // Act
        var result = await _service.GetStreamHealthAsync(session.Value!.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Fps.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateStreamMetadataAsync_UpdatesMetadata()
    {
        // Arrange
        await _service.AuthenticateAsync(StreamingPlatformType.Twitch, "auth_code");
        var config = new StreamConfiguration(
            "Test Stream",
            "Test Game",
            new List<StreamingPlatformType> { StreamingPlatformType.Twitch },
            new StreamQuality("1080p60", 1920, 1080, 60, 6000, 160),
            true,
            true,
            false);

        var session = await _service.StartStreamAsync(config);
        var newMetadata = new StreamMetadata("New Title", "New Game", null, null, null, null);

        // Act
        var result = await _service.UpdateStreamMetadataAsync(session.Value!.Id, newMetadata);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
