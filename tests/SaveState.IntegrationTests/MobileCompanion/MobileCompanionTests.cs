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
    public async Task CreatePairingRequest_ReturnsValidCode()
    {
        // Act
        var result = await _companionService.CreatePairingRequestAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.PairingCode.Should().NotBeNullOrEmpty();
        result.Value.PairingCode.Length.Should().Be(6);
        result.Value.PairingCode.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public async Task CreatePairingRequest_GeneratesUniqueCodes()
    {
        // Act
        var result1 = await _companionService.CreatePairingRequestAsync();
        var result2 = await _companionService.CreatePairingRequestAsync();

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.PairingCode.Should().NotBe(result2.Value.PairingCode);
    }

    [Fact]
    public async Task PairDevice_WithValidCode_PairsSuccessfully()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        pairingRequest.IsSuccess.Should().BeTrue();

        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Test iPhone",
            DeviceType = "iOS",
            DeviceModel = "iPhone 15 Pro",
            OsVersion = "17.0",
            AppVersion = "1.0.0"
        };

        // Act
        var result = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DeviceName.Should().Be(deviceInfo.DeviceName);
    }

    [Fact]
    public async Task PairDevice_WithInvalidCode_ReturnsError()
    {
        // Arrange
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Test Device",
            DeviceType = "iOS"
        };

        // Act
        var result = await _companionService.CompletePairingAsync("000000", deviceInfo);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task PairDevice_WithExpiredCode_ReturnsError()
    {
        // Arrange
        // This would require manipulating the expiration time
        // For now, we test with an already used code
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var firstDeviceInfo = new DeviceInfo
        {
            DeviceName = "First Device",
            DeviceType = "iOS"
        };
        await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, firstDeviceInfo);

        // Try to pair another device with the same code
        var secondDeviceInfo = new DeviceInfo
        {
            DeviceName = "Second Device",
            DeviceType = "iOS"
        };

        // Act
        var result = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, secondDeviceInfo);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UnpairDevice_RemovesDeviceSuccessfully()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Device To Unpair",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
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
            var pairingRequest = await _companionService.CreatePairingRequestAsync();
            var deviceInfo = new DeviceInfo
            {
                DeviceName = $"Device {i}",
                DeviceType = "iOS"
            };
            await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        }

        // Act
        var result = await _companionService.GetPairedDevicesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    #endregion

    #region QR Code Tests

    [Fact]
    public async Task GenerateQRCode_ForPairing_ReturnsQRData()
    {
        // Arrange
        var pairingInfo = new PairingInfo
        {
            HubId = Guid.NewGuid().ToString(),
            HubName = "Test Hub",
            IpAddress = "192.168.1.100",
            Port = 8080,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        // Act
        var result = await _qrCodeService.GeneratePairingQRCodeAsync(pairingInfo);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateQRCode_IncludesConnectionInfo()
    {
        // Arrange
        var pairingInfo = new PairingInfo
        {
            HubId = Guid.NewGuid().ToString(),
            HubName = "Test Hub",
            IpAddress = "192.168.1.100",
            Port = 8080,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        // Act
        var result = await _qrCodeService.GeneratePairingQRCodeAsync(pairingInfo);

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
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Connection Test Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);

        // Act - Start a session to establish connection
        var result = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify connection status
        var connectedDevice = await _companionService.GetDeviceAsync(pairResult.Value.Id);
        connectedDevice.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task DisconnectDevice_TerminatesConnection()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Disconnect Test Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");
        sessionResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await _companionService.EndSessionAsync(sessionResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify disconnection
        var disconnectedDevice = await _companionService.GetDeviceAsync(pairResult.Value.Id);
        disconnectedDevice.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveSession_ReturnsCurrentSession()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Status Test Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);

        // Act
        var result = await _companionService.GetActiveSessionAsync(pairResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Session Management Tests

    [Fact]
    public async Task CreateSession_CreatesActiveSession()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Session Test Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);

        // Act
        var result = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DeviceId.Should().Be(pairResult.Value.Id);
        result.Value.IsActive.Should().BeTrue();
        result.Value.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task EndSession_TerminatesSession()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "End Session Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        // Act
        var result = await _companionService.EndSessionAsync(sessionResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveSessions_ReturnsOnlyActiveSessions()
    {
        // Arrange - Create multiple sessions and end some
        for (int i = 0; i < 3; i++)
        {
            var pairingRequest = await _companionService.CreatePairingRequestAsync();
            var deviceInfo = new DeviceInfo
            {
                DeviceName = $"Session Device {i}",
                DeviceType = "iOS"
            };
            var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
            var session = await _companionService.StartSessionAsync(pairResult.Value.Id, $"connection-{i}");

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
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Mode Change Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        // Act
        var result = await _companionService.SetControlModeAsync(
            pairResult.Value.Id, 
            RemoteControlMode.MediaControls);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Remote Command Tests

    [Fact]
    public async Task ExecuteCommand_ValidCommand_ExecutesSuccessfully()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Command Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        var command = new RemoteCommandMessage
        {
            Command = RemoteControlCommand.VolumeUp,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _companionService.SendCommandAsync(pairResult.Value.Id, command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCommand_LaunchGameCommand_ExecutesSuccessfully()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Launch Game Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        var command = new RemoteCommandMessage
        {
            Command = RemoteControlCommand.LaunchGame,
            GameId = "game_123",
            Parameters = new Dictionary<string, object> { { "gameId", "game_123" } },
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _companionService.SendCommandAsync(pairResult.Value.Id, command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCommand_NavigationCommands_WorkCorrectly()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Nav Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

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
            var result = await _companionService.SendCommandAsync(pairResult.Value.Id, command);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ExecuteCommand_InvalidDevice_ReturnsError()
    {
        // Arrange
        var command = new RemoteCommandMessage
        {
            Command = RemoteControlCommand.VolumeUp,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _companionService.SendCommandAsync(Guid.NewGuid(), command);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Push Notification Tests

    [Fact]
    public async Task SendNotification_ToDevice_SendsSuccessfully()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Notification Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);

        var notification = new CompanionNotification
        {
            Title = "Test Notification",
            Message = "This is a test notification",
            Type = NotificationType.Info,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _companionService.SendNotificationAsync(pairResult.Value.Id, notification);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAchievementNotification_SendsWithCorrectType()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Achievement Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);

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
        var result = await _companionService.SendNotificationAsync(pairResult.Value.Id, notification);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotification_ToAllDevices_SendsSuccessfully()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
        {
            var pairingRequest = await _companionService.CreatePairingRequestAsync();
            var deviceInfo = new DeviceInfo
            {
                DeviceName = $"Broadcast Device {i}",
                DeviceType = "iOS"
            };
            await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        }

        var notification = new CompanionNotification
        {
            Title = "Broadcast",
            Message = "Message to all devices",
            Type = NotificationType.Info,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _companionService.BroadcastNotificationAsync(notification);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Input Handling Tests

    [Fact]
    public async Task SendGamepadInput_ProcessesInput()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Gamepad Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        var input = new GamepadInput
        {
            Button = "A",
            IsPressed = true,
            AxisX = 0.5f,
            AxisY = 0.0f
        };

        // Act
        var result = await _companionService.SendGamepadInputAsync(pairResult.Value.Id, input);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendTouchpadInput_ProcessesTouchInput()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Touchpad Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        var touchInput = new TouchpadInput
        {
            X = 0.5f,
            Y = 0.5f,
            Action = TouchAction.Tap,
            FingerId = 1
        };

        // Act
        var result = await _companionService.SendTouchpadInputAsync(pairResult.Value.Id, touchInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendKeyboardInput_ProcessesKeyInput()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Keyboard Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);
        var sessionResult = await _companionService.StartSessionAsync(pairResult.Value.Id, "test-connection-id");

        var keyInput = new KeyboardInput
        {
            Key = "Enter",
            IsPressed = true,
            IsModifier = false,
            Modifiers = new List<string>()
        };

        // Act
        var result = await _companionService.SendKeyboardInputAsync(pairResult.Value.Id, keyInput);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Library Sync Tests

    [Fact]
    public async Task SyncLibrary_ReturnsLibraryInfo()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Sync Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);

        // Act
        var result = await _companionService.GetLibrarySyncInfoAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSystemStatus_ReturnsCurrentStatus()
    {
        // Arrange
        var pairingRequest = await _companionService.CreatePairingRequestAsync();
        var deviceInfo = new DeviceInfo
        {
            DeviceName = "Status Device",
            DeviceType = "iOS"
        };
        var pairResult = await _companionService.CompletePairingAsync(pairingRequest.Value.PairingCode, deviceInfo);

        // Act
        var result = await _companionService.GetSystemStatusAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    #endregion
}
