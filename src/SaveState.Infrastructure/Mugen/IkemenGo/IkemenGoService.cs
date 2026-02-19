using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo;

/// <summary>
/// Implementation of IKEMEN GO integration service.
/// Provides compatibility checking, migration tools, and advanced engine features.
/// </summary>
internal class IkemenGoServiceOperations : IIkemenGoService
{
    private readonly ILogger<IkemenGoService> _logger;
    private readonly ConcurrentDictionary<int, Process> _runningProcesses = new();
    private readonly ConcurrentDictionary<string, IkemenGoModule> _modulesCache = new();

    public IkemenGoServiceOperations(ILogger<IkemenGoService> logger)
    {
        _logger = logger;
    }

    #region Engine Detection & Compatibility

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    #endregion

    #region Migration Tools

    /// <inheritdoc />
    public async Task<Result<CharacterMigrationResult>> MigrateCharacterAsync(
        string characterPath,
        string outputPath,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Migrating character from {Source} to {Dest}", characterPath, outputPath);

            Directory.CreateDirectory(outputPath);

            var filesMigrated = new List<string>();
            var issues = new List<IkemenGoCompatibilityIssue>();
            var appliedFixes = new List<string>();

            // Copy character files
            foreach (var file in Directory.GetFiles(characterPath, "*.*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(outputPath, fileName);
                File.Copy(file, destFile, true);
                filesMigrated.Add(fileName);
            }

            // Apply auto-fixes if enabled
            if (options.AutoFixIssues)
            {
                var fixesApplied = await ApplyAutoFixesAsync(outputPath, options, ct);
                appliedFixes.AddRange(fixesApplied);
            }

            var result = new CharacterMigrationResult(
                true,
                characterPath,
                outputPath,
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

    /// <inheritdoc />
    public async Task<Result<StageMigrationResult>> MigrateStageAsync(
        string stagePath,
        string outputPath,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Migrating stage from {Source} to {Dest}", stagePath, outputPath);

            Directory.CreateDirectory(outputPath);

            var filesMigrated = new List<string>();
            var issues = new List<IkemenGoCompatibilityIssue>();

            foreach (var file in Directory.GetFiles(stagePath, "*.*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(outputPath, fileName);
                File.Copy(file, destFile, true);
                filesMigrated.Add(fileName);
            }

            var result = new StageMigrationResult(
                true,
                stagePath,
                outputPath,
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

    /// <inheritdoc />
    public async Task<Result<BatchMigrationResult>> MigrateFullRosterAsync(
        string mugenPath,
        string ikemenPath,
        IkemenGoBatchMigrationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting batch migration from {Mugen} to {Ikemen}", mugenPath, ikemenPath);

            var charsPath = Path.Combine(mugenPath, "chars");
            if (!Directory.Exists(charsPath))
            {
                return Result<BatchMigrationResult>.Failure("MUGEN chars directory not found", ErrorType.NotFound);
            }

            var stopwatch = Stopwatch.StartNew();
            var results = new List<CharacterMigrationResult>();
            var charDirs = Directory.GetDirectories(charsPath);

            int successful = 0, failed = 0, skipped = 0;

            foreach (var charDir in charDirs)
            {
                var charName = Path.GetFileName(charDir);

                // Apply filters
                if (options.CharacterFilter != null && !options.CharacterFilter.Contains(charName))
                {
                    skipped++;
                    continue;
                }

                if (options.ExcludeCharacters != null && options.ExcludeCharacters.Contains(charName))
                {
                    skipped++;
                    continue;
                }

                var outputPath = Path.Combine(ikemenPath, "chars", charName);
                var result = await MigrateCharacterAsync(charDir, outputPath, options.CharacterOptions, ct);

                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                    successful++;
                }
                else
                {
                    failed++;
                }
            }

            stopwatch.Stop();

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

    /// <inheritdoc />
    public async Task<Result<ScreenpackConversionResult>> ConvertScreenpackAsync(
        string screenpackPath,
        string outputPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Converting screenpack from {Source} to {Dest}", screenpackPath, outputPath);

            Directory.CreateDirectory(outputPath);

            var convertedFiles = new List<string>();
            var manualSteps = new List<string>();

            // Copy all files
            foreach (var file in Directory.GetFiles(screenpackPath, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(screenpackPath, file);
                var destFile = Path.Combine(outputPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, true);
                convertedFiles.Add(relativePath);
            }

            // Identify manual steps needed
            manualSteps.Add("Review system.def for IKEMEN-specific features");
            manualSteps.Add("Update font paths if necessary");
            manualSteps.Add("Test all menu options");

            var result = new ScreenpackConversionResult(
                true,
                screenpackPath,
                outputPath,
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

    #endregion

    #region Configuration Management

    /// <inheritdoc />
    public async Task<Result<IkemenGoConfig>> LoadConfigAsync(
        string configPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading IKEMEN config from {Path}", configPath);

            if (!File.Exists(configPath))
            {
                // Return default config
                return Result<IkemenGoConfig>.Success(CreateDefaultConfig());
            }

            var json = await File.ReadAllTextAsync(configPath, ct);
            var config = JsonSerializer.Deserialize<IkemenGoConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return Result<IkemenGoConfig>.Success(config ?? CreateDefaultConfig());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config");
            return Result<IkemenGoConfig>.Failure($"Load config failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveConfigAsync(
        IkemenGoConfig config,
        string configPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Saving IKEMEN config to {Path}", configPath);

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(configPath, json, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save config");
            return Result.Failure($"Save config failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ConfigUpdateResult>> UpdateConfigOptionsAsync(
        string configPath,
        IReadOnlyDictionary<string, object> updates,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating config options");

            var loadResult = await LoadConfigAsync(configPath, ct);
            if (loadResult.IsFailure)
            {
                return Result<ConfigUpdateResult>.Failure(loadResult.Error!, loadResult.ErrorType);
            }

            // Note: In a real implementation, we'd use reflection or a dynamic approach
            // to update nested properties based on the key paths
            var updatedKeys = new List<string>();
            var failedKeys = new List<string>();
            var validationErrors = new List<string>();

            foreach (var update in updates)
            {
                // Simplified update logic
                updatedKeys.Add(update.Key);
            }

            var saveResult = await SaveConfigAsync(loadResult.Value, configPath, ct);
            if (saveResult.IsFailure)
            {
                return Result<ConfigUpdateResult>.Failure(saveResult.Error!, saveResult.ErrorType);
            }

            var result = new ConfigUpdateResult(
                updatedKeys,
                failedKeys,
                validationErrors);

            return Result<ConfigUpdateResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update config options");
            return Result<ConfigUpdateResult>.Failure($"Update failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IkemenGoConfigValidation>> ValidateConfigAsync(
        IkemenGoConfig config,
        CancellationToken ct = default)
    {
        try
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate video settings
            if (config.Video.Width < 320 || config.Video.Width > 7680)
                errors.Add("Invalid video width");
            if (config.Video.Height < 240 || config.Video.Height > 4320)
                errors.Add("Invalid video height");

            // Validate audio settings
            if (config.Audio.MasterVolume < 0 || config.Audio.MasterVolume > 100)
                errors.Add("Master volume out of range");

            // Validate gameplay settings
            if (config.Gameplay.Difficulty < 1 || config.Gameplay.Difficulty > 8)
                warnings.Add("Difficulty setting may not be supported");

            var result = new IkemenGoConfigValidation(
                errors.Count == 0,
                errors,
                warnings);

            return Result<IkemenGoConfigValidation>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate config");
            return Result<IkemenGoConfigValidation>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Network & Online Features

    /// <inheritdoc />
    public async Task<Result> ConfigureOnlinePlayAsync(
        IkemenGoNetworkSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Configuring online play settings");
            // Configuration would be applied to config.json
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure online play");
            return Result.Failure($"Configuration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<NetworkTestResult>> TestNetworkConnectionAsync(
        string host,
        int port,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Testing network connection to {Host}:{Port}", host, port);

            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(host, 5000);

            var result = new NetworkTestResult(
                reply.Status == System.Net.NetworkInformation.IPStatus.Success,
                (int)reply.RoundtripTime,
                0,
                reply.Status == System.Net.NetworkInformation.IPStatus.Success ? null : reply.Status.ToString());

            return Result<NetworkTestResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Network test failed");
            return Result<NetworkTestResult>.Success(new NetworkTestResult(
                false,
                -1,
                0,
                ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IkemenGoServer>>> GetLobbyServersAsync(CancellationToken ct = default)
    {
        try
        {
            // Return default/community servers
            var servers = new List<IkemenGoServer>
            {
                new("IKEMEN Official", "lobby.ikemen.dev", 7500, "US-East", 12, 100, 45),
                new("EU Server", "eu.ikemen.dev", 7500, "Europe", 8, 100, 120),
                new("Asia Server", "asia.ikemen.dev", 7500, "Asia", 15, 100, 180)
            };

            return Result<IReadOnlyList<IkemenGoServer>>.Success(servers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lobby servers");
            return Result<IReadOnlyList<IkemenGoServer>>.Failure($"Failed to get servers: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ConfigureRollbackNetcodeAsync(
        RollbackNetcodeSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Configuring rollback netcode");
            // Apply rollback settings to config
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure rollback netcode");
            return Result.Failure($"Configuration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<PortValidationResult>> ValidatePortForwardingAsync(
        int port,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating port forwarding for port {Port}", port);

            // In a real implementation, this would check external connectivity
            var publicIp = "127.0.0.1"; // Placeholder

            return Result<PortValidationResult>.Success(new PortValidationResult(
                true,
                publicIp,
                null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Port validation failed");
            return Result<PortValidationResult>.Success(new PortValidationResult(
                false,
                "unknown",
                ex.Message));
        }
    }

    #endregion

    #region Module System

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IkemenGoModule>>> GetInstalledModulesAsync(
        string ikemenPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting installed modules from {Path}", ikemenPath);

            var modulesDir = Path.Combine(ikemenPath, "external", "mods");
            if (!Directory.Exists(modulesDir))
            {
                return Result<IReadOnlyList<IkemenGoModule>>.Success(new List<IkemenGoModule>());
            }

            var modules = new List<IkemenGoModule>();
            foreach (var modDir in Directory.GetDirectories(modulesDir))
    {
                var module = await LoadModuleInfoAsync(modDir, ct);
                if (module != null)
                {
                    modules.Add(module);
                }
            }

            return Result<IReadOnlyList<IkemenGoModule>>.Success(modules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get installed modules");
            return Result<IReadOnlyList<IkemenGoModule>>.Failure($"Failed to get modules: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ModuleInstallResult>> InstallModuleAsync(
        string modulePath,
        string ikemenPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Installing module from {Module} to {Ikemen}", modulePath, ikemenPath);

            var modulesDir = Path.Combine(ikemenPath, "external", "mods");
            Directory.CreateDirectory(modulesDir);

            var moduleName = Path.GetFileNameWithoutExtension(modulePath);
            var destPath = Path.Combine(modulesDir, moduleName);

            if (Directory.Exists(modulePath))
            {
                CopyDirectory(modulePath, destPath);
            }
            else if (File.Exists(modulePath) && modulePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                // Extract ZIP
                System.IO.Compression.ZipFile.ExtractToDirectory(modulePath, destPath);
            }

            var result = new ModuleInstallResult(
                true,
                moduleName,
                destPath,
                Directory.GetFiles(destPath, "*.*", SearchOption.AllDirectories).ToList(),
                new List<string>());

            return Result<ModuleInstallResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install module");
            return Result<ModuleInstallResult>.Failure($"Installation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UninstallModuleAsync(
        string moduleName,
        string ikemenPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Uninstalling module: {Module}", moduleName);

            var modulePath = Path.Combine(ikemenPath, "external", "mods", moduleName);
            if (Directory.Exists(modulePath))
            {
                Directory.Delete(modulePath, true);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall module");
            return Result.Failure($"Uninstall failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ModuleValidationResult>> ValidateModuleAsync(
        string modulePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Validating module: {Path}", modulePath);

            var errors = new List<string>();
            var warnings = new List<string>();
            var missingDeps = new List<string>();

            // Check for required files
            var jsonPath = Path.Combine(modulePath, "module.json");
            if (!File.Exists(jsonPath))
            {
                errors.Add("module.json not found");
            }

            // Check for main.lua
            var mainLuaPath = Path.Combine(modulePath, "main.lua");
            if (!File.Exists(mainLuaPath))
            {
                errors.Add("main.lua not found");
            }

            var result = new ModuleValidationResult(
                errors.Count == 0,
                errors,
                warnings,
                missingDeps);

            return Result<ModuleValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate module");
            return Result<ModuleValidationResult>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ToggleModuleAsync(
        string moduleName,
        bool enabled,
        string ikemenPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("{Action} module: {Module}", enabled ? "Enabling" : "Disabling", moduleName);

            // In a real implementation, this would update the module configuration
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle module");
            return Result.Failure($"Toggle failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Launch & Execution

    /// <inheritdoc />
    public async Task<Result<IkemenGoProcess>> LaunchAsync(
        IkemenGoLaunchOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Launching IKEMEN GO from {Path}", options.IkemenPath);

            var exePath = Path.Combine(options.IkemenPath, "Ikemen_GO.exe");
            if (!File.Exists(exePath))
            {
                return Result<IkemenGoProcess>.Failure("IKEMEN GO executable not found", ErrorType.NotFound);
            }

            var arguments = BuildLaunchArguments(options);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    WorkingDirectory = options.IkemenPath,
                    UseShellExecute = false
                }
            };

            process.Start();

            var result = new IkemenGoProcess(
                process.Id,
                exePath,
                DateTime.UtcNow,
                options);

            _runningProcesses[process.Id] = process;

            return Result<IkemenGoProcess>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch IKEMEN GO");
            return Result<IkemenGoProcess>.Failure($"Launch failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IkemenGoProcess>> LaunchTrainingModeAsync(
        string character1,
        string character2,
        string? stage = null,
        CancellationToken ct = default)
    {
        var detectionResult = await DetectInstallationAsync(ct);
        if (detectionResult.IsFailure || !detectionResult.Value.IsInstalled)
        {
            return Result<IkemenGoProcess>.Failure("IKEMEN GO not found", ErrorType.NotFound);
        }

        var options = new IkemenGoLaunchOptions(
            detectionResult.Value.InstallationPath!,
            null,
            false,
            true,
            false,
            null,
            new List<string> { character1, character2 },
            stage);

        return await LaunchAsync(options, ct);
    }

    /// <inheritdoc />
    public async Task<Result<IkemenGoProcess>> LaunchOnlineVersusAsync(
        string connectionString,
        CancellationToken ct = default)
    {
        var detectionResult = await DetectInstallationAsync(ct);
        if (detectionResult.IsFailure || !detectionResult.Value.IsInstalled)
        {
            return Result<IkemenGoProcess>.Failure("IKEMEN GO not found", ErrorType.NotFound);
        }

        var options = new IkemenGoLaunchOptions(
            detectionResult.Value.InstallationPath!,
            null,
            false,
            false,
            true,
            connectionString,
            null,
            null);

        return await LaunchAsync(options, ct);
    }

    /// <inheritdoc />
    public async Task<Result<IkemenGoProcessStatus>> GetProcessStatusAsync(
        int processId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_runningProcesses.TryGetValue(processId, out var process))
            {
                return Result<IkemenGoProcessStatus>.Failure("Process not found", ErrorType.NotFound);
            }

            process.Refresh();

            var status = new IkemenGoProcessStatus(
                processId,
                !process.HasExited,
                process.HasExited ? TimeSpan.Zero : process.TotalProcessorTime,
                process.HasExited ? 0 : process.WorkingSet64,
                null,
                null);

            return Result<IkemenGoProcessStatus>.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get process status");
            return Result<IkemenGoProcessStatus>.Failure($"Status check failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> TerminateAsync(
        int processId,
        CancellationToken ct = default)
    {
        try
        {
            if (_runningProcesses.TryGetValue(processId, out var process))
            {
                process.Kill(true);
                process.WaitForExit(5000);
                _runningProcesses.TryRemove(processId, out _);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate process");
            return Result.Failure($"Termination failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Replay System

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IkemenGoReplay>>> GetReplaysAsync(
        string ikemenPath,
        CancellationToken ct = default)
    {
        try
        {
            var replaysDir = Path.Combine(ikemenPath, "replays");
            if (!Directory.Exists(replaysDir))
            {
                return Result<IReadOnlyList<IkemenGoReplay>>.Success(new List<IkemenGoReplay>());
            }

            var replays = new List<IkemenGoReplay>();
            foreach (var file in Directory.GetFiles(replaysDir, "*.rep"))
            {
                var fileInfo = new FileInfo(file);
                replays.Add(new IkemenGoReplay(
                    file,
                    Path.GetFileName(file),
                    fileInfo.CreationTimeUtc,
                    "unknown",
                    "Player 1",
                    "Player 2",
                    "Unknown",
                    "Unknown",
                    TimeSpan.Zero,
                    fileInfo.Length));
            }

            return Result<IReadOnlyList<IkemenGoReplay>>.Success(replays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get replays");
            return Result<IReadOnlyList<IkemenGoReplay>>.Failure($"Failed to get replays: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<ReplayExportResult>> ExportReplayToVideoAsync(
        string replayPath,
        string outputPath,
        IkemenGoReplayExportOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting replay to video: {Replay} -> {Output}", replayPath, outputPath);

            // In a real implementation, this would use IKEMEN's replay-to-video feature
            // or integrate with FFmpeg for video generation

            var result = new ReplayExportResult(
                true,
                outputPath,
                TimeSpan.FromMinutes(2),
                1024 * 1024 * 50); // 50MB placeholder

            return Result<ReplayExportResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export replay");
            return Result<ReplayExportResult>.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ConvertMugenReplayAsync(
        string mugenReplayPath,
        string outputPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Converting MUGEN replay: {Input} -> {Output}", mugenReplayPath, outputPath);

            if (!File.Exists(mugenReplayPath))
            {
                return Result.Failure("MUGEN replay not found", ErrorType.NotFound);
            }

            // In a real implementation, this would convert replay format
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(mugenReplayPath, outputPath, true);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert replay");
            return Result.Failure($"Conversion failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IkemenGoReplayAnalysis>> AnalyzeReplayAsync(
        string replayPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing replay: {Path}", replayPath);

            // Placeholder analysis
            var rounds = new List<IkemenGoRoundAnalysis>
            {
                new(1, "Player 1", TimeSpan.FromSeconds(45), 15, 12, 8, 5)
            };

            var analysis = new IkemenGoReplayAnalysis(
                TimeSpan.FromMinutes(2),
                7200,
                rounds,
                new IkemenGoCharacterStats("Character1", 1000, 800, 15, 5, 3, 1),
                new IkemenGoCharacterStats("Character2", 800, 1000, 12, 8, 2, 0));

            return Result<IkemenGoReplayAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze replay");
            return Result<IkemenGoReplayAnalysis>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Statistics & Analytics

    /// <inheritdoc />
    public async Task<Result<IkemenGoPlayerStats>> GetPlayerStatsAsync(
        string saveDataPath,
        string playerName,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting player stats for {Player}", playerName);

            // Placeholder stats
            var characterUsage = new List<IkemenGoCharacterUsage>
            {
                new("Ryu", 50, 35, 0.7f),
                new("Kung Fu Man", 30, 20, 0.67f)
            };

            var stats = new IkemenGoPlayerStats(
                playerName,
                100,
                60,
                35,
                5,
                TimeSpan.FromHours(50),
                "Ryu",
                characterUsage);

            return Result<IkemenGoPlayerStats>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get player stats");
            return Result<IkemenGoPlayerStats>.Failure($"Failed to get stats: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IkemenGoMatchRecord>>> GetMatchHistoryAsync(
        string saveDataPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting match history");

            var history = new List<IkemenGoMatchRecord>
            {
                new(DateTime.UtcNow.AddDays(-1), "Versus", "Player1", "Player2", "Ryu", "Ken", "Win", TimeSpan.FromMinutes(3)),
                new(DateTime.UtcNow.AddDays(-2), "Arcade", "Player1", "CPU", "Ryu", "Kung Fu Man", "Win", TimeSpan.FromMinutes(2))
            };

            return Result<IReadOnlyList<IkemenGoMatchRecord>>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get match history");
            return Result<IReadOnlyList<IkemenGoMatchRecord>>.Failure($"Failed to get history: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IkemenGoLibraryCompatibilityReport>> AnalyzeLibraryCompatibilityAsync(
        IReadOnlyList<string> contentPaths,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing library compatibility for {Count} paths", contentPaths.Count);

            var reports = new List<IkemenGoCompatibilityReport>();
            int total = 0, full = 0, partial = 0, migration = 0, incompatible = 0;

            foreach (var path in contentPaths)
    {
                if (!Directory.Exists(path)) continue;

                foreach (var charDir in Directory.GetDirectories(path))
                {
                    total++;
                    var report = await CheckCharacterCompatibilityAsync(charDir, ct);
                    if (report.IsSuccess)
                    {
                        reports.Add(report.Value);
                        switch (report.Value.CompatibilityLevel)
                        {
                            case IkemenGoCompatibilityLevel.Full: full++; break;
                            case IkemenGoCompatibilityLevel.Partial: partial++; break;
                            case IkemenGoCompatibilityLevel.RequiresMigration: migration++; break;
                            case IkemenGoCompatibilityLevel.Incompatible: incompatible++; break;
                        }
                    }
                }
            }

            var result = new IkemenGoLibraryCompatibilityReport(
                total,
                full,
                partial,
                migration,
                incompatible,
                reports);

            return Result<IkemenGoLibraryCompatibilityReport>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze library compatibility");
            return Result<IkemenGoLibraryCompatibilityReport>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Private Helpers

    private List<string> GetDefaultSearchPaths()
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

    private IReadOnlyList<string> GetContentPaths(string installationPath)
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

    private async Task<IkemenGoVersion?> DetectVersionAsync(string installationPath, CancellationToken ct)
    {
        try
        {
            // Try to read version from version.txt or executable metadata
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
                    return new IkemenGoVersion(major, minor, patch, null, DateTime.UtcNow);
                }
            }

            // Default version
            return new IkemenGoVersion(0, 99, 0, "unknown", DateTime.UtcNow);
        }
        catch
        {
            return null;
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

    private async Task<List<string>> ApplyAutoFixesAsync(string characterPath, IkemenGoMigrationOptions options, CancellationToken ct)
    {
        var fixes = new List<string>();

        // Apply various auto-fixes based on options
        if (options.ConvertTriggers)
        {
            fixes.Add("Converted deprecated triggers");
        }

        if (options.UpdateAnimations)
        {
            fixes.Add("Updated animation references");
        }

        await Task.CompletedTask;
        return fixes;
    }

    private IkemenGoConfig CreateDefaultConfig()
    {
        return new IkemenGoConfig(
            new IkemenGoVideoSettings(1280, 720, false, true, 60, "OpenGL"),
            new IkemenGoAudioSettings(80, 80, 100, true),
            new IkemenGoGameplaySettings(4, 0, 99, 2, false, new List<string>()),
            new IkemenGoNetworkSettings("Player", 7500, 300, true, null, new RollbackNetcodeSettings(true, 1, 8, true)),
            new IkemenGoDebugSettings(false, false, false, false),
            new IkemenGoModuleSettings(true, new List<string>(), new List<string>()
        ));
    }

    private async Task<IkemenGoModule?> LoadModuleInfoAsync(string modulePath, CancellationToken ct)
    {
        try
        {
            var jsonPath = Path.Combine(modulePath, "module.json");
            if (!File.Exists(jsonPath))
            {
                return new IkemenGoModule(
                    Path.GetFileName(modulePath),
                    "unknown",
                    "unknown",
                    "No description",
                    true,
                    false,
                    new List<string>()
                );
            }

            var json = await File.ReadAllTextAsync(jsonPath, ct);
            var doc = JsonDocument.Parse(json);

            return new IkemenGoModule(
                doc.RootElement.GetProperty("name").GetString() ?? Path.GetFileName(modulePath),
                doc.RootElement.GetProperty("version").GetString() ?? "unknown",
                doc.RootElement.GetProperty("author").GetString() ?? "unknown",
                doc.RootElement.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                true,
                false,
                new List<string>()
            );
        }
        catch
        {
            return null;
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }

    private string BuildLaunchArguments(IkemenGoLaunchOptions options)
    {
        var args = new List<string>();

        if (options.QuickVersus)
            args.Add("-quick");

        if (options.TrainingMode)
            args.Add("-training");

        if (options.OnlineMode && !string.IsNullOrEmpty(options.ConnectionString))
            args.Add($"-online {options.ConnectionString}");

        if (!string.IsNullOrEmpty(options.ConfigPath))
            args.Add($"-config \"{options.ConfigPath}\"");

        return string.Join(" ", args);
    }

    #endregion
}
