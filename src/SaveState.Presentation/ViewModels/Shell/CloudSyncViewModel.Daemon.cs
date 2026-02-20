using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.SaveStates.Services.DTOs;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Partial class containing daemon management and conflict resolution for CloudSyncViewModel.
/// </summary>
public partial class CloudSyncViewModel
{
    /// <summary>
    /// Applies daemon status updates to observable properties.
    /// </summary>
    private void ApplyDaemonStatus(SaveStateCloudDaemonStatus status)
    {
        IsBackgroundSyncEnabled = status.Enabled;
        BackgroundDaemonState = !status.Enabled
            ? "Disabled"
            : status.IsRunning ? "Running" : "Stopped";
        BackgroundDaemonLastSync = status.LastSyncAtUtc.HasValue
            ? status.LastSyncAtUtc.Value.ToLocalTime().ToString("g")
            : "Never";
        BackgroundDaemonSummary =
            $"{status.SuccessfulSyncCount} successful | {status.FailedSyncCount} failed | {status.ConflictCount} conflicts | {status.SkippedCount} skipped";
        BackgroundDaemonMessage = status.LastMessage;

        var healthSnapshot = EvaluateDaemonHealth(status);
        BackgroundDaemonHealthStatus = healthSnapshot.Status;
        BackgroundDaemonHealthCue = healthSnapshot.Cue;
        ShowResolveConflictsQuickAction = healthSnapshot.ShowResolveConflictsQuickAction;
        ShowRetrySyncQuickAction = healthSnapshot.ShowRetrySyncQuickAction;
        ShowConfigureProviderQuickAction = healthSnapshot.ShowConfigureProviderQuickAction;
        HasBackgroundQuickActions =
            ShowResolveConflictsQuickAction ||
            ShowRetrySyncQuickAction ||
            ShowConfigureProviderQuickAction;

        ProcessDaemonAlertNotifications(status);
    }

    /// <summary>
    /// Evaluates the health of the background sync daemon.
    /// </summary>
    private static DaemonHealthSnapshot EvaluateDaemonHealth(SaveStateCloudDaemonStatus status)
    {
        if (!status.Enabled)
        {
            return new DaemonHealthSnapshot(
                "Disabled",
                "Background sync daemon is disabled. Enable it in cloud sync settings.",
                status.ConflictCount > 0,
                false,
                true);
        }

        if (status.FailedSyncCount > 0)
        {
            var failureLabel = status.FailedSyncCount == 1 ? "failure" : "failures";
            return new DaemonHealthSnapshot(
                "Critical",
                $"Background sync reported {status.FailedSyncCount} {failureLabel}. Retry sync and review provider settings.",
                status.ConflictCount > 0,
                true,
                true);
        }

        if (status.ConflictCount > 0)
        {
            var conflictLabel = status.ConflictCount == 1 ? "conflict" : "conflicts";
            return new DaemonHealthSnapshot(
                "Warning",
                $"Background sync detected {status.ConflictCount} {conflictLabel}. Resolve conflicts to prevent data divergence.",
                true,
                false,
                false);
        }

        if (!status.IsRunning)
        {
            return new DaemonHealthSnapshot(
                "Stopped",
                "Background sync daemon is not running. Retry sync or review daemon settings.",
                false,
                true,
                true);
        }

        return new DaemonHealthSnapshot(
            "Healthy",
            "Background sync is operating normally.",
            false,
            false,
            false);
    }

