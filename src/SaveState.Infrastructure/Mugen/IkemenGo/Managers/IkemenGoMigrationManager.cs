using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo.Managers;

/// <summary>
/// Manages MUGEN to IKEMEN GO content migration.
/// </summary>
public sealed class IkemenGoMigrationManager
{
    private readonly ILogger<IkemenGoMigrationManager> _logger;
    private readonly ITimeProvider _timeProvider;

    // Regex patterns for detecting MUGEN-specific issues
    private static readonly Regex DeprecatedTriggerRegex = new(
        @"Var\s*\(\s*" + "\"" + @"|SysVar\s*\(\s*" + "\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex OldCommandRegex = new(
        @"command\s*=\s*" + "\"" + @"[^" + "\"" + @"]*[~\^]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="IkemenGoMigrationManager"/> class.
    /// </summary>
    public IkemenGoMigrationManager(
        ILogger<IkemenGoMigrationManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Migrates a MUGEN character to IKEMEN GO format.
    /// </summary>
    public async Task<Result<CharacterMigrationResult>> MigrateCharacterAsync(
        string source,
        string dest,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Migrating character from {Source} to {Dest}", source, dest);

            if (!Directory.Exists(source))
            {
                return Result<CharacterMigrationResult>.Failure("Source character directory not found", ErrorType.NotFound);
            }

            Directory.CreateDirectory(dest);

            var stopwatch = Stopwatch.StartNew();
            var filesMigrated = new List<string>();
            var issues = new List<IkemenGoCompatibilityIssue>();
            var appliedFixes = new List<string>();

            // Get all files in source
            var sourceFiles = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);

            foreach (var file in sourceFiles)
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(source, file);
                var destFile = Path.Combine(dest, relativePath);
                var destDir = Path.GetDirectoryName(destFile)!;

                Directory.CreateDirectory(destDir);

                // Check file type for specific handling
                var extension = Path.GetExtension(file).ToLowerInvariant();
                
                switch (extension)
                {
                    case ".cmd":
                        var cmdResult = await MigrateCmdFileAsync(file, destFile, options, ct);
                        filesMigrated.Add(relativePath);
                        issues.AddRange(cmdResult.Issues);
                        appliedFixes.AddRange(cmdResult.AppliedFixes);
                        break;

                    case ".cns":
                    case ".st":
                        var stateResult = await MigrateStateFileAsync(file, destFile, options, ct);
                        filesMigrated.Add(relativePath);
                        issues.AddRange(stateResult.Issues);
                        appliedFixes.AddRange(stateResult.AppliedFixes);
                        break;

                    case ".def":
                        var defResult = await MigrateDefFileAsync(file, destFile, options, ct);
                        filesMigrated.Add(relativePath);
                        issues.AddRange(defResult.Issues);
                        appliedFixes.AddRange(defResult.AppliedFixes);
                        break;

                    default:
                        // Binary files - just copy
                        File.Copy(file, destFile, true);
                        filesMigrated.Add(relativePath);
                        break;
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Character migration completed: {Files} files, {Fixes} fixes applied in {Ms}ms",
                filesMigrated.Count, appliedFixes.Count, stopwatch.ElapsedMilliseconds);

            var result = new CharacterMigrationResult(
                true,
                source,
                dest,
                filesMigrated,
                issues,
                appliedFixes);

            return Result<CharacterMigrationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate character");
            return Result<CharacterMigrationResult>.Failure($"Migration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Migrates a MUGEN stage to IKEMEN GO format.
    /// </summary>
    public async Task<Result<StageMigrationResult>> MigrateStageAsync(
        string source,
        string dest,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Migrating stage from {Source} to {Dest}", source, dest);

            if (!Directory.Exists(source))
            {
                return Result<StageMigrationResult>.Failure("Source stage directory not found", ErrorType.NotFound);
            }

            Directory.CreateDirectory(dest);

            var filesMigrated = new List<string>();
            var issues = new List<IkemenGoCompatibilityIssue>();

            foreach (var file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(source, file);
                var destFile = Path.Combine(dest, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                var extension = Path.GetExtension(file).ToLowerInvariant();

                if (extension == ".def")
                {
                    var content = await File.ReadAllTextAsync(file, ct);
                    
                    // Check for IKEMEN-specific stage features
                    if (!content.Contains("zoffsetlink", StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new IkemenGoCompatibilityIssue(
                            IkemenGoIssueSeverity.Info,
                            "STAGE_ZOFFSET",
                            "Stage may benefit from zoffsetlink for IKEMEN GO",
                            relativePath,
                            null));
                    }

                    await File.WriteAllTextAsync(destFile, content, ct);
                }
                else
                {
                    File.Copy(file, destFile, true);
                }

                filesMigrated.Add(relativePath);
            }

            var result = new StageMigrationResult(
                true,
                source,
                dest,
                filesMigrated,
                issues);

            return Result<StageMigrationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate stage");
            return Result<StageMigrationResult>.Failure($"Migration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Migrates entire MUGEN roster to IKEMEN GO.
    /// </summary>
    public async Task<Result<BatchMigrationResult>> MigrateFullRosterAsync(
        string source,
        string dest,
        IkemenGoBatchMigrationOptions options,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting batch migration from {Source} to {Dest}", source, dest);

            var charsPath = Path.Combine(source, "chars");
            if (!Directory.Exists(charsPath))
            {
                return Result<BatchMigrationResult>.Failure("MUGEN chars directory not found", ErrorType.NotFound);
            }

            var stopwatch = Stopwatch.StartNew();
            var results = new List<CharacterMigrationResult>();
            var charDirs = Directory.GetDirectories(charsPath);

            int successful = 0, failed = 0, skipped = 0;

            for (int i = 0; i < charDirs.Length; i++)
            {
                ct.ThrowIfCancellationRequested();

                var charDir = charDirs[i];
                var charName = Path.GetFileName(charDir);

                progress?.Report(new MigrationProgress(
                    charName,
                    i + 1,
                    charDirs.Length,
                    "Starting"));

                var outputPath = Path.Combine(dest, "chars", charName);
                
                // Check if already exists
                if (Directory.Exists(outputPath) && !options.CharacterOptions.BackupOriginals)
                {
                    skipped++;
                    progress?.Report(new MigrationProgress(
                        charName,
                        i + 1,
                        charDirs.Length,
                        "Skipped (exists)"));
                    continue;
                }

                var result = await MigrateCharacterAsync(charDir, outputPath, options.CharacterOptions, ct);

                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                    successful++;
                    progress?.Report(new MigrationProgress(
                        charName,
                        i + 1,
                        charDirs.Length,
                        "Completed"));
                }
                else
                {
                    failed++;
                    progress?.Report(new MigrationProgress(
                        charName,
                        i + 1,
                        charDirs.Length,
                        $"Failed: {result.Error}"));
                }
            }

            // Migrate stages if present
            var stagesPath = Path.Combine(source, "stages");
            if (Directory.Exists(stagesPath))
            {
                var destStagesPath = Path.Combine(dest, "stages");
                Directory.CreateDirectory(destStagesPath);

                foreach (var stageDir in Directory.GetDirectories(stagesPath))
                {
                    ct.ThrowIfCancellationRequested();
                    
                    var stageName = Path.GetFileName(stageDir);
                    var destStagePath = Path.Combine(destStagesPath, stageName);
                    await MigrateStageAsync(stageDir, destStagePath, options.StageOptions, ct);
                }
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Batch migration completed: {Total} characters, {Success} successful, {Failed} failed, {Skipped} skipped in {Duration}",
                charDirs.Length, successful, failed, skipped, stopwatch.Elapsed);

            var batchResult = new BatchMigrationResult(
                charDirs.Length,
                successful,
                failed,
                skipped,
                stopwatch.Elapsed,
                results);

            return Result<BatchMigrationResult>.Success(batchResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate full roster");
            return Result<BatchMigrationResult>.Failure($"Batch migration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Converts MUGEN screenpack to IKEMEN GO format.
    /// </summary>
    public async Task<Result<ScreenpackConversionResult>> ConvertScreenpackAsync(
        string source,
        string output,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Converting screenpack from {Source} to {Output}", source, output);

            if (!Directory.Exists(source))
            {
                return Result<ScreenpackConversionResult>.Failure("Source screenpack not found", ErrorType.NotFound);
            }

            Directory.CreateDirectory(output);

            var convertedFiles = new List<string>();
            var manualSteps = new List<string>();

            // Copy all files
            foreach (var file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(source, file);
                var destFile = Path.Combine(output, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                var extension = Path.GetExtension(file).ToLowerInvariant();

                if (extension == ".def" || extension == ".fx" || extension == ".lua")
                {
                    var content = await File.ReadAllTextAsync(file, ct);
                    
                    // Check for IKEMEN-specific system.def features
                    if (relativePath.EndsWith("system.def", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!content.Contains("menu.itemname", StringComparison.OrdinalIgnoreCase))
                        {
                            manualSteps.Add("Review menu.itemname options for IKEMEN GO menu system");
                        }
                        if (!content.Contains("menu.window", StringComparison.OrdinalIgnoreCase))
                        {
                            manualSteps.Add("Consider adding menu.window definitions for better IKEMEN compatibility");
                        }
                    }

                    await File.WriteAllTextAsync(destFile, content, ct);
                }
                else
                {
                    File.Copy(file, destFile, true);
                }

                convertedFiles.Add(relativePath);
            }

            // Add standard manual steps
            manualSteps.Add("Review system.def for IKEMEN-specific features");
            manualSteps.Add("Update font paths if necessary");
            manualSteps.Add("Test all menu options in IKEMEN GO");
            manualSteps.Add("Check motif compatibility with IKEMEN's extended features");

            var result = new ScreenpackConversionResult(
                true,
                source,
                output,
                convertedFiles,
                manualSteps);

            return Result<ScreenpackConversionResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert screenpack");
            return Result<ScreenpackConversionResult>.Failure($"Conversion failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<MigrationFileResult> MigrateCmdFileAsync(
        string source,
        string dest,
        IkemenGoMigrationOptions options,
        CancellationToken ct)
    {
        var issues = new List<IkemenGoCompatibilityIssue>();
        var appliedFixes = new List<string>();

        var content = await File.ReadAllTextAsync(source, ct);
        var originalContent = content;

        // Check for deprecated triggers
        if (DeprecatedTriggerRegex.IsMatch(content))
        {
            issues.Add(new IkemenGoCompatibilityIssue(
                IkemenGoIssueSeverity.Warning,
                "DEPRECATED_TRIGGERS",
                "Character uses deprecated trigger syntax",
                Path.GetFileName(source),
                null));

            if (options.AutoFixIssues && options.ConvertTriggers)
            {
                // Simple conversion of Var("name") to Var(name)
                content = DeprecatedTriggerRegex.Replace(content, match =>
                {
                    var trigger = match.Value.Contains("SysVar", StringComparison.OrdinalIgnoreCase) 
                        ? "SysVar(" 
                        : "Var(";
                    return trigger;
                });
                appliedFixes.Add("Converted deprecated Var/SysVar trigger syntax");
            }
        }

        // Check for old command notation
        if (OldCommandRegex.IsMatch(content))
        {
            issues.Add(new IkemenGoCompatibilityIssue(
                IkemenGoIssueSeverity.Info,
                "OLD_COMMAND_NOTATION",
                "Command uses old ~ or ^ notation",
                Path.GetFileName(source),
                null));
        }

        if (content != originalContent)
        {
            await File.WriteAllTextAsync(dest, content, ct);
        }
        else
        {
            File.Copy(source, dest, true);
        }

        return new MigrationFileResult(issues, appliedFixes);
    }

    private async Task<MigrationFileResult> MigrateStateFileAsync(
        string source,
        string dest,
        IkemenGoMigrationOptions options,
        CancellationToken ct)
    {
        var issues = new List<IkemenGoCompatibilityIssue>();
        var appliedFixes = new List<string>();

        var content = await File.ReadAllTextAsync(source, ct);
        var originalContent = content;

        // Check for IKEMEN-specific state controller features
        if (content.Contains("ScreenBound", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new IkemenGoCompatibilityIssue(
                IkemenGoIssueSeverity.Info,
                "SCREENBOUND_CHECK",
                "Verify ScreenBound behavior in IKEMEN GO",
                Path.GetFileName(source),
                null));
        }

        if (content != originalContent)
        {
            await File.WriteAllTextAsync(dest, content, ct);
        }
        else
        {
            File.Copy(source, dest, true);
        }

        return new MigrationFileResult(issues, appliedFixes);
    }

    private async Task<MigrationFileResult> MigrateDefFileAsync(
        string source,
        string dest,
        IkemenGoMigrationOptions options,
        CancellationToken ct)
    {
        var issues = new List<IkemenGoCompatibilityIssue>();
        var appliedFixes = new List<string>();

        var content = await File.ReadAllTextAsync(source, ct);

        // Check for IKEMEN-specific .def extensions
        if (!content.Contains("ikemen", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new IkemenGoCompatibilityIssue(
                IkemenGoIssueSeverity.Info,
                "NO_IKEMEN_FEATURES",
                "Character .def does not specify IKEMEN-specific features",
                Path.GetFileName(source),
                null));
        }

        await File.WriteAllTextAsync(dest, content, ct);

        return new MigrationFileResult(issues, appliedFixes);
    }

    private record MigrationFileResult(
        List<IkemenGoCompatibilityIssue> Issues,
        List<string> AppliedFixes);
}

/// <summary>
/// Migration progress information.
/// </summary>
public record MigrationProgress(
    string CurrentItem,
    int CurrentIndex,
    int TotalItems,
    string Status);
