using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Engine responsible for validating parsed import data.
/// </summary>
public interface IValidationEngine
{
    /// <summary>
    /// Validates parsed data for a specific section.
    /// </summary>
    ValidationReport Validate(ParsedData data, string section);

    /// <summary>
    /// Validates a complete backup with all its sections.
    /// </summary>
    ValidationReport ValidateBackup(ParsedData data);
}
