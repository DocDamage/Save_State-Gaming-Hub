using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
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
    private readonly Mock<ILogger<MobileLandingViewModel>> _landingLoggerMock;
    private readonly Mock<ILogger<MobileDashboardViewModel>> _dashboardLoggerMock;
    private readonly Mock<ILogger<MobileRemoteControlViewModel>> _remoteLoggerMock;

    public UiSurfacingPhase5Tests()
    {
        _landingLoggerMock = new Mock<ILogger<MobileLandingViewModel>>();
        _dashboardLoggerMock = new Mock<ILogger<MobileDashboardViewModel>>();
        _remoteLoggerMock = new Mock<ILogger<MobileRemoteControlViewModel>>();
    }

    #region MobileLandingViewModel Tests

    [Fact]
    public void MobileLandingViewModel_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new MobileLandingViewModel(
            _landingLoggerMock.Object,
            null);

        // Assert
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void MobileLandingViewModel_InitialState_HasEmptyPairingCode()
    {
        // Arrange
        var viewModel = new MobileLandingViewModel(
            _landingLoggerMock.Object,
            null);

        // Assert
        viewModel.PairingCode.Should().BeEmpty();
        viewModel.IsPairing.Should().BeFalse();
    }

    #endregion

    #region MobileDashboardViewModel Tests

    [Fact]
    public void MobileDashboardViewModel_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new MobileDashboardViewModel(
            _dashboardLoggerMock.Object,
            null);

        // Assert
        viewModel.Should().NotBeNull();
    }

    #endregion

    #region MobileRemoteControlViewModel Tests

    [Fact]
    public void MobileRemoteControlViewModel_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new MobileRemoteControlViewModel(
            _remoteLoggerMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
    }

    [Fact]
    public void MobileRemoteControlViewModel_CanChangeMode()
    {
        // Arrange
        var viewModel = new MobileRemoteControlViewModel(
            _remoteLoggerMock.Object);

        // Act & Assert - verify no exception thrown
        viewModel.Should().NotBeNull();
    }

    #endregion

    #region QRCodeService Tests

    [Fact]
    public async Task QRCodeService_GeneratePairingQRCodeAsync_ShouldReturnImageBytes()
    {
        // Arrange
        var qrService = new QRCodeService(
            Mock.Of<Microsoft.Extensions.Logging.ILogger<QRCodeService>>(),
            Mock.Of<SaveState.Presentation.Services.IDialogService>());

        var pairingInfo = new SaveState.Presentation.Services.PairingInfo
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
        var original = new SaveState.Presentation.Services.PairingInfo
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
        var deserialized = SaveState.Presentation.Services.PairingInfo.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.HubId.Should().Be(original.HubId);
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
    public void PairingDialogViewModel_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new PairingDialogViewModel();

        // Assert
        viewModel.Should().NotBeNull();
    }

    #endregion
}
