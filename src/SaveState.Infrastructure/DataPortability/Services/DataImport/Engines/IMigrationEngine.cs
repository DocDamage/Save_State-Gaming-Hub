using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Engine responsible for migrating data between different versions.
/// </summary>
public interface IMigrationEngine
{
    /// <summary>
    /// Migrates parsed data to the current version.
    /// </summary>
    Task<MigrationResult> MigrateAsync(ParsedData data, Version? targetVersion = null, CancellationToken ct = default);
}
