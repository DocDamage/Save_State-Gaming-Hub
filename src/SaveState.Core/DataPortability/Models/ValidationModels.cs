namespace SaveState.Core.DataPortability.Models;

/// <summary>
/// Represents a validation error found during import validation.
/// </summary>
public record ValidationError(
    string Message,
    string? PropertyName = null,
    string? Section = null,
    ValidationResult Severity = ValidationResult.Error);

/// <summary>
/// Represents a validation warning found during import validation.
/// </summary>
public record ValidationWarning(
    string Message,
    string? PropertyName = null,
    string? Section = null);

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationReport
{
    public bool IsValid => !Errors.Any(e => e.Severity == ValidationResult.Error || e.Severity == ValidationResult.Critical);
    public List<ValidationError> Errors { get; init; } = new();
    public List<ValidationWarning> Warnings { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}
