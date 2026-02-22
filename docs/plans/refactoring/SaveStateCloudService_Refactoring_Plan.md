# SaveStateCloudService Refactoring Plan

## Overview

**File:** `src/SaveState.Infrastructure/SaveStates/SaveStateCloudService.cs`  
**Current Lines:** 1,006  
**Target Lines:** ~180 lines (coordinator) + 6 managers (~120-180 lines each)  
**Pattern:** Manager Pattern with Coordinator

---

## File Statistics

| Metric | Current | Target |
|--------|---------|--------|
| Total Lines | 1,006 | ~980 (split across 7 files) |
| Public Methods | 7 | 7 (delegated) |
| Private Methods | 28 | 0 (moved to managers) |
| Conflict Strategies | 3 (KeepLocal, KeepBoth, KeepCloud) | 3 (in ConflictManager) |
| File Operations | 10+ | Encapsulated in managers |
| Responsibilities | 7 | 1 (coordinator only) |

---

## Responsibility Analysis

### Current Responsibilities (Violating SRP)

1. **Cloud Provider Resolution & Authentication**
   - Provider selection based on preferences
   - Authentication state management
   - Provider normalization

2. **Save State Resolution & Building**
   - Local save state lookup by ID or game
   - Version building with hash computation
   - Storage path construction

3. **Encryption Management**
   - Key fingerprint calculation
   - File encryption for upload
   - File decryption for download
   - Fingerprint validation

4. **Version History Management**
   - Local version tracking
   - Cloud version retrieval
   - Version metadata persistence
   - Version timeline management

5. **Conflict Detection & Resolution**
   - Conflict type determination
   - Resolution strategy execution
   - KeepBoth snapshot creation
   - KeepCloud restoration

6. **Upload/Download Orchestration**
   - Payload preparation
   - Temporary file management
   - Progress tracking
   - Cleanup handling

7. **Cloud Sync Status Building**
   - Status object construction
   - Message generation
   - Conflict flag management

---

## Proposed Manager Classes

### 1. CloudProviderManager

**Responsibility:** Cloud provider selection and authentication

**Key Methods:**
```csharp
public sealed class CloudProviderManager
{
    public CloudProviderManager(
        IEnumerable<ICloudStorageProvider> cloudProviders,
        IUserPreferencesService preferencesService);
    
    public async Task<Result<ICloudStorageProvider>> ResolveProviderAsync(
        CancellationToken ct = default);
    
    public async Task<Result<ICloudStorageProvider>> GetPreferredProviderAsync(
        string? preferredName,
        CancellationToken ct = default);
    
    private static string NormalizeProviderName(string? providerName);
    private static bool IsLocalProvider(string? providerName);
}
```

---

### 2. SaveStateResolutionManager

**Responsibility:** Local save state lookup and validation

**Key Methods:**
```csharp
public sealed class SaveStateResolutionManager
{
    public SaveStateResolutionManager(ISaveStateRepository saveStateRepository);
    
    public async Task<Result<SaveState>> ResolveLocalSaveStateAsync(
        Guid gameId,
        Guid? saveStateId,
        CancellationToken ct = default);
    
    public async Task<Result<SaveState>> ResolveTrackedLocalSaveStateAsync(
        Guid gameId,
        Guid? saveStateId,
        CancellationToken ct = default);
    
    public async Task<SaveState?> GetLatestLocalSaveStateAsync(
        Guid gameId,
        CancellationToken ct = default);
}
```

---

### 3. SaveStateEncryptionManager

**Responsibility:** Encryption/decryption operations

**Key Methods:**
```csharp
public sealed class SaveStateEncryptionManager
{
    public SaveStateEncryptionManager(ICloudSaveEncryptionService encryptionService);
    
    public Result<string?> ResolveEncryptionKeyFingerprint(string? encryptionKey);
    
    public async Task<Result<string>> ResolveUploadPayloadPathAsync(
        string localSavePath,
        string? encryptionKey,
        ICollection<string> tempFiles,
        CancellationToken ct);
    
    public async Task<Result<string>> DecryptDownloadedPayloadAsync(
        string downloadPath,
        SaveStateCloudVersion cloudVersion,
        SaveStateCloudMetadata metadata,
        ICollection<string> tempFiles,
        CancellationToken ct);
}
```

---

### 4. SaveStateVersionManager

**Responsibility:** Version building and history management

