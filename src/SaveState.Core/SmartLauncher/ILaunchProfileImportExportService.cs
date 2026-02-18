// Copyright (c) 2026 SaveStateReborn. All rights reserved.

namespace SaveState.Core.SmartLauncher;

/// <summary>
/// Service for importing and exporting launch profiles.
/// </summary>
public interface ILaunchProfileImportExportService
{
    /// <summary>
    /// Exports a profile to JSON string.
    /// </summary>
    Task<string> ExportProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Exports all profiles to JSON string.
    /// </summary>
    Task<string> ExportAllProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Imports a profile from JSON string.
    /// </summary>
    Task<ImportResult> ImportProfileAsync(string json, bool overwriteExisting = false, CancellationToken ct = default);

    /// <summary>
    /// Imports multiple profiles from JSON string.
    /// </summary>
    Task<BatchImportResult> ImportProfilesAsync(string json, bool overwriteExisting = false, CancellationToken ct = default);

    /// <summary>
    /// Validates a profile JSON without importing.
    /// </summary>
    ValidationResult ValidateProfileJson(string json);
}

/// <summary>
/// Result of a profile import operation.
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public LaunchProfile? ImportedProfile { get; set; }
    public bool WasExisting { get; set; }
}

/// <summary>
/// Result of a batch import operation.
/// </summary>
public class BatchImportResult
{
    public int TotalProfiles { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public int SkippedImports { get; set; }
    public List<ImportError> Errors { get; set; } = new();
}

/// <summary>
/// Import error details.
/// </summary>
public class ImportError
{
    public string ProfileName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Result of JSON validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int ProfileCount { get; set; }
}

/// <summary>
/// DTO for profile export/import.
/// </summary>
public class LaunchProfileDto
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; }
    public List<LaunchProfileExportData> Profiles { get; set; } = new();
}

/// <summary>
/// Export data for a single profile.
/// </summary>
public class LaunchProfileExportData
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "High";
    public bool DisableGameMode { get; set; }
    public bool DisableFullscreenOptimizations { get; set; } = true;
    public bool RunAsAdministrator { get; set; }
    public List<string> ProcessesToSuspend { get; set; } = new();
    public List<string> ServicesToStop { get; set; } = new();
    public string? PowerPlanGuid { get; set; }
    public int? EstimatedPerformanceGain { get; set; }
    public PerformanceSettingsExportData PerformanceSettings { get; set; } = new();
    public DisplaySettingsExportData? DisplaySettings { get; set; }
}

/// <summary>
/// Export data for performance settings.
/// </summary>
public class PerformanceSettingsExportData
{
    public bool EnableMemoryOptimization { get; set; } = true;
    public bool EnableCPUParking { get; set; } = true;
    public bool DisableVisualEffects { get; set; }
    public bool ClearStandbyList { get; set; }
    public int? TargetFPS { get; set; }
    public bool EnableHardwareGPUScheduling { get; set; } = true;
}

/// <summary>
/// Export data for display settings.
/// </summary>
public class DisplaySettingsExportData
{
    public int? ResolutionWidth { get; set; }
    public int? ResolutionHeight { get; set; }
    public int? RefreshRate { get; set; }
    public bool? EnableHDR { get; set; }
    public bool DisableFullscreenOptimizations { get; set; } = true;
    public bool? OverrideDPIScaling { get; set; }
}
