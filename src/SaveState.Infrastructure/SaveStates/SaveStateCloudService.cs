using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.SaveStates;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Services.DTOs;
using SaveState.Core.Sync;
using SaveStateEntity = SaveState.Core.SaveStates.Entities.SaveState;

namespace SaveState.Infrastructure.SaveStates;

/// <summary>
/// Cloud synchronization service for save states with conflict detection and version tracking.
/// </summary>
public sealed class SaveStateCloudService : ISaveStateCloudService
{
    private const string CloudRootPath = "savestates";

    private readonly ISaveStateRepository _saveStateRepository;
    private readonly IUserPreferencesService _preferencesService;
    private readonly IReadOnlyList<ICloudStorageProvider> _cloudProviders;
    private readonly ICloudSaveEncryptionService _encryptionService;
    private readonly ILogger<SaveStateCloudService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly SaveStateCloudVersionStore _versionStore;

    public SaveStateCloudService(
        ISaveStateRepository saveStateRepository,
        IUserPreferencesService preferencesService,
        IEnumerable<ICloudStorageProvider> cloudProviders,
        ICloudSaveEncryptionService encryptionService,
        ILogger<SaveStateCloudService> logger,
        ITimeProvider timeProvider,
        string? versionHistoryRootPath = null)
    {
        _saveStateRepository = saveStateRepository;
        _preferencesService = preferencesService;
        _cloudProviders = cloudProviders.ToArray();
        _encryptionService = encryptionService;
        _logger = logger;
        _timeProvider = timeProvider;

        var resolvedVersionHistoryRootPath = versionHistoryRootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveState",
            "CloudSync",
            "Versions");
        var jsonOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        _versionStore = new SaveStateCloudVersionStore(
            logger,
            resolvedVersionHistoryRootPath,
            jsonOptions);
    }

    /// <inheritdoc />
    public async Task<Result<SaveStateCloudSyncStatus>> SyncSaveStateAsync(
        Guid gameId,
        SaveStateCloudMetadata metadata,
        CancellationToken ct = default)
    {
        metadata ??= new SaveStateCloudMetadata();

        var providerResult = await ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsFailure || providerResult.Value is null)
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                providerResult.Error ?? "Cloud provider is not configured.",
                providerResult.ErrorType);
        }

        var provider = providerResult.Value;

        var localSaveResult = await ResolveLocalSaveStateAsync(gameId, metadata.SaveStateId, ct).ConfigureAwait(false);
        if (localSaveResult.IsFailure || localSaveResult.Value is null)
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                localSaveResult.Error ?? "Local save state was not found.",
                localSaveResult.ErrorType);
        }

        var localSave = localSaveResult.Value;
        var isEncrypted = !string.IsNullOrWhiteSpace(metadata.EncryptionKey);
        var keyFingerprintResult = ResolveEncryptionKeyFingerprint(metadata.EncryptionKey);
        if (keyFingerprintResult.IsFailure)
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                keyFingerprintResult.Error ?? "Failed to compute encryption key fingerprint.",
                keyFingerprintResult.ErrorType);
        }
        var keyFingerprint = keyFingerprintResult.Value;

        var localVersionResult = await BuildVersionAsync(
            localSave,
            metadata.VersionName,
            metadata.DeviceName,
            isEncrypted,
            keyFingerprint,
            ct).ConfigureAwait(false);
        if (localVersionResult.IsFailure || localVersionResult.Value is null)
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                localVersionResult.Error ?? "Could not build local save state version metadata.",
                localVersionResult.ErrorType);
        }

        var localVersion = localVersionResult.Value;
        var cloudVersion = await _versionStore.GetCloudLatestVersionAsync(provider, gameId, ct).ConfigureAwait(false);
        var conflictType = DetermineConflictType(localVersion, cloudVersion);
        if (ShouldBlockUpload(metadata, conflictType))
        {
            return Result.Success(BuildBlockedSyncStatus(
                gameId,
                provider.ProviderName,
                isEncrypted,
                conflictType,
                localVersion,
                cloudVersion));
        }

        var tempFiles = new List<string>();

        try
        {
            var uploadPathResult = await ResolveUploadPayloadPathAsync(
                localSave.FilePath,
                metadata.EncryptionKey,
                tempFiles,
                ct).ConfigureAwait(false);
            if (uploadPathResult.IsFailure || string.IsNullOrWhiteSpace(uploadPathResult.Value))
            {
                return Result.Failure<SaveStateCloudSyncStatus>(
                    uploadPathResult.Error ?? "Failed to prepare save state payload for upload.",
                    uploadPathResult.ErrorType);
            }

            var uploadResult = await provider.UploadFileAsync(
                uploadPathResult.Value,
                localVersion.StoragePath,
                ct).ConfigureAwait(false);

            if (uploadResult.IsFailure || !uploadResult.Value)
            {
                return Result.Failure<SaveStateCloudSyncStatus>(
                    uploadResult.Error ?? $"Failed to upload save state to {provider.ProviderName}.",
                    uploadResult.IsFailure ? uploadResult.ErrorType : ErrorType.External);
            }

            await _versionStore.AppendVersionAsync(gameId, localVersion, ct).ConfigureAwait(false);
            await _versionStore.UploadVersionMetadataAsync(provider, localVersion, ct).ConfigureAwait(false);

            return Result.Success(BuildUploadedSyncStatus(
                gameId,
                provider.ProviderName,
                isEncrypted,
                conflictType,
                localVersion,
                cloudVersion));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                "Save state cloud sync was cancelled.",
                ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save state cloud sync failed for game {GameId}", gameId);
            return Result.Failure<SaveStateCloudSyncStatus>(
                $"Save state cloud sync failed: {ex.Message}",
                ErrorType.Internal);
        }
        finally
        {
            foreach (var tempFile in tempFiles)
            {
                TryDelete(tempFile);
            }
        }
    }

    private static bool ShouldBlockUpload(SaveStateCloudMetadata metadata, SaveStateConflictType conflictType)
    {
        if (metadata.ForceUpload)
        {
            return false;
        }

        return conflictType is SaveStateConflictType.CloudNewer or SaveStateConflictType.BothModified;
    }

    private SaveStateCloudSyncStatus BuildBlockedSyncStatus(
        Guid gameId,
        string providerName,
        bool isEncrypted,
        SaveStateConflictType conflictType,
        SaveStateCloudVersion localVersion,
        SaveStateCloudVersion? cloudVersion)
    {
        return new SaveStateCloudSyncStatus
        {
            GameId = gameId,
            Provider = providerName,
            Uploaded = false,
            Downloaded = false,
            HasConflict = true,
            ConflictType = conflictType,
            SyncedAtUtc = _timeProvider.UtcNow,
            IsEncrypted = isEncrypted,
            Message = "Sync blocked due to conflict. Pass ForceUpload to override.",
            LocalVersion = localVersion,
            CloudVersion = cloudVersion
        };
    }

    private SaveStateCloudSyncStatus BuildUploadedSyncStatus(
        Guid gameId,
        string providerName,
        bool isEncrypted,
        SaveStateConflictType conflictType,
        SaveStateCloudVersion localVersion,
        SaveStateCloudVersion? cloudVersion)
    {
        return new SaveStateCloudSyncStatus
        {
            GameId = gameId,
            Provider = providerName,
            Uploaded = true,
            Downloaded = false,
            HasConflict = conflictType != SaveStateConflictType.None,
            ConflictType = conflictType,
            SyncedAtUtc = _timeProvider.UtcNow,
            IsEncrypted = isEncrypted,
            Message = "Save state synchronized to cloud.",
            LocalVersion = localVersion,
            CloudVersion = cloudVersion
        };
    }

    private Result<string?> ResolveEncryptionKeyFingerprint(string? encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return Result.Success<string?>(null);
        }

        var fingerprintResult = _encryptionService.GetKeyFingerprint(encryptionKey);
        if (fingerprintResult.IsFailure)
        {
            return Result.Failure<string?>(
                fingerprintResult.Error ?? "Failed to compute encryption key fingerprint.",
                fingerprintResult.ErrorType);
        }

        return Result.Success<string?>(fingerprintResult.Value);
    }

    private async Task<Result<string>> ResolveUploadPayloadPathAsync(
        string localSavePath,
        string? encryptionKey,
        ICollection<string> tempFiles,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return Result.Success(localSavePath);
        }

        var encryptionResult = await _encryptionService.EncryptFileAsync(
            localSavePath,
            encryptionKey,
            ct).ConfigureAwait(false);

        if (encryptionResult.IsFailure || string.IsNullOrWhiteSpace(encryptionResult.Value))
        {
            return Result.Failure<string>(
                encryptionResult.Error ?? "Failed to encrypt save state.",
                encryptionResult.ErrorType);
        }

        tempFiles.Add(encryptionResult.Value);
        return Result.Success(encryptionResult.Value);
    }

    /// <inheritdoc />
    public async Task<Result<SaveStateConflictResolution>> DetectConflictsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        var providerResult = await ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsFailure || providerResult.Value is null)
        {
            return Result.Failure<SaveStateConflictResolution>(
                providerResult.Error ?? "Cloud provider is not configured.",
                providerResult.ErrorType);
        }

        var provider = providerResult.Value;

        var localVersion = await GetLatestLocalVersionAsync(gameId, ct).ConfigureAwait(false);
        var cloudVersion = await _versionStore.GetCloudLatestVersionAsync(provider, gameId, ct).ConfigureAwait(false);
        var conflictType = DetermineConflictType(localVersion, cloudVersion);

        var details = conflictType switch
        {
            SaveStateConflictType.None => "No conflict detected.",
            SaveStateConflictType.LocalNewer => "Local save state is newer than cloud version.",
            SaveStateConflictType.CloudNewer => "Cloud save state is newer than local version.",
            SaveStateConflictType.BothModified => "Both local and cloud versions were modified.",
            SaveStateConflictType.DeletedOnOneSide => "Save state exists on only one side.",
            _ => "Conflict state is unknown."
        };

        var resolution = new SaveStateConflictResolution
        {
            GameId = gameId,
            Type = conflictType,
            DetectedAtUtc = _timeProvider.UtcNow,
            LocalVersion = localVersion,
            CloudVersion = cloudVersion,
            Details = details
        };

        return Result.Success(resolution);
    }

    /// <inheritdoc />
    public async Task<Result<SaveStateCloudVersion>> CreateVersionAsync(
        Guid gameId,
        string versionName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(versionName))
        {
            return Result.Failure<SaveStateCloudVersion>("Version name is required.", ErrorType.Validation);
        }

        var localSaveResult = await ResolveLocalSaveStateAsync(gameId, null, ct).ConfigureAwait(false);
        if (localSaveResult.IsFailure || localSaveResult.Value is null)
        {
            return Result.Failure<SaveStateCloudVersion>(
                localSaveResult.Error ?? "Local save state was not found.",
                localSaveResult.ErrorType);
        }

        var localVersionResult = await BuildVersionAsync(
            localSaveResult.Value,
            versionName,
            deviceName: Environment.MachineName,
            isEncrypted: false,
            encryptionKeyFingerprint: null,
            ct).ConfigureAwait(false);
        if (localVersionResult.IsFailure || localVersionResult.Value is null)
        {
            return Result.Failure<SaveStateCloudVersion>(
                localVersionResult.Error ?? "Failed to create version metadata.",
                localVersionResult.ErrorType);
        }

        var version = localVersionResult.Value;
        await _versionStore.AppendVersionAsync(gameId, version, ct).ConfigureAwait(false);

        var providerResult = await ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsSuccess && providerResult.Value is not null)
        {
            await _versionStore.UploadVersionMetadataAsync(providerResult.Value, version, ct).ConfigureAwait(false);
        }

        return Result.Success(version);
    }

    /// <inheritdoc />
    public async Task<Result<SaveStateCloudSyncStatus>> ResolveConflictAsync(
        Guid gameId,
        SaveStateConflictResolutionStrategy strategy,
        SaveStateCloudMetadata? metadata = null,
        CancellationToken ct = default)
    {
        metadata ??= new SaveStateCloudMetadata();

        return strategy switch
        {
            SaveStateConflictResolutionStrategy.KeepLocal => await SyncSaveStateAsync(
                    gameId,
                    metadata with { ForceUpload = true },
                    ct)
                .ConfigureAwait(false),
            SaveStateConflictResolutionStrategy.KeepBoth => await ResolveKeepBothAsync(gameId, metadata, ct)
                .ConfigureAwait(false),
            SaveStateConflictResolutionStrategy.KeepCloud => await ResolveKeepCloudAsync(gameId, metadata, ct)
                .ConfigureAwait(false),
            _ => Result.Failure<SaveStateCloudSyncStatus>(
                $"Conflict strategy '{strategy}' is not supported for automatic resolution.",
                ErrorType.Validation)
        };
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SaveStateCloudVersion>>> GetVersionHistoryAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        var versions = await _versionStore.LoadVersionHistoryAsync(gameId, ct).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<SaveStateCloudVersion>>(versions);
    }

    private async Task<Result<SaveStateCloudSyncStatus>> ResolveKeepBothAsync(
        Guid gameId,
        SaveStateCloudMetadata metadata,
        CancellationToken ct)
    {
        var snapshotName = $"Conflict Snapshot {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss}";
        var snapshotResult = await CreateVersionAsync(gameId, snapshotName, ct).ConfigureAwait(false);
        if (snapshotResult.IsFailure)
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                snapshotResult.Error ?? "Failed to create conflict snapshot before keep-both resolution.",
                snapshotResult.ErrorType);
        }

        var uploadMetadata = metadata with
        {
            ForceUpload = true,
            VersionName = string.IsNullOrWhiteSpace(metadata.VersionName)
                ? $"Conflict KeepBoth {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss}"
                : metadata.VersionName
        };

        return await SyncSaveStateAsync(gameId, uploadMetadata, ct).ConfigureAwait(false);
    }

    private async Task<Result<SaveStateCloudSyncStatus>> ResolveKeepCloudAsync(
        Guid gameId,
        SaveStateCloudMetadata metadata,
        CancellationToken ct)
    {
        var providerResult = await ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsFailure || providerResult.Value is null)
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                providerResult.Error ?? "Cloud provider is not configured.",
                providerResult.ErrorType);
        }

        var provider = providerResult.Value;
        var cloudVersion = await _versionStore.GetCloudLatestVersionAsync(provider, gameId, ct).ConfigureAwait(false);
        if (cloudVersion is null || string.IsNullOrWhiteSpace(cloudVersion.StoragePath))
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                $"No cloud save state is available for game '{gameId}'.",
                ErrorType.NotFound);
        }

        var tempFiles = new List<string>();

        try
        {
            var restoreResult = await RestoreCloudVersionAsync(
                gameId,
                provider,
                cloudVersion,
                metadata,
                tempFiles,
                ct).ConfigureAwait(false);
            if (restoreResult.IsFailure || restoreResult.Value is null)
            {
                return Result.Failure<SaveStateCloudSyncStatus>(
                    restoreResult.Error ?? $"Failed to restore cloud save state from {provider.ProviderName}.",
                    restoreResult.ErrorType);
            }
            var restore = restoreResult.Value;

            await _versionStore.AppendVersionAsync(gameId, cloudVersion, ct).ConfigureAwait(false);
            return Result.Success(BuildKeepCloudStatus(
                gameId,
                provider.ProviderName,
                cloudVersion,
                restore.LocalVersion));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                "Cloud conflict resolution was cancelled.",
                ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve keep-cloud conflict for game {GameId}", gameId);
            return Result.Failure<SaveStateCloudSyncStatus>(
                $"Failed to resolve keep-cloud conflict: {ex.Message}",
                ErrorType.Internal);
        }
        finally
        {
            foreach (var tempFile in tempFiles)
            {
                TryDelete(tempFile);
            }
        }
    }

    private sealed record KeepCloudRestoreResult(
        SaveStateEntity SaveState,
        SaveStateCloudVersion? LocalVersion);

    private SaveStateCloudSyncStatus BuildKeepCloudStatus(
        Guid gameId,
        string providerName,
        SaveStateCloudVersion cloudVersion,
        SaveStateCloudVersion? localVersion)
    {
        return new SaveStateCloudSyncStatus
        {
            GameId = gameId,
            Provider = providerName,
            Uploaded = false,
            Downloaded = true,
            HasConflict = false,
            ConflictType = SaveStateConflictType.None,
            SyncedAtUtc = _timeProvider.UtcNow,
            IsEncrypted = cloudVersion.IsEncrypted,
            Message = "Cloud save state restored locally.",
            LocalVersion = localVersion,
            CloudVersion = cloudVersion
        };
    }

    private async Task<Result<KeepCloudRestoreResult>> RestoreCloudVersionAsync(
        Guid gameId,
        ICloudStorageProvider provider,
        SaveStateCloudVersion cloudVersion,
        SaveStateCloudMetadata metadata,
        ICollection<string> tempFiles,
        CancellationToken ct)
    {
        var downloadPath = Path.Combine(Path.GetTempPath(), $"savestate-cloud-download-{Guid.NewGuid():N}.bin");
        tempFiles.Add(downloadPath);

        var payloadPathResult = await DownloadCloudPayloadAsync(
            provider,
            cloudVersion,
            metadata,
            downloadPath,
            tempFiles,
            ct).ConfigureAwait(false);
        if (payloadPathResult.IsFailure || string.IsNullOrWhiteSpace(payloadPathResult.Value))
        {
            return Result.Failure<KeepCloudRestoreResult>(
                payloadPathResult.Error ?? $"Failed to download cloud save state from {provider.ProviderName}.",
                payloadPathResult.ErrorType);
        }

        var targetSaveState = await ResolveTrackedLocalSaveStateAsync(
            gameId,
            metadata.SaveStateId ?? cloudVersion.SaveStateId,
            ct).ConfigureAwait(false);
        var targetPath = ResolveRestoreTargetPath(gameId, cloudVersion, targetSaveState);
        EnsureDirectoryExistsForFile(targetPath);

        var persistedSaveResult = await PersistRestoredSaveStateAsync(
            gameId,
            targetSaveState,
            targetPath,
            payloadPathResult.Value,
            ct).ConfigureAwait(false);
        if (persistedSaveResult.IsFailure || persistedSaveResult.Value is null)
        {
            return Result.Failure<KeepCloudRestoreResult>(
                persistedSaveResult.Error ?? "Failed to persist restored cloud save state locally.",
                persistedSaveResult.ErrorType);
        }

        var localVersion = await TryBuildRestoredLocalVersionAsync(
            persistedSaveResult.Value,
            metadata.DeviceName,
            ct).ConfigureAwait(false);
        return Result.Success(new KeepCloudRestoreResult(persistedSaveResult.Value, localVersion));
    }

    private string ResolveRestoreTargetPath(
        Guid gameId,
        SaveStateCloudVersion cloudVersion,
        SaveStateEntity? targetSaveState)
    {
        if (targetSaveState is not null && !string.IsNullOrWhiteSpace(targetSaveState.FilePath))
        {
            return targetSaveState.FilePath;
        }

        return BuildLocalRestorePath(gameId, cloudVersion);
    }

    private static void EnsureDirectoryExistsForFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private async Task<Result<SaveStateEntity>> PersistRestoredSaveStateAsync(
        Guid gameId,
        SaveStateEntity? trackedSaveState,
        string targetPath,
        string payloadPath,
        CancellationToken ct)
    {
        File.Copy(payloadPath, targetPath, overwrite: true);
        var fileSize = new FileInfo(targetPath).Length;

        if (trackedSaveState is null)
        {
            var createdSaveState = SaveStateEntity.Create(gameId, targetPath, TimeSpan.Zero);
            createdSaveState.SetDescription($"Restored from cloud {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            createdSaveState.SetFileSize(fileSize);
            await _saveStateRepository.AddAsync(createdSaveState, ct).ConfigureAwait(false);
            return Result.Success(createdSaveState);
        }

        trackedSaveState.SetFileSize(fileSize);
        await _saveStateRepository.UpdateAsync(trackedSaveState, ct).ConfigureAwait(false);
        return Result.Success(trackedSaveState);
    }

    private async Task<SaveStateCloudVersion?> TryBuildRestoredLocalVersionAsync(
        SaveStateEntity targetSaveState,
        string? deviceName,
        CancellationToken ct)
    {
        var localVersionResult = await BuildVersionAsync(
            targetSaveState,
            requestedVersionName: "Local Restored",
            deviceName: deviceName,
            isEncrypted: false,
            encryptionKeyFingerprint: null,
            ct).ConfigureAwait(false);
        return localVersionResult.IsSuccess ? localVersionResult.Value : null;
    }

    private async Task<Result<string>> DownloadCloudPayloadAsync(
        ICloudStorageProvider provider,
        SaveStateCloudVersion cloudVersion,
        SaveStateCloudMetadata metadata,
        string downloadPath,
        ICollection<string> tempFiles,
        CancellationToken ct)
    {
        var downloadResult = await provider.DownloadFileAsync(
            cloudVersion.StoragePath,
            downloadPath,
            ct).ConfigureAwait(false);

        if (downloadResult.IsFailure || !downloadResult.Value || !File.Exists(downloadPath))
        {
            return Result.Failure<string>(
                downloadResult.Error ?? $"Failed to download save state from {provider.ProviderName}.",
                downloadResult.IsFailure ? downloadResult.ErrorType : ErrorType.External);
        }

        if (!cloudVersion.IsEncrypted)
        {
            return Result.Success(downloadPath);
        }

        if (string.IsNullOrWhiteSpace(metadata.EncryptionKey))
        {
            return Result.Failure<string>(
                "Cloud save state is encrypted but no encryption key was provided.",
                ErrorType.Validation);
        }

        if (!string.IsNullOrWhiteSpace(cloudVersion.EncryptionKeyFingerprint))
        {
            var fingerprintResult = _encryptionService.GetKeyFingerprint(metadata.EncryptionKey);
            if (fingerprintResult.IsFailure || string.IsNullOrWhiteSpace(fingerprintResult.Value))
            {
                return Result.Failure<string>(
                    fingerprintResult.Error ?? "Failed to validate encryption key fingerprint.",
                    fingerprintResult.ErrorType);
            }

            if (!string.Equals(
                    fingerprintResult.Value,
                    cloudVersion.EncryptionKeyFingerprint,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string>(
                    "Provided encryption key does not match cloud payload fingerprint.",
                    ErrorType.Validation);
            }
        }

        var decryptResult = await _encryptionService.DecryptFileAsync(
            downloadPath,
            metadata.EncryptionKey,
            ct).ConfigureAwait(false);
        if (decryptResult.IsFailure || string.IsNullOrWhiteSpace(decryptResult.Value))
        {
            return Result.Failure<string>(
                decryptResult.Error ?? "Failed to decrypt cloud save state payload.",
                decryptResult.ErrorType);
        }

        tempFiles.Add(decryptResult.Value);
        return Result.Success(decryptResult.Value);
    }

    private async Task<SaveStateEntity?> ResolveTrackedLocalSaveStateAsync(
        Guid gameId,
        Guid? saveStateId,
        CancellationToken ct)
    {
        if (saveStateId.HasValue)
        {
            var byId = await _saveStateRepository.GetByIdAsync(saveStateId.Value, ct).ConfigureAwait(false);
            if (byId is not null && byId.GameId == gameId)
            {
                return byId;
            }
        }

        var saveStates = await _saveStateRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
        return saveStates
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();
    }

    private async Task<Result<ICloudStorageProvider>> ResolveProviderAsync(CancellationToken ct)
    {
        if (_cloudProviders.Count == 0)
        {
            return Result.Failure<ICloudStorageProvider>(
                "No cloud storage provider is registered.",
                ErrorType.NotFound);
        }

        var preferredProviderName = await _preferencesService.GetPreferredCloudProviderAsync(ct).ConfigureAwait(false);
        var normalizedPreferredProviderName = NormalizeProviderName(preferredProviderName);
        var provider = !string.IsNullOrWhiteSpace(normalizedPreferredProviderName)
            ? _cloudProviders.FirstOrDefault(p =>
                string.Equals(
                    NormalizeProviderName(p.ProviderName),
                    normalizedPreferredProviderName,
                    StringComparison.Ordinal))
            : null;

        provider ??= _cloudProviders.FirstOrDefault(p => !IsLocalProvider(p.ProviderName))
                    ?? _cloudProviders.FirstOrDefault();

        if (provider is null)
        {
            return Result.Failure<ICloudStorageProvider>(
                "No compatible cloud provider is available.",
                ErrorType.NotFound);
        }

        if (!provider.IsAuthenticated)
        {
            var authenticated = await provider.AuthenticateAsync(ct).ConfigureAwait(false);
            if (!authenticated)
            {
                return Result.Failure<ICloudStorageProvider>(
                    $"Authentication failed for cloud provider '{provider.ProviderName}'.",
                    ErrorType.Unauthorized);
            }
        }

        return Result.Success(provider);
    }

    private async Task<Result<SaveStateEntity>> ResolveLocalSaveStateAsync(
        Guid gameId,
        Guid? saveStateId,
        CancellationToken ct)
    {
        if (saveStateId.HasValue)
        {
            var explicitSaveState = await _saveStateRepository.GetByIdAsync(saveStateId.Value, ct).ConfigureAwait(false);
            if (explicitSaveState is null || explicitSaveState.GameId != gameId)
            {
                return Result.Failure<SaveStateEntity>(
                    $"Save state '{saveStateId}' was not found for game '{gameId}'.",
                    ErrorType.NotFound);
            }

            if (!File.Exists(explicitSaveState.FilePath))
            {
                return Result.Failure<SaveStateEntity>(
                    $"Save state file '{explicitSaveState.FilePath}' does not exist.",
                    ErrorType.NotFound);
            }

            return Result.Success(explicitSaveState);
        }

        var saveStates = await _saveStateRepository.GetByGameIdAsync(gameId, ct).ConfigureAwait(false);
        var latest = saveStates
            .Where(s => File.Exists(s.FilePath))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            return Result.Failure<SaveStateEntity>(
                $"No local save state was found for game '{gameId}'.",
                ErrorType.NotFound);
        }

        return Result.Success(latest);
    }

    private async Task<SaveStateCloudVersion?> GetLatestLocalVersionAsync(Guid gameId, CancellationToken ct)
    {
        var localSaveResult = await ResolveLocalSaveStateAsync(gameId, null, ct).ConfigureAwait(false);
        if (localSaveResult.IsFailure || localSaveResult.Value is null)
        {
            return null;
        }

        var versionResult = await BuildVersionAsync(
            localSaveResult.Value,
            requestedVersionName: "Local Latest",
            deviceName: Environment.MachineName,
            isEncrypted: false,
            encryptionKeyFingerprint: null,
            ct).ConfigureAwait(false);

        return versionResult.IsSuccess ? versionResult.Value : null;
    }

    private async Task<Result<SaveStateCloudVersion>> BuildVersionAsync(
        SaveStateEntity saveState,
        string? requestedVersionName,
        string? deviceName,
        bool isEncrypted,
        string? encryptionKeyFingerprint,
        CancellationToken ct)
    {
        if (!File.Exists(saveState.FilePath))
        {
            return Result.Failure<SaveStateCloudVersion>(
                $"Save state file '{saveState.FilePath}' does not exist.",
                ErrorType.NotFound);
        }

        var hash = await ComputeFileHashAsync(saveState.FilePath, ct).ConfigureAwait(false);
        if (hash.IsFailure || string.IsNullOrWhiteSpace(hash.Value))
        {
            return Result.Failure<SaveStateCloudVersion>(
                hash.Error ?? "Unable to compute file hash.",
                hash.ErrorType);
        }

        var versionCreatedAt = _timeProvider.UtcNow;
        var versionName = string.IsNullOrWhiteSpace(requestedVersionName)
            ? $"Auto {versionCreatedAt:yyyy-MM-dd HH:mm:ss}"
            : requestedVersionName.Trim();

        var version = new SaveStateCloudVersion
        {
            Id = Guid.NewGuid(),
            GameId = saveState.GameId,
            SaveStateId = saveState.Id,
            VersionName = versionName,
            StoragePath = BuildCloudSavePath(saveState, isEncrypted),
            ContentHash = hash.Value,
            FileSizeBytes = new FileInfo(saveState.FilePath).Length,
            CreatedAtUtc = versionCreatedAt,
            SourceSaveStateCreatedAtUtc = saveState.CreatedAt,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? Environment.MachineName : deviceName.Trim(),
            IsEncrypted = isEncrypted,
            EncryptionKeyFingerprint = encryptionKeyFingerprint
        };

        return Result.Success(version);
    }

    private async Task<Result<string>> ComputeFileHashAsync(string filePath, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
            return Result.Success(Convert.ToHexString(hash));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash save state file {Path}", filePath);
            return Result.Failure<string>($"Failed to hash save state file: {ex.Message}", ErrorType.Internal);
        }
    }

    private string BuildLocalRestorePath(Guid gameId, SaveStateCloudVersion cloudVersion)
    {
        var extension = ResolveRestoreExtension(cloudVersion.StoragePath, cloudVersion.IsEncrypted);
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SaveStates", gameId.ToString());
        Directory.CreateDirectory(directory);

        var fileName = $"savestate_cloud_{_timeProvider.UtcNow:yyyyMMdd_HHmmss}_{cloudVersion.Id:N}{extension}";
        return Path.Combine(directory, fileName);
    }

    private static string ResolveRestoreExtension(string storagePath, bool isEncrypted)
    {
        var fileName = Path.GetFileName(storagePath.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ".state";
        }

        if (isEncrypted && fileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
        }

        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension) ? ".state" : extension;
    }

    private static string BuildCloudSavePath(SaveStateEntity saveState, bool isEncrypted)
    {
        var extension = Path.GetExtension(saveState.FilePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".state";
        }

        var fileName = $"{saveState.Id}{extension}";
        if (isEncrypted)
        {
            fileName += ".enc";
        }

        return $"{CloudRootPath}/{saveState.GameId}/{fileName}";
    }

    private SaveStateConflictType DetermineConflictType(
        SaveStateCloudVersion? localVersion,
        SaveStateCloudVersion? cloudVersion)
    {
        if (localVersion is null && cloudVersion is null)
        {
            return SaveStateConflictType.None;
        }

        if (localVersion is null || cloudVersion is null)
        {
            return SaveStateConflictType.DeletedOnOneSide;
        }

        if (string.Equals(localVersion.ContentHash, cloudVersion.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            return SaveStateConflictType.None;
        }

        var timeDifferenceSeconds = Math.Abs((localVersion.CreatedAtUtc - cloudVersion.CreatedAtUtc).TotalSeconds);
        if (timeDifferenceSeconds <= 5)
        {
            return SaveStateConflictType.BothModified;
        }

        return localVersion.CreatedAtUtc > cloudVersion.CreatedAtUtc
            ? SaveStateConflictType.LocalNewer
            : SaveStateConflictType.CloudNewer;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    private static string NormalizeProviderName(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return string.Empty;
        }

        var normalizedChars = providerName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(normalizedChars);
    }

    private static bool IsLocalProvider(string? providerName)
    {
        var normalizedName = NormalizeProviderName(providerName);
        return normalizedName is "local" or "localstorage" or "filesystem";
    }
}
