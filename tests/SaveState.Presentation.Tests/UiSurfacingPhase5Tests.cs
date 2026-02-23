using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.MobileCompanion;
using Xunit;

namespace SaveState.Presentation.Tests;

/// <summary>
/// Tests for Phase 5: Mobile Companion App UI surfacing
/// </summary>
public class UiSurfacingPhase5Tests
{
    private readonly Mock<IMobileCompanionService> _mobileServiceMock;
    private readonly Mock<IQRCodeService> _qrCodeServiceMock;
    private readonly Mock<IMobileConnectionManager> _connectionManagerMock;
    private readonly Mock<IDialogService> _dialogServiceMock;

    public UiSurfacingPhase5Tests()
    {
        _mobileServiceMock = new Mock<IMobileCompanionService>();
        _qrCodeServiceMock = new Mock<IQRCodeService>();
        _connectionManagerMock = new Mock<IMobileConnectionManager>();
        _dialogServiceMock = new Mock<IDialogService>();
    }

    #region MobileLandingViewModel Tests

    [Fact]
    public async Task MobileLandingViewModel_StartPairingAsync_ShouldGenerateCode()
    {
        // Arrange
        var pairingRequest = new PairingRequest
        {
            Id = Guid.NewGuid(),
            PairingCode = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mobileServiceMock
            .Setup(s => s.CreatePairingRequestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PairingRequest>.Success(pairingRequest));

        var viewModel = new MobileLandingViewModel(
            _mobileServiceMock.Object,
            _qrCodeServiceMock.Object,
            _connectionManagerMock.Object);

        // Act
        await viewModel.StartPairingAsync();

        // Assert
        viewModel.PairingCode.Should().Be("123456");
        viewModel.IsPairing.Should().BeTrue();
    }

    [Fact]
    public void MobileLandingViewModel_ValidatePairingCode_ShouldRejectInvalidCodes()
    {
        // Arrange
        var viewModel = new MobileLandingViewModel(
            _mobileServiceMock.Object,
            _qrCodeServiceMock.Object,
            _connectionManagerMock.Object);

        // Act & Assert
        viewModel.ValidatePairingCode("").Should().BeFalse();
        viewModel.ValidatePairingCode("123").Should().BeFalse();
        viewModel.ValidatePairingCode("1234567").Should().BeFalse();
        viewModel.ValidatePairingCode("abcdef").Should().BeFalse();
        viewModel.ValidatePairingCode("123456").Should().BeTrue();
    }

    #endregion

    #region MobileDashboardViewModel Tests

    [Fact]
    public async Task MobileDashboardViewModel_LoadDashboardAsync_ShouldFetchData()
    {
        // Arrange
        var systemStatus = new SystemStatus
        {
            IsOnline = true,
            CpuUsage = 45.5f,
            MemoryUsage = 62.0f,
            CurrentlyPlayingGame = "Elden Ring"
        };

        var libraryInfo = new LibrarySyncInfo
        {
            TotalGames = 150,
            RecentlyPlayedCount = 5,
            RecentlyPlayed = new List<GameSummary>
            {
                new() { Id = Guid.NewGuid(), Name = "Game 1" },
                new() { Id = Guid.NewGuid(), Name = "Game 2" }
            }
        };

        _connectionManagerMock
            .Setup(c => c.GetSystemStatusAsync())
            .ReturnsAsync(systemStatus);

        _connectionManagerMock
            .Setup(c => c.SyncLibraryAsync())
            .ReturnsAsync(libraryInfo);

        var viewModel = new MobileDashboardViewModel(
            _connectionManagerMock.Object,
            _dialogServiceMock.Object);

        // Act
        await viewModel.LoadDashboardAsync();

        // Assert
        viewModel.SystemStatus.Should().NotBeNull();
        viewModel.SystemStatus.CurrentlyPlayingGame.Should().Be("Elden Ring");
        viewModel.RecentGames.Should().HaveCount(2);
    }

    [Fact]
    public void MobileDashboardViewModel_ConnectionStatus_ShouldUpdateIsConnected()
    {
        // Arrange
        var viewModel = new MobileDashboardViewModel(
            _connectionManagerMock.Object,
            _dialogServiceMock.Object);

        // Act - simulate connection status change
        _connectionManagerMock.Raise(
            c => c.OnStatusChanged += null,
            new MobileConnectionStatusEventArgs(MobileConnectionStatus.Connected));

        // Assert
        viewModel.IsConnected.Should().BeTrue();
    }

    #endregion

    #region MobileRemoteControlViewModel Tests

    [Fact]
    public async Task MobileRemoteControlViewModel_SendButtonPressAsync_ShouldCallManager()
    {
        // Arrange
        var viewModel = new MobileRemoteControlViewModel(
            _connectionManagerMock.Object);

        _connectionManagerMock.Setup(c => c.IsConnected).Returns(true);

        // Act
        await viewModel.SendButtonPressAsync("A");

        // Assert
        _connectionManagerMock.Verify(
            c => c.SendGamepadInputAsync("A", true),
            Times.Once);
    }

    [Fact]
    public async Task MobileRemoteControlViewModel_SwitchModeAsync_ShouldChangeMode()
    {
        // Arrange
        var viewModel = new MobileRemoteControlViewModel(
            _connectionManagerMock.Object);

        // Act
        await viewModel.SwitchModeAsync(RemoteControlMode.Touchpad);

        // Assert
        viewModel.CurrentMode.Should().Be(RemoteControlMode.Touchpad);
    }

    [Theory]
    [InlineData(RemoteControlMode.Gamepad)]
    [InlineData(RemoteControlMode.Touchpad)]
    [InlineData(RemoteControlMode.Media)]
    public void MobileRemoteControlViewModel_ModeProperties_ShouldReflectCurrentMode(RemoteControlMode mode)
    {
        // Arrange
        var viewModel = new MobileRemoteControlViewModel(
            _connectionManagerMock.Object);

        // Act
        viewModel.CurrentMode = mode;

        // Assert
        viewModel.IsGamepadMode.Should().Be(mode == RemoteControlMode.Gamepad);
        viewModel.IsTouchpadMode.Should().Be(mode == RemoteControlMode.Touchpad);
        viewModel.IsMediaMode.Should().Be(mode == RemoteControlMode.Media);
    }

    #endregion

    #region QRCodeService Tests

    [Fact]
    public async Task QRCodeService_GeneratePairingQRCodeAsync_ShouldReturnImageBytes()
    {
        // Arrange
        var qrService = new QRCodeService(
            Mock.Of<Microsoft.Extensions.Logging.ILogger<QRCodeService>>(),
            _dialogServiceMock.Object);

        var pairingInfo = new PairingInfo
        {
            HubId = "test-hub",
            HubName = "Test Hub",
            IpAddress = "192.168.1.100",
            Port = 5000
        };

        // Act
        var result = await qrService.GeneratePairingQRCodeAsync(pairingInfo, 256);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PairingInfo_ToJson_FromJson_ShouldRoundTrip()
    {
        // Arrange
        var original = new PairingInfo
        {
            HubId = "test-hub",
            HubName = "Test Hub",
            IpAddress = "192.168.1.100",
            Port = 5000,
            Token = "secret-token",
            PairingCode = "123456"
        };

        // Act
        var json = original.ToJson();
        var deserialized = PairingInfo.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.HubId.Should().Be(original.HubId);
        deserialized.HubName.Should().Be(original.HubName);
        deserialized.IpAddress.Should().Be(original.IpAddress);
        deserialized.Port.Should().Be(original.Port);
        deserialized.Token.Should().Be(original.Token);
        deserialized.PairingCode.Should().Be(original.PairingCode);
    }

    #endregion

    #region Security Tests

    [Fact]
    public void MobileCompanionSecurity_GeneratePairingCode_ShouldReturn6Digits()
    {
        // Arrange
        var security = new Infrastructure.MobileCompanion.Security.MobileCompanionSecurity(
            Mock.Of<Microsoft.Extensions.Logging.ILogger<Infrastructure.MobileCompanion.Security.MobileCompanionSecurity>>());

        // Act
        var code = security.GeneratePairingCode();

        // Assert
        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void MobileCompanionSecurity_ValidatePairingCode_ShouldValidateCorrectly()
    {
        // Arrange
        var security = new Infrastructure.MobileCompanion.Security.MobileCompanionSecurity(
            Mock.Of<Microsoft.Extensions.Logging.ILogger<Infrastructure.MobileCompanion.Security.MobileCompanionSecurity>>());

        // Act & Assert
        security.ValidatePairingCode("123456").Should().BeTrue();
        security.ValidatePairingCode("000000").Should().BeTrue();
        security.ValidatePairingCode("999999").Should().BeTrue();
        security.ValidatePairingCode("12345").Should().BeFalse();
        security.ValidatePairingCode("1234567").Should().BeFalse();
        security.ValidatePairingCode("abcdef").Should().BeFalse();
        security.ValidatePairingCode("").Should().BeFalse();
        security.ValidatePairingCode(null).Should().BeFalse();
    }

    [Fact]
    public void MobileCompanionSecurity_EncryptDecrypt_ShouldRoundTrip()
    {
        // Arrange
        var security = new Infrastructure.MobileCompanion.Security.MobileCompanionSecurity(
            Mock.Of<Microsoft.Extensions.Logging.ILogger<Infrastructure.MobileCompanion.Security.MobileCompanionSecurity>>());

        var key = security.GenerateRandomBytes(32);
        var originalData = Encoding.UTF8.GetBytes("Hello, World!");

        // Act
        var encrypted = security.EncryptData(originalData, key);
        var decrypted = security.DecryptData(encrypted, key);

        // Assert
        decrypted.Should().Equal(originalData);
    }

    [Fact]
    public void MobileCompanionSecurity_GenerateKeyPair_ShouldCreateValidKeys()
    {
        // Arrange
        var security = new Infrastructure.MobileCompanion.Security.MobileCompanionSecurity(
            Mock.Of<Microsoft.Extensions.Logging.ILogger<Infrastructure.MobileCompanion.Security.MobileCompanionSecurity>>());

        // Act
        var (publicKey, privateKey) = security.GenerateKeyPair();

        // Assert
        publicKey.Should().NotBeNull();
        privateKey.Should().NotBeNull();
        publicKey.Length.Should().BeGreaterThan(0);
        privateKey.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region PairingDialogViewModel Tests

    [Fact]
    public void PairingDialogViewModel_AcceptPairing_ShouldSetResult()
    {
        // Arrange
        var viewModel = new PairingDialogViewModel
        {
            DeviceName = "Test iPhone",
            DeviceType = "iOS"
        };

        // Act
        viewModel.AcceptPairing();

        // Assert
        viewModel.IsAccepted.Should().BeTrue();
    }

    [Fact]
    public void PairingDialogViewModel_TogglePermission_ShouldToggleState()
    {
        // Arrange
        var viewModel = new PairingDialogViewModel();
        var permission = viewModel.Permissions[0];
        var initialState = permission.IsGranted;

        // Act
        viewModel.TogglePermission(permission);

        // Assert
        permission.IsGranted.Should().Be(!initialState);
    }

    #endregion
}

public class MobileConnectionStatusEventArgs : EventArgs
{
    public MobileConnectionStatus Status { get; }
    
    public MobileConnectionStatusEventArgs(MobileConnectionStatus status)
    {
        Status = status;
    }
}
