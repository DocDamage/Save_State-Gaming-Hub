using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SaveState.Core.Services.Mods
{
    /// <summary>
    /// Validates mods for security, compatibility, and integrity.
    /// Prevents malicious or incompatible mods from being loaded.
    /// </summary>
    public interface IModValidator
    {
        Task<ModValidationResult> ValidateModAsync(string modPath, ModManifest manifest);
        Task<bool> ValidateModIntegrityAsync(string modPath, string expectedHash);
        void RegisterCustomRule(IModValidationRule rule);
    }

    public class ModValidator : IModValidator
    {
        private readonly ILogger<ModValidator>? _logger;
        private readonly List<IModValidationRule> _rules = new();
        private readonly ModValidatorSettings _settings;

        // Dangerous patterns to detect in code
        private static readonly string[] DangerousPatterns = new[]
        {
            @"Process\.Start",
            @"Runtime\.GetRuntime",
            @"System\.Reflection\.Assembly\.Load",
            @"WebClient|HttpClient",
            @"File\.Delete|Directory\.Delete",
            @"Registry\.",
            @"Marshal\.Copy|Marshal\.Write",
            @"DllImport",
            @"unsafe\s+\{",
            @"stackalloc",
            @"Environment\.Exit",
            @"PowerShell|cmd\.exe|bash"
        };

        // Maximum allowed file sizes
        private const long MaxModSizeBytes = 50 * 1024 * 1024; // 50 MB
        private const long MaxSingleFileBytes = 10 * 1024 * 1024; // 10 MB
        private const int MaxFileCount = 500;

        public ModValidator(ModValidatorSettings? settings = null, ILogger<ModValidator>? logger = null)
        {
            _settings = settings ?? new ModValidatorSettings();
            _logger = logger;
            InitializeDefaultRules();
        }

        private void InitializeDefaultRules()
        {
            _rules.Add(new ManifestValidationRule());
            _rules.Add(new FileSystemSecurityRule());
            _rules.Add(new CodeSecurityRule());
            _rules.Add(new PermissionValidationRule());
        }

        public async Task<ModValidationResult> ValidateModAsync(string modPath, ModManifest manifest)
        {
            var result = new ModValidationResult { IsValid = true };

            if (!Directory.Exists(modPath))
            {
                result.IsValid = false;
                result.Errors.Add($"Mod path does not exist: {modPath}");
                return result;
            }

            try
            {
                // Run all validation rules
                foreach (var rule in _rules)
                {
                    var ruleResult = await rule.ValidateAsync(modPath, manifest, _settings);
                    
                    if (!ruleResult.IsValid)
                    {
                        result.IsValid = false;
                        result.Errors.AddRange(ruleResult.Errors);
                    }
                    
                    result.Warnings.AddRange(ruleResult.Warnings);
                }

                // Check file sizes
                var sizeValidation = await ValidateFileSizesAsync(modPath);
                if (!sizeValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(sizeValidation.Errors);
                }

                // Scan for dangerous code patterns
                if (_settings.ScanForDangerousCode)
                {
                    var codeValidation = await ScanCodeForDangerousPatternsAsync(modPath);
                    if (!codeValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Errors.AddRange(codeValidation.Errors);
                    }
                    result.Warnings.AddRange(codeValidation.Warnings);
                }

                // Validate dependencies
                var depValidation = ValidateDependencies(manifest);
                if (!depValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(depValidation.Errors);
                }

                _logger?.LogInformation(
                    "Mod validation complete for {ModId}: Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}",
                    manifest.Id, result.IsValid, result.Errors.Count, result.Warnings.Count
                );
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Mod validation failed for {ModPath}", modPath);
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
            }

            return result;
        }

        public async Task<bool> ValidateModIntegrityAsync(string modPath, string expectedHash)
        {
            try
            {
                var actualHash = await ComputeModHashAsync(modPath);
                return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to validate mod integrity for {ModPath}", modPath);
                return false;
            }
        }

        public void RegisterCustomRule(IModValidationRule rule)
        {
            _rules.Add(rule);
        }

        private Task<ModValidationResult> ValidateFileSizesAsync(string modPath)
        {
            var result = new ModValidationResult { IsValid = true };

            try
            {
                var files = Directory.GetFiles(modPath, "*", SearchOption.AllDirectories);
                
                if (files.Length > MaxFileCount)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Too many files: {files.Length} (max: {MaxFileCount})");
                    return Task.FromResult(result);
                }

                long totalSize = 0;
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;

                    if (fileInfo.Length > MaxSingleFileBytes)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"File too large: {Path.GetFileName(file)} ({fileInfo.Length / 1024 / 1024}MB)");
                    }
                }

                if (totalSize > MaxModSizeBytes)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Total mod size too large: {totalSize / 1024 / 1024}MB (max: {MaxModSizeBytes / 1024 / 1024}MB)");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"File size validation failed: {ex.Message}");
            }

            return Task.FromResult(result);
        }

        private async Task<ModValidationResult> ScanCodeForDangerousPatternsAsync(string modPath)
        {
            var result = new ModValidationResult { IsValid = true };

            var codeExtensions = new[] { ".cs", ".js", ".lua", ".py", ".ps1" };
            var files = Directory.GetFiles(modPath, "*", SearchOption.AllDirectories)
                .Where(f => codeExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var relativeFile = Path.GetRelativePath(modPath, file);

                    foreach (var pattern in DangerousPatterns)
                    {
                        if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                        {
                            if (_settings.BlockDangerousPatterns)
                            {
                                result.IsValid = false;
                                result.Errors.Add($"Dangerous pattern detected in {relativeFile}: {pattern}");
                            }
                            else
                            {
                                result.Warnings.Add($"Potentially dangerous pattern in {relativeFile}: {pattern}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not scan file {File}", file);
                }
            }

            return result;
        }

        private ModValidationResult ValidateDependencies(ModManifest manifest)
        {
            var result = new ModValidationResult { IsValid = true };

            if (manifest.Dependencies == null || !manifest.Dependencies.Any())
                return result;

            // Check for circular dependencies (simplified check)
            var seen = new HashSet<string> { manifest.Id };
            foreach (var dep in manifest.Dependencies)
            {
                if (seen.Contains(dep))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Circular dependency detected: {dep}");
                }
            }

            return result;
        }

        private async Task<string> ComputeModHashAsync(string modPath)
        {
            using var sha256 = SHA256.Create();
            using var stream = new MemoryStream();

            var files = Directory.GetFiles(modPath, "*", SearchOption.AllDirectories)
                .OrderBy(f => f);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(modPath, file);
                var pathBytes = System.Text.Encoding.UTF8.GetBytes(relativePath);
                await stream.WriteAsync(pathBytes);
                
                var fileBytes = await File.ReadAllBytesAsync(file);
                await stream.WriteAsync(fileBytes);
            }

            stream.Position = 0;
            var hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    #region Validation Rules

    public interface IModValidationRule
    {
        string Name { get; }
        Task<ModValidationResult> ValidateAsync(string modPath, ModManifest manifest, ModValidatorSettings settings);
    }

    public class ManifestValidationRule : IModValidationRule
    {
        public string Name => "Manifest Validation";

        public Task<ModValidationResult> ValidateAsync(string modPath, ModManifest manifest, ModValidatorSettings settings)
        {
            var result = new ModValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(manifest.Id))
            {
                result.IsValid = false;
                result.Errors.Add("Manifest missing required field: Id");
            }
            else if (!Regex.IsMatch(manifest.Id, @"^[a-zA-Z0-9_.-]+$"))
            {
                result.IsValid = false;
                result.Errors.Add("Manifest Id contains invalid characters");
            }

            if (string.IsNullOrWhiteSpace(manifest.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Manifest missing required field: Name");
            }

            if (string.IsNullOrWhiteSpace(manifest.Version))
            {
                result.Warnings.Add("Manifest missing recommended field: Version");
            }
            else if (!Version.TryParse(manifest.Version, out _) && !Regex.IsMatch(manifest.Version, @"^\d+\.\d+(\.\d+)?(-[\w.]+)?$"))
            {
                result.Warnings.Add($"Manifest Version '{manifest.Version}' is not a standard version format");
            }

            if (string.IsNullOrWhiteSpace(manifest.EntryPoint))
            {
                result.Warnings.Add("Manifest missing EntryPoint - mod may not function correctly");
            }

            return Task.FromResult(result);
        }
    }

    public class FileSystemSecurityRule : IModValidationRule
    {
        public string Name => "File System Security";

        private static readonly string[] ForbiddenExtensions = new[] { ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".scr" };

        public Task<ModValidationResult> ValidateAsync(string modPath, ModManifest manifest, ModValidatorSettings settings)
        {
            var result = new ModValidationResult { IsValid = true };

            var files = Directory.GetFiles(modPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                var fileName = Path.GetFileName(file);

                // Check for forbidden extensions
                if (ForbiddenExtensions.Contains(ext) && !settings.AllowExecutables)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Forbidden file type: {fileName}");
                }

                // Check for path traversal attempts
                var relativePath = Path.GetRelativePath(modPath, file);
                if (relativePath.Contains(".."))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Path traversal attempt detected: {relativePath}");
                }

                // Check for hidden files (potential stealth malware)
                if (fileName.StartsWith(".") && !settings.AllowHiddenFiles)
                {
                    result.Warnings.Add($"Hidden file detected: {fileName}");
                }
            }

            return Task.FromResult(result);
        }
    }

    public class CodeSecurityRule : IModValidationRule
    {
        public string Name => "Code Security";

        public Task<ModValidationResult> ValidateAsync(string modPath, ModManifest manifest, ModValidatorSettings settings)
        {
            var result = new ModValidationResult { IsValid = true };

            // Check entry point exists
            if (!string.IsNullOrWhiteSpace(manifest.EntryPoint))
            {
                var entryPointPath = Path.Combine(modPath, manifest.EntryPoint);
                if (!File.Exists(entryPointPath))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Entry point file not found: {manifest.EntryPoint}");
                }
            }

            return Task.FromResult(result);
        }
    }

    public class PermissionValidationRule : IModValidationRule
    {
        public string Name => "Permission Validation";

        public Task<ModValidationResult> ValidateAsync(string modPath, ModManifest manifest, ModValidatorSettings settings)
        {
            var result = new ModValidationResult { IsValid = true };

            var perms = manifest.Permissions;

            // Warn about sensitive permissions
            if (perms.CanWriteMemory)
            {
                result.Warnings.Add("Mod requests memory write access - use with caution");
            }

            if (perms.CanAccessNetwork)
            {
                result.Warnings.Add("Mod requests network access - may send data externally");
            }

            if (perms.CanInjectCode)
            {
                if (!settings.AllowCodeInjection)
                {
                    result.IsValid = false;
                    result.Errors.Add("Code injection is not allowed by current settings");
                }
                else
                {
                    result.Warnings.Add("Mod requests code injection - high risk permission");
                }
            }

            if (perms.CanAccessFileSystem)
            {
                result.Warnings.Add("Mod requests file system access");
            }

            return Task.FromResult(result);
        }
    }

    #endregion

    #region Models

    public class ModValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class ModValidatorSettings
    {
        public bool ScanForDangerousCode { get; set; } = true;
        public bool BlockDangerousPatterns { get; set; } = true;
        public bool AllowExecutables { get; set; } = false;
        public bool AllowHiddenFiles { get; set; } = false;
        public bool AllowCodeInjection { get; set; } = false;
        public int MaxValidationTimeMs { get; set; } = 30000;
    }

    #endregion
}