**Key Methods:**
```csharp
public sealed class SaveStateVersionManager
{
    public SaveStateVersionManager(
        SaveStateCloudVersionStore versionStore,
        ITimeProvider timeProvider);
    
    public async Task<Result<SaveStateCloudVersion>> BuildVersionAsync(
        SaveState saveState,
        string? requestedVersionName,
        string? deviceName,
        bool isEncrypted,
        string? encryptionKeyFingerprint,
        CancellationToken ct);
    
    public async Task<IReadOnlyList<SaveStateCloudVersion>> GetVersionHistoryAsync(
        Guid gameId,
        CancellationToken ct);
    
    public async Task<SaveStateCloudVersion?> GetLatestCloudVersionAsync(
        ICloudStorageProvider provider,
        Guid gameId,
        CancellationToken ct);
    
    public async Task AppendVersionAsync(
        Guid gameId,
        SaveStateCloudVersion version,
        CancellationToken ct);
    
    public async Task UploadVersionMetadataAsync(
        ICloudStorageProvider provider,
        SaveStateCloudVersion version,
        CancellationToken ct);
    
    private async Task<Result<string>> ComputeFileHashAsync(
        string filePath,
        CancellationToken ct);
    
    private static string BuildCloudSavePath(SaveState saveState, bool isEncrypted);
}
```

---

### 5. SaveStateConflictManager

**Responsibility:** Conflict detection and resolution

**Key Methods:**
```csharp
public sealed class SaveStateConflictManager
{
    public SaveStateConflictManager(
        SaveStateVersionManager versionManager,
        SaveStateEncryptionManager encryptionManager,
        SaveStateResolutionManager resolutionManager,
        SaveStateCloudVersionStore versionStore,
        ISaveStateRepository saveStateRepository,
        ITimeProvider timeProvider);
    
    // Detection
    public SaveStateConflictType DetermineConflictType(
        SaveStateCloudVersion? localVersion,
        SaveStateCloudVersion? cloudVersion);
    
    public bool ShouldBlockUpload(SaveStateCloudMetadata metadata, SaveStateConflictType conflictType);
    
    public async Task<Result<SaveStateConflictResolution>> DetectConflictsAsync(
        Guid gameId,
        ICloudStorageProvider provider,
        CancellationToken ct);
    
    // Resolution
    public async Task<Result<SaveStateCloudSyncStatus>> ResolveKeepBothAsync(
        Guid gameId,
        SaveStateCloudMetadata metadata,
        Func<Guid, SaveStateCloudMetadata, CancellationToken, Task<Result<SaveStateCloudSyncStatus>>> syncFunc,
        CancellationToken ct);
    
    public async Task<Result<SaveStateCloudSyncStatus>> ResolveKeepCloudAsync(
        Guid gameId,
        SaveStateCloudMetadata metadata,
        ICloudStorageProvider provider,
        CancellationToken ct);
    
    // Restoration helpers
    private async Task<Result<KeepCloudRestoreResult>> RestoreCloudVersionAsync(
        Guid gameId,
        ICloudStorageProvider provider,
        SaveStateCloudVersion cloudVersion,
        SaveStateCloudMetadata metadata,
        ICollection<string> tempFiles,
        CancellationToken ct);
    
    private async Task<Result<string>> DownloadCloudPayloadAsync(
        ICloudStorageProvider provider,
        SaveStateCloudVersion cloudVersion,
        string downloadPath,
        CancellationToken ct);
    
    private string ResolveRestoreTargetPath(
        Guid gameId,
        SaveStateCloudVersion cloudVersion,
        SaveState? targetSaveState);
    
    private static void EnsureDirectoryExistsForFile(string filePath);
    
    private async Task<Result<SaveState>> PersistRestoredSaveStateAsync(
        Guid gameId,
        SaveState? trackedSaveState,
        string targetPath,
        string payloadPath,
        CancellationToken ct);
    
    private async Task<SaveStateCloudVersion?> TryBuildRestoredLocalVersionAsync(
        SaveState targetSaveState,
        string? deviceName,
        CancellationToken ct);
    
    private static string BuildLocalRestorePath(Guid gameId, SaveStateCloudVersion cloudVersion, ITimeProvider timeProvider);
    private static string ResolveRestoreExtension(string storagePath, bool isEncrypted);
}
```

---

### 6. CloudSyncStatusBuilder

**Responsibility:** Sync status object construction

