using MediatR;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using SaveState.Application.CloudServices.Queries;
using SaveState.Application.Sync.Commands;
using SaveState.Application.Sync.Queries;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Services.DTOs;
using SaveState.Core.Sync;
using SaveState.Core.Sync.Services;
using SaveState.Core.Sync.Services.DTOs;
using SaveState.Infrastructure.SaveStates;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Shell;
using SaveState.Tests.Infrastructure;

namespace SaveState.Presentation.Tests.ViewModels;

public class CloudSyncViewModelConflictResolutionTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ISyncService> _syncServiceMock = new();
    private readonly Mock<ICloudGamingManager> _cloudGamingManagerMock = new();
    private readonly Mock<INetworkQualityMonitor> _networkMonitorMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<CloudSyncViewModel>> _loggerMock = new();
    private readonly Mock<ICloudCatalogService> _cloudCatalogServiceMock = new();
    private readonly Mock<ISaveStateCloudService> _saveStateCloudServiceMock = new();
    private readonly Mock<IGameRepository> _gameRepositoryMock = new();
    private readonly Mock<ISaveStateCloudSyncMonitor> _monitorMock = new();
    private readonly TestTimeProvider _timeProvider = new(new DateTime(2026, 2, 13, 10, 0, 0, DateTimeKind.Utc));

    public CloudSyncViewModelConflictResolutionTests()
    {
        _syncServiceMock.SetupGet(service => service.Status).Returns(SyncStatus.Idle);
        _syncServiceMock.SetupGet(service => service.ActiveProviderName).Returns("Local Storage");
        _syncServiceMock
            .Setup(service => service.GetConflictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SyncConflictEventArgs>());
        _networkMonitorMock.SetupGet(monitor => monitor.IsMonitoring).Returns(false);

        _mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<GetCloudProvidersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<CloudGamingProvider>>(Array.Empty<CloudGamingProvider>()));
        _mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<GetActiveCloudSessionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<CloudSession>>(Array.Empty<CloudSession>()));
        _mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<GetNetworkQualityCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new NetworkQualityInfo(24, 0, 400, "Excellent")));
        _mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<GetCloudSyncSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CloudSyncSettingsDto(
                PreferredProvider: "GoogleDrive",
                AutoSyncOnExit: true,
                OneDriveClientId: string.Empty,
                GoogleDriveClientId: string.Empty,
                EnableBackgroundFailureAlerts: true,
                EnableBackgroundConflictAlerts: true,
                BackgroundAlertCooldownSeconds: 60)));
        _mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<GetBackupHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<SaveState.Application.CloudServices.Services.BackupMetadata>>("No backups"));

        _cloudCatalogServiceMock
            .Setup(service => service.GetCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CloudCatalog(
                "test",
                DateTimeOffset.UtcNow,
                Array.Empty<CloudCatalogEntry>())));

        _gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Game>());

        _monitorMock
            .SetupGet(monitor => monitor.CurrentStatus)
            .Returns(new SaveStateCloudDaemonStatus
            {
                Enabled = true,
                IsRunning = false,
                UpdatedAtUtc = _timeProvider.UtcNow,
                LastSyncAtUtc = null,
                LastGameId = null,
                SuccessfulSyncCount = 0,
                FailedSyncCount = 0,
                ConflictCount = 0,
                SkippedCount = 0,
                LastMessage = "ok"
            });
    }

    [Fact]
    public async Task ViewConflictsAsync_WithSaveStateKeepLocal_ForceUploadsLocal()
    {
        // Arrange
        var game = Game.Create("Chrono Trigger");
        var dialogKey = $"SaveState::{game.Id:N}::{game.Title}";
        _gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { game });

        _saveStateCloudServiceMock
            .Setup(service => service.DetectConflictsAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateConflictResolution
            {
                GameId = game.Id,
                Type = SaveStateConflictType.CloudNewer,
                DetectedAtUtc = _timeProvider.UtcNow,
                LocalVersion = CreateVersion(game.Id, 1024, "local"),
                CloudVersion = CreateVersion(game.Id, 2048, "cloud")
            }));

        _dialogServiceMock
            .Setup(service => service.ShowConflictResolutionDialogAsync(It.IsAny<SyncConflictViewModel[]>()))
            .ReturnsAsync(new ConflictResolutionResult(new Dictionary<string, string>
            {
                [dialogKey] = "Keep Local"
            }));

        _saveStateCloudServiceMock
            .Setup(service => service.ResolveConflictAsync(
                game.Id,
                SaveStateConflictResolutionStrategy.KeepLocal,
                It.Is<SaveStateCloudMetadata>(metadata =>
                    metadata.ForceUpload &&
                    metadata.VersionName != null &&
                    metadata.VersionName.Contains("Conflict KeepLocal", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateCloudSyncStatus
            {
                GameId = game.Id,
                Provider = "Local Storage",
                Uploaded = true,
                Downloaded = false,
                HasConflict = false,
                ConflictType = SaveStateConflictType.None,
                SyncedAtUtc = _timeProvider.UtcNow,
                IsEncrypted = false,
                Message = "resolved"
            }));

        var viewModel = CreateSut();

        // Act
        await viewModel.ViewConflictsCommand.ExecuteAsync(null);

        // Assert
        _saveStateCloudServiceMock.Verify(service => service.ResolveConflictAsync(
            game.Id,
            SaveStateConflictResolutionStrategy.KeepLocal,
            It.Is<SaveStateCloudMetadata>(metadata => metadata.ForceUpload),
            It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(
            notification => notification.ShowSuccess(
                It.Is<string>(message => message.Contains("1 of 1", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ViewConflictsAsync_WithSaveStateKeepCloud_UsesConflictResolutionApi()
    {
        // Arrange
        var game = Game.Create("Mega Man X");
        var dialogKey = $"SaveState::{game.Id:N}::{game.Title}";
        _gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { game });

        _saveStateCloudServiceMock
            .Setup(service => service.DetectConflictsAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateConflictResolution
            {
                GameId = game.Id,
                Type = SaveStateConflictType.CloudNewer,
                DetectedAtUtc = _timeProvider.UtcNow,
                LocalVersion = CreateVersion(game.Id, 1024, "local"),
                CloudVersion = CreateVersion(game.Id, 3072, "cloud")
            }));

        _dialogServiceMock
            .Setup(service => service.ShowConflictResolutionDialogAsync(It.IsAny<SyncConflictViewModel[]>()))
            .ReturnsAsync(new ConflictResolutionResult(new Dictionary<string, string>
            {
                [dialogKey] = "Keep Cloud"
            }));

        _saveStateCloudServiceMock
            .Setup(service => service.ResolveConflictAsync(
                game.Id,
                SaveStateConflictResolutionStrategy.KeepCloud,
                It.IsAny<SaveStateCloudMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateCloudSyncStatus
            {
                GameId = game.Id,
                Provider = "Local Storage",
                Uploaded = false,
                Downloaded = true,
                HasConflict = false,
                ConflictType = SaveStateConflictType.None,
                SyncedAtUtc = _timeProvider.UtcNow,
                IsEncrypted = false,
                Message = "resolved"
            }));

        var viewModel = CreateSut();

        // Act
        await viewModel.ViewConflictsCommand.ExecuteAsync(null);

        // Assert
        _saveStateCloudServiceMock.Verify(service => service.ResolveConflictAsync(
            game.Id,
            SaveStateConflictResolutionStrategy.KeepCloud,
            It.IsAny<SaveStateCloudMetadata>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(
            notification => notification.ShowSuccess(
                It.Is<string>(message => message.Contains("1 of 1", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ViewConflictsAsync_WithNoFileAndSaveStateConflicts_ShowsInfo()
    {
        // Arrange
        var viewModel = CreateSut();

        // Act
        await viewModel.ViewConflictsCommand.ExecuteAsync(null);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowInfo("No conflicts detected.", null, It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task ViewConflictsAsync_WithEncryptedSaveStateKeepCloud_PromptsForKeyAndResolves()
    {
        // Arrange
        var game = Game.Create("Silent Hill");
        var dialogKey = $"SaveState::{game.Id:N}::{game.Title}";
        _gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { game });

        _saveStateCloudServiceMock
            .Setup(service => service.DetectConflictsAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateConflictResolution
            {
                GameId = game.Id,
                Type = SaveStateConflictType.BothModified,
                DetectedAtUtc = _timeProvider.UtcNow,
                LocalVersion = CreateVersion(game.Id, 1024, "local"),
                CloudVersion = CreateVersion(game.Id, 3072, "cloud") with
                {
                    IsEncrypted = true,
                    EncryptionKeyFingerprint = "A1B2C3D4"
                }
            }));

        _dialogServiceMock
            .Setup(service => service.ShowConflictResolutionDialogAsync(It.IsAny<SyncConflictViewModel[]>()))
            .ReturnsAsync(new ConflictResolutionResult(new Dictionary<string, string>
            {
                [dialogKey] = "Keep Cloud"
            }));
        _dialogServiceMock
            .Setup(service => service.ShowInputDialogAsync(
                "Cloud Save Encryption Key",
                It.Is<string>(message => message.Contains(dialogKey, StringComparison.Ordinal)),
                "Encryption key",
                true))
            .ReturnsAsync("secret-key");

        _saveStateCloudServiceMock
            .Setup(service => service.ResolveConflictAsync(
                game.Id,
                SaveStateConflictResolutionStrategy.KeepCloud,
                It.Is<SaveStateCloudMetadata>(metadata => metadata.EncryptionKey == "secret-key"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateCloudSyncStatus
            {
                GameId = game.Id,
                Provider = "Local Storage",
                Uploaded = false,
                Downloaded = true,
                HasConflict = false,
                ConflictType = SaveStateConflictType.None,
                SyncedAtUtc = _timeProvider.UtcNow,
                IsEncrypted = true,
                Message = "resolved"
            }));

        var viewModel = CreateSut();

        // Act
        await viewModel.ViewConflictsCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(service => service.ShowInputDialogAsync(
            "Cloud Save Encryption Key",
            It.IsAny<string>(),
            "Encryption key",
            true), Times.Once);
        _saveStateCloudServiceMock.Verify(service => service.ResolveConflictAsync(
            game.Id,
            SaveStateConflictResolutionStrategy.KeepCloud,
            It.Is<SaveStateCloudMetadata>(metadata => metadata.EncryptionKey == "secret-key"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ViewConflictsAsync_WithEncryptedSaveStateKeepCloudAndNoKey_ShowsError()
    {
        // Arrange
        var game = Game.Create("Parasite Eve");
        var dialogKey = $"SaveState::{game.Id:N}::{game.Title}";
        _gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { game });

        _saveStateCloudServiceMock
            .Setup(service => service.DetectConflictsAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateConflictResolution
            {
                GameId = game.Id,
                Type = SaveStateConflictType.CloudNewer,
                DetectedAtUtc = _timeProvider.UtcNow,
                LocalVersion = CreateVersion(game.Id, 1024, "local"),
                CloudVersion = CreateVersion(game.Id, 4096, "cloud") with
                {
                    IsEncrypted = true,
                    EncryptionKeyFingerprint = "AA11BB22"
                }
            }));

        _dialogServiceMock
            .Setup(service => service.ShowConflictResolutionDialogAsync(It.IsAny<SyncConflictViewModel[]>()))
            .ReturnsAsync(new ConflictResolutionResult(new Dictionary<string, string>
            {
                [dialogKey] = "Keep Cloud"
            }));
        _dialogServiceMock
            .Setup(service => service.ShowInputDialogAsync(
                "Cloud Save Encryption Key",
                It.IsAny<string>(),
                "Encryption key",
                true))
            .ReturnsAsync((string?)null);

        var viewModel = CreateSut();

        // Act
        await viewModel.ViewConflictsCommand.ExecuteAsync(null);

        // Assert
        _saveStateCloudServiceMock.Verify(service => service.ResolveConflictAsync(
            game.Id,
            SaveStateConflictResolutionStrategy.KeepCloud,
            It.IsAny<SaveStateCloudMetadata>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _notificationServiceMock.Verify(
            notification => notification.ShowError(
                It.Is<string>(message =>
                    message.Contains("No conflicts were resolved", StringComparison.Ordinal) &&
                    message.Contains("no encryption key was provided", StringComparison.OrdinalIgnoreCase)),
                null,
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task ViewConflictsAsync_WithSharedEncryptedFingerprint_PromptsForKeyOnce()
    {
        // Arrange
        var gameOne = Game.Create("Final Fantasy IX");
        var gameTwo = Game.Create("Vagrant Story");
        var dialogKeyOne = $"SaveState::{gameOne.Id:N}::{gameOne.Title}";
        var dialogKeyTwo = $"SaveState::{gameTwo.Id:N}::{gameTwo.Title}";

        _gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { gameOne, gameTwo });

        _saveStateCloudServiceMock
            .Setup(service => service.DetectConflictsAsync(gameOne.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateConflictResolution
            {
                GameId = gameOne.Id,
                Type = SaveStateConflictType.BothModified,
                DetectedAtUtc = _timeProvider.UtcNow,
                LocalVersion = CreateVersion(gameOne.Id, 1024, "local-one"),
                CloudVersion = CreateVersion(gameOne.Id, 3072, "cloud-one") with
                {
                    IsEncrypted = true,
                    EncryptionKeyFingerprint = "SHARED123"
                }
            }));
        _saveStateCloudServiceMock
            .Setup(service => service.DetectConflictsAsync(gameTwo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateConflictResolution
            {
                GameId = gameTwo.Id,
                Type = SaveStateConflictType.CloudNewer,
                DetectedAtUtc = _timeProvider.UtcNow,
                LocalVersion = CreateVersion(gameTwo.Id, 2048, "local-two"),
                CloudVersion = CreateVersion(gameTwo.Id, 4096, "cloud-two") with
                {
                    IsEncrypted = true,
                    EncryptionKeyFingerprint = "SHARED123"
                }
            }));

        _dialogServiceMock
            .Setup(service => service.ShowConflictResolutionDialogAsync(It.IsAny<SyncConflictViewModel[]>()))
            .ReturnsAsync(new ConflictResolutionResult(new Dictionary<string, string>
            {
                [dialogKeyOne] = "Keep Cloud",
                [dialogKeyTwo] = "Keep Cloud"
            }));
        _dialogServiceMock
            .Setup(service => service.ShowInputDialogAsync(
                "Cloud Save Encryption Key",
                It.IsAny<string>(),
                "Encryption key",
                true))
            .ReturnsAsync("shared-secret");

        _saveStateCloudServiceMock
            .Setup(service => service.ResolveConflictAsync(
                gameOne.Id,
                SaveStateConflictResolutionStrategy.KeepCloud,
                It.Is<SaveStateCloudMetadata>(metadata => metadata.EncryptionKey == "shared-secret"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateCloudSyncStatus
            {
                GameId = gameOne.Id,
                Provider = "Local Storage",
                Uploaded = false,
                Downloaded = true,
                HasConflict = false,
                ConflictType = SaveStateConflictType.None,
                SyncedAtUtc = _timeProvider.UtcNow,
                IsEncrypted = true,
                Message = "resolved"
            }));
        _saveStateCloudServiceMock
            .Setup(service => service.ResolveConflictAsync(
                gameTwo.Id,
                SaveStateConflictResolutionStrategy.KeepCloud,
                It.Is<SaveStateCloudMetadata>(metadata => metadata.EncryptionKey == "shared-secret"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateCloudSyncStatus
            {
                GameId = gameTwo.Id,
                Provider = "Local Storage",
                Uploaded = false,
                Downloaded = true,
                HasConflict = false,
                ConflictType = SaveStateConflictType.None,
                SyncedAtUtc = _timeProvider.UtcNow,
                IsEncrypted = true,
                Message = "resolved"
            }));

        var viewModel = CreateSut();

        // Act
        await viewModel.ViewConflictsCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(service => service.ShowInputDialogAsync(
            "Cloud Save Encryption Key",
            It.IsAny<string>(),
            "Encryption key",
            true), Times.Once);
        _saveStateCloudServiceMock.Verify(service => service.ResolveConflictAsync(
            gameOne.Id,
            SaveStateConflictResolutionStrategy.KeepCloud,
            It.Is<SaveStateCloudMetadata>(metadata => metadata.EncryptionKey == "shared-secret"),
            It.IsAny<CancellationToken>()), Times.Once);
        _saveStateCloudServiceMock.Verify(service => service.ResolveConflictAsync(
            gameTwo.Id,
            SaveStateConflictResolutionStrategy.KeepCloud,
            It.Is<SaveStateCloudMetadata>(metadata => metadata.EncryptionKey == "shared-secret"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ViewBackgroundSyncDetailsAsync_ShowsDaemonTelemetry()
    {
        // Arrange
        var nowUtc = new DateTime(2026, 2, 13, 18, 45, 0, DateTimeKind.Utc);
        var status = new SaveStateCloudDaemonStatus
        {
            Enabled = true,
            IsRunning = true,
            UpdatedAtUtc = nowUtc,
            LastSyncAtUtc = nowUtc.AddMinutes(-7),
            LastGameId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SuccessfulSyncCount = 12,
            FailedSyncCount = 1,
            ConflictCount = 2,
            SkippedCount = 3,
            LastMessage = "Cycle completed with minor issues."
        };

        _monitorMock
            .SetupGet(monitor => monitor.CurrentStatus)
            .Returns(status);
        _dialogServiceMock
            .Setup(service => service.ShowInformationAsync(
                "Background Save-State Sync",
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var viewModel = CreateSut();

        // Act
        await viewModel.ViewBackgroundSyncDetailsCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(service => service.ShowInformationAsync(
            "Background Save-State Sync",
            It.Is<string>(details =>
                details.Contains("Enabled: True", StringComparison.Ordinal) &&
                details.Contains("Running: True", StringComparison.Ordinal) &&
                details.Contains("Successful syncs: 12", StringComparison.Ordinal) &&
                details.Contains("Failed syncs: 1", StringComparison.Ordinal) &&
                details.Contains("Conflicts: 2", StringComparison.Ordinal) &&
                details.Contains("Action: Open 'View Conflicts'", StringComparison.Ordinal))),
            Times.Once);
    }

    [Fact]
    public async Task ViewBackgroundSyncDetailsAsync_WhenDialogFails_ShowsNotificationError()
    {
        // Arrange
        _dialogServiceMock
            .Setup(service => service.ShowInformationAsync(
                "Background Save-State Sync",
                It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Dialog unavailable"));

        var viewModel = CreateSut();

        // Act
        await viewModel.ViewBackgroundSyncDetailsCommand.ExecuteAsync(null);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowError("Failed to show background sync details", null, It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void ApplyDaemonStatus_WhenFailureCountIncreases_ShowsProactiveFailureAlert()
    {
        // Arrange
        var viewModel = CreateSut();
        var firstFailureStatus = CreateDaemonStatus(
            successfulSyncCount: 4,
            failedSyncCount: 1,
            conflictCount: 0,
            skippedCount: 0,
            message: "Upload timeout");

        // Act
        InvokeApplyDaemonStatus(viewModel, firstFailureStatus);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowError(
                It.Is<string>(message =>
                    message.Contains("1 new failure", StringComparison.OrdinalIgnoreCase) &&
                    message.Contains("Upload timeout", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void ApplyDaemonStatus_WhenConflictCountIncreases_ShowsProactiveConflictAlert()
    {
        // Arrange
        var viewModel = CreateSut();
        var conflictStatus = CreateDaemonStatus(
            successfulSyncCount: 3,
            failedSyncCount: 0,
            conflictCount: 2,
            skippedCount: 1,
            message: "Conflict detected");

        // Act
        InvokeApplyDaemonStatus(viewModel, conflictStatus);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowWarning(
                It.Is<string>(message =>
                    message.Contains("2 new conflicts", StringComparison.OrdinalIgnoreCase) &&
                    message.Contains("View Conflicts", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void ApplyDaemonStatus_WhenDaemonHealthy_ShowsHealthyCueAndNoQuickActions()
    {
        // Arrange
        var viewModel = CreateSut();
        var healthyStatus = CreateDaemonStatus(
            successfulSyncCount: 8,
            failedSyncCount: 0,
            conflictCount: 0,
            skippedCount: 1,
            message: "All clear");

        // Act
        InvokeApplyDaemonStatus(viewModel, healthyStatus);

        // Assert
        viewModel.BackgroundDaemonHealthStatus.Should().Be("Healthy");
        viewModel.BackgroundDaemonHealthCue.Should().Be("Background sync is operating normally.");
        viewModel.HasBackgroundQuickActions.Should().BeFalse();
        viewModel.ShowResolveConflictsQuickAction.Should().BeFalse();
        viewModel.ShowRetrySyncQuickAction.Should().BeFalse();
        viewModel.ShowConfigureProviderQuickAction.Should().BeFalse();
    }

    [Fact]
    public void ApplyDaemonStatus_WhenDaemonHasConflicts_ExposesResolveQuickAction()
    {
        // Arrange
        var viewModel = CreateSut();
        var status = CreateDaemonStatus(
            successfulSyncCount: 3,
            failedSyncCount: 0,
            conflictCount: 2,
            skippedCount: 0,
            message: "Conflicts detected");

        // Act
        InvokeApplyDaemonStatus(viewModel, status);

        // Assert
        viewModel.BackgroundDaemonHealthStatus.Should().Be("Warning");
        viewModel.HasBackgroundQuickActions.Should().BeTrue();
        viewModel.ShowResolveConflictsQuickAction.Should().BeTrue();
        viewModel.ShowRetrySyncQuickAction.Should().BeFalse();
        viewModel.ShowConfigureProviderQuickAction.Should().BeFalse();
    }

    [Fact]
    public void ApplyDaemonStatus_WhenDaemonHasFailures_ExposesRetryAndSettingsQuickActions()
    {
        // Arrange
        var viewModel = CreateSut();
        var status = CreateDaemonStatus(
            successfulSyncCount: 2,
            failedSyncCount: 3,
            conflictCount: 1,
            skippedCount: 0,
            message: "Provider timeout");

        // Act
        InvokeApplyDaemonStatus(viewModel, status);

        // Assert
        viewModel.BackgroundDaemonHealthStatus.Should().Be("Critical");
        viewModel.HasBackgroundQuickActions.Should().BeTrue();
        viewModel.ShowResolveConflictsQuickAction.Should().BeTrue();
        viewModel.ShowRetrySyncQuickAction.Should().BeTrue();
        viewModel.ShowConfigureProviderQuickAction.Should().BeTrue();
    }

    [Fact]
    public void ApplyDaemonStatus_WhenUpdatesArriveWithinCooldown_DebouncesFailureAlerts()
    {
        // Arrange
        var viewModel = CreateSut();

        var firstFailureStatus = CreateDaemonStatus(
            successfulSyncCount: 5,
            failedSyncCount: 1,
            conflictCount: 0,
            skippedCount: 0,
            message: "First failure");
        var secondFailureStatus = CreateDaemonStatus(
            successfulSyncCount: 5,
            failedSyncCount: 2,
            conflictCount: 0,
            skippedCount: 0,
            message: "Second failure");
        var heartbeatStatus = CreateDaemonStatus(
            successfulSyncCount: 5,
            failedSyncCount: 2,
            conflictCount: 0,
            skippedCount: 0,
            message: "Heartbeat");

        // Act
        InvokeApplyDaemonStatus(viewModel, firstFailureStatus);
        _timeProvider.Advance(TimeSpan.FromSeconds(10));
        InvokeApplyDaemonStatus(viewModel, secondFailureStatus);

        _notificationServiceMock.Verify(
            notification => notification.ShowError(It.IsAny<string>(), null, It.IsAny<int>()),
            Times.Once);

        _timeProvider.Advance(TimeSpan.FromSeconds(61));
        InvokeApplyDaemonStatus(viewModel, heartbeatStatus);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowError(It.IsAny<string>(), null, It.IsAny<int>()),
            Times.Exactly(2));
    }

    [Fact]
    public void ApplyDaemonStatus_WhenFailureAlertsDisabled_DoesNotShowFailureAlert()
    {
        // Arrange
        var viewModel = CreateSut();
        SetPrivateField(viewModel, "_daemonFailureAlertsEnabled", false);
        var status = CreateDaemonStatus(
            successfulSyncCount: 5,
            failedSyncCount: 1,
            conflictCount: 0,
            skippedCount: 0,
            message: "Failure should be muted");

        // Act
        InvokeApplyDaemonStatus(viewModel, status);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowError(It.IsAny<string>(), null, It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public void ApplyDaemonStatus_WhenCooldownConfigured_UsesConfiguredValue()
    {
        // Arrange
        var viewModel = CreateSut();
        SetPrivateField(viewModel, "_daemonAlertCooldownSeconds", 15);

        var firstFailureStatus = CreateDaemonStatus(
            successfulSyncCount: 5,
            failedSyncCount: 1,
            conflictCount: 0,
            skippedCount: 0,
            message: "First failure");
        var secondFailureStatus = CreateDaemonStatus(
            successfulSyncCount: 5,
            failedSyncCount: 2,
            conflictCount: 0,
            skippedCount: 0,
            message: "Second failure");
        var heartbeatStatus = CreateDaemonStatus(
            successfulSyncCount: 5,
            failedSyncCount: 2,
            conflictCount: 0,
            skippedCount: 0,
            message: "Heartbeat");

        // Act
        InvokeApplyDaemonStatus(viewModel, firstFailureStatus);
        _timeProvider.Advance(TimeSpan.FromSeconds(10));
        InvokeApplyDaemonStatus(viewModel, secondFailureStatus);
        _timeProvider.Advance(TimeSpan.FromSeconds(6));
        InvokeApplyDaemonStatus(viewModel, heartbeatStatus);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowError(It.IsAny<string>(), null, It.IsAny<int>()),
            Times.Exactly(2));
    }

    [Fact]
    public void OnSyncConflictDetected_WhenTriggeredWithinCooldown_ShowsSingleWarning()
    {
        // Arrange
        var viewModel = CreateSut();
        var firstConflict = new SyncConflictEventArgs
        {
            LocalPath = @"C:\Saves\first.state",
            RemotePath = "first.state",
            LocalModified = _timeProvider.UtcNow,
            RemoteModified = _timeProvider.UtcNow,
            RemoteSize = 1024
        };
        var secondConflict = new SyncConflictEventArgs
        {
            LocalPath = @"C:\Saves\second.state",
            RemotePath = "second.state",
            LocalModified = _timeProvider.UtcNow,
            RemoteModified = _timeProvider.UtcNow,
            RemoteSize = 2048
        };

        // Act
        InvokeOnSyncConflictDetected(viewModel, firstConflict);
        _timeProvider.Advance(TimeSpan.FromSeconds(5));
        InvokeOnSyncConflictDetected(viewModel, secondConflict);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowWarning(
                It.Is<string>(message => message.Contains("View Conflicts", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void OnSyncConflictDetected_WhenTriggeredAfterCooldown_ShowsWarningAgain()
    {
        // Arrange
        var viewModel = CreateSut();
        var conflict = new SyncConflictEventArgs
        {
            LocalPath = @"C:\Saves\conflict.state",
            RemotePath = "conflict.state",
            LocalModified = _timeProvider.UtcNow,
            RemoteModified = _timeProvider.UtcNow,
            RemoteSize = 512
        };

        // Act
        InvokeOnSyncConflictDetected(viewModel, conflict);
        _timeProvider.Advance(TimeSpan.FromSeconds(16));
        InvokeOnSyncConflictDetected(viewModel, conflict);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowWarning(
                It.Is<string>(message => message.Contains("View Conflicts", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task RetryBackgroundSyncAsync_WhenTriggered_RunsManualSync()
    {
        // Arrange
        _syncServiceMock
            .Setup(service => service.SyncAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult(
                Success: true,
                FilesUploaded: 1,
                FilesDownloaded: 2,
                Conflicts: 0,
                Errors: Array.Empty<string>()));
        var viewModel = CreateSut();

        // Act
        await viewModel.RetryBackgroundSyncCommand.ExecuteAsync(null);

        // Assert
        _syncServiceMock.Verify(service => service.SyncAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenBackgroundSyncSettingsAsync_WhenTriggered_OpensProviderConfigDialog()
    {
        // Arrange
        _dialogServiceMock
            .Setup(service => service.ShowCloudProviderConfigDialogAsync(It.IsAny<CloudProviderConfigResult?>()))
            .ReturnsAsync((CloudProviderConfigResult?)null);
        var viewModel = CreateSut();

        // Act
        await viewModel.OpenBackgroundSyncSettingsCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(
            service => service.ShowCloudProviderConfigDialogAsync(It.IsAny<CloudProviderConfigResult?>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveBackgroundConflictsAsync_WhenTriggered_UsesConflictResolutionFlow()
    {
        // Arrange
        var viewModel = CreateSut();

        // Act
        await viewModel.ResolveBackgroundConflictsCommand.ExecuteAsync(null);

        // Assert
        _notificationServiceMock.Verify(
            notification => notification.ShowInfo("No conflicts detected.", null, It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task DaemonCycle_ConflictDetection_QuickActionRecovery_CompletesEndToEndFlow()
    {
        // Arrange
        var fileConflict = new SyncConflictEventArgs
        {
            LocalPath = @"C:\Saves\chrono-trigger.state",
            RemotePath = "chrono-trigger.state",
            LocalModified = _timeProvider.UtcNow,
            RemoteModified = _timeProvider.UtcNow.AddSeconds(5),
            RemoteSize = 4096
        };

        _syncServiceMock
            .Setup(service => service.GetConflictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { fileConflict });
        _syncServiceMock
            .Setup(service => service.ResolveConflictAsync(
                fileConflict.LocalPath,
                "Keep Local",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _syncServiceMock
            .Setup(service => service.SyncAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult(
                Success: true,
                FilesUploaded: 2,
                FilesDownloaded: 1,
                Conflicts: 0,
                Errors: Array.Empty<string>()));

        _dialogServiceMock
            .Setup(service => service.ShowConflictResolutionDialogAsync(It.IsAny<SyncConflictViewModel[]>()))
            .ReturnsAsync(new ConflictResolutionResult(new Dictionary<string, string>
            {
                [fileConflict.RemotePath] = "Keep Local"
            }));
        _dialogServiceMock
            .Setup(service => service.ShowCloudProviderConfigDialogAsync(It.IsAny<CloudProviderConfigResult?>()))
            .ReturnsAsync((CloudProviderConfigResult?)null);

        var viewModel = CreateSut();
        var degradedStatus = CreateDaemonStatus(
            successfulSyncCount: 5,
            failedSyncCount: 1,
            conflictCount: 1,
            skippedCount: 0,
            message: "Cycle failed due to provider timeout.");

        // Act: daemon cycle enters degraded state.
        InvokeApplyDaemonStatus(viewModel, degradedStatus);

        // Act: conflict signal is raised during transfer.
        InvokeOnSyncConflictDetected(viewModel, fileConflict);

        // Act: quick-action recovery path.
        await viewModel.ResolveBackgroundConflictsCommand.ExecuteAsync(null);
        await viewModel.RetryBackgroundSyncCommand.ExecuteAsync(null);
        await viewModel.OpenBackgroundSyncSettingsCommand.ExecuteAsync(null);

        // Act: daemon reports healthy state after recovery.
        var recoveredStatus = CreateDaemonStatus(
            successfulSyncCount: 8,
            failedSyncCount: 0,
            conflictCount: 0,
            skippedCount: 0,
            message: "Cycle completed successfully.");
        InvokeApplyDaemonStatus(viewModel, recoveredStatus);

        // Assert
        viewModel.BackgroundDaemonHealthStatus.Should().Be("Healthy");
        viewModel.HasBackgroundQuickActions.Should().BeFalse();
        viewModel.ShowResolveConflictsQuickAction.Should().BeFalse();
        viewModel.ShowRetrySyncQuickAction.Should().BeFalse();
        viewModel.ShowConfigureProviderQuickAction.Should().BeFalse();

        _syncServiceMock.Verify(service => service.GetConflictsAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _syncServiceMock.Verify(service => service.ResolveConflictAsync(
            fileConflict.LocalPath,
            "Keep Local",
            It.IsAny<CancellationToken>()), Times.Once);
        _syncServiceMock.Verify(service => service.SyncAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dialogServiceMock.Verify(
            service => service.ShowCloudProviderConfigDialogAsync(It.IsAny<CloudProviderConfigResult?>()),
            Times.Once);

        _notificationServiceMock.Verify(
            notification => notification.ShowWarning(
                It.Is<string>(message => message.Contains("Sync conflicts detected during transfer", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
        _notificationServiceMock.Verify(
            notification => notification.ShowSuccess(
                It.Is<string>(message => message.Contains("Successfully resolved 1 of 1", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
        _notificationServiceMock.Verify(
            notification => notification.ShowSuccess(
                It.Is<string>(message => message.Contains("Sync complete", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task DaemonProcessorCycle_ConflictDetection_QuickActionRecovery_CompletesIntegrationFlow()
    {
        // Arrange
        var game = Game.Create("Chrono Trigger Integration");
        var daemonGameRepositoryMock = new Mock<IGameRepository>();
        daemonGameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { game });

        _gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { game });

        _saveStateCloudServiceMock
            .Setup(service => service.DetectConflictsAsync(game.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateConflictResolution
            {
                GameId = game.Id,
                Type = SaveStateConflictType.None,
                DetectedAtUtc = _timeProvider.UtcNow
            }));

        _saveStateCloudServiceMock
            .SetupSequence(service => service.SyncSaveStateAsync(
                game.Id,
                It.IsAny<SaveStateCloudMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateCloudSyncStatus
            {
                GameId = game.Id,
                Provider = "Local Storage",
                Uploaded = false,
                Downloaded = false,
                HasConflict = true,
                ConflictType = SaveStateConflictType.BothModified,
                SyncedAtUtc = _timeProvider.UtcNow,
                IsEncrypted = false,
                Message = "Background conflict detected"
            }))
            .ReturnsAsync(Result.Success(new SaveStateCloudSyncStatus
            {
                GameId = game.Id,
                Provider = "Local Storage",
                Uploaded = true,
                Downloaded = false,
                HasConflict = false,
                ConflictType = SaveStateConflictType.None,
                SyncedAtUtc = _timeProvider.UtcNow,
                IsEncrypted = false,
                Message = "Background sync recovered"
            }));

        var fileConflict = new SyncConflictEventArgs
        {
            LocalPath = @"C:\Saves\chrono-trigger-integration.state",
            RemotePath = "chrono-trigger-integration.state",
            LocalModified = _timeProvider.UtcNow,
            RemoteModified = _timeProvider.UtcNow.AddSeconds(5),
            RemoteSize = 4096
        };

        _syncServiceMock
            .Setup(service => service.GetConflictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { fileConflict });
        _syncServiceMock
            .Setup(service => service.ResolveConflictAsync(
                fileConflict.LocalPath,
                "Keep Local",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _syncServiceMock
            .Setup(service => service.SyncAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult(
                Success: true,
                FilesUploaded: 1,
                FilesDownloaded: 0,
                Conflicts: 0,
                Errors: Array.Empty<string>()));

        _dialogServiceMock
            .Setup(service => service.ShowConflictResolutionDialogAsync(It.IsAny<SyncConflictViewModel[]>()))
            .ReturnsAsync(new ConflictResolutionResult(new Dictionary<string, string>
            {
                [fileConflict.RemotePath] = "Keep Local"
            }));
        _dialogServiceMock
            .Setup(service => service.ShowCloudProviderConfigDialogAsync(It.IsAny<CloudProviderConfigResult?>()))
            .ReturnsAsync((CloudProviderConfigResult?)null);

        var daemonOptions = CreateCloudSyncOptions(enabled: true, maxGamesPerCycle: 1);
        var monitor = new SaveStateCloudSyncMonitor(_timeProvider, Options.Create(daemonOptions));
        var daemonProcessor = CreateDaemonProcessor(
            daemonGameRepositoryMock.Object,
            _saveStateCloudServiceMock.Object,
            daemonOptions,
            monitor);
        var viewModel = CreateSut(monitor);

        // Act: first daemon cycle surfaces conflict telemetry.
        await daemonProcessor.ProcessCycleAsync();
        InvokeApplyDaemonStatus(viewModel, monitor.CurrentStatus);

        // Act: manual conflict signal + quick actions execute recovery path.
        InvokeOnSyncConflictDetected(viewModel, fileConflict);
        await viewModel.ResolveBackgroundConflictsCommand.ExecuteAsync(null);
        await viewModel.RetryBackgroundSyncCommand.ExecuteAsync(null);
        await viewModel.OpenBackgroundSyncSettingsCommand.ExecuteAsync(null);

        // Act: second daemon cycle succeeds after recovery.
        await daemonProcessor.ProcessCycleAsync();
        var recoveredStatus = monitor.CurrentStatus with
        {
            FailedSyncCount = 0,
            ConflictCount = 0,
            IsRunning = true,
            LastMessage = "Cycle completed successfully."
        };
        InvokeApplyDaemonStatus(viewModel, recoveredStatus);

        // Assert
        monitor.CurrentStatus.SuccessfulSyncCount.Should().Be(1);
        monitor.CurrentStatus.ConflictCount.Should().Be(1);

        viewModel.BackgroundDaemonHealthStatus.Should().Be("Healthy");
        viewModel.HasBackgroundQuickActions.Should().BeFalse();
        viewModel.ShowResolveConflictsQuickAction.Should().BeFalse();
        viewModel.ShowRetrySyncQuickAction.Should().BeFalse();
        viewModel.ShowConfigureProviderQuickAction.Should().BeFalse();

        _saveStateCloudServiceMock.Verify(service => service.SyncSaveStateAsync(
            game.Id,
            It.IsAny<SaveStateCloudMetadata>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _syncServiceMock.Verify(service => service.ResolveConflictAsync(
            fileConflict.LocalPath,
            "Keep Local",
            It.IsAny<CancellationToken>()), Times.Once);
        _syncServiceMock.Verify(service => service.SyncAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dialogServiceMock.Verify(
            service => service.ShowCloudProviderConfigDialogAsync(It.IsAny<CloudProviderConfigResult?>()),
            Times.Once);

        _notificationServiceMock.Verify(
            notification => notification.ShowWarning(
                It.Is<string>(message => message.Contains("Sync conflicts detected during transfer", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
        _notificationServiceMock.Verify(
            notification => notification.ShowSuccess(
                It.Is<string>(message => message.Contains("Successfully resolved 1 of 1", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
        _notificationServiceMock.Verify(
            notification => notification.ShowSuccess(
                It.Is<string>(message => message.Contains("Sync complete", StringComparison.Ordinal)),
                null,
                It.IsAny<int>()),
            Times.Once);
    }

    private CloudSyncViewModel CreateSut(ISaveStateCloudSyncMonitor? monitor = null)
    {
        return new CloudSyncViewModel(
            _mediatorMock.Object,
            _syncServiceMock.Object,
            _cloudGamingManagerMock.Object,
            _networkMonitorMock.Object,
            _notificationServiceMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object,
            _cloudCatalogServiceMock.Object,
            _timeProvider,
            _saveStateCloudServiceMock.Object,
            _gameRepositoryMock.Object,
            monitor ?? _monitorMock.Object);
    }

    private SaveStateCloudSyncDaemonProcessor CreateDaemonProcessor(
        IGameRepository gameRepository,
        ISaveStateCloudService cloudService,
        CloudSyncOptions options,
        SaveStateCloudSyncMonitor monitor)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => gameRepository);
        services.AddScoped(_ => cloudService);

        var provider = services.BuildServiceProvider();
        return new SaveStateCloudSyncDaemonProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            _timeProvider,
            monitor,
            NullLogger<SaveStateCloudSyncDaemonProcessor>.Instance);
    }

    private static CloudSyncOptions CreateCloudSyncOptions(bool enabled, int maxGamesPerCycle)
    {
        return new CloudSyncOptions
        {
            SaveStateDaemon = new SaveStateCloudDaemonOptions
            {
                Enabled = enabled,
                IntervalSeconds = 60,
                MaxGamesPerCycle = maxGamesPerCycle,
                ForceUploadOnConflict = false
            }
        };
    }

    private SaveStateCloudDaemonStatus CreateDaemonStatus(
        int successfulSyncCount,
        int failedSyncCount,
        int conflictCount,
        int skippedCount,
        string message)
    {
        return new SaveStateCloudDaemonStatus
        {
            Enabled = true,
            IsRunning = true,
            UpdatedAtUtc = _timeProvider.UtcNow,
            LastSyncAtUtc = _timeProvider.UtcNow,
            LastGameId = Guid.NewGuid(),
            SuccessfulSyncCount = successfulSyncCount,
            FailedSyncCount = failedSyncCount,
            ConflictCount = conflictCount,
            SkippedCount = skippedCount,
            LastMessage = message
        };
    }

    private static void InvokeApplyDaemonStatus(CloudSyncViewModel viewModel, SaveStateCloudDaemonStatus status)
    {
        var method = typeof(CloudSyncViewModel).GetMethod(
            "ApplyDaemonStatus",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (method is null)
        {
            throw new InvalidOperationException("ApplyDaemonStatus method was not found.");
        }

        method.Invoke(viewModel, new object[] { status });
    }

    private static void InvokeOnSyncConflictDetected(CloudSyncViewModel viewModel, SyncConflictEventArgs conflict)
    {
        var method = typeof(CloudSyncViewModel).GetMethod(
            "OnSyncConflictDetected",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (method is null)
        {
            throw new InvalidOperationException("OnSyncConflictDetected method was not found.");
        }

        method.Invoke(viewModel, new object?[] { null, conflict });
    }

    private static void SetPrivateField<TValue>(CloudSyncViewModel viewModel, string fieldName, TValue value)
    {
        var field = typeof(CloudSyncViewModel).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' was not found.");
        }

        field.SetValue(viewModel, value);
    }

    private static SaveStateCloudVersion CreateVersion(Guid gameId, long sizeBytes, string suffix)
    {
        return new SaveStateCloudVersion
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            SaveStateId = Guid.NewGuid(),
            VersionName = $"version-{suffix}",
            StoragePath = $"savestates/{gameId:N}/{suffix}.state",
            ContentHash = $"{suffix}-hash",
            FileSizeBytes = sizeBytes,
            CreatedAtUtc = new DateTime(2026, 2, 13, 9, 0, 0, DateTimeKind.Utc),
            IsEncrypted = false
        };
    }
}