    /// <summary>
    /// Processes daemon alert notifications with cooldown.
    /// </summary>
    private void ProcessDaemonAlertNotifications(SaveStateCloudDaemonStatus status)
    {
        if (_lastDaemonStatusSnapshot is null)
        {
            _lastDaemonStatusSnapshot = status;
            return;
        }

        var failureDelta = Math.Max(0, status.FailedSyncCount - _lastDaemonStatusSnapshot.FailedSyncCount);
        var conflictDelta = Math.Max(0, status.ConflictCount - _lastDaemonStatusSnapshot.ConflictCount);

        if (_daemonFailureAlertsEnabled && failureDelta > 0)
        {
            _pendingDaemonFailureAlerts += failureDelta;
        }
        else if (!_daemonFailureAlertsEnabled)
        {
            _pendingDaemonFailureAlerts = 0;
        }

        if (_daemonConflictAlertsEnabled && conflictDelta > 0)
        {
            _pendingDaemonConflictAlerts += conflictDelta;
        }
        else if (!_daemonConflictAlertsEnabled)
        {
            _pendingDaemonConflictAlerts = 0;
        }

        var nowUtc = _timeProvider.UtcNow;
        var alertCooldown = TimeSpan.FromSeconds(ClampDaemonAlertCooldownSeconds(_daemonAlertCooldownSeconds));

        if (_daemonFailureAlertsEnabled &&
            _pendingDaemonFailureAlerts > 0 &&
            nowUtc - _lastDaemonFailureAlertAtUtc >= alertCooldown)
        {
            var failureLabel = _pendingDaemonFailureAlerts == 1 ? "failure" : "failures";
            _notificationService.ShowError(
                $"Background save-state sync reported {_pendingDaemonFailureAlerts} new {failureLabel}. {status.LastMessage}");
            _pendingDaemonFailureAlerts = 0;
            _lastDaemonFailureAlertAtUtc = nowUtc;
        }

        if (_daemonConflictAlertsEnabled &&
            _pendingDaemonConflictAlerts > 0 &&
            nowUtc - _lastDaemonConflictAlertAtUtc >= alertCooldown)
        {
            var conflictLabel = _pendingDaemonConflictAlerts == 1 ? "conflict" : "conflicts";
            _notificationService.ShowWarning(
                $"Background save-state sync detected {_pendingDaemonConflictAlerts} new {conflictLabel}. Open 'View Conflicts' to resolve.");
            _pendingDaemonConflictAlerts = 0;
            _lastDaemonConflictAlertAtUtc = nowUtc;
        }

        _lastDaemonStatusSnapshot = status;
    }

    /// <summary>
    /// Clamps daemon alert cooldown to valid range.
    /// </summary>
    private static int ClampDaemonAlertCooldownSeconds(int cooldownSeconds)
    {
        if (cooldownSeconds < MinDaemonAlertCooldownSeconds)
        {
            return MinDaemonAlertCooldownSeconds;
        }

        if (cooldownSeconds > MaxDaemonAlertCooldownSeconds)
        {
            return MaxDaemonAlertCooldownSeconds;
        }

        return cooldownSeconds;
    }

    /// <summary>
    /// Appends save-state conflicts to the conflict list.
    /// </summary>
    private async Task<Dictionary<string, SaveStateConflictEntry>> AppendSaveStateConflictsAsync(ICollection<Services.SyncConflictViewModel> conflicts)
    {
        var map = new Dictionary<string, SaveStateConflictEntry>(StringComparer.Ordinal);

        IReadOnlyList<Core.GameLibrary.Entities.Game> games;
        try
        {
            games = await _gameRepository.GetAllAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate games for save-state conflict detection");
            return map;
        }

        foreach (var game in games
                     .OrderByDescending(g => g.LastPlayedAt ?? g.UpdatedAt ?? g.CreatedAt)
                     .Take(25))
        {
            SaveStateConflictResolution? saveConflict = null;
            try
            {
                var detectResult = await _saveStateCloudService.DetectConflictsAsync(game.Id).ConfigureAwait(false);
                if (detectResult.IsFailure || detectResult.Value is null || detectResult.Value.Type == SaveStateConflictType.None)
                {
                    continue;
                }

                saveConflict = detectResult.Value;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to detect save-state conflict for game {GameId}", game.Id);
            }

            if (saveConflict is null)
            {
                continue;
            }

            var displayKey = $"SaveState::{game.Id:N}::{game.Title}";
            if (map.ContainsKey(displayKey))
            {
                continue;
            }

            conflicts.Add(new Services.SyncConflictViewModel(
                displayKey,
                (saveConflict.LocalVersion?.CreatedAtUtc ?? _timeProvider.UtcNow).ToLocalTime(),
                (saveConflict.CloudVersion?.CreatedAtUtc ?? _timeProvider.UtcNow).ToLocalTime(),
                saveConflict.LocalVersion?.FileSizeBytes ?? 0,
                saveConflict.CloudVersion?.FileSizeBytes ?? 0));

            map[displayKey] = new SaveStateConflictEntry(game.Id, saveConflict);
        }

        return map;
    }

