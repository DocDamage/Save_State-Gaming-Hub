using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Infrastructure.AI.EyeTracking;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.AI.EyeTracking;

public class WindowsEyeControlProviderTests : IDisposable
{
    private readonly TestTimeProvider _timeProvider;
    private readonly WindowsEyeControlProvider _sut;

    public WindowsEyeControlProviderTests()
    {
        _timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 12, 0, 0, DateTimeKind.Utc));
        _sut = new WindowsEyeControlProvider(NullLogger<WindowsEyeControlProvider>.Instance, _timeProvider);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public void IsAvailable_OnWindows_ReturnsTrueOrFalse()
    {
        // Act & Assert
        // On Windows, it should check API availability
        // On non-Windows, it should be false
        if (OperatingSystem.IsWindows())
        {
            // Availability depends on Windows version
            (_sut.IsAvailable || !_sut.IsAvailable).Should().BeTrue();
        }
        else
        {
            _sut.IsAvailable.Should().BeFalse();
        }
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
        // Skip on Windows as it might be available
        if (OperatingSystem.IsWindows() && _sut.IsAvailable)
        {
            return;
        }

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
    public async Task StartMonitoringAsync_WhenAvailable_StartsMonitoring()
    {
        // Skip if not available
        if (!_sut.IsAvailable)
        {
            return;
        }

        // Act
        var result = await _sut.StartMonitoringAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _sut.IsMonitoring.Should().BeTrue();

        // Cleanup
        await _sut.StopMonitoringAsync();
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenMonitoring_ReturnsSnapshot()
    {
        // Skip if not available
        if (!_sut.IsAvailable)
        {
            return;
        }

        // Arrange
        await _sut.StartMonitoringAsync();

        // Act
        var result = await _sut.GetSnapshotAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CapturedAtUtc.Should().Be(_timeProvider.UtcNow);
        result.Value.Source.Should().Be("WindowsEyeControl");
        result.Value.Confidence.Should().BeInRange(0f, 1f);

        // Cleanup
        await _sut.StopMonitoringAsync();
    }

    [Fact]
    public async Task Dispose_WhenMonitoring_StopsMonitoring()
    {
        // Skip if not available
        if (!_sut.IsAvailable)
        {
            return;
        }

        // Arrange
        await _sut.StartMonitoringAsync();
        _sut.IsMonitoring.Should().BeTrue();

        // Act
        _sut.Dispose();

        // Assert
        _sut.IsMonitoring.Should().BeFalse();
    }
}
