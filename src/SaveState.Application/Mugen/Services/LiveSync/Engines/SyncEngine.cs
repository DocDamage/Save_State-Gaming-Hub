using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.LiveSync;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.LiveSync.Engines;

/// <summary>
/// Core synchronization engine for managing cross-platform data sync operations.
/// </summary>
public class SyncEngine
{
    private readonly ILogger<SyncEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, SyncState> _syncStates = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastSyncAttempts = new();

    public SyncEngine(ILogger<SyncEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Calculates the sync health for a unified account.
    /// </summary>
    /// <param name="account">The unified account to analyze.</param>
    /// <returns>A SyncHealth object containing health metrics.</returns>
    public SyncHealth CalculateSyncHealth(UnifiedAccount account)
    {
        _logger.LogDebug("Calculating sync health for account {AccountId}", account.AccountId);

        var health = new SyncHealth
        {
            LastSuccessfulSync = GetLastSuccessfulSyncTime(account),
            ConsecutiveFailures = GetConsecutiveFailures(account.AccountId)
        };

        // Calculate platform-specific health
        float totalPlatformHealth = 0;
        int platformCount = 0;

        foreach (var platform in account.LinkedPlatforms)
        {
            var platformHealth = CalculatePlatformHealth(platform.Value);
            health.PlatformHealth[platform.Key] = platformHealth;
            totalPlatformHealth += platformHealth;
            platformCount++;

            if (platformHealth < 0.5f)
            {
                health.Issues.Add($"Platform {platform.Key} has poor sync health: {platformHealth:P0}");
            }
        }

        // Calculate overall score
        health.Score = platformCount > 0 ? totalPlatformHealth / platformCount : 1.0f;

        // Determine health status based on score and failures
        health.Status = health.Score switch
        {
            >= 0.95f when health.ConsecutiveFailures == 0 => SyncHealthStatus.Excellent,
            >= 0.80f when health.ConsecutiveFailures < 2 => SyncHealthStatus.Good,
            >= 0.60f => SyncHealthStatus.Fair,
            >= 0.40f => SyncHealthStatus.Poor,
            _ => SyncHealthStatus.Critical
        };

        if (health.ConsecutiveFailures > 3)
        {
            health.Status = SyncHealthStatus.Critical;
            health.Issues.Add($"Multiple consecutive failures: {health.ConsecutiveFailures}");
        }

        if ((_timeProvider.UtcNow - health.LastSuccessfulSync).TotalDays > 7)
        {
            health.Issues.Add("Last successful sync was over 7 days ago");
        }

        _logger.LogInformation(
            "Sync health calculated for {AccountId}: {Status} (Score: {Score:F2})",
            account.AccountId, health.Status, health.Score);

        return health;
    }

    /// <summary>
    /// Calculates data completeness for a unified account.
    /// </summary>
    /// <param name="account">The unified account to analyze.</param>
    /// <returns>A DataCompleteness object containing completeness metrics.</returns>
    public DataCompleteness CalculateDataCompleteness(UnifiedAccount account)
    {
        _logger.LogDebug("Calculating data completeness for account {AccountId}", account.AccountId);

        var completeness = new DataCompleteness();
        var categories = new Dictionary<string, float>();

        // Check profile completeness
        float profileCompleteness = CalculateProfileCompleteness(account);
        categories["Profile"] = profileCompleteness;

        // Check preferences completeness
        float preferencesCompleteness = account.Preferences != null ? 1.0f : 0.0f;
        categories["Preferences"] = preferencesCompleteness;

        // Check statistics completeness
        float statisticsCompleteness = CalculateStatisticsCompleteness(account.Statistics);
        categories["Statistics"] = statisticsCompleteness;

        // Check platform data completeness
        float platformCompleteness = CalculatePlatformDataCompleteness(account);
        categories["PlatformData"] = platformCompleteness;

        // Check for missing fields
        if (string.IsNullOrEmpty(account.DisplayName))
            completeness.MissingFields.Add("DisplayName");
        if (string.IsNullOrEmpty(account.Email))
            completeness.MissingFields.Add("Email");
        if (string.IsNullOrEmpty(account.ProfilePictureUrl))
            completeness.MissingFields.Add("ProfilePictureUrl");

        // Identify incomplete platforms
        foreach (var platform in account.LinkedPlatforms)
        {
            if (platform.Value.SyncStatus == SyncStatus.Error ||
                platform.Value.SyncStatus == SyncStatus.Inactive)
            {
                completeness.IncompletePlatforms.Add(platform.Key);
            }
        }

        // Calculate overall completeness
        completeness.OverallCompleteness = categories.Values.Count > 0
            ? categories.Values.Average()
            : 1.0f;

        completeness.Categories = categories;

        _logger.LogInformation(
            "Data completeness calculated for {AccountId}: {Completeness:P0}",
            account.AccountId, completeness.OverallCompleteness);

        return completeness;
    }

    /// <summary>
    /// Performs a synchronization operation for an account.
    /// </summary>
    /// <param name="accountId">The account ID to sync.</param>
    /// <param name="direction">The sync direction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A SyncResult containing the operation results.</returns>
    public async Task<SyncResult> PerformSyncAsync(
        string accountId,
        SyncDirection direction,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting sync for account {AccountId} with direction {Direction}",
            accountId, direction);

        var stopwatch = Stopwatch.StartNew();
        var result = new SyncResult();
        _lastSyncAttempts[accountId] = _timeProvider.UtcNow;

        try
        {
            // Update sync state
            _syncStates[accountId] = new SyncState
            {
                AccountId = accountId,
                Status = SyncStatus.Active,
                LastSyncAt = _timeProvider.UtcNow,
                CurrentOperation = "Initializing"
            };

            // Simulate sync operations based on direction
            int itemsToSync = await GetItemsToSyncCountAsync(accountId, direction, ct);
            result.ItemsSynced = 0;

            _syncStates[accountId].CurrentOperation = "Syncing data";

            // Process sync in batches
            const int batchSize = 100;
            int processedItems = 0;

            while (processedItems < itemsToSync)
            {
                ct.ThrowIfCancellationRequested();

                int batchCount = Math.Min(batchSize, itemsToSync - processedItems);
                int syncedInBatch = await ProcessSyncBatchAsync(accountId, direction, batchCount, ct);
                result.ItemsSynced += syncedInBatch;
                processedItems += batchCount;

                // Simulate progress
                await Task.Delay(10, ct);
            }

            // Check for conflicts
            result.ConflictsFound = await DetectConflictsAsync(accountId, ct);

            // Update state on success
            _syncStates[accountId].Status = SyncStatus.Completed;
            _syncStates[accountId].ProgressPercentage = 100;

            // Reset consecutive failures on success
            _lastSyncAttempts.TryRemove(accountId, out _);

            result.Success = true;

            _logger.LogInformation(
                "Sync completed for account {AccountId}: {ItemsSynced} items synced, {Conflicts} conflicts in {Duration:F2}s",
                accountId, result.ItemsSynced, result.ConflictsFound, stopwatch.Elapsed.TotalSeconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Sync cancelled for account {AccountId}", accountId);

            if (_syncStates.TryGetValue(accountId, out var state))
            {
                state.Status = SyncStatus.Failed;
            }

            result.Success = false;
            result.ErrorMessage = "Sync operation was cancelled";
            IncrementConsecutiveFailures(accountId);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for account {AccountId}", accountId);

            if (_syncStates.TryGetValue(accountId, out var state))
            {
                state.Status = SyncStatus.Error;
            }

            result.Success = false;
            result.ErrorMessage = ex.Message;
            IncrementConsecutiveFailures(accountId);
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Performs a synchronization using a sync session with progress reporting.
    /// </summary>
    /// <param name="session">The sync session.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A SyncResult containing the operation results.</returns>
    public async Task<SyncResult> PerformSyncAsync(
        SyncSession session,
        IProgress<SyncProgress> progress,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting sync session {SessionId} for account {AccountId}",
            session.SessionId, session.AccountId);

        var stopwatch = Stopwatch.StartNew();
        var result = new SyncResult();

        try
        {
            var syncProgress = new SyncProgress
            {
                TotalItems = 100,
                ProcessedItems = 0,
                CurrentPhase = "Preparing",
                EstimatedTimeRemaining = TimeSpan.FromMinutes(2)
            };

            progress?.Report(syncProgress);

            // Determine direction based on mode
            var direction = session.Mode switch
            {
                SyncMode.Full => SyncDirection.Bidirectional,
                SyncMode.Incremental => SyncDirection.Bidirectional,
                SyncMode.PreferencesOnly => SyncDirection.Upload,
                SyncMode.ProgressOnly => SyncDirection.Bidirectional,
                _ => SyncDirection.Bidirectional
            };

            // Update progress phases
            syncProgress.CurrentPhase = "Syncing to target platforms";
            progress?.Report(syncProgress);

            int totalItems = session.TargetPlatforms.Count * 25;
            int processedItems = 0;

            foreach (var platform in session.TargetPlatforms)
            {
                ct.ThrowIfCancellationRequested();

                syncProgress.CurrentPhase = $"Syncing to {platform}";
                progress?.Report(syncProgress);

                // Simulate platform sync
                await Task.Delay(100, ct);
                processedItems += 25;
                syncProgress.ProcessedItems = processedItems;
                progress?.Report(syncProgress);
            }

            syncProgress.CurrentPhase = "Finalizing";
            syncProgress.ProcessedItems = totalItems;
            progress?.Report(syncProgress);

            result.ItemsSynced = totalItems;
            result.ConflictsFound = 0;
            result.Success = true;

            _logger.LogInformation(
                "Sync session {SessionId} completed: {ItemsSynced} items in {Duration:F2}s",
                session.SessionId, result.ItemsSynced, stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync session {SessionId} failed", session.SessionId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    #region Private Helper Methods

    private float CalculatePlatformHealth(PlatformAccount platform)
    {
        var timeSinceLastSync = _timeProvider.UtcNow - platform.LastSyncAt;

        // Health degrades based on time since last sync
        float health = timeSinceLastSync.TotalHours switch
        {
            < 1 => 1.0f,
            < 24 => 0.9f,
            < 72 => 0.7f,
            < 168 => 0.5f,
            _ => 0.3f
        };

        // Adjust based on sync status
        health = platform.SyncStatus switch
        {
            SyncStatus.Active => health,
            SyncStatus.Completed => Math.Max(health, 0.95f),
            SyncStatus.Error => health * 0.5f,
            SyncStatus.Inactive => health * 0.3f,
            _ => health
        };

        return Math.Clamp(health, 0.0f, 1.0f);
    }

    private DateTime GetLastSuccessfulSyncTime(UnifiedAccount account)
    {
        if (account.LinkedPlatforms.Count == 0)
            return account.LastLoginAt;

        return account.LinkedPlatforms.Values
            .Where(p => p.LastSyncAt > DateTime.MinValue)
            .Select(p => p.LastSyncAt)
            .DefaultIfEmpty(account.LastLoginAt)
            .Max();
    }

    private int GetConsecutiveFailures(string accountId)
    {
        // Check if we have recent failed attempts
        if (_lastSyncAttempts.TryGetValue(accountId, out var lastAttempt))
        {
            var timeSinceAttempt = _timeProvider.UtcNow - lastAttempt;
            if (timeSinceAttempt.TotalMinutes < 30)
            {
                // This is a simplified logic - in production you'd track actual failures
                return 1;
            }
        }
        return 0;
    }

    private void IncrementConsecutiveFailures(string accountId)
    {
        // Track failure for health calculations
        _lastSyncAttempts[accountId] = _timeProvider.UtcNow;
    }

    private float CalculateProfileCompleteness(UnifiedAccount account)
    {
        int fields = 0;
        int filledFields = 0;

        if (!string.IsNullOrEmpty(account.DisplayName)) filledFields++;
        fields++;
        if (!string.IsNullOrEmpty(account.Email)) filledFields++;
        fields++;
        if (!string.IsNullOrEmpty(account.ProfilePictureUrl)) filledFields++;
        fields++;

        return fields > 0 ? (float)filledFields / fields : 1.0f;
    }

    private float CalculateStatisticsCompleteness(UnifiedStatistics? statistics)
    {
        if (statistics == null) return 0.0f;

        int fields = 0;
        int filledFields = 0;

        if (statistics.TotalPlayTime > TimeSpan.Zero) filledFields++;
        fields++;
        if (statistics.PlatformsUsed?.Count > 0) filledFields++;
        fields++;
        if (!string.IsNullOrEmpty(statistics.FavoriteCharacter)) filledFields++;
        fields++;
        if (statistics.AchievementCount > 0) filledFields++;
        fields++;

        return fields > 0 ? (float)filledFields / fields : 1.0f;
    }

    private float CalculatePlatformDataCompleteness(UnifiedAccount account)
    {
        if (account.LinkedPlatforms.Count == 0) return 1.0f;

        int healthyPlatforms = account.LinkedPlatforms.Values
            .Count(p => p.SyncStatus == SyncStatus.Active || p.SyncStatus == SyncStatus.Completed);

        return (float)healthyPlatforms / account.LinkedPlatforms.Count;
    }

    private async Task<int> GetItemsToSyncCountAsync(string accountId, SyncDirection direction, CancellationToken ct)
    {
        // Simulate fetching items count
        await Task.Delay(50, ct);
        return new Random().Next(50, 500);
    }

    private async Task<int> ProcessSyncBatchAsync(
        string accountId,
        SyncDirection direction,
        int batchSize,
        CancellationToken ct)
    {
        // Simulate processing batch
        await Task.Delay(20, ct);
        return batchSize;
    }

    private async Task<int> DetectConflictsAsync(string accountId, CancellationToken ct)
    {
        // Simulate conflict detection
        await Task.Delay(30, ct);
        return new Random().Next(0, 5);
    }

    #endregion
}
