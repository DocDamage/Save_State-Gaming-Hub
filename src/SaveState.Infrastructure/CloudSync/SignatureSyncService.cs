using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Core.CloudSync.Services;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.CloudSync;

/// <summary>
/// Background service that automatically syncs cloud signatures to the local database.
/// Runs on a configurable interval to keep local signatures up to date.
/// </summary>
public class SignatureSyncService : BackgroundService
{
    private readonly ICloudSignatureDatabase _cloudDb;
    private readonly ILogger<SignatureSyncService> _logger;
    private readonly TimeSpan _syncInterval;
    private DateTime _lastSync = DateTime.MinValue;
    private readonly object _syncLock = new();
    private readonly List<GameMemorySignature> _localSignatures = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureSyncService"/> class.
    /// </summary>
    public SignatureSyncService(
        ICloudSignatureDatabase cloudDb,
        ILogger<SignatureSyncService> logger,
        IConfiguration configuration)
    {
        _cloudDb = cloudDb;
        _logger = logger;
        _syncInterval = TimeSpan.FromHours(
            configuration.GetValue("CloudSignatureDatabase:SyncIntervalHours", 24));
    }

    /// <summary>
    /// Event raised when new signatures are synced.
    /// </summary>
    public event EventHandler<SignatureSyncEventArgs>? SignaturesSynced;

    /// <summary>
    /// Event raised when a sync operation fails.
    /// </summary>
    public event EventHandler<SignatureSyncErrorEventArgs>? SyncFailed;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Signature sync service started. Sync interval: {Interval}", 
            _syncInterval);

        // Perform initial sync on startup
        await PerformSyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    await PerformSyncAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Signature sync service stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signature sync");
                SyncFailed?.Invoke(this, new SignatureSyncErrorEventArgs(ex.Message, ex));
            }
        }
    }

    /// <summary>
    /// Manually triggers a sync operation.
    /// </summary>
    public async Task ForceSyncAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Manual sync triggered");
        await PerformSyncAsync(ct);
    }

    /// <summary>
    /// Gets the timestamp of the last successful sync.
    /// </summary>
    public DateTime GetLastSyncTime()
    {
        lock (_syncLock)
        {
            return _lastSync;
        }
    }

    /// <summary>
    /// Gets all locally synced signatures.
    /// </summary>
    public IReadOnlyList<GameMemorySignature> GetLocalSignatures()
    {
        lock (_syncLock)
        {
            return _localSignatures.ToList().AsReadOnly();
        }
    }

    private async Task PerformSyncAsync(CancellationToken ct)
    {
        DateTime syncStartTime;
        lock (_syncLock)
        {
            syncStartTime = _lastSync;
        }

        _logger.LogInformation("Starting signature sync. Last sync: {LastSync}", syncStartTime);

        var result = await _cloudDb.GetChangesSinceAsync(syncStartTime, ct);
        
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to get signature changes: {Error}", result.Error);
            SyncFailed?.Invoke(this, new SignatureSyncErrorEventArgs(result.Error!, null));
            return;
        }

        var changes = result.Value;
        int added = 0, updated = 0, deprecated = 0;

        lock (_syncLock)
        {
            // Add new signatures
            foreach (var signature in changes.NewSignatures)
            {
                if (AddSignatureToLocalDb(signature))
                {
                    added++;
                }
            }

            // Update existing signatures
            foreach (var signature in changes.UpdatedSignatures)
            {
                if (UpdateSignatureInLocalDb(signature))
                {
                    updated++;
                }
            }

            // Mark deprecated signatures
            foreach (var signatureId in changes.DeprecatedSignatures)
            {
                if (DeprecateSignature(signatureId))
                {
                    deprecated++;
                }
            }

            _lastSync = changes.SyncTimestamp;
        }
        
        _logger.LogInformation(
            "Signature sync completed. Added: {Added}, Updated: {Updated}, Deprecated: {Deprecated}",
            added,
            updated,
            deprecated);

        // Raise event
        if (added > 0 || updated > 0 || deprecated > 0)
        {
            SignaturesSynced?.Invoke(this, new SignatureSyncEventArgs(
                added,
                updated,
                deprecated,
                changes.NewSignatures,
                changes.UpdatedSignatures,
                changes.DeprecatedSignatures));
        }
    }

    private bool AddSignatureToLocalDb(CloudSignature cloudSig)
    {
        try
        {
            // Check if signature already exists
            if (_localSignatures.Any(s => s.CloudId == cloudSig.Id))
            {
                _logger.LogDebug("Signature {SignatureId} already exists, skipping", cloudSig.Id);
                return false;
            }

            var localSig = new GameMemorySignature
            {
                GameTitle = cloudSig.GameTitle,
                Name = cloudSig.Name,
                Pattern = cloudSig.Pattern,
                Offset = cloudSig.Offset,
                ValueType = cloudSig.ValueType,
                Description = cloudSig.Description,
                CloudId = cloudSig.Id,
                CloudVersion = cloudSig.UpdatedAt,
                IsFromCloud = true,
                IsEnabled = true,
                Tags = new List<string> { cloudSig.Category, "cloud" }
            };

            _localSignatures.Add(localSig);
            _logger.LogDebug("Added signature {SignatureId} for {GameTitle}/{Name}", 
                cloudSig.Id, cloudSig.GameTitle, cloudSig.Name);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding signature {SignatureId} to local DB", cloudSig.Id);
            return false;
        }
    }

    private bool UpdateSignatureInLocalDb(CloudSignature cloudSig)
    {
        try
        {
            var existing = _localSignatures.FirstOrDefault(s => s.CloudId == cloudSig.Id);
            
            if (existing == null)
            {
                _logger.LogWarning("Signature {SignatureId} not found for update", cloudSig.Id);
                return false;
            }

            // Only update if cloud version is newer
            if (existing.CloudVersion.HasValue && existing.CloudVersion.Value >= cloudSig.UpdatedAt)
            {
                _logger.LogDebug("Signature {SignatureId} is up to date", cloudSig.Id);
                return false;
            }

            existing.Pattern = cloudSig.Pattern;
            existing.Offset = cloudSig.Offset;
            existing.ValueType = cloudSig.ValueType;
            existing.Description = cloudSig.Description;
            existing.CloudVersion = cloudSig.UpdatedAt;
            existing.GameVersion = cloudSig.GameVersion;

            _logger.LogDebug("Updated signature {SignatureId} for {GameTitle}/{Name}",
                cloudSig.Id, cloudSig.GameTitle, cloudSig.Name);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating signature {SignatureId}", cloudSig.Id);
            return false;
        }
    }

    private bool DeprecateSignature(string signatureId)
    {
        try
        {
            var existing = _localSignatures.FirstOrDefault(s => s.CloudId == signatureId);
            
            if (existing == null)
            {
                return false;
            }

            existing.IsDeprecated = true;
            existing.IsEnabled = false;
            
            _logger.LogDebug("Deprecated signature {SignatureId}", signatureId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deprecating signature {SignatureId}", signatureId);
            return false;
        }
    }
}