**Key Methods:**
```csharp
public sealed class CloudSyncStatusBuilder
{
    public CloudSyncStatusBuilder(ITimeProvider timeProvider);
    
    public SaveStateCloudSyncStatus BuildBlockedSyncStatus(
        Guid gameId,
        string providerName,
        bool isEncrypted,
        SaveStateConflictType conflictType,
        SaveStateCloudVersion localVersion,
        SaveStateCloudVersion? cloudVersion);
    
    public SaveStateCloudSyncStatus BuildUploadedSyncStatus(
        Guid gameId,
        string providerName,
        bool isEncrypted,
        SaveStateConflictType conflictType,
        SaveStateCloudVersion localVersion,
        SaveStateCloudVersion? cloudVersion);
    
    public SaveStateCloudSyncStatus BuildKeepCloudStatus(
        Guid gameId,
        string providerName,
        SaveStateCloudVersion cloudVersion,
        SaveStateCloudVersion? localVersion);
    
    public SaveStateCloudSyncStatus BuildDownloadedSyncStatus(
        Guid gameId,
        string providerName,
        SaveStateCloudVersion cloudVersion,
        SaveStateCloudVersion? localVersion);
}
```

---

## Before/After Code Structure

### BEFORE (Current)

```csharp
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

    public SaveStateCloudService(...) { ... }

    // Main Sync Operation
    public async Task<Result<SaveStateCloudSyncStatus>> SyncSaveStateAsync(...) { ... }
    
    // Conflict Detection
    public async Task<Result<SaveStateConflictResolution>> DetectConflictsAsync(...) { ... }
    
    // Version Management
    public async Task<Result<SaveStateCloudVersion>> CreateVersionAsync(...) { ... }
    public async Task<Result<IReadOnlyList<SaveStateCloudVersion>>> GetVersionHistoryAsync(...) { ... }
    
    // Conflict Resolution
    public async Task<Result<SaveStateCloudSyncStatus>> ResolveConflictAsync(...) { ... }
    
    // ~28 private helper methods...
}
```

**Problems:**
- 1,006 lines in single file
- Complex control flow for sync operation
- Mix of encryption, file I/O, and cloud operations
- Hard to follow conflict resolution paths
- Temporary file cleanup scattered

---

### AFTER (Refactored)

#### Coordinator: SaveStateCloudService

