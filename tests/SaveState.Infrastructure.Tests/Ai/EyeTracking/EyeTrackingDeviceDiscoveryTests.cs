using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Infrastructure.Assistant;

namespace SaveState.Infrastructure.Tests.AI.EyeTracking;

public class EyeTrackingDeviceDiscoveryTests : IDisposable
{
    private readonly EyeTrackingDeviceDiscoveryService _sut;

    public EyeTrackingDeviceDiscoveryTests()
    {
        _sut = new EyeTrackingDeviceDiscoveryService(
            NullLogger<EyeTrackingDeviceDiscoveryService>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public async Task DiscoverDevicesAsync_ReturnsListOfDevices()
    {
        // Act
        var result = await _sut.DiscoverDevicesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        // Result may be empty or contain devices depending on system
    }

    [Fact]
    public async Task DiscoverDevicesAsync_AfterDisposed_ReturnsFailure()
    {
        // Arrange
        _sut.Dispose();

        // Act
        var result = await _sut.DiscoverDevicesAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void IsAnyDeviceAvailable_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.IsAnyDeviceAvailable();
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
}
