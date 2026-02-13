using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Services.DTOs;
using SaveState.Infrastructure.SaveStates;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.SaveStates;

public class SaveStateCloudSyncDaemonProcessorTests
{
    [Fact]
    public async Task ProcessCycleAsync_WhenDaemonDisabled_DoesNotQueryRepositories()
    {
        // Arrange
        var gameRepositoryMock = new Mock<IGameRepository>(MockBehavior.Strict);
        var cloudServiceMock = new Mock<ISaveStateCloudService>(MockBehavior.Strict);

        var timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 18, 0, 0, DateTimeKind.Utc));
        var options = CreateOptions(enabled: false);
        var monitor = new SaveStateCloudSyncMonitor(timeProvider, Options.Create(options));
        var processor = CreateProcessor(gameRepositoryMock.Object, cloudServiceMock.Object, options, timeProvider, monitor);

        // Act
        await processor.ProcessCycleAsync();

        // Assert
        monitor.CurrentStatus.Enabled.Should().BeFalse();
        monitor.CurrentStatus.SuccessfulSyncCount.Should().Be(0);
        gameRepositoryMock.Verify(
            repository => repository.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        cloudServiceMock.Verify(
            service => service.SyncSaveStateAsync(It.IsAny<Guid>(), It.IsAny<SaveStateCloudMetadata>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessCycleAsync_WhenSyncSucceeds_RecordsSuccess()
    {
        // Arrange
        var game = Game.Create("Chrono Trigger");
        var gameRepositoryMock = new Mock<IGameRepository>();
        gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Game> { game });

        var cloudServiceMock = new Mock<ISaveStateCloudService>();
        cloudServiceMock
            .Setup(service => service.SyncSaveStateAsync(game.Id, It.IsAny<SaveStateCloudMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new SaveStateCloudSyncStatus
            {
                GameId = game.Id,
                Provider = "Local Storage",
                Uploaded = true,
                Downloaded = false,
                HasConflict = false,
                ConflictType = SaveStateConflictType.None,
                SyncedAtUtc = new DateTime(2026, 2, 13, 18, 30, 0, DateTimeKind.Utc),
                IsEncrypted = false,
                Message = "Background sync completed"
            }));

        var timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 18, 30, 0, DateTimeKind.Utc));
        var options = CreateOptions(enabled: true);
        var monitor = new SaveStateCloudSyncMonitor(timeProvider, Options.Create(options));
        var processor = CreateProcessor(gameRepositoryMock.Object, cloudServiceMock.Object, options, timeProvider, monitor);

        // Act
        await processor.ProcessCycleAsync();

        // Assert
        var snapshot = monitor.CurrentStatus;
        snapshot.SuccessfulSyncCount.Should().Be(1);
        snapshot.FailedSyncCount.Should().Be(0);
        snapshot.ConflictCount.Should().Be(0);
        snapshot.SkippedCount.Should().Be(0);
        snapshot.LastGameId.Should().Be(game.Id);
        snapshot.LastMessage.Should().Contain("Background sync completed");
    }

    [Fact]
    public async Task ProcessCycleAsync_WhenSaveStateMissing_RecordsSkipped()
    {
        // Arrange
        var game = Game.Create("Final Fantasy VI");
        var gameRepositoryMock = new Mock<IGameRepository>();
        gameRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Game> { game });

        var cloudServiceMock = new Mock<ISaveStateCloudService>();
        cloudServiceMock
            .Setup(service => service.SyncSaveStateAsync(game.Id, It.IsAny<SaveStateCloudMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SaveStateCloudSyncStatus>("No local save state", ErrorType.NotFound));

        var timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 19, 0, 0, DateTimeKind.Utc));
        var options = CreateOptions(enabled: true);
        var monitor = new SaveStateCloudSyncMonitor(timeProvider, Options.Create(options));
        var processor = CreateProcessor(gameRepositoryMock.Object, cloudServiceMock.Object, options, timeProvider, monitor);

        // Act
        await processor.ProcessCycleAsync();

        // Assert
        var snapshot = monitor.CurrentStatus;
        snapshot.SkippedCount.Should().Be(1);
        snapshot.FailedSyncCount.Should().Be(0);
        snapshot.SuccessfulSyncCount.Should().Be(0);
        snapshot.LastMessage.Should().Contain("Skipped");
        snapshot.LastGameId.Should().Be(game.Id);
    }

    private static SaveStateCloudSyncDaemonProcessor CreateProcessor(
        IGameRepository gameRepository,
        ISaveStateCloudService cloudService,
        CloudSyncOptions options,
        TestTimeProvider timeProvider,
        SaveStateCloudSyncMonitor monitor)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => gameRepository);
        services.AddScoped(_ => cloudService);

        var provider = services.BuildServiceProvider();
        return new SaveStateCloudSyncDaemonProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            timeProvider,
            monitor,
            NullLogger<SaveStateCloudSyncDaemonProcessor>.Instance);
    }

    private static CloudSyncOptions CreateOptions(bool enabled)
    {
        return new CloudSyncOptions
        {
            SaveStateDaemon = new SaveStateCloudDaemonOptions
            {
                Enabled = enabled,
                IntervalSeconds = 60,
                MaxGamesPerCycle = 10,
                ForceUploadOnConflict = false
            }
        };
    }
}