/// <summary>
/// Event arguments for successful signature sync.
/// </summary>
public class SignatureSyncEventArgs : EventArgs
{
    /// <summary>
    /// Number of new signatures added.
    /// </summary>
    public int AddedCount { get; }
    
    /// <summary>
    /// Number of signatures updated.
    /// </summary>
    public int UpdatedCount { get; }
    
    /// <summary>
    /// Number of signatures deprecated.
    /// </summary>
    public int DeprecatedCount { get; }
    
    /// <summary>
    /// New signatures that were added.
    /// </summary>
    public IReadOnlyList<CloudSignature> NewSignatures { get; }
    
    /// <summary>
    /// Signatures that were updated.
    /// </summary>
    public IReadOnlyList<CloudSignature> UpdatedSignatures { get; }
    
    /// <summary>
    /// IDs of signatures that were deprecated.
    /// </summary>
    public IReadOnlyList<string> DeprecatedSignatureIds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureSyncEventArgs"/> class.
    /// </summary>
    public SignatureSyncEventArgs(
        int addedCount,
        int updatedCount,
        int deprecatedCount,
        List<CloudSignature> newSignatures,
        List<CloudSignature> updatedSignatures,
        List<string> deprecatedSignatureIds)
    {
        AddedCount = addedCount;
        UpdatedCount = updatedCount;
        DeprecatedCount = deprecatedCount;
        NewSignatures = newSignatures.AsReadOnly();
        UpdatedSignatures = updatedSignatures.AsReadOnly();
        DeprecatedSignatureIds = deprecatedSignatureIds.AsReadOnly();
    }
}

/// <summary>
/// Event arguments for failed signature sync.
/// </summary>
public class SignatureSyncErrorEventArgs : EventArgs
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Error { get; }
    
    /// <summary>
    /// Exception that caused the error, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureSyncErrorEventArgs"/> class.
    /// </summary>
    public SignatureSyncErrorEventArgs(string error, Exception? exception)
    {
        Error = error;
        Exception = exception;
    }
}

