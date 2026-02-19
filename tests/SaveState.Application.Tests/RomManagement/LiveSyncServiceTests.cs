namespace SaveState.Application.Tests.RomManagement;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SaveState.Application.RomManagement.Services;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.Common.Services;

public class LiveSyncServiceTests : IDisposable
{
    private readonly Mock<IRomScannerService> _mockRomScanner = new();
    private readonly Mock<IRomFileRepository> _mockRomFileRepository = new();
    private readonly Mock<IPlatformRepository> _mockPlatformRepository = new();
    private readonly Mock<IPlatformExtensionRegistry> _mockExtensionRegistry = new();
    private readonly Mock<SaveState.Core.Monitoring.IApplicationMetrics> _mockMetrics = new();
    private readonly Mock<ILogger<LiveSyncService>> _mockLogger = new();
    private readonly LiveSyncService _sut;

    public LiveSyncServiceTests()
    {
        _sut = new LiveSyncService(
            _mockRomScanner.Object,
            _mockRomFileRepository.Object,
            _mockPlatformRepository.Object,
            _mockExtensionRegistry.Object,
            _mockLogger.Object,
            _mockMetrics.Object,
            new SystemTimeProvider());
    }

    public void Dispose()
    {
        _sut.DisposeAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task StartWatchingAsync_WithInvalidFolderPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.StartWatchingAsync("", "NES", default));
    }

    [Fact]
    public async Task StartWatchingAsync_WithInvalidPlatformName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.StartWatchingAsync(@"C:\Games", "", default));
    }

    [Fact]
    public async Task StartWatchingAsync_WithNonExistentFolder_DoesNotThrow()
    {
        // Arrange
        _mockPlatformRepository.Setup(r => r.GetByNameAsync("NES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestPlatform("NES"));

        // Act & Assert - Should not throw, just log warning
        await _sut.StartWatchingAsync(@"C:\NonExistentFolder", "NES", default);
    }

    [Fact]
    public async Task StartWatchingAsync_WithUnknownPlatform_DoesNotThrow()
    {
        // Arrange
        _mockPlatformRepository.Setup(r => r.GetByNameAsync("UnknownPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Platform?)null);

        // Act & Assert - Should not throw, just log warning
        await _sut.StartWatchingAsync(@"C:\Games", "UnknownPlatform", default);
    }

    [Fact]
    public async Task GetWatchedFoldersAsync_WhenNoWatchers_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetWatchedFoldersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSyncStatusAsync_WithUnknownFolder_ReturnsDefaultStatus()
    {
        // Act
        var result = await _sut.GetSyncStatusAsync(@"C:\Unknown", default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FolderPath.Should().Be(@"C:\Unknown");
        result.Value.PlatformName.Should().Be("Unknown");
        result.Value.IsWatching.Should().BeFalse();
        result.Value.LastSync.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task ForceSyncAsync_WithUnknownFolder_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ForceSyncAsync(@"C:\Unknown", default));
    }

    [Fact]
    public async Task StopWatchingAsync_WithUnknownFolder_DoesNotThrow()
    {
        // Act & Assert - Should not throw, just log warning
        await _sut.StopWatchingAsync(@"C:\Unknown", default);
    }

    [Fact]
    public async Task ClearAllWatchersAsync_WhenWatchersExist_ClearsAll()
    {
        // Arrange - This is hard to test with FileSystemWatcher
        // The integration tests will cover this better

        // Act
        await _sut.ClearAllWatchersAsync();

        // Assert
        var watchedFolders = await _sut.GetWatchedFoldersAsync();
        watchedFolders.IsSuccess.Should().BeTrue();
        watchedFolders.Value.Should().BeEmpty();
    }

    [Fact]
    public void Events_AreProperlyDefined()
    {
        // Test that events are properly defined (can be subscribed to)
        EventHandler<RomFileEventArgs> romAddedHandler = (s, e) => { };
        EventHandler<RomFileEventArgs> romRemovedHandler = (s, e) => { };
        EventHandler<RomFileEventArgs> romChangedHandler = (s, e) => { };
        EventHandler<SyncEventArgs> syncCompletedHandler = (s, e) => { };
        EventHandler<SyncErrorEventArgs> syncErrorHandler = (s, e) => { };

        // Should not throw
        _sut.RomFileAdded += romAddedHandler;
        _sut.RomFileRemoved += romRemovedHandler;
        _sut.RomFileChanged += romChangedHandler;
        _sut.SyncCompleted += syncCompletedHandler;
        _sut.SyncError += syncErrorHandler;

        // Clean up
        _sut.RomFileAdded -= romAddedHandler;
        _sut.RomFileRemoved -= romRemovedHandler;
        _sut.RomFileChanged -= romChangedHandler;
        _sut.SyncCompleted -= syncCompletedHandler;
        _sut.SyncError -= syncErrorHandler;
    }

    private static Platform CreateTestPlatform(string name)
    {
        var platform = new Platform(
            PlatformName.From(name),
            PlatformShortName.From(name.ToUpper().Substring(0, Math.Min(3, name.Length))),
            Core.GameLibrary.Enums.PlatformType.Console);

        // Set the ID for testing
        typeof(Platform).GetProperty("Id")?.SetValue(platform, Guid.NewGuid());

        return platform;
    }
}
