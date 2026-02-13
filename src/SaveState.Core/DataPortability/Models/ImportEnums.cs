namespace SaveState.Core.DataPortability.Models;

/// <summary>
/// Supported import formats.
/// </summary>
public enum ImportFormat
{
    Unknown,
    Json,
    Xml,
    Csv,
    BackupZip
}

/// <summary>
/// Current status of an import job.
/// </summary>
public enum ImportStatus
{
    Pending,
    DetectingFormat,
    Parsing,
    Validating,
    Migrating,
    Executing,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Result of a validation check.
/// </summary>
public enum ValidationResult
{
    Valid,
    Warning,
    Error,
    Critical
}
