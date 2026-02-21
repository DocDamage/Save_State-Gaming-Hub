using SaveState.Core.Common.Services;

namespace SaveState.Core.DataPortability.Models;

/// <summary>
/// Represents an import job with its configuration and state.
/// </summary>
public class ImportJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FilePath { get; init; } = string.Empty;
    public ImportFormat Format { get; set; } = ImportFormat.Unknown;
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public ImportOptions Options { get; init; } = new();
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();

    public ImportJob(ITimeProvider? timeProvider = null)
    {
        StartedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Options for configuring import behavior.
/// </summary>
public record ImportOptions(
    bool MergeWithExisting = true,
    bool SkipValidation = false,
    bool SkipMigration = false,
    bool CreateBackupBeforeImport = true,
    bool DryRun = false);

/// <summary>
/// Result of an import operation.
/// </summary>
public record ImportStatistics(
    int ItemsImported,
    int ItemsSkipped,
    int ItemsFailed,
    int ItemsTotal)
{
    public bool HasErrors => ItemsFailed > 0;
    public bool IsComplete => ItemsImported + ItemsSkipped + ItemsFailed >= ItemsTotal;
}
