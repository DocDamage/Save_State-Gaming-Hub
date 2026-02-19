using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Infrastructure.Assistant;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.AI.EyeTracking;

public class CompositeEyeTrackingProviderTests : IDisposable
{
    private readonly TestTimeProvider _timeProvider;
    private readonly List<Mock<IEyeTrackingMonitor>> _providerMocks;
    private readonly List<IEyeTrackingMonitor> _providers;

    public CompositeEyeTrackingProviderTests()
    {
        _timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 12, 0, 0, DateTimeKind.Utc));
        _providerMocks = new List<Mock<IEyeTrackingMonitor>>();
        _providers = new List<IEyeTrackingMonitor>();
    }

    public void Dispose()
    {
        // Dispose any real providers created during tests
        foreach (var provider in _providers.OfType<IDisposable>())
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void IsAvailable_WhenNoProvidersAvailable_ReturnsFalse()
    {
        // Arrange
        var mock = new Mock<IEyeTrackingMonitor>();
        mock.SetupGet(p => p.IsAvailable).Returns(false);
        _providers.Add(mock.Object);

        var sut = CreateSut();

        // Assert
        sut.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenAtLeastOneProviderAvailable_ReturnsTrue()
    {
        // Arrange
        var mock1 = new Mock<IEyeTrackingMonitor>();
        mock1.SetupGet(p => p.IsAvailable).Returns(false);
        
        var mock2 = new Mock<IEyeTrackingMonitor>();
        mock2.SetupGet(p => p.IsAvailable).Returns(true);
        
        _providers.Add(mock1.Object);
        _providers.Add(mock2.Object);

        var sut = CreateSut();

        // Assert
        sut.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task StartMonitoringAsync_WhenFirstProviderSucceeds_UsesFirstProvider()
    {
        // Arrange
        var mock1 = new Mock<IEyeTrackingMonitor>();
        mock1.SetupGet(p => p.IsAvailable).Returns(true);
        mock1.Setup(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        mock1.SetupGet(p => p.IsMonitoring).Returns(true);
        
        var mock2 = new Mock<IEyeTrackingMonitor>();
        mock2.SetupGet(p => p.IsAvailable).Returns(true);
        
        _providers.Add(mock1.Object);
        _providers.Add(mock2.Object);

        var sut = CreateSut();

        // Act
        var result = await sut.StartMonitoringAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock1.Verify(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()), Times.Once);
        mock2.Verify(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()), Times.Never);
        sut.ActiveProvider.Should().Be(mock1.Object);
    }

    [Fact]
    public async Task StartMonitoringAsync_WhenFirstProviderFails_TriesSecondProvider()
    {
        // Arrange
        var mock1 = new Mock<IEyeTrackingMonitor>();
        mock1.SetupGet(p => p.IsAvailable).Returns(true);
        mock1.Setup(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Failed", ErrorType.External));
        
        var mock2 = new Mock<IEyeTrackingMonitor>();
        mock2.SetupGet(p => p.IsAvailable).Returns(true);
        mock2.Setup(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        mock2.SetupGet(p => p.IsMonitoring).Returns(true);
        
        _providers.Add(mock1.Object);
        _providers.Add(mock2.Object);

        var sut = CreateSut();

        // Act
        var result = await sut.StartMonitoringAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock1.Verify(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()), Times.Once);
        mock2.Verify(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()), Times.Once);
        sut.ActiveProvider.Should().Be(mock2.Object);
    }

    [Fact]
    public async Task StartMonitoringAsync_WhenAllProvidersFail_ReturnsFailure()
    {
        // Arrange
        var mock1 = new Mock<IEyeTrackingMonitor>();
        mock1.SetupGet(p => p.IsAvailable).Returns(true);
        mock1.Setup(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Failed 1", ErrorType.External));
        
        var mock2 = new Mock<IEyeTrackingMonitor>();
        mock2.SetupGet(p => p.IsAvailable).Returns(true);
        mock2.Setup(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Failed 2", ErrorType.External));
        
        _providers.Add(mock1.Object);
        _providers.Add(mock2.Object);

        var sut = CreateSut();

        // Act
        var result = await sut.StartMonitoringAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotImplemented);
    }

    [Fact]
    public async Task StopMonitoringAsync_StopsActiveProvider()
    {
        // Arrange
        var mock1 = new Mock<IEyeTrackingMonitor>();
        mock1.SetupGet(p => p.IsAvailable).Returns(true);
        mock1.Setup(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        mock1.SetupGet(p => p.IsMonitoring).Returns(true);
        mock1.Setup(p => p.StopMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        
        _providers.Add(mock1.Object);

        var sut = CreateSut();
        await sut.StartMonitoringAsync();

        // Act
        var result = await sut.StopMonitoringAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        mock1.Verify(p => p.StopMonitoringAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenNotMonitoring_ReturnsFailure()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GetSnapshotAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenMonitoring_DelegatesToActiveProvider()
    {
        // Arrange
        var snapshot = new EyeTrackingSnapshot(
            CapturedAtUtc: _timeProvider.UtcNow,
            IsLookingAtScreen: true,
            LookAwayDurationSeconds: 0,
            Confidence: 0.95f,
            Source: "Test");

        var mock1 = new Mock<IEyeTrackingMonitor>();
        mock1.SetupGet(p => p.IsAvailable).Returns(true);
        mock1.Setup(p => p.StartMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        mock1.SetupGet(p => p.IsMonitoring).Returns(true);
        mock1.Setup(p => p.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(snapshot));
        
        _providers.Add(mock1.Object);

        var sut = CreateSut();
        await sut.StartMonitoringAsync();

        // Act
        var result = await sut.GetSnapshotAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(snapshot);
    }

    [Fact]
    public async Task GetAllSnapshotsAsync_ReturnsSnapshotsFromAllProviders()
    {
        // Arrange
        var snapshot1 = new EyeTrackingSnapshot(
            CapturedAtUtc: _timeProvider.UtcNow,
            IsLookingAtScreen: true,
            LookAwayDurationSeconds: 0,
            Confidence: 0.95f,
            Source: "Provider1");

        var mock1 = new Mock<IEyeTrackingMonitor>();
        mock1.SetupGet(p => p.IsAvailable).Returns(true);
        mock1.SetupGet(p => p.IsMonitoring).Returns(true);
        mock1.Setup(p => p.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(snapshot1));

        var mock2 = new Mock<IEyeTrackingMonitor>();
        mock2.SetupGet(p => p.IsAvailable).Returns(true);
        mock2.SetupGet(p => p.IsMonitoring).Returns(false);
        
        _providers.Add(mock1.Object);
        _providers.Add(mock2.Object);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAllSnapshotsAsync();
        var snapshots = result.Value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        snapshots.Should().HaveCount(2);
        snapshots![0].ProviderName.Should().Be(mock1.Object.GetType().Name);
        snapshots[0].Snapshot.Should().NotBeNull();
        snapshots[1].ProviderName.Should().Be(mock2.Object.GetType().Name);
        snapshots[1].Snapshot.Should().BeNull();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        sut.Dispose();
        var act = () => sut.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetSnapshotAsync_AfterDisposed_ReturnsFailure()
    {
        // Arrange
        var sut = CreateSut();
        sut.Dispose();

        // Act
        var result = await sut.GetSnapshotAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    private CompositeEyeTrackingProvider CreateSut()
    {
        return new CompositeEyeTrackingProvider(
            NullLogger<CompositeEyeTrackingProvider>.Instance,
            _timeProvider,
            _providers);
    }
}
