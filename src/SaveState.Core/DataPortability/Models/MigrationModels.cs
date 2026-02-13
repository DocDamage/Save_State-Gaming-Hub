namespace SaveState.Core.DataPortability.Models;

/// <summary>
/// Represents a migration step in the data migration process.
/// </summary>
public class MigrationStep
{
    public int Order { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Func<Dictionary<string, object>, Dictionary<string, object>> Transform { get; init; } = d => d;
    public Version? TargetVersion { get; init; }
}

/// <summary>
/// Log entry for a migration operation.
/// </summary>
public record MigrationLogEntry(
    string StepName,
    DateTime ExecutedAt,
    bool Success,
    string? ErrorMessage = null);

/// <summary>
/// Result of a migration operation.
/// </summary>
public class MigrationResult
{
    public bool Success { get; init; }
    public Version? SourceVersion { get; init; }
    public Version? TargetVersion { get; init; }
    public List<MigrationLogEntry> Log { get; init; } = new();
    public Dictionary<string, object>? MigratedData { get; init; }
}
