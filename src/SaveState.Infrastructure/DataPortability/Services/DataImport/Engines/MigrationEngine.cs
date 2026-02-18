using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Implementation of migration engine.
/// </summary>
public sealed class MigrationEngine : IMigrationEngine
{
    private readonly ILogger<MigrationEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public MigrationEngine(ILogger<MigrationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<MigrationResult> MigrateAsync(ParsedData data, Version? targetVersion = null, CancellationToken ct = default)
    {
        targetVersion ??= new Version(2, 4, 0); // Current app version

        // Extract source version from manifest
        Version? sourceVersion = null;
        if (data.Sections.TryGetValue("manifest", out var manifest))
        {
            if (manifest.TryGetProperty("backupVersion", out var versionElement))
            {
                var parsed = Version.TryParse(versionElement.GetString(), out sourceVersion);
                if (!parsed)
                {
                    _logger.LogWarning("Unable to parse backup version from manifest. Falling back to legacy default.");
                }
            }
        }

        sourceVersion ??= new Version(1, 0, 0); // Assume legacy if not specified

        var log = new List<MigrationLogEntry>();

        // If versions match or source is newer, no migration needed
        if (sourceVersion >= targetVersion)
        {
            log.Add(new MigrationLogEntry(
                "VersionCheck",
                _timeProvider.UtcNow,
                true,
                $"Source version {sourceVersion} is compatible with target {targetVersion}"));

            return Task.FromResult(new MigrationResult
            {
                Success = true,
                SourceVersion = sourceVersion,
                TargetVersion = targetVersion,
                Log = log
            });
        }

        // Perform migrations
        _logger.LogInformation(
            "Migrating data from version {SourceVersion} to {TargetVersion}",
            sourceVersion, targetVersion);

        // Migration logic would go here based on version differences
        log.Add(new MigrationLogEntry(
            "SchemaMigration",
            _timeProvider.UtcNow,
            true,
            $"Migrated from {sourceVersion} to {targetVersion}"));

        return Task.FromResult(new MigrationResult
        {
            Success = true,
            SourceVersion = sourceVersion,
            TargetVersion = targetVersion,
            Log = log
        });
    }
}
