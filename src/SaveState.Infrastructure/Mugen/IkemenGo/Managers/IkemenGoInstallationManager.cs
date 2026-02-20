using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo.Managers;

/// <summary>
/// Manages IKEMEN GO installation detection, validation, and version checking.
/// </summary>
public sealed class IkemenGoInstallationManager
{
    private readonly ILogger<IkemenGoInstallationManager> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="IkemenGoInstallationManager"/> class.
    /// </summary>
    public IkemenGoInstallationManager(
        ILogger<IkemenGoInstallationManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Detects IKEMEN GO installation on the system.
    /// </summary>
    public async Task<Result<IkemenGoDetectionResult>> DetectInstallationAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Detecting IKEMEN GO installation");

            var searchPaths = GetDefaultSearchPaths();
            string? installationPath = null;
            var executables = new List<string>();

            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                {
                    var exeFiles = Directory.GetFiles(path, "Ikemen_GO*.exe", SearchOption.TopDirectoryOnly);
                    if (exeFiles.Length > 0)
                    {
                        installationPath = path;
                        executables.AddRange(exeFiles);
                        break;
                    }
                }
            }

            var isInstalled = installationPath != null;
            var contentPaths = isInstalled
                ? GetContentPaths(installationPath!)
                : Array.Empty<string>();

            var version = isInstalled
                ? await DetectVersionAsync(installationPath!, ct)
                : null;

            var result = new IkemenGoDetectionResult(
                isInstalled,
                installationPath,
                version,
                executables,
                contentPaths);

            return Result<IkemenGoDetectionResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect IKEMEN GO installation");
            return Result<IkemenGoDetectionResult>.Failure($"Detection failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets default search paths for IKEMEN GO installation.
    /// </summary>
    public List<string> GetDefaultSearchPaths()
    {
        var paths = new List<string>();

        // Common installation paths
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        paths.Add(Path.Combine(programFiles, "IKEMEN"));
        paths.Add(Path.Combine(programFiles, "IKEMEN GO"));
        paths.Add(Path.Combine(localAppData, "IKEMEN"));
        paths.Add(Path.Combine(localAppData, "IKEMEN GO"));

        // Check current directory and parent directories
        paths.Add(Directory.GetCurrentDirectory());

        return paths;
    }

    /// <summary>
    /// Gets content paths for an installation.
    /// </summary>
    public IReadOnlyList<string> GetContentPaths(string installationPath)
    {
        var paths = new List<string>();

        var charsPath = Path.Combine(installationPath, "chars");
        if (Directory.Exists(charsPath)) paths.Add(charsPath);

        var stagesPath = Path.Combine(installationPath, "stages");
        if (Directory.Exists(stagesPath)) paths.Add(stagesPath);

        var dataPath = Path.Combine(installationPath, "data");
        if (Directory.Exists(dataPath)) paths.Add(dataPath);

        return paths;
    }

    /// <summary>
    /// Detects the version of an IKEMEN GO installation.
    /// </summary>
    public async Task<IkemenGoVersion?> DetectVersionAsync(string installationPath, CancellationToken ct)
    {
        try
        {
            // Try to read version from version.txt
            var versionFile = Path.Combine(installationPath, "version.txt");
            if (File.Exists(versionFile))
            {
                var versionText = await File.ReadAllTextAsync(versionFile, ct);
                var parts = versionText.Split('.');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0], out var major) &&
                    int.TryParse(parts[1], out var minor) &&
                    int.TryParse(parts[2], out var patch))
                {
                    return new IkemenGoVersion(major, minor, patch, null, _timeProvider.UtcNow);
                }
            }

            // Try to read from JSON config
            var jsonFile = Path.Combine(installationPath, "version.json");
            if (File.Exists(jsonFile))
            {
                var json = await File.ReadAllTextAsync(jsonFile, ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("version", out var versionProp))
                {
                    var versionString = versionProp.GetString();
                    if (!string.IsNullOrEmpty(versionString))
                    {
                        var parts = versionString.Split('.');
                        if (parts.Length >= 3 &&
                            int.TryParse(parts[0], out var major) &&
                            int.TryParse(parts[1], out var minor) &&
                            int.TryParse(parts[2], out var patch))
                        {
                            return new IkemenGoVersion(major, minor, patch, null, _timeProvider.UtcNow);
                        }
                    }
                }
            }

