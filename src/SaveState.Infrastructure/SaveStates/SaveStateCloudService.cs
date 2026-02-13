using System.Security.Cryptography;
using System.Text.Json;
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
    private readonly string _versionHistoryRootPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

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

        _versionHistoryRootPath = versionHistoryRootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveState",
            "CloudSync",
            "Versions");

        Directory.CreateDirectory(_versionHistoryRootPath);
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
        var keyFingerprint = isEncrypted
            ? _encryptionService.GetKeyFingerprint(metadata.EncryptionKey!)
            : null;

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
        var cloudVersion = await GetCloudLatestVersionAsync(provider, gameId, ct).ConfigureAwait(false);
        var conflictType = DetermineConflictType(localVersion, cloudVersion);
        var hasBlockingConflict = !metadata.ForceUpload &&
                                  (conflictType == SaveStateConflictType.CloudNewer ||
                                   conflictType == SaveStateConflictType.BothModified);

        if (hasBlockingConflict)
        {
            var blockedStatus = new SaveStateCloudSyncStatus
            {
                GameId = gameId,
                Provider = provider.ProviderName,
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

            return Result.Success(blockedStatus);
        }

        var tempFiles = new List<string>();

        try
        {
            var uploadPath = localSave.FilePath;
            if (isEncrypted)
            {
                var encryptionResult = await _encryptionService.EncryptFileAsync(
                    localSave.FilePath,
                    metadata.EncryptionKey!,
                    ct).ConfigureAwait(false);

                if (encryptionResult.IsFailure || string.IsNullOrWhiteSpace(encryptionResult.Value))
                {
                    return Result.Failure<SaveStateCloudSyncStatus>(
                        encryptionResult.Error ?? "Failed to encrypt save state.",
                        encryptionResult.ErrorType);
                }

                uploadPath = encryptionResult.Value;
                tempFiles.Add(uploadPath);
            }

            var uploaded = await provider.UploadFileAsync(
                uploadPath,
                localVersion.StoragePath,
                ct).ConfigureAwait(false);

            if (!uploaded)
            {
                return Result.Failure<SaveStateCloudSyncStatus>(
                    $"Failed to upload save state to {provider.ProviderName}.",
                    ErrorType.External);
            }

            await AppendVersionAsync(gameId, localVersion, ct).ConfigureAwait(false);
            await UploadVersionMetadataAsync(provider, localVersion, ct).ConfigureAwait(false);

            var status = new SaveStateCloudSyncStatus
            {
                GameId = gameId,
                Provider = provider.ProviderName,
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

            return Result.Success(status);
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
        var cloudVersion = await GetCloudLatestVersionAsync(provider, gameId, ct).ConfigureAwait(false);
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
        await AppendVersionAsync(gameId, version, ct).ConfigureAwait(false);

        var providerResult = await ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsSuccess && providerResult.Value is not null)
        {
            await UploadVersionMetadataAsync(providerResult.Value, version, ct).ConfigureAwait(false);
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
        var versions = await LoadVersionHistoryAsync(gameId, ct).ConfigureAwait(false);
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
        var cloudVersion = await GetCloudLatestVersionAsync(provider, gameId, ct).ConfigureAwait(false);
        if (cloudVersion is null || string.IsNullOrWhiteSpace(cloudVersion.StoragePath))
        {
            return Result.Failure<SaveStateCloudSyncStatus>(
                $"No cloud save state is available for game '{gameId}'.",
                ErrorType.NotFound);
        }

        var tempFiles = new List<string>();

        try
        {
            var downloadPath = Path.Combine(Path.GetTempPath(), $"savestate-cloud-download-{Guid.NewGuid():N}.bin");
            tempFiles.Add(downloadPath);

            var downloaded = await provider.DownloadFileAsync(
                cloudVersion.StoragePath,
                downloadPath,
                ct).ConfigureAwait(false);

            if (!downloaded || !File.Exists(downloadPath))
            {
                return Result.Failure<SaveStateCloudSyncStatus>(
                    $"Failed to download cloud save state from {provider.ProviderName}.",
                    ErrorType.External);
            }

            var payloadPath = downloadPath;
            if (cloudVersion.IsEncrypted)
            {
                if (string.IsNullOrWhiteSpace(metadata.EncryptionKey))
                {
                    return Result.Failure<SaveStateCloudSyncStatus>(
                        "Encryption key is required to restore this cloud save state.",
                        ErrorType.Validation);
                }

                if (!string.IsNullOrWhiteSpace(cloudVersion.EncryptionKeyFingerprint))
                {
                    var fingerprint = _encryptionService.GetKeyFingerprint(metadata.EncryptionKey);
                    if (!string.Equals(
                            fingerprint,
                            cloudVersion.EncryptionKeyFingerprint,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return Result.Failure<SaveStateCloudSyncStatus>(
                            "The provided encryption key does not match the cloud save state key fingerprint.",
                            ErrorType.Validation);
                    }
                }

                var decryptResult = await _encryptionService.DecryptFileAsync(
                    downloadPath,
                    metadata.EncryptionKey!,
                    ct).ConfigureAwait(false);
                if (decryptResult.IsFailure || string.IsNullOrWhiteSpace(decryptResult.Value))
                {
                    return Result.Failure<SaveStateCloudSyncStatus>(
                        decryptResult.Error ?? "Failed to decrypt cloud save state payload.",
                        decryptResult.ErrorType);
                }

                payloadPath = decryptResult.Value;
                tempFiles.Add(payloadPath);
            }

            var targetSaveState = await ResolveTrackedLocalSaveStateAsync(
                gameId,
                metadata.SaveStateId ?? cloudVersion.SaveStateId,
                ct).ConfigureAwait(false);

            var targetPath = string.IsNullOrWhiteSpace(targetSaveState?.FilePath)
                ? BuildLocalRestorePath(gameId, cloudVersion)
                : targetSaveState!.FilePath;

            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(payloadPath, targetPath, overwrite: true);
            var fileSize = new FileInfo(targetPath).Length;

            if (targetSaveState is null)
            {
                targetSaveState = SaveStateEntity.Create(gameId, targetPath, TimeSpan.Zero);
                targetSaveState.SetDescription($"Restored from cloud {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                targetSaveState.SetFileSize(fileSize);
                await _saveStateRepository.AddAsync(targetSaveState, ct).ConfigureAwait(false);
            }
            else
            {
                targetSaveState.SetFileSize(fileSize);
                await _saveStateRepository.UpdateAsync(targetSaveState, ct).ConfigureAwait(false);
            }

            await AppendVersionAsync(gameId, cloudVersion, ct).ConfigureAwait(false);

            SaveStateCloudVersion? localVersion = null;
            var localVersionResult = await BuildVersionAsync(
                targetSaveState,
                requestedVersionName: "Local Restored",
                deviceName: metadata.DeviceName,
                isEncrypted: false,
                encryptionKeyFingerprint: null,
                ct).ConfigureAwait(false);
            if (localVersionResult.IsSuccess)
            {
                localVersion = localVersionResult.Value;
            }

            var status = new SaveStateCloudSyncStatus
            {
                GameId = gameId,
                Provider = provider.ProviderName,
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

            return Result.Success(status);
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

    private static string BuildCloudLatestVersionPath(Guid gameId) =>
        $"{CloudRootPath}/{gameId}/latest.json";

    private static string BuildCloudVersionPath(Guid gameId, Guid versionId) =>
        $"{CloudRootPath}/{gameId}/versions/{versionId}.json";

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

    private async Task<SaveStateCloudVersion?> GetCloudLatestVersionAsync(
        ICloudStorageProvider provider,
        Guid gameId,
        CancellationToken ct)
    {
        var tempMetadataPath = Path.Combine(Path.GetTempPath(), $"savestate-cloud-latest-{Guid.NewGuid():N}.json");
        try
        {
            var downloaded = await provider.DownloadFileAsync(
                BuildCloudLatestVersionPath(gameId),
                tempMetadataPath,
                ct).ConfigureAwait(false);

            if (!downloaded || !File.Exists(tempMetadataPath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(tempMetadataPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SaveStateCloudVersion>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cloud latest version metadata for game {GameId}", gameId);
            return null;
        }
        finally
        {
            TryDelete(tempMetadataPath);
        }
    }

    private async Task UploadVersionMetadataAsync(
        ICloudStorageProvider provider,
        SaveStateCloudVersion version,
        CancellationToken ct)
    {
        var tempMetadataPath = Path.Combine(Path.GetTempPath(), $"savestate-cloud-version-{Guid.NewGuid():N}.json");
        try
        {
            var json = JsonSerializer.Serialize(version, _jsonOptions);
            await File.WriteAllTextAsync(tempMetadataPath, json, ct).ConfigureAwait(false);

            var uploadedVersionMetadata = await provider.UploadFileAsync(
                tempMetadataPath,
                BuildCloudVersionPath(version.GameId, version.Id),
                ct).ConfigureAwait(false);

            if (!uploadedVersionMetadata)
            {
                _logger.LogWarning(
                    "Failed to upload version metadata for game {GameId}, version {VersionId}",
                    version.GameId,
                    version.Id);
            }

            var uploadedLatestMetadata = await provider.UploadFileAsync(
                tempMetadataPath,
                BuildCloudLatestVersionPath(version.GameId),
                ct).ConfigureAwait(false);

            if (!uploadedLatestMetadata)
            {
                _logger.LogWarning(
                    "Failed to upload latest metadata marker for game {GameId}, version {VersionId}",
                    version.GameId,
                    version.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish cloud version metadata for game {GameId}", version.GameId);
        }
        finally
        {
            TryDelete(tempMetadataPath);
        }
    }

    private async Task AppendVersionAsync(Guid gameId, SaveStateCloudVersion version, CancellationToken ct)
    {
        var existing = await LoadVersionHistoryAsync(gameId, ct).ConfigureAwait(false);
        var updated = existing
            .Where(v => v.Id != version.Id)
            .Append(version)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToList();

        await SaveVersionHistoryAsync(gameId, updated, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<SaveStateCloudVersion>> LoadVersionHistoryAsync(Guid gameId, CancellationToken ct)
    {
        var historyPath = GetVersionHistoryFilePath(gameId);
        if (!File.Exists(historyPath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(historyPath, ct).ConfigureAwait(false);
            var items = JsonSerializer.Deserialize<List<SaveStateCloudVersion>>(json, _jsonOptions) ?? [];
            return items.OrderByDescending(v => v.CreatedAtUtc).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cloud version history for game {GameId}", gameId);
            return [];
        }
    }

    private async Task SaveVersionHistoryAsync(
        Guid gameId,
        IReadOnlyList<SaveStateCloudVersion> versions,
        CancellationToken ct)
    {
        var historyPath = GetVersionHistoryFilePath(gameId);
        var json = JsonSerializer.Serialize(versions, _jsonOptions);
        await File.WriteAllTextAsync(historyPath, json, ct).ConfigureAwait(false);
    }

    private string GetVersionHistoryFilePath(Guid gameId)
    {
        Directory.CreateDirectory(_versionHistoryRootPath);
        return Path.Combine(_versionHistoryRootPath, $"{gameId:N}.json");
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
