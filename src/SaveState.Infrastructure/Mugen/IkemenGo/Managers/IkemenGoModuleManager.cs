using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo.Managers;

/// <summary>
/// Manages IKEMEN GO Lua module lifecycle.
/// </summary>
public sealed class IkemenGoModuleManager
{
    private readonly ILogger<IkemenGoModuleManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, IkemenGoModule> _modulesCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IkemenGoModuleManager"/> class.
    /// </summary>
    public IkemenGoModuleManager(
        ILogger<IkemenGoModuleManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets installed Lua modules.
    /// </summary>
    public async Task<Result<IReadOnlyList<IkemenGoModule>>> GetInstalledModulesAsync(
        string modulesPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting installed modules from {Path}", modulesPath);

            if (!Directory.Exists(modulesPath))
            {
                return Result<IReadOnlyList<IkemenGoModule>>.Success(new List<IkemenGoModule>());
            }

            var modules = new List<IkemenGoModule>();
            foreach (var modDir in Directory.GetDirectories(modulesPath))
            {
                ct.ThrowIfCancellationRequested();

                var module = await LoadModuleInfoAsync(modDir, ct);
                if (module != null)
                {
                    modules.Add(module);
                    _modulesCache[module.Name] = module;
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

    /// <summary>
    /// Installs a Lua module.
    /// </summary>
    public async Task<Result<ModuleInstallResult>> InstallModuleAsync(
        string modulesPath,
        string source,
        ModuleInstallOptions? options = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Installing module from {Source} to {ModulesPath}", source, modulesPath);

            Directory.CreateDirectory(modulesPath);

            var moduleName = Path.GetFileNameWithoutExtension(source);
            var destPath = Path.Combine(modulesPath, moduleName);

            // Check if module already exists
            if (Directory.Exists(destPath) && !(options?.Overwrite ?? false))
            {
                return Result<ModuleInstallResult>.Failure(
                    $"Module '{moduleName}' already exists. Use Overwrite=true to replace.",
                    ErrorType.Conflict);
            }

            // Remove existing if overwriting
            if (Directory.Exists(destPath))
            {
                Directory.Delete(destPath, true);
            }

            List<string> installedFiles;

            if (Directory.Exists(source))
            {
                // Copy from directory
                installedFiles = CopyDirectory(source, destPath);
            }
            else if (File.Exists(source))
            {
                if (source.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract ZIP
                    ZipFile.ExtractToDirectory(source, destPath);
                    installedFiles = Directory.GetFiles(destPath, "*.*", SearchOption.AllDirectories).ToList();
                }
                else if (source.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                {
                    // Single Lua file
                    Directory.CreateDirectory(destPath);
                    File.Copy(source, Path.Combine(destPath, "main.lua"));
                    installedFiles = new List<string> { "main.lua" };
                }
                else
                {
                    return Result<ModuleInstallResult>.Failure("Unsupported file type", ErrorType.Validation);
                }
            }
            else if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
            {
                // Download from URL
                using var httpClient = new HttpClient();
                var tempFile = Path.Combine(Path.GetTempPath(), $"ikemen_module_{Guid.NewGuid()}.zip");
                try
                {
                    var data = await httpClient.GetByteArrayAsync(source, ct);
                    await File.WriteAllBytesAsync(tempFile, data, ct);
                    ZipFile.ExtractToDirectory(tempFile, destPath);
                    installedFiles = Directory.GetFiles(destPath, "*.*", SearchOption.AllDirectories).ToList();
                }
                finally
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
            else
            {
                return Result<ModuleInstallResult>.Failure("Source not found", ErrorType.NotFound);
            }

            // Validate the installed module
            var validationResult = await ValidateModuleAsync(destPath, ct);
            if (validationResult.IsFailure)
            {
                // Clean up on validation failure
                Directory.Delete(destPath, true);
                return Result<ModuleInstallResult>.Failure(
                    $"Module validation failed: {validationResult.Error}",
                    ErrorType.Validation);
            }

            var result = new ModuleInstallResult(
                true,
                moduleName,
                destPath,
                installedFiles,
                validationResult.Value.MissingDependencies);

            _logger.LogInformation("Module '{ModuleName}' installed successfully", moduleName);
            return Result<ModuleInstallResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install module");
            return Result<ModuleInstallResult>.Failure($"Installation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Uninstalls a Lua module.
    /// </summary>
    public async Task<Result> UninstallModuleAsync(
        string modulesPath,
        string moduleId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Uninstalling module: {ModuleId}", moduleId);

            var modulePath = Path.Combine(modulesPath, moduleId);
            if (!Directory.Exists(modulePath))
            {
                return Result.Failure($"Module '{moduleId}' not found", ErrorType.NotFound);
            }

            await Task.Run(() => Directory.Delete(modulePath, true), ct);
            _modulesCache.TryRemove(moduleId, out _);

            _logger.LogInformation("Module '{ModuleId}' uninstalled successfully", moduleId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall module");
            return Result.Failure($"Uninstall failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Validates a Lua module.
    /// </summary>
    public async Task<Result<ModuleValidationResult>> ValidateModuleAsync(
        string modulePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Validating module: {Path}", modulePath);

            var errors = new List<string>();
            var warnings = new List<string>();
            var missingDeps = new List<string>();

            if (!Directory.Exists(modulePath))
            {
                return Result<ModuleValidationResult>.Failure("Module path not found", ErrorType.NotFound);
            }

            // Check for module.json
            var jsonPath = Path.Combine(modulePath, "module.json");
            if (!File.Exists(jsonPath))
            {
                warnings.Add("module.json not found - using defaults");
            }
            else
            {
                try
                {
                    var json = await File.ReadAllTextAsync(jsonPath, ct);
                    var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Check required fields
                    if (!root.TryGetProperty("name", out _))
                        warnings.Add("module.json missing 'name' field");
                    if (!root.TryGetProperty("version", out _))
                        warnings.Add("module.json missing 'version' field");

                    // Check dependencies
                    if (root.TryGetProperty("dependencies", out var deps))
                    {
                        foreach (var dep in deps.EnumerateArray())
                        {
                            var depName = dep.GetString();
                            if (!string.IsNullOrEmpty(depName))
                            {
                                var depPath = Path.Combine(Path.GetDirectoryName(modulePath)!, depName);
                                if (!Directory.Exists(depPath))
                                {
                                    missingDeps.Add(depName);
                                }
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    errors.Add($"Invalid module.json: {ex.Message}");
                }
            }

            // Check for main.lua
            var mainLuaPath = Path.Combine(modulePath, "main.lua");
            if (!File.Exists(mainLuaPath))
            {
                errors.Add("main.lua not found - required for module execution");
            }

            // Check for init.lua as alternative entry point
            var initLuaPath = Path.Combine(modulePath, "init.lua");
            if (!File.Exists(mainLuaPath) && File.Exists(initLuaPath))
            {
                warnings.Add("Using init.lua instead of main.lua");
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

    /// <summary>
    /// Enables/disables a module.
    /// </summary>
    public async Task<Result> ToggleModuleAsync(
        string modulesPath,
        string moduleId,
        bool enabled,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("{Action} module: {ModuleId}", enabled ? "Enabling" : "Disabling", moduleId);

            var modulePath = Path.Combine(modulesPath, moduleId);
            if (!Directory.Exists(modulePath))
            {
                return Result.Failure($"Module '{moduleId}' not found", ErrorType.NotFound);
            }

            // Update module.json
            var jsonPath = Path.Combine(modulePath, "module.json");
            if (File.Exists(jsonPath))
            {
                var json = await File.ReadAllTextAsync(jsonPath, ct);
                using var doc = JsonDocument.Parse(json);
                
                var config = new Dictionary<string, object>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    config[prop.Name] = prop.Value;
                }
                
                config["enabled"] = enabled;
                config["lastToggled"] = _timeProvider.UtcNow;

                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = JsonSerializer.Serialize(config, options);
                await File.WriteAllTextAsync(jsonPath, updatedJson, ct);
            }
            else
            {
                // Create new module.json
                var config = new Dictionary<string, object>
                {
                    ["name"] = moduleId,
                    ["version"] = "1.0.0",
                    ["enabled"] = enabled,
                    ["lastToggled"] = _timeProvider.UtcNow
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var newJson = JsonSerializer.Serialize(config, options);
                await File.WriteAllTextAsync(jsonPath, newJson, ct);
            }

            _logger.LogInformation("Module '{ModuleId}' {Action} successfully", moduleId, enabled ? "enabled" : "disabled");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle module");
            return Result.Failure($"Toggle failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets module information from cache or loads it.
    /// </summary>
    public async Task<IkemenGoModule?> GetModuleAsync(
        string modulesPath,
        string moduleId,
        CancellationToken ct = default)
    {
        if (_modulesCache.TryGetValue(moduleId, out var cachedModule))
        {
            return cachedModule;
        }

        var modulePath = Path.Combine(modulesPath, moduleId);
        if (!Directory.Exists(modulePath))
        {
            return null;
        }

        return await LoadModuleInfoAsync(modulePath, ct);
    }

    /// <summary>
    /// Clears the module cache.
    /// </summary>
    public void ClearCache()
    {
        _modulesCache.Clear();
        _logger.LogDebug("Module cache cleared");
    }

    private async Task<IkemenGoModule?> LoadModuleInfoAsync(string modulePath, CancellationToken ct)
    {
        try
        {
            var moduleName = Path.GetFileName(modulePath);
            var jsonPath = Path.Combine(modulePath, "module.json");
            
            if (!File.Exists(jsonPath))
            {
                // Return basic module info without metadata
                var isEnabled = !moduleName.StartsWith("_"); // Convention: disabled modules start with _
                
                return new IkemenGoModule(
                    moduleName,
                    "unknown",
                    "unknown",
                    "No description available",
                    isEnabled,
                    false,
                    new List<string>());
            }

            var json = await File.ReadAllTextAsync(jsonPath, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.TryGetProperty("name", out var nameProp) 
                ? nameProp.GetString() ?? moduleName 
                : moduleName;
            
            var version = root.TryGetProperty("version", out var verProp) 
                ? verProp.GetString() ?? "unknown" 
                : "unknown";
            
            var author = root.TryGetProperty("author", out var authProp) 
                ? authProp.GetString() ?? "unknown" 
                : "unknown";
            
            var description = root.TryGetProperty("description", out var descProp) 
                ? descProp.GetString() ?? "" 
                : "";
            
            var moduleIsEnabled = !root.TryGetProperty("enabled", out var enabledProp) 
                || enabledProp.GetBoolean();
            
            var isOfficial = root.TryGetProperty("official", out var officialProp) 
                && officialProp.GetBoolean();

            var dependencies = new List<string>();
            if (root.TryGetProperty("dependencies", out var depsProp))
            {
                foreach (var dep in depsProp.EnumerateArray())
                {
                    var depName = dep.GetString();
                    if (!string.IsNullOrEmpty(depName))
                    {
                        dependencies.Add(depName);
                    }
                }
            }

            return new IkemenGoModule(
                name,
                version,
                author,
                description,
                moduleIsEnabled,
                isOfficial,
                dependencies);
        }
        catch
        {
            return null;
        }
    }

    private List<string> CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        var copiedFiles = new List<string>();

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
            copiedFiles.Add(fileName);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var destSubDir = Path.Combine(destDir, dirName);
            var subFiles = CopyDirectory(dir, destSubDir);
            copiedFiles.AddRange(subFiles.Select(f => $"{dirName}/{f}"));
        }

        return copiedFiles;
    }
}

/// <summary>
/// Options for module installation.
/// </summary>
public record ModuleInstallOptions(
    bool Overwrite = false,
    bool SkipValidation = false,
    bool EnableImmediately = true);
