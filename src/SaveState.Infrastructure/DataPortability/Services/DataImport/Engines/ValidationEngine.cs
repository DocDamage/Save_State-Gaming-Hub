using SaveState.Core.DataPortability.Models;

namespace SaveState.Infrastructure.DataPortability.Services.DataImport.Engines;

/// <summary>
/// Implementation of validation engine.
/// </summary>
public sealed class ValidationEngine : IValidationEngine
{
    public ValidationReport Validate(ParsedData data, string section)
    {
        var report = new ValidationReport();

        if (!data.IsValid)
        {
            foreach (var error in data.Errors)
            {
                report.Errors.Add(new ValidationError(
                    error.Message,
                    Section: section,
                    Severity: ValidationResult.Error));
            }
            return report;
        }

        // Validate specific sections
        if (!string.IsNullOrEmpty(section) && !data.Sections.ContainsKey(section.ToLowerInvariant()))
        {
            // Section not found - this might be okay for optional sections
            report.Warnings.Add(new ValidationWarning(
                $"Section '{section}' not found in import data",
                Section: section));
        }

        return report;
    }

    public ValidationReport ValidateBackup(ParsedData data)
    {
        var report = new ValidationReport();

        if (!data.IsValid)
        {
            foreach (var error in data.Errors)
            {
                report.Errors.Add(new ValidationError(
                    error.Message,
                    Severity: ValidationResult.Error));
            }
            return report;
        }

        // Validate backup has at least one recognizable section
        var requiredSections = new[] { "game_library", "user_settings", "manifest" };
        var hasAnySection = requiredSections.Any(s => data.Sections.ContainsKey(s));

        if (!hasAnySection)
        {
            report.Warnings.Add(new ValidationWarning(
                "Backup does not contain any recognized sections"));
        }

        // Check for manifest
        if (!data.Sections.ContainsKey("manifest"))
        {
            report.Warnings.Add(new ValidationWarning(
                "Backup manifest not found - version information unavailable"));
        }

        return report;
    }
}