```csharp
public sealed class SaveStateCloudService : ISaveStateCloudService
{
    private readonly CloudProviderManager _providerManager;
    private readonly SaveStateResolutionManager _resolutionManager;
    private readonly SaveStateEncryptionManager _encryptionManager;
    private readonly SaveStateVersionManager _versionManager;
    private readonly SaveStateConflictManager _conflictManager;
    private readonly CloudSyncStatusBuilder _statusBuilder;
    private readonly ILogger<SaveStateCloudService> _logger;

    public SaveStateCloudService(
        CloudProviderManager providerManager,
        SaveStateResolutionManager resolutionManager,
        SaveStateEncryptionManager encryptionManager,
        SaveStateVersionManager versionManager,
        SaveStateConflictManager conflictManager,
        CloudSyncStatusBuilder statusBuilder,
        ILogger<SaveStateCloudService> logger)
    {
        _providerManager = providerManager;
        _resolutionManager = resolutionManager;
        _encryptionManager = encryptionManager;
        _versionManager = versionManager;
        _conflictManager = conflictManager;
        _statusBuilder = statusBuilder;
        _logger = logger;
    }

    public async Task<Result<SaveStateCloudSyncStatus>> SyncSaveStateAsync(
        Guid gameId,
        SaveStateCloudMetadata metadata,
        CancellationToken ct = default)
    {
        metadata ??= new SaveStateCloudMetadata();

        // Resolve provider
        var providerResult = await _providerManager.ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsFailure)
            return Result.Failure<SaveStateCloudSyncStatus>(providerResult.Error!, providerResult.ErrorType);
        var provider = providerResult.Value;

        // Resolve local save
        var localSaveResult = await _resolutionManager.ResolveLocalSaveStateAsync(
            gameId, metadata.SaveStateId, ct).ConfigureAwait(false);
        if (localSaveResult.IsFailure)
            return Result.Failure<SaveStateCloudSyncStatus>(localSaveResult.Error!, localSaveResult.ErrorType);
        var localSave = localSaveResult.Value;

        // Get encryption fingerprint
        var keyFingerprintResult = _encryptionManager.ResolveEncryptionKeyFingerprint(metadata.EncryptionKey);
        if (keyFingerprintResult.IsFailure)
            return Result.Failure<SaveStateCloudSyncStatus>(keyFingerprintResult.Error!, keyFingerprintResult.ErrorType);
        var isEncrypted = !string.IsNullOrWhiteSpace(metadata.EncryptionKey);

        // Build local version
        var localVersionResult = await _versionManager.BuildVersionAsync(
            localSave, metadata.VersionName, metadata.DeviceName, 
            isEncrypted, keyFingerprintResult.Value, ct).ConfigureAwait(false);
        if (localVersionResult.IsFailure)
            return Result.Failure<SaveStateCloudSyncStatus>(localVersionResult.Error!, localVersionResult.ErrorType);
        var localVersion = localVersionResult.Value;

        // Get cloud version and check conflicts
        var cloudVersion = await _versionManager.GetLatestCloudVersionAsync(provider, gameId, ct).ConfigureAwait(false);
        var conflictType = _conflictManager.DetermineConflictType(localVersion, cloudVersion);
        
        if (_conflictManager.ShouldBlockUpload(metadata, conflictType))
        {
            return Result.Success(_statusBuilder.BuildBlockedSyncStatus(
                gameId, provider.ProviderName, isEncrypted, conflictType, localVersion, cloudVersion));
        }

        // Prepare and upload
        return await ExecuteUploadAsync(gameId, localSave, localVersion, provider, metadata, isEncrypted, conflictType, cloudVersion, ct);
    }

    private async Task<Result<SaveStateCloudSyncStatus>> ExecuteUploadAsync(
        Guid gameId,
        SaveState localSave,
        SaveStateCloudVersion localVersion,
        ICloudStorageProvider provider,
        SaveStateCloudMetadata metadata,
        bool isEncrypted,
        SaveStateConflictType conflictType,
        SaveStateCloudVersion? cloudVersion,
        CancellationToken ct)
    {
        var tempFiles = new List<string>();
        try
        {
            // Prepare payload
            var uploadPathResult = await _encryptionManager.ResolveUploadPayloadPathAsync(
                localSave.FilePath, metadata.EncryptionKey, tempFiles, ct).ConfigureAwait(false);
            if (uploadPathResult.IsFailure)
                return Result.Failure<SaveStateCloudSyncStatus>(uploadPathResult.Error!, uploadPathResult.ErrorType);

            // Upload
            var uploadResult = await provider.UploadFileAsync(
                uploadPathResult.Value, localVersion.StoragePath, ct).ConfigureAwait(false);
            if (uploadResult.IsFailure || !uploadResult.Value)
                return Result.Failure<SaveStateCloudSyncStatus>(
                    uploadResult.Error ?? "Upload failed",
                    uploadResult.IsFailure ? uploadResult.ErrorType : ErrorType.External);

            // Record version
            await _versionManager.AppendVersionAsync(gameId, localVersion, ct).ConfigureAwait(false);
            await _versionManager.UploadVersionMetadataAsync(provider, localVersion, ct).ConfigureAwait(false);

            return Result.Success(_statusBuilder.BuildUploadedSyncStatus(
                gameId, provider.ProviderName, isEncrypted, conflictType, localVersion, cloudVersion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloud sync failed for game {GameId}", gameId);
            return Result.Failure<SaveStateCloudSyncStatus>($"Sync failed: {ex.Message}", ErrorType.Internal);
        }
        finally
        {
            CleanupTempFiles(tempFiles);
        }
    }

    public async Task<Result<SaveStateConflictResolution>> DetectConflictsAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        var providerResult = await _providerManager.ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsFailure)
            return Result.Failure<SaveStateConflictResolution>(providerResult.Error!, providerResult.ErrorType);
        
        return await _conflictManager.DetectConflictsAsync(gameId, providerResult.Value, ct).ConfigureAwait(false);
    }

    public async Task<Result<SaveStateCloudVersion>> CreateVersionAsync(
        Guid gameId,
        string versionName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(versionName))
            return Result.Failure<SaveStateCloudVersion>("Version name required", ErrorType.Validation);

        var localSaveResult = await _resolutionManager.ResolveLocalSaveStateAsync(gameId, null, ct).ConfigureAwait(false);
        if (localSaveResult.IsFailure)
            return Result.Failure<SaveStateCloudVersion>(localSaveResult.Error!, localSaveResult.ErrorType);

        var versionResult = await _versionManager.BuildVersionAsync(
            localSaveResult.Value, versionName, Environment.MachineName, false, null, ct).ConfigureAwait(false);
        if (versionResult.IsFailure)
            return Result.Failure<SaveStateCloudVersion>(versionResult.Error!, versionResult.ErrorType);

        await _versionManager.AppendVersionAsync(gameId, versionResult.Value, ct).ConfigureAwait(false);
        
        var providerResult = await _providerManager.ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsSuccess)
        {
            await _versionManager.UploadVersionMetadataAsync(providerResult.Value, versionResult.Value, ct).ConfigureAwait(false);
        }

        return versionResult;
    }

    public async Task<Result<IReadOnlyList<SaveStateCloudVersion>>> GetVersionHistoryAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        return Result.Success(await _versionManager.GetVersionHistoryAsync(gameId, ct).ConfigureAwait(false));
    }

    public async Task<Result<SaveStateCloudSyncStatus>> ResolveConflictAsync(
        Guid gameId,
        SaveStateConflictResolutionStrategy strategy,
        SaveStateCloudMetadata? metadata = null,
        CancellationToken ct = default)
    {
        metadata ??= new SaveStateCloudMetadata();
        var providerResult = await _providerManager.ResolveProviderAsync(ct).ConfigureAwait(false);
        if (providerResult.IsFailure)
            return Result.Failure<SaveStateCloudSyncStatus>(providerResult.Error!, providerResult.ErrorType);

        return strategy switch
        {
            SaveStateConflictResolutionStrategy.KeepLocal => await SyncSaveStateAsync(
                gameId, metadata with { ForceUpload = true }, ct).ConfigureAwait(false),
            SaveStateConflictResolutionStrategy.KeepBoth => await _conflictManager.ResolveKeepBothAsync(
                gameId, metadata, SyncSaveStateAsync, ct).ConfigureAwait(false),
            SaveStateConflictResolutionStrategy.KeepCloud => await _conflictManager.ResolveKeepCloudAsync(
                gameId, metadata, providerResult.Value, ct).ConfigureAwait(false),
            _ => Result.Failure<SaveStateCloudSyncStatus>(
                $"Strategy '{strategy}' not supported", ErrorType.Validation)
        };
    }

    private static void CleanupTempFiles(List<string> tempFiles)
    {
        foreach (var file in tempFiles)
        {
            try { if (File.Exists(file)) File.Delete(file); }
            catch { /* Best effort */ }
        }
    }
}
```

