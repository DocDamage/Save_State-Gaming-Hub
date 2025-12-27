using Xunit;
using SaveState.Core.Services;
using SaveState.Core.Services.Netplay;
using SaveState.Core.Services.Input;

namespace SaveState.Tests;

/// <summary>
/// Service Configuration Tests - verifies core services are properly configured
/// </summary>
public class ServiceConfigurationTests
{
    [Fact]
    public void NetplayService_Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = NetplayService.Instance;
        var instance2 = NetplayService.Instance;

        // Assert
        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void SpectatorService_Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = SpectatorService.Instance;
        var instance2 = SpectatorService.Instance;

        // Assert
        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void HotkeyService_Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = HotkeyService.Instance;
        var instance2 = HotkeyService.Instance;

        // Assert
        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void GamepadService_Instance_IsSingleton()
    {
        // Arrange & Act
        var instance1 = GamepadService.Instance;
        var instance2 = GamepadService.Instance;

        // Assert
        Assert.NotNull(instance1);
        Assert.Same(instance1, instance2);
    }
}

/// <summary>
/// Netplay Service Tests
/// </summary>
public class NetplayServiceTests
{
    [Fact]
    public void NetplayService_InitialState_IsDisconnected()
    {
        // Arrange
        var service = NetplayService.Instance;

        // Assert
        Assert.False(service.IsConnected);
        Assert.False(service.IsHost);
        Assert.Null(service.CurrentSession);
    }
}

/// <summary>
/// Spectator Service Tests
/// </summary>
public class SpectatorServiceTests
{
    [Fact]
    public void SpectatorService_InitialState_IsNotStreaming()
    {
        // Arrange
        var service = SpectatorService.Instance;

        // Assert
        Assert.False(service.IsStreaming);
        Assert.False(service.IsWatching);
        Assert.Equal(0, service.ViewerCount);
    }

    [Fact]
    public void SpectatorService_StartStreaming_IsStreaming()
    {
        // Arrange
        var service = SpectatorService.Instance;

        // Act
        service.StartStreaming();

        // Assert
        Assert.True(service.IsStreaming);

        // Cleanup
        service.StopStreaming();
    }

    [Fact]
    public void SpectatorService_StopStreaming_IsNotStreaming()
    {
        // Arrange
        var service = SpectatorService.Instance;
        service.StartStreaming();

        // Act
        service.StopStreaming();

        // Assert
        Assert.False(service.IsStreaming);
    }

    [Fact]
    public void SpectatorService_SetBufferDelay_ClampedWithinRange()
    {
        // Arrange
        var service = SpectatorService.Instance;

        // Act - test clamping to minimum
        service.SetBufferDelay(1);
        Assert.Equal(10, service.BufferDelay);

        // Act - test clamping to maximum
        service.SetBufferDelay(200);
        Assert.Equal(120, service.BufferDelay);

        // Act - test valid value
        service.SetBufferDelay(60);
        Assert.Equal(60, service.BufferDelay);
    }
}

/// <summary>
/// Hotkey Service Tests
/// </summary>
public class HotkeyServiceTests
{
    [Fact]
    public void HotkeyService_GetAllBindings_ReturnsDefaultBindings()
    {
        // Arrange
        var service = HotkeyService.Instance;

        // Act
        var bindings = service.GetAllBindings();

        // Assert
        Assert.NotEmpty(bindings);
    }

    [Fact]
    public void HotkeyService_GetBinding_ReturnsCorrectBinding()
    {
        // Arrange
        var service = HotkeyService.Instance;

        // Act
        var binding = service.GetBinding(HotkeyAction.OpenLibrary);

        // Assert
        Assert.NotNull(binding);
        Assert.Equal(HotkeyAction.OpenLibrary, binding.Action);
    }

    [Fact]
    public void HotkeyService_GetActionDescription_ReturnsDescription()
    {
        // Arrange
        var service = HotkeyService.Instance;

        // Act
        var description = service.GetActionDescription(HotkeyAction.OpenLibrary);

        // Assert
        Assert.Equal("Open Game Library", description);
    }
}

/// <summary>
/// Gamepad Service Tests
/// </summary>
public class GamepadServiceTests
{
    [Fact]
    public void GamepadService_InitialState_NotPolling()
    {
        // Arrange
        var service = GamepadService.Instance;

        // Assert
        Assert.False(service.IsPolling);
    }

    [Fact]
    public void GamepadService_GetConnectedCount_ReturnsZeroInitially()
    {
        // Arrange
        var service = GamepadService.Instance;

        // Assert
        Assert.Equal(0, service.GetConnectedCount());
    }

    [Fact]
    public void GamepadService_GetGamepad_ReturnsNullForInvalidIndex()
    {
        // Arrange
        var service = GamepadService.Instance;

        // Act
        var gamepad = service.GetGamepad(0);

        // Assert
        Assert.Null(gamepad);
    }

    [Fact]
    public void GamepadService_IsButtonPressed_ReturnsFalseForNoGamepad()
    {
        // Arrange
        var service = GamepadService.Instance;

        // Act
        var isPressed = service.IsButtonPressed(0, GamepadButton.A);

        // Assert
        Assert.False(isPressed);
    }
}