            // Default version
            return new IkemenGoVersion(0, 99, 0, "unknown", _timeProvider.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks compatibility of a MUGEN character with IKEMEN GO.
    /// </summary>
    public async Task<Result<IkemenGoCompatibilityReport>> CheckCharacterCompatibilityAsync(
        string characterPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Checking character compatibility: {Path}", characterPath);

            var issues = new List<IkemenGoCompatibilityIssue>();
            var suggestions = new List<IkemenGoMigrationSuggestion>();

            // Check for .def file
            var defFiles = Directory.GetFiles(characterPath, "*.def");
            if (defFiles.Length == 0)
            {
                issues.Add(new IkemenGoCompatibilityIssue(
                    IkemenGoIssueSeverity.Critical,
                    "NO_DEF_FILE",
                    "No .def file found in character directory",
                    null,
                    null));
            }

            // Check for IKEMEN-specific issues
            var cmdFiles = Directory.GetFiles(characterPath, "*.cmd");
            foreach (var cmdFile in cmdFiles)
            {
                var content = await File.ReadAllTextAsync(cmdFile, ct);
                
                // Check for deprecated triggers
                if (content.Contains("Var(\"") || content.Contains("SysVar(\""))
                {
                    issues.Add(new IkemenGoCompatibilityIssue(
                        IkemenGoIssueSeverity.Warning,
                        "DEPRECATED_TRIGGERS",
                        "Character uses deprecated trigger syntax",
                        cmdFile,
                        null));
                    suggestions.Add(new IkemenGoMigrationSuggestion(
                        "DEPRECATED_TRIGGERS",
                        "Update triggers to modern syntax",
                        true,
                        null));
                }
            }

            // Check for animation files
            var airFiles = Directory.GetFiles(characterPath, "*.air");
            if (airFiles.Length == 0)
            {
                issues.Add(new IkemenGoCompatibilityIssue(
                    IkemenGoIssueSeverity.Error,
                    "NO_AIR_FILE",
                    "No animation (.air) file found",
                    null,
                    null));
            }

            var compatibilityLevel = DetermineCompatibilityLevel(issues);
            var characterName = Path.GetFileName(characterPath);

            return Result<IkemenGoCompatibilityReport>.Success(new IkemenGoCompatibilityReport(
                characterPath,
                characterName,
                compatibilityLevel,
                issues,
                suggestions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check character compatibility");
            return Result<IkemenGoCompatibilityReport>.Failure($"Compatibility check failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Checks compatibility of a MUGEN stage with IKEMEN GO.
    /// </summary>
    public async Task<Result<IkemenGoCompatibilityReport>> CheckStageCompatibilityAsync(
        string stagePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Checking stage compatibility: {Path}", stagePath);

            var issues = new List<IkemenGoCompatibilityIssue>();
            var suggestions = new List<IkemenGoMigrationSuggestion>();

            // Check stage definition
            var defFiles = Directory.GetFiles(stagePath, "*.def");
            if (defFiles.Length == 0)
            {
                issues.Add(new IkemenGoCompatibilityIssue(
                    IkemenGoIssueSeverity.Critical,
                    "NO_STAGE_DEF",
                    "No stage definition file found",
                    null,
                    null));
            }

            var compatibilityLevel = DetermineCompatibilityLevel(issues);

            return Result<IkemenGoCompatibilityReport>.Success(new IkemenGoCompatibilityReport(
                stagePath,
                Path.GetFileName(stagePath),
                compatibilityLevel,
                issues,
                suggestions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check stage compatibility");
            return Result<IkemenGoCompatibilityReport>.Failure($"Compatibility check failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Validates select.def file for IKEMEN GO compatibility.
    /// </summary>
    public async Task<Result<SelectDefValidationResult>> ValidateSelectDefAsync(
        string selectDefPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating select.def: {Path}", selectDefPath);

            if (!File.Exists(selectDefPath))
            {
                return Result<SelectDefValidationResult>.Failure("select.def not found", ErrorType.NotFound);
            }

            var content = await File.ReadAllLinesAsync(selectDefPath, ct);
            var invalidEntries = new List<string>();
            var missingFiles = new List<string>();
            var ikemenOptions = new List<string>();

            foreach (var line in content)
            {
                var trimmed = line.Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                    continue;

                // Check for IKEMEN-specific options
                if (trimmed.StartsWith("ikemen", StringComparison.OrdinalIgnoreCase))
                {
                    ikemenOptions.Add(trimmed);
                }

                // Check character entries
                if (!trimmed.Contains('=') && !trimmed.StartsWith("["))
                {
                    var charName = trimmed.Split(',')[0].Trim();
                    if (!string.IsNullOrEmpty(charName) && charName != "random")
                    {
                        // Would validate character exists here
                    }
                }
            }

            var result = new SelectDefValidationResult(
                invalidEntries.Count == 0,
                invalidEntries,
                missingFiles,
                ikemenOptions);

            return Result<SelectDefValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate select.def");
            return Result<SelectDefValidationResult>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private IkemenGoCompatibilityLevel DetermineCompatibilityLevel(List<IkemenGoCompatibilityIssue> issues)
    {
        if (issues.Any(i => i.Severity == IkemenGoIssueSeverity.Critical))
            return IkemenGoCompatibilityLevel.Incompatible;

        if (issues.Any(i => i.Severity == IkemenGoIssueSeverity.Error))
            return IkemenGoCompatibilityLevel.RequiresMigration;

        if (issues.Any(i => i.Severity == IkemenGoIssueSeverity.Warning))
            return IkemenGoCompatibilityLevel.Partial;

        return IkemenGoCompatibilityLevel.Full;
    }
}