**Benefits:**
- ~180 lines (82% reduction)
- Clear sync workflow
- Each manager has single responsibility
- Easy to test conflict scenarios
- Cleanup centralized

---

## New File Structure

```
src/SaveState.Infrastructure/SaveStates/
├── SaveStateCloudService.cs                     # Coordinator (~180 lines)
├── Managers/
│   ├── CloudProviderManager.cs                  # Provider resolution (~120 lines)
│   ├── SaveStateResolutionManager.cs            # Local save lookup (~100 lines)
│   ├── SaveStateEncryptionManager.cs            # Encryption ops (~140 lines)
│   ├── SaveStateVersionManager.cs               # Version management (~160 lines)
│   ├── SaveStateConflictManager.cs              # Conflict handling (~180 lines)
│   └── CloudSyncStatusBuilder.cs                # Status building (~100 lines)
└── (existing files unchanged)
```

---

## Key Challenges and Edge Cases

### 1. Callback Pattern for ResolveKeepBoth

**Challenge:** ResolveKeepBoth needs to call back to sync after creating snapshot.

**Solution:** Pass sync function as delegate:
```csharp
public async Task<Result<SaveStateCloudSyncStatus>> ResolveKeepBothAsync(
    Guid gameId,
    SaveStateCloudMetadata metadata,
    Func<Guid, SaveStateCloudMetadata, CancellationToken, Task<Result<SaveStateCloudSyncStatus>>> syncFunc,
    CancellationToken ct)
{
    var snapshotResult = await CreateVersionAsync(gameId, snapshotName, ct);
    if (snapshotResult.IsFailure) return ...;
    
    return await syncFunc(gameId, metadata with { ForceUpload = true }, ct);
}
```

---