    /// <summary>
    /// Resolves a single save-state conflict.
    /// </summary>
    private async Task<ConflictApplyResult> ResolveSaveStateConflictAsync(
        string conflictKey,
        SaveStateConflictEntry conflictEntry,
        IDictionary<string, string> encryptionKeyCache,
        string strategy)
    {
        var normalized = strategy.Trim().ToLowerInvariant();
        if (normalized == "skip")
        {
            return ConflictApplyResult.Failed();
        }

        var conflictStrategy = normalized switch
        {
            "keep local" => SaveStateConflictResolutionStrategy.KeepLocal,
            "keep cloud" => SaveStateConflictResolutionStrategy.KeepCloud,
            "keep both" => SaveStateConflictResolutionStrategy.KeepBoth,
            _ => SaveStateConflictResolutionStrategy.PromptUser
        };

        if (conflictStrategy == SaveStateConflictResolutionStrategy.PromptUser)
        {
            _logger.LogWarning(
                "Unknown save-state conflict strategy '{Strategy}' for game {GameId}",
                strategy,
                conflictEntry.GameId);
            return ConflictApplyResult.Failed($"Unsupported strategy '{strategy}' for save-state conflict '{conflictKey}'.");
        }

        var metadata = new SaveStateCloudMetadata
        {
            DeviceName = Environment.MachineName,
            ForceUpload = conflictStrategy is SaveStateConflictResolutionStrategy.KeepLocal or SaveStateConflictResolutionStrategy.KeepBoth,
            VersionName = conflictStrategy switch
            {
                SaveStateConflictResolutionStrategy.KeepLocal => $"Conflict KeepLocal {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss}",
                SaveStateConflictResolutionStrategy.KeepBoth => $"Conflict KeepBoth {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss}",
                _ => null
            }
        };

        if (conflictStrategy == SaveStateConflictResolutionStrategy.KeepCloud &&
            conflictEntry.Conflict.CloudVersion?.IsEncrypted == true)
        {
            var encryptionCacheKey = BuildEncryptionCacheKey(conflictEntry);
            if (!encryptionKeyCache.TryGetValue(encryptionCacheKey, out var encryptionKey) ||
                string.IsNullOrWhiteSpace(encryptionKey))
            {
                encryptionKey = await _dialogService.ShowInputDialogAsync(
                    "Cloud Save Encryption Key",
                    $"Enter the encryption key to restore cloud save conflict '{conflictKey}'.",
                    "Encryption key",
                    isSensitive: true).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(encryptionKey))
                {
                    return ConflictApplyResult.Failed(
                        $"Skipped encrypted save-state conflict '{conflictKey}' because no encryption key was provided.");
                }

                encryptionKey = encryptionKey.Trim();
                encryptionKeyCache[encryptionCacheKey] = encryptionKey;
            }

            metadata = metadata with
            {
                EncryptionKey = encryptionKey
            };
        }

        var resolveResult = await _saveStateCloudService.ResolveConflictAsync(
            conflictEntry.GameId,
            conflictStrategy,
            metadata).ConfigureAwait(false);
        if (resolveResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to resolve save-state conflict for game {GameId} with strategy {Strategy}: {Error}",
                conflictEntry.GameId,
                strategy,
                resolveResult.Error);
            return ConflictApplyResult.Failed(
                $"Save-state conflict '{conflictKey}' failed with strategy '{strategy}': {resolveResult.Error ?? "unknown error"}.");
        }

        return ConflictApplyResult.Successful();
    }

    /// <summary>
    /// Builds the encryption cache key for a conflict entry.
    /// </summary>
    private static string BuildEncryptionCacheKey(SaveStateConflictEntry conflictEntry)
    {
        var fingerprint = conflictEntry.Conflict.CloudVersion?.EncryptionKeyFingerprint;
        if (!string.IsNullOrWhiteSpace(fingerprint))
        {
            return $"fingerprint:{fingerprint.Trim()}";
        }

        return $"game:{conflictEntry.GameId:N}";
    }

    /// <summary>
    /// Builds a failure summary from a list of failure messages.
    /// </summary>
    private static string BuildFailureSummary(IReadOnlyList<string> failureMessages)
    {
        if (failureMessages.Count == 0)
        {
            return "No failure details were provided.";
        }

        var distinctFailures = failureMessages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .ToList();

        if (distinctFailures.Count == 0)
        {
            return "No failure details were provided.";
        }

        var summary = string.Join(" | ", distinctFailures);
        if (failureMessages.Count > distinctFailures.Count)
        {
            summary += $" | +{failureMessages.Count - distinctFailures.Count} more";
        }

        return summary;
    }
}
