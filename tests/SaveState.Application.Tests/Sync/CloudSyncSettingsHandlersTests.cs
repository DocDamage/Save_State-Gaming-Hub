using FluentAssertions;
using Moq;
using SaveState.Application.Sync.Commands;
using SaveState.Application.Sync.Commands.Handlers;
using SaveState.Application.Sync.Queries;
using SaveState.Application.Sync.Queries.Handlers;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Tests.Sync;

public class CloudSyncSettingsHandlersTests
{
    [Fact]
    public async Task UpdateHandler_WhenCalled_PersistsDaemonAlertSettings()
    {
        // Arrange
        var preferencesMock = new Mock<IUserPreferencesService>();
        preferencesMock
            .Setup(service => service.SetPreferredCloudProviderAsync("GoogleDrive", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        preferencesMock
            .Setup(service => service.SetAutoSyncOnExitAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        preferencesMock
            .Setup(service => service.SetCloudClientIdAsync("OneDrive", "onedrive-id", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        preferencesMock
            .Setup(service => service.SetCloudClientIdAsync("Google Drive", "googledrive-id", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        preferencesMock
            .Setup(service => service.SetBackgroundSyncFailureAlertsEnabledAsync(false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        preferencesMock
            .Setup(service => service.SetBackgroundSyncConflictAlertsEnabledAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        preferencesMock
            .Setup(service => service.SetBackgroundSyncAlertCooldownSecondsAsync(120, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateCloudSyncSettingsCommandHandler(preferencesMock.Object);
        var command = new UpdateCloudSyncSettingsCommand(
            PreferredProvider: "GoogleDrive",
            AutoSyncOnExit: true,
            OneDriveClientId: "onedrive-id",
            GoogleDriveClientId: "googledrive-id",
            EnableBackgroundFailureAlerts: false,
            EnableBackgroundConflictAlerts: true,
            BackgroundAlertCooldownSeconds: 120);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        preferencesMock.Verify(
            service => service.SetBackgroundSyncFailureAlertsEnabledAsync(false, It.IsAny<CancellationToken>()),
            Times.Once);
        preferencesMock.Verify(
            service => service.SetBackgroundSyncConflictAlertsEnabledAsync(true, It.IsAny<CancellationToken>()),
            Times.Once);
        preferencesMock.Verify(
            service => service.SetBackgroundSyncAlertCooldownSecondsAsync(120, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task QueryHandler_WhenCalled_ReturnsDaemonAlertSettings()
    {
        // Arrange
        var preferencesMock = new Mock<IUserPreferencesService>();
        preferencesMock
            .Setup(service => service.GetPreferredCloudProviderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("OneDrive");
        preferencesMock
            .Setup(service => service.GetAutoSyncOnExitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        preferencesMock
            .Setup(service => service.GetCloudClientIdAsync("OneDrive", It.IsAny<CancellationToken>()))
            .ReturnsAsync("one-id");
        preferencesMock
            .Setup(service => service.GetCloudClientIdAsync("Google Drive", It.IsAny<CancellationToken>()))
            .ReturnsAsync("google-id");
        preferencesMock
            .Setup(service => service.GetBackgroundSyncFailureAlertsEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        preferencesMock
            .Setup(service => service.GetBackgroundSyncConflictAlertsEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        preferencesMock
            .Setup(service => service.GetBackgroundSyncAlertCooldownSecondsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(180);

        var handler = new GetCloudSyncSettingsQueryHandler(preferencesMock.Object);

        // Act
        var result = await handler.Handle(new GetCloudSyncSettingsQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.EnableBackgroundFailureAlerts.Should().BeFalse();
        result.Value.EnableBackgroundConflictAlerts.Should().BeTrue();
        result.Value.BackgroundAlertCooldownSeconds.Should().Be(180);
    }
}
