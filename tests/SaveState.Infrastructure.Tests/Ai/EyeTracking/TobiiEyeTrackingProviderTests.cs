using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Infrastructure.AI.EyeTracking;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.AI.EyeTracking;

public class TobiiEyeTrackingProviderTests
{
    private readonly TestTimeProvider _timeProvider;
    private readonly TobiiEyeTrackingProvider _sut;

    public TobiiEyeTrackingProviderTests()
    {
        _timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 12, 0, 0, DateTimeKind.Utc));
        _sut = new TobiiEyeTrackingProvider(NullLogger<TobiiEyeTrackingProvider>.Instance, _timeProvider);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public void IsAvailable_WhenSdkNotInstalled_ReturnsFalse()
    {
        // Assert
        // Since we don't have the actual Tobii SDK installed in test environment
        _sut.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsMonitoring_Initially_ReturnsFalse()
    {
        // Assert
        _sut.IsMonitoring.Should().BeFalse();
    }

    [Fact]
    public async Task StartMonitoringAsync_WhenNotAvailable_ReturnsNotImplemented()
    {
        // Act
        var result = await _sut.StartMonitoringAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotImplemented);
    }

    [Fact]
    public async Task StopMonitoringAsync_WhenNotMonitoring_ReturnsSuccess()
    {
        // Act
        var result = await _sut.StopMonitoringAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenNotMonitoring_ReturnsValidationFailure()
    {
        // Act
        var result = await _sut.GetSnapshotAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Dispose_WhenNotMonitoring_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert
        _sut.Dispose();
        var act = () => _sut.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetSnapshotAsync_AfterDisposed_ReturnsValidationFailure()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var result = await _sut.GetSnapshotAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Contain("disposed");
    }

    [Fact]
    public async Task StartMonitoringAsync_AfterDisposed_ReturnsValidationFailure()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var result = await _sut.StartMonitoringAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Should().Contain("disposed");
    }
}
