using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;
using SaveState.Infrastructure.Performance;
using System.Runtime.InteropServices;
using Xunit;

namespace SaveState.Infrastructure.Tests.Performance;

/// <summary>
/// Tests for AudioOptimizer Windows Core Audio API integration.
///
/// NOTE: These tests are skipped by default because they interact with real Windows
/// audio APIs, which can:
/// 1. Open the Windows Sound Settings panel
/// 2. Modify system audio configuration
/// 3. Cause test host instability
///
/// To run these tests manually, remove the Skip attributes and run in isolation.
/// See: docs/plans/STACK_OVERFLOW_FIX_PLAN_2026-01-17.md
/// </summary>
public class AudioOptimizerWindowsCoreAudioTests
{
    private readonly AudioOptimizer _audioOptimizer;

    public AudioOptimizerWindowsCoreAudioTests()
    {
        _audioOptimizer = new AudioOptimizer(NullLogger<AudioOptimizer>.Instance);
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task GetAvailableDevicesAsync_OnWindows_ShouldReturnRealDevices()
    {
        // Arrange & Act
        var result = await _audioOptimizer.GetAvailableDevicesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value);

        // On Windows, should have real devices
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Should have at least one real device with actual ID
            Assert.Contains(result.Value, d => !string.IsNullOrEmpty(d.Id) && d.Id != "default");

            // Should have IsDefault set on at least one device
            Assert.Contains(result.Value, d => d.IsDefault);
        }
        else
        {
            // On non-Windows, should have fallback default device
            Assert.Single(result.Value);
            Assert.Equal("default", result.Value.First().Id);
        }
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task SetTemporaryDeviceAsync_OnNonWindows_ShouldReturnNotImplemented()
    {
        // Skip this test on Windows as we want to test the non-Windows path
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var deviceId = "test-device";

        // Act
        var result = await _audioOptimizer.SetTemporaryDeviceAsync(deviceId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotImplemented, result.ErrorType);
        Assert.Contains("Windows", result.Error ?? string.Empty);
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task SetTemporaryDeviceAsync_WithInvalidDevice_ShouldReturnFailure()
    {
        // Only test on Windows where the feature is implemented
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var invalidDeviceId = "invalid-device-id-12345";

        // Act
        var result = await _audioOptimizer.SetTemporaryDeviceAsync(invalidDeviceId);

        // Assert
        // Should fail because device doesn't exist
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.ExternalService, result.ErrorType);
    }

    [Fact(Skip = "Integration test - requires admin permissions and modifies system settings")]
    public async Task SetTemporaryDeviceAsync_WithValidDevice_ShouldSucceed()
    {
        // Only test on Windows where the feature is implemented
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        // First get a real device
        var devicesResult = await _audioOptimizer.GetAvailableDevicesAsync();
        Assert.True(devicesResult.IsSuccess);

        var device = devicesResult.Value!.First(d => d.IsEnabled);

        // Act
        var result = await _audioOptimizer.SetTemporaryDeviceAsync(device.Id);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task GetAvailableDevicesAsync_DevicesShouldHaveValidProperties()
    {
        // Arrange & Act
        var result = await _audioOptimizer.GetAvailableDevicesAsync();

        // Assert
        Assert.True(result.IsSuccess);

        foreach (var device in result.Value!)
        {
            Assert.False(string.IsNullOrWhiteSpace(device.Id));
            Assert.False(string.IsNullOrWhiteSpace(device.Name));
            Assert.NotEqual(AudioDeviceType.Other, device.Type); // Should categorize device type
        }
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task GetCurrentSettingsAsync_ShouldReturnValidSettings()
    {
        // Arrange & Act
        var result = await _audioOptimizer.GetCurrentSettingsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var settings = result.Value;
        Assert.True(settings.SampleRate > 0);
        Assert.True(settings.BitDepth > 0);
        Assert.True(settings.BufferSize > 0);
        Assert.True(settings.Channels > 0);
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task CreateGameProfileAsync_ShouldCreateValidProfile()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var settings = new AudioSettings(
            SampleRate: 48000,
            BitDepth: 24,
            BufferSize: 480,
            Channels: 2,
            ExclusiveMode: true,
            SpatialAudio: false,
            LatencyMode: AudioLatencyMode.Low);

        // Act
        var result = await _audioOptimizer.CreateGameProfileAsync(gameId, settings);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(gameId, result.Value.GameId);
        Assert.Equal(settings, result.Value.Settings);
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task ApplyProfileAsync_WithValidProfile_ShouldSucceed()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var settings = AudioOptimizer.Presets.LowLatencyGaming;
        var createResult = await _audioOptimizer.CreateGameProfileAsync(gameId, settings);
        Assert.True(createResult.IsSuccess);

        // Act
        var applyResult = await _audioOptimizer.ApplyProfileAsync(createResult.Value!.Id);

        // Assert
        Assert.True(applyResult.IsSuccess);
        Assert.NotNull(createResult.Value.LastAppliedAt);
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task RevertSettingsAsync_WithoutApplying_ShouldFail()
    {
        // Arrange & Act
        var result = await _audioOptimizer.RevertSettingsAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact(Skip = "Integration test - interacts with real Windows audio APIs")]
    public async Task RevertSettingsAsync_AfterApplyingProfile_ShouldSucceed()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var settings = AudioOptimizer.Presets.CompetitiveGaming;
        var createResult = await _audioOptimizer.CreateGameProfileAsync(gameId, settings);
        await _audioOptimizer.ApplyProfileAsync(createResult.Value!.Id);

        // Act
        var revertResult = await _audioOptimizer.RevertSettingsAsync();

        // Assert
        Assert.True(revertResult.IsSuccess);
    }

    [Theory(Skip = "Integration test - interacts with real Windows audio APIs")]
    [InlineData(AudioLatencyMode.UltraLow)]
    [InlineData(AudioLatencyMode.Low)]
    [InlineData(AudioLatencyMode.Balanced)]
    [InlineData(AudioLatencyMode.Default)]
    public async Task Presets_AllPresets_ShouldApplySuccessfully(AudioLatencyMode mode)
    {
        // Arrange
        var gameId = Guid.NewGuid();
        AudioSettings settings = mode switch
        {
            AudioLatencyMode.UltraLow => AudioOptimizer.Presets.LowLatencyGaming,
            AudioLatencyMode.Low => AudioOptimizer.Presets.CompetitiveGaming,
            AudioLatencyMode.Balanced => AudioOptimizer.Presets.CinematicGaming,
            _ => AudioOptimizer.Presets.Default
        };

        // Act
        var createResult = await _audioOptimizer.CreateGameProfileAsync(gameId, settings);
        var applyResult = await _audioOptimizer.ApplyProfileAsync(createResult.Value!.Id);

        // Assert
        Assert.True(createResult.IsSuccess);
        Assert.True(applyResult.IsSuccess);
        Assert.Equal(mode, createResult.Value.Settings.LatencyMode);
    }
}