### 2. Circular Dependency Avoidance

**Challenge:** ConflictManager needs VersionManager, but both need repository access.

**Solution:** Accept both in constructor, DI handles:
```csharp
public SaveStateConflictManager(
    SaveStateVersionManager versionManager,
    SaveStateResolutionManager resolutionManager,
    SaveStateCloudVersionStore versionStore,
    ISaveStateRepository saveStateRepository,
    ITimeProvider timeProvider)
```

---

### 3. Temp File Cleanup Consistency

**Challenge:** Temp files created in multiple places need cleanup.

**Solution:** Coordinator owns cleanup, managers populate list:
```csharp
// In coordinator
var tempFiles = new List<string>();
try
{
    var path = await _encryptionManager.EncryptAsync(file, key, tempFiles, ct);
    // ... use path
}
finally
{
    CleanupTempFiles(tempFiles);
}

// Manager adds to list
public async Task<Result<string>> EncryptAsync(..., ICollection<string> tempFiles, ...)
{
    var tempPath = Path.GetTempFileName();
    tempFiles.Add(tempPath);
    // ... encrypt to tempPath
    return Result.Success(tempPath);
}
```

---

### 4. VersionStore Shared State

**Challenge:** VersionStore is currently created in constructor.

**Solution:** Register as singleton or accept in manager:
```csharp
// Option 1: Register VersionStore in DI
services.AddSingleton<SaveStateCloudVersionStore>();

// Option 2: Create path resolution service
public interface IVersionStorePathResolver
{
    string ResolveVersionHistoryPath();
}
```

---

### 5. Status Building Duplication

**Challenge:** Multiple status building methods with similar logic.

**Solution:** Centralize in CloudSyncStatusBuilder:
```csharp
public SaveStateCloudSyncStatus BuildStatus(
    Guid gameId,
    string provider,
    bool uploaded,
    bool downloaded,
    bool isEncrypted,
    SaveStateConflictType conflict,
    SaveStateCloudVersion local,
    SaveStateCloudVersion? cloud,
    string message)
{
    return new SaveStateCloudSyncStatus
    {
        GameId = gameId,
        Provider = provider,
        Uploaded = uploaded,
        Downloaded = downloaded,
        HasConflict = conflict != SaveStateConflictType.None,
        ConflictType = conflict,
        SyncedAtUtc = _timeProvider.UtcNow,
        IsEncrypted = isEncrypted,
        Message = message,
        LocalVersion = local,
        CloudVersion = cloud
    };
}
```

---

## Migration Steps

1. **Create CloudProviderManager**
   - Extract provider resolution logic
   - Add unit tests

2. **Create SaveStateResolutionManager**
   - Extract save state lookup methods
   - Add unit tests

3. **Create SaveStateEncryptionManager**
   - Extract encryption/decryption methods
   - Add unit tests

4. **Create SaveStateVersionManager**
   - Extract version building and history
   - Add unit tests

5. **Create SaveStateConflictManager**
   - Extract conflict detection and resolution
   - Add unit tests

6. **Create CloudSyncStatusBuilder**
   - Extract status building methods
   - Add unit tests

7. **Refactor SaveStateCloudService**
   - Inject all managers
   - Simplify to coordination
   - Ensure cleanup in finally blocks

8. **Update Tests**
   - Unit tests for each manager
   - Integration tests for coordinator
   - Conflict scenario tests

---

## Estimated Effort

| Task | Estimated Time |
|------|----------------|
| Create CloudProviderManager | 2 hours |
| Create SaveStateResolutionManager | 1.5 hours |
| Create SaveStateEncryptionManager | 2 hours |
| Create SaveStateVersionManager | 2.5 hours |
| Create SaveStateConflictManager | 3 hours |
| Create CloudSyncStatusBuilder | 1 hour |
| Refactor SaveStateCloudService | 2 hours |
| Update Unit Tests | 4 hours |
| Integration Testing | 2 hours |
| **Total** | **20 hours** |

---

## Success Criteria

- [ ] SaveStateCloudService under 200 lines
- [ ] All managers under 200 lines each
- [ ] Existing tests pass without modification
- [ ] New manager unit tests achieve 80%+ coverage
- [ ] No regression in sync functionality
- [ ] Conflict resolution works for all strategies
- [ ] Encryption/decryption still works
- [ ] Temp file cleanup verified
- [ ] Build succeeds with 0 warnings
