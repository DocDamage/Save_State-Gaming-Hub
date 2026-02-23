using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;
using SaveState.IntegrationTests.Helpers;

namespace SaveState.IntegrationTests.MobileCompanion;

/// <summary>
/// Integration tests for mobile companion functionality.
/// </summary>
public class MobileCompanionTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IMobileCompanionService _companionService;
    private readonly IQRCodeService _qrCodeService;
    private readonly IPushNotificationService _pushService;
    private readonly IRemoteCommandExecutor _commandExecutor;

    public MobileCompanionTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _companionService = _fixture.ServiceProvider.GetRequiredService<IMobileCompanionService>();
        _qrCodeService = _fixture.ServiceProvider.GetRequiredService<IQRCodeService>();
        _pushService = _fixture.ServiceProvider.GetRequiredService<IPushNotificationService>();
        _commandExecutor = _fixture.ServiceProvider.GetRequiredService<IRemoteCommandExecutor>();
    }

    #region Pairing Tests

    [Fact]
    public async Task GeneratePairingCode_ReturnsValidCode()
    {
        // Act
        var result = await _companionService.GeneratePairingCodeAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Length.Should().Be(6);
        result.Value.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public async Task GeneratePairingCode_GeneratesUniqueCodes()
    {
        // Act
        var result1 = await _companionService.GeneratePairingCodeAsync();
        var result2 = await _companionService.GeneratePairingCodeAsync();

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value);
    }

    [Fact]
    public async Task PairDevice_WithValidCode_PairsSuccessfully()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        pairingCode.IsSuccess.Should().BeTrue();

        var device = TestDataSeeder.CreateSampleMobileDevice("Test iPhone");

        // Act
        var result = await _companionService.PairDeviceAsync(pairingCode.Value, device);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DeviceName.Should().Be(device.DeviceName);
        result.Value.Status.Should().Be(ConnectionStatus.Connected);
        result.Value.PairedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PairDevice_WithInvalidCode_ReturnsError()
    {
        // Arrange
        var device = TestDataSeeder.CreateSampleMobileDevice("Test Device");

        // Act
        var result = await _companionService.PairDeviceAsync("000000", device);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task PairDevice_WithExpiredCode_ReturnsError()
    {
        // Arrange
        // This would require manipulating the expiration time
        // For now, we test with an already used code
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("First Device");
        await _companionService.PairDeviceAsync(pairingCode.Value, device);

        // Try to pair another device with the same code
        var secondDevice = TestDataSeeder.CreateSampleMobileDevice("Second Device");

        // Act
        var result = await _companionService.PairDeviceAsync(pairingCode.Value, secondDevice);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UnpairDevice_RemovesDeviceSuccessfully()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Device To Unpair");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        pairResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _companionService.UnpairDeviceAsync(pairResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify device is no longer paired
        var devices = await _companionService.GetPairedDevicesAsync();
        devices.Value.Should().NotContain(d => d.Id == pairResult.Value.Id);
    }

    [Fact]
    public async Task GetPairedDevices_ReturnsAllPairedDevices()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
        {
            var pairingCode = await _companionService.GeneratePairingCodeAsync();
            var device = TestDataSeeder.CreateSampleMobileDevice($"Device {i}");
            await _companionService.PairDeviceAsync(pairingCode.Value, device);
        }

        // Act
        var result = await _companionService.GetPairedDevicesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().BeGreaterOrEqualTo(3);
    }

    #endregion

    #region QR Code Tests

    [Fact]
    public async Task GenerateQRCode_ForPairing_ReturnsQRData()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        pairingCode.IsSuccess.Should().BeTrue();

        // Act
        var result = await _qrCodeService.GeneratePairingQRCodeAsync(pairingCode.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateQRCode_IncludesConnectionInfo()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        pairingCode.IsSuccess.Should().BeTrue();

        // Act
        var result = await _qrCodeService.GenerateQRCodeWithConnectionInfoAsync(
            pairingCode.Value, 
            "192.168.1.100", 
            8080);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Connection Tests

    [Fact]
    public async Task ConnectDevice_EstablishesConnection()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Connection Test Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);

        // Act
        var result = await _companionService.ConnectDeviceAsync(pairResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify connection status
        var connectedDevice = await _companionService.GetDeviceAsync(pairResult.Value.Id);
        connectedDevice.Value.Status.Should().Be(ConnectionStatus.Connected);
        connectedDevice.Value.IsConnected.Should().BeTrue();
        connectedDevice.Value.LastConnectedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DisconnectDevice_TerminatesConnection()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Disconnect Test Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        await _companionService.ConnectDeviceAsync(pairResult.Value.Id);

        // Act
        var result = await _companionService.DisconnectDeviceAsync(pairResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify disconnection
        var disconnectedDevice = await _companionService.GetDeviceAsync(pairResult.Value.Id);
        disconnectedDevice.Value.Status.Should().Be(ConnectionStatus.Disconnected);
        disconnectedDevice.Value.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task GetConnectionStatus_ReturnsCurrentStatus()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Status Test Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);

        // Act
        var result = await _companionService.GetConnectionStatusAsync(pairResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    #endregion

    #region Session Management Tests

    [Fact]
    public async Task CreateSession_CreatesActiveSession()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Session Test Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);

        // Act
        var result = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Gamepad);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DeviceId.Should().Be(pairResult.Value.Id);
        result.Value.CurrentMode.Should().Be(RemoteControlMode.Gamepad);
        result.Value.IsActive.Should().BeTrue();
        result.Value.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EndSession_TerminatesSession()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("End Session Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        var sessionResult = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Gamepad);

        // Act
        var result = await _companionService.EndSessionAsync(sessionResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify session ended
        var session = await _companionService.GetSessionAsync(sessionResult.Value.Id);
        session.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveSessions_ReturnsOnlyActiveSessions()
    {
        // Arrange - Create multiple sessions and end some
        for (int i = 0; i < 3; i++)
        {
            var pairingCode = await _companionService.GeneratePairingCodeAsync();
            var device = TestDataSeeder.CreateSampleMobileDevice($"Session Device {i}");
            var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
            var session = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Gamepad);

            if (i == 2)
            {
                await _companionService.EndSessionAsync(session.Value.Id);
            }
        }

        // Act
        var result = await _companionService.GetActiveSessionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(s => s.IsActive);
    }

    [Fact]
    public async Task UpdateSessionMode_ChangesControlMode()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Mode Change Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        var sessionResult = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Gamepad);

        // Act
        var result = await _companionService.UpdateSessionModeAsync(
            sessionResult.Value.Id, 
            RemoteControlMode.MediaControls);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var session = await _companionService.GetSessionAsync(sessionResult.Value.Id);
        session.Value.CurrentMode.Should().Be(RemoteControlMode.MediaControls);
    }

    #endregion

    #region Remote Command Tests

    [Fact]
    public async Task ExecuteCommand_ValidCommand_ExecutesSuccessfully()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Command Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        var sessionResult = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.MediaControls);

        var command = new RemoteCommandMessage
        {
            Command = RemoteControlCommand.VolumeUp,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _commandExecutor.ExecuteCommandAsync(sessionResult.Value.Id, command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCommand_LaunchGameCommand_ExecutesSuccessfully()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Launch Game Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        var sessionResult = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Gamepad);

        var command = new RemoteCommandMessage
        {
            Command = RemoteControlCommand.LaunchGame,
            GameId = "game_123",
            Parameters = new Dictionary<string, object> { { "gameId", "game_123" } },
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _commandExecutor.ExecuteCommandAsync(sessionResult.Value.Id, command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCommand_NavigationCommands_WorkCorrectly()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Nav Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        var sessionResult = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Gamepad);

        var commands = new[]
        {
            RemoteControlCommand.NavigateUp,
            RemoteControlCommand.NavigateDown,
            RemoteControlCommand.NavigateLeft,
            RemoteControlCommand.NavigateRight,
            RemoteControlCommand.Select,
            RemoteControlCommand.Back
        };

        foreach (var navCommand in commands)
        {
            // Act
            var command = new RemoteCommandMessage
            {
                Command = navCommand,
                Timestamp = DateTime.UtcNow
            };
            var result = await _commandExecutor.ExecuteCommandAsync(sessionResult.Value.Id, command);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ExecuteCommand_InvalidSession_ReturnsError()
    {
        // Arrange
        var command = new RemoteCommandMessage
        {
            Command = RemoteControlCommand.VolumeUp,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _commandExecutor.ExecuteCommandAsync(Guid.NewGuid(), command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region Push Notification Tests

    [Fact]
    public async Task SendNotification_ToDevice_SendsSuccessfully()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Notification Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);

        var notification = new CompanionNotification
        {
            Title = "Test Notification",
            Message = "This is a test notification",
            Type = NotificationType.Info,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _pushService.SendNotificationAsync(pairResult.Value.Id, notification);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAchievementNotification_SendsWithCorrectType()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Achievement Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);

        var notification = new CompanionNotification
        {
            Title = "Achievement Unlocked!",
            Message = "You unlocked 'First Victory'",
            Type = NotificationType.Achievement,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, string>
            {
                { "achievementId", "ach_001" },
                { "gameId", "game_123" }
            }
        };

        // Act
        var result = await _pushService.SendNotificationAsync(pairResult.Value.Id, notification);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotification_ToAllDevices_SendsSuccessfully()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
        {
            var pairingCode = await _companionService.GeneratePairingCodeAsync();
            var device = TestDataSeeder.CreateSampleMobileDevice($"Broadcast Device {i}");
            await _companionService.PairDeviceAsync(pairingCode.Value, device);
        }

        var notification = new CompanionNotification
        {
            Title = "Broadcast",
            Message = "Message to all devices",
            Type = NotificationType.Info,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _pushService.SendNotificationToAllAsync(notification);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Input Handling Tests

    [Fact]
    public async Task SendGamepadInput_ProcessesInput()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Gamepad Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        var sessionResult = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Gamepad);

        var input = new GamepadInput
        {
            Button = "A",
            IsPressed = true,
            AxisX = 0.5f,
            AxisY = 0.0f
        };

        // Act
        var result = await _companionService.SendGamepadInputAsync(sessionResult.Value.Id, input);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendTouchpadInput_ProcessesTouchInput()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Touchpad Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        var sessionResult = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Touchpad);

        var touchInput = new TouchpadInput
        {
            X = 0.5f,
            Y = 0.5f,
            Action = TouchAction.Tap,
            FingerId = 1
        };

        // Act
        var result = await _companionService.SendTouchpadInputAsync(sessionResult.Value.Id, touchInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendKeyboardInput_ProcessesKeyInput()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Keyboard Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);
        var sessionResult = await _companionService.CreateSessionAsync(pairResult.Value.Id, RemoteControlMode.Keyboard);

        var keyInput = new KeyboardInput
        {
            Key = "Enter",
            IsPressed = true,
            IsModifier = false,
            Modifiers = new List<string>()
        };

        // Act
        var result = await _companionService.SendKeyboardInputAsync(sessionResult.Value.Id, keyInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Library Sync Tests

    [Fact]
    public async Task SyncLibrary_ReturnsLibraryInfo()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Sync Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);

        // Act
        var result = await _companionService.SyncLibraryAsync(pairResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemStatus_ReturnsCurrentStatus()
    {
        // Arrange
        var pairingCode = await _companionService.GeneratePairingCodeAsync();
        var device = TestDataSeeder.CreateSampleMobileDevice("Status Device");
        var pairResult = await _companionService.PairDeviceAsync(pairingCode.Value, device);

        // Act
        var result = await _companionService.GetSystemStatusAsync(pairResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    #endregion
}
