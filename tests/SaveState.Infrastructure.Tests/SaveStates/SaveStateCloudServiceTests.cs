using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Services.DTOs;
using SaveState.Core.Sync;
using SaveState.Infrastructure.SaveStates;
using SaveState.Infrastructure.Sync;
using SaveState.Tests.Infrastructure;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.Tests.SaveStates;

public class SaveStateCloudServiceTests : IDisposable
{
    private readonly string _localRoot;
    private readonly string _cloudRoot;
    private readonly string _historyRoot;
    private readonly TestTimeProvider _timeProvider;
    private readonly Mock<ISaveStateRepository> _saveStateRepositoryMock;
    private readonly Mock<IUserPreferencesService> _preferencesMock;
    private readonly ICloudStorageProvider _localCloudProvider;
    private readonly CloudSaveEncryptionService _encryptionService;

    public SaveStateCloudServiceTests()
    {
        _localRoot = Path.Combine(Path.GetTempPath(), $"savestate-local-{Guid.NewGuid():N}");
        _cloudRoot = Path.Combine(Path.GetTempPath(), $"savestate-cloud-{Guid.NewGuid():N}");
        _historyRoot = Path.Combine(Path.GetTempPath(), $"savestate-history-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_localRoot);
        Directory.CreateDirectory(_cloudRoot);
        Directory.CreateDirectory(_historyRoot);

        _timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 12, 0, 0, DateTimeKind.Utc));
        _saveStateRepositoryMock = new Mock<ISaveStateRepository>(MockBehavior.Strict);
        _preferencesMock = new Mock<IUserPreferencesService>(MockBehavior.Strict);
        _localCloudProvider = new LocalFileStorageProvider(_cloudRoot, NullLogger<LocalFileStorageProvider>.Instance);
        _encryptionService = new CloudSaveEncryptionService(NullLogger<CloudSaveEncryptionService>.Instance);
    }

    [Fact]
    public async Task CreateVersionAsync_WithLatestSaveState_PersistsVersionHistory()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var saveState = CreateSaveState(gameId, "version-create.state", "version data");
        SetupCommonMocks(gameId, saveState);

        var sut = CreateSut();

        // Act
        var createResult = await sut.CreateVersionAsync(gameId, "Checkpoint A");
        var historyResult = await sut.GetVersionHistoryAsync(gameId);

        // Assert
        createResult.IsSuccess.Should().BeTrue();
        createResult.Value.Should().NotBeNull();
        createResult.Value!.VersionName.Should().Be("Checkpoint A");

        historyResult.IsSuccess.Should().BeTrue();
        historyResult.Value.Should().ContainSingle(v => v.Id == createResult.Value.Id);
    }

    [Fact]
    public async Task SyncSaveStateAsync_WithEncryption_UploadsEncryptedPayloadAndMetadata()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var saveState = CreateSaveState(gameId, "sync-encrypted.state", "state payload");
        SetupCommonMocks(gameId, saveState);

        var sut = CreateSut();

        // Act
        var result = await sut.SyncSaveStateAsync(gameId, new SaveStateCloudMetadata
        {
            EncryptionKey = "unit-test-key",
            VersionName = "Encrypted Upload",
            DeviceName = "TestRig"
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Uploaded.Should().BeTrue();
        result.Value.IsEncrypted.Should().BeTrue();
        result.Value.LocalVersion.Should().NotBeNull();

        var extension = Path.GetExtension(saveState.FilePath);
        var expectedCloudFile = Path.Combine(
            _cloudRoot,
            "savestates",
            gameId.ToString(),
            $"{saveState.Id}{extension}.enc");
        File.Exists(expectedCloudFile).Should().BeTrue();

        var expectedLatestMetadata = Path.Combine(
            _cloudRoot,
            "savestates",
            gameId.ToString(),
            "latest.json");
        File.Exists(expectedLatestMetadata).Should().BeTrue();
    }

    [Fact]
    public async Task DetectConflictsAsync_WhenCloudVersionMissing_ReturnsDeletedOnOneSide()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var saveState = CreateSaveState(gameId, "sync-missing-cloud.state", "state payload");
        SetupCommonMocks(gameId, saveState);

        var sut = CreateSut();

        // Act
        var result = await sut.DetectConflictsAsync(gameId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(SaveStateConflictType.DeletedOnOneSide);
        result.Value.LocalVersion.Should().NotBeNull();
        result.Value.CloudVersion.Should().BeNull();
    }

    [Fact]
    public async Task ResolveConflictAsync_KeepCloud_DownloadsCloudPayloadToLocalSave()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var saveState = CreateSaveState(gameId, "local-keep-cloud.state", "local-old");
        SetupCommonMocks(gameId, saveState);
        _saveStateRepositoryMock
            .Setup(r => r.UpdateAsync(saveState, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await SeedCloudLatestVersionAsync(gameId, saveState, "cloud-new", isEncrypted: false, encryptionKey: null);
        var sut = CreateSut();

        // Act
        var result = await sut.ResolveConflictAsync(gameId, SaveStateConflictResolutionStrategy.KeepCloud);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Downloaded.Should().BeTrue();
        result.Value.ConflictType.Should().Be(SaveStateConflictType.None);
        File.ReadAllText(saveState.FilePath).Should().Be("cloud-new");
        _saveStateRepositoryMock.Verify(r => r.UpdateAsync(saveState, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveConflictAsync_KeepCloud_WithEncryptedPayload_DecryptsBeforeRestore()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var saveState = CreateSaveState(gameId, "local-keep-cloud-encrypted.state", "local-old");
        var encryptionKey = "unit-test-key";

        SetupCommonMocks(gameId, saveState);
        _saveStateRepositoryMock
            .Setup(r => r.UpdateAsync(saveState, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await SeedCloudLatestVersionAsync(gameId, saveState, "cloud-encrypted", isEncrypted: true, encryptionKey);
        var sut = CreateSut();

        // Act
        var result = await sut.ResolveConflictAsync(
            gameId,
            SaveStateConflictResolutionStrategy.KeepCloud,
            new SaveStateCloudMetadata { EncryptionKey = encryptionKey });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Downloaded.Should().BeTrue();
        result.Value.IsEncrypted.Should().BeTrue();
        File.ReadAllText(saveState.FilePath).Should().Be("cloud-encrypted");
    }

    [Fact]
    public async Task SyncSaveStateAsync_WhenPreferredProviderUsesAlias_UsesMatchingProvider()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var saveState = CreateSaveState(gameId, "sync-provider-alias.state", "state payload");
        SetupCommonMocks(gameId, saveState, preferredProvider: "GoogleDrive");

        var oneDriveProvider = new RecordingCloudStorageProvider("OneDrive");
        var googleDriveProvider = new RecordingCloudStorageProvider("Google Drive");
        var sut = CreateSut(oneDriveProvider, googleDriveProvider);

        // Act
        var result = await sut.SyncSaveStateAsync(gameId, new SaveStateCloudMetadata());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Provider.Should().Be("Google Drive");
        googleDriveProvider.UploadCalls.Should().BeGreaterThan(0);
        oneDriveProvider.UploadCalls.Should().Be(0);
    }

    public void Dispose()
    {
        TryDeleteDirectory(_localRoot);
        TryDeleteDirectory(_cloudRoot);
        TryDeleteDirectory(_historyRoot);
    }

    private SaveStateCloudService CreateSut(params ICloudStorageProvider[] providers)
    {
        return new SaveStateCloudService(
            _saveStateRepositoryMock.Object,
            _preferencesMock.Object,
            providers.Length == 0 ? new[] { _localCloudProvider } : providers,
            _encryptionService,
            NullLogger<SaveStateCloudService>.Instance,
            _timeProvider,
            _historyRoot);
    }

    private void SetupCommonMocks(Guid gameId, SaveStateEntity saveState, string preferredProvider = "Local Storage")
    {
        _saveStateRepositoryMock
            .Setup(r => r.GetByGameIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SaveStateEntity> { saveState });

        _saveStateRepositoryMock
            .Setup(r => r.GetByIdAsync(saveState.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saveState);

        _preferencesMock
            .Setup(p => p.GetPreferredCloudProviderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferredProvider);
    }

    private SaveStateEntity CreateSaveState(Guid gameId, string fileName, string fileContents)
    {
        var filePath = Path.Combine(_localRoot, fileName);
        File.WriteAllText(filePath, fileContents);
        return SaveStateEntity.Create(gameId, filePath, TimeSpan.FromMinutes(5));
    }

    private async Task<SaveStateCloudVersion> SeedCloudLatestVersionAsync(
        Guid gameId,
        SaveStateEntity saveState,
        string cloudContents,
        bool isEncrypted,
        string? encryptionKey)
    {
        var extension = Path.GetExtension(saveState.FilePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".state";
        }

        var payloadSeedPath = Path.Combine(_localRoot, $"cloud-seed-{Guid.NewGuid():N}{extension}");
        await File.WriteAllTextAsync(payloadSeedPath, cloudContents);

        var remotePath = $"savestates/{gameId}/{saveState.Id}{extension}";
        var sourcePayloadPath = payloadSeedPath;
        string? fingerprint = null;
        string? encryptedPayloadPath = null;

        if (isEncrypted)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new InvalidOperationException("Encryption key is required when seeding encrypted cloud payloads.");
            }

            var encryptResult = await _encryptionService.EncryptFileAsync(payloadSeedPath, encryptionKey);
            encryptResult.IsSuccess.Should().BeTrue();
            encryptResult.Value.Should().NotBeNullOrWhiteSpace();

            encryptedPayloadPath = encryptResult.Value!;
            sourcePayloadPath = encryptedPayloadPath;
            remotePath += ".enc";
            fingerprint = _encryptionService.GetKeyFingerprint(encryptionKey);
        }

        try
        {
            var cloudPayloadPath = Path.Combine(_cloudRoot, remotePath.Replace('/', Path.DirectorySeparatorChar));
            var cloudPayloadDirectory = Path.GetDirectoryName(cloudPayloadPath);
            if (!string.IsNullOrWhiteSpace(cloudPayloadDirectory))
            {
                Directory.CreateDirectory(cloudPayloadDirectory);
            }

            File.Copy(sourcePayloadPath, cloudPayloadPath, overwrite: true);

            var version = new SaveStateCloudVersion
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                SaveStateId = saveState.Id,
                VersionName = "Cloud Latest",
                StoragePath = remotePath,
                ContentHash = $"cloud-{Guid.NewGuid():N}",
                FileSizeBytes = new FileInfo(cloudPayloadPath).Length,
                CreatedAtUtc = _timeProvider.UtcNow.AddMinutes(-2),
                IsEncrypted = isEncrypted,
                EncryptionKeyFingerprint = fingerprint
            };

            var metadataPath = Path.Combine(_cloudRoot, "savestates", gameId.ToString(), "latest.json");
            var metadataDirectory = Path.GetDirectoryName(metadataPath);
            if (!string.IsNullOrWhiteSpace(metadataDirectory))
            {
                Directory.CreateDirectory(metadataDirectory);
            }

            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(version, options);
            await File.WriteAllTextAsync(metadataPath, json);

            return version;
        }
        finally
        {
            if (File.Exists(payloadSeedPath))
            {
                File.Delete(payloadSeedPath);
            }

            if (!string.IsNullOrWhiteSpace(encryptedPayloadPath) && File.Exists(encryptedPayloadPath))
            {
                File.Delete(encryptedPayloadPath);
            }
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best effort test cleanup.
        }
    }

    private sealed class RecordingCloudStorageProvider : ICloudStorageProvider
    {
        public RecordingCloudStorageProvider(string providerName)
        {
            ProviderName = providerName;
        }

        public string ProviderName { get; }

        public bool IsAuthenticated => true;

        public int UploadCalls { get; private set; }

        public Task<bool> AuthenticateAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken ct = default)
        {
            UploadCalls++;
            return Task.FromResult(true);
        }

        public Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> DeleteFileAsync(string remotePath, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<IReadOnlyList<CloudFileInfo>> ListFilesAsync(string remotePath, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CloudFileInfo>>(Array.Empty<CloudFileInfo>());

        public Task<Result<CloudFileInfo>> GetFileInfoAsync(string remotePath, CancellationToken ct = default)
            => Task.FromResult(Result.Failure<CloudFileInfo>("Not found", ErrorType.NotFound));

        public Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default)
            => Task.FromResult(false);
    }
}
