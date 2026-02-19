namespace SaveState.Application.Mugen.Services.LiveSync.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.LiveSync;
using SaveState.Core.Common.Services;
using System.Diagnostics;

/// <summary>
/// Engine for migrating platform data between different platforms.
/// </summary>
public class MigrationEngine
{
    private readonly ILogger<MigrationEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public MigrationEngine(ILogger<MigrationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Migrates data from the source platform to the target platform.
    /// </summary>
    /// <param name="sourceData">The source platform data to migrate from.</param>
    /// <param name="targetPlatform">The target platform type.</param>
    /// <param name="request">The migration request containing options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="MigrationResult"/> indicating the outcome of the migration.</returns>
    public async Task<MigrationResult> MigrateAsync(
        PlatformData sourceData,
        PlatformType targetPlatform,
        PlatformMigrationRequest request,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();
        var itemsMigrated = 0;

        try
        {
            _logger.LogInformation(
                "Starting migration from {SourcePlatform} to {TargetPlatform} for account {AccountId}",
                sourceData.Platform,
                targetPlatform,
                sourceData.AccountId);

            // Validate source data
            if (string.IsNullOrEmpty(sourceData.AccountId))
            {
                throw new InvalidOperationException("Source data has no AccountId");
            }

            // Create target platform data
            var targetData = new PlatformData
            {
                AccountId = sourceData.AccountId,
                Platform = targetPlatform,
                LastUpdated = _timeProvider.UtcNow,
                Version = sourceData.Version
            };

            // Migrate game progress if requested
            if (request.MigrateProgress)
            {
                if (sourceData.GameProgress?.Count > 0)
                {
                    targetData.GameProgress = new Dictionary<string, object>(sourceData.GameProgress);
                    itemsMigrated += sourceData.GameProgress.Count;
                    _logger.LogDebug(
                        "Migrated {Count} game progress items for account {AccountId}",
                        sourceData.GameProgress.Count,
                        sourceData.AccountId);
                }
                else
                {
                    targetData.GameProgress = new Dictionary<string, object>();
                    warnings.Add("No game progress data found to migrate");
                    _logger.LogWarning(
                        "No game progress data to migrate for account {AccountId}",
                        sourceData.AccountId);
                }
            }
            else
            {
                targetData.GameProgress = new Dictionary<string, object>();
            }

            // Migrate achievements if requested
            if (request.MigrateAchievements)
            {
                if (sourceData.Achievements?.Count > 0)
                {
                    targetData.Achievements = new List<string>(sourceData.Achievements);
                    itemsMigrated += sourceData.Achievements.Count;
                    _logger.LogDebug(
                        "Migrated {Count} achievements for account {AccountId}",
                        sourceData.Achievements.Count,
                        sourceData.AccountId);
                }
                else
                {
                    targetData.Achievements = new List<string>();
                    warnings.Add("No achievements found to migrate");
                    _logger.LogWarning(
                        "No achievements to migrate for account {AccountId}",
                        sourceData.AccountId);
                }
            }
            else
            {
                targetData.Achievements = new List<string>();
            }

            // Migrate preferences if requested
            if (request.MigratePreferences)
            {
                if (sourceData.Preferences?.Count > 0)
                {
                    targetData.Preferences = new Dictionary<string, object>(sourceData.Preferences);
                    itemsMigrated += sourceData.Preferences.Count;
                    _logger.LogDebug(
                        "Migrated {Count} preferences for account {AccountId}",
                        sourceData.Preferences.Count,
                        sourceData.AccountId);
                }
                else
                {
                    targetData.Preferences = new Dictionary<string, object>();
                    warnings.Add("No preferences found to migrate");
                    _logger.LogWarning(
                        "No preferences to migrate for account {AccountId}",
                        sourceData.AccountId);
                }
            }
            else
            {
                targetData.Preferences = new Dictionary<string, object>();
            }

            // Copy statistics (always included as metadata)
            if (sourceData.Statistics?.Count > 0)
            {
                targetData.Statistics = new Dictionary<string, object>(sourceData.Statistics);
            }
            else
            {
                targetData.Statistics = new Dictionary<string, object>();
            }

            // Simulate async persistence operation
            await Task.Delay(10, ct).ConfigureAwait(false);

            // Handle source data deletion if requested
            if (request.DeleteSourceData)
            {
                _logger.LogInformation(
                    "Deleting source data for account {AccountId} on platform {SourcePlatform}",
                    sourceData.AccountId,
                    sourceData.Platform);
                // In a real implementation, this would delete the source data from storage
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Migration completed successfully for account {AccountId}. Migrated {ItemsMigrated} items in {DurationMs}ms",
                sourceData.AccountId,
                itemsMigrated,
                stopwatch.ElapsedMilliseconds);

            return new MigrationResult
            {
                Success = true,
                ItemsMigrated = itemsMigrated,
                Duration = stopwatch.Elapsed,
                Warnings = warnings.Count > 0 ? warnings.AsReadOnly() : null
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Migration was cancelled for account {AccountId} after {DurationMs}ms",
                sourceData.AccountId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Migration failed for account {AccountId} after {DurationMs}ms: {ErrorMessage}",
                sourceData.AccountId,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            return new MigrationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ItemsMigrated = itemsMigrated,
                Duration = stopwatch.Elapsed,
                Warnings = warnings.Count > 0 ? warnings.AsReadOnly() : null
            };
        }
    }
}
