using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.WebBrowser.ExtensionSupport;

/// <summary>
/// Implementation of the extension manager.
/// </summary>
public class ExtensionManager : IExtensionManager
{
    private readonly ILogger<ExtensionManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, BrowserExtension> _extensions = new();
    private readonly string _extensionsDirectory;

    public ExtensionManager(ILogger<ExtensionManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _extensionsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveState",
            "Extensions");

        Directory.CreateDirectory(_extensionsDirectory);
    }

    /// <inheritdoc />
    public event EventHandler<ExtensionLoadedEventArgs>? ExtensionLoaded;

    /// <inheritdoc />
    public event EventHandler<ExtensionUnloadedEventArgs>? ExtensionUnloaded;

    /// <inheritdoc />
    public Task<IReadOnlyList<BrowserExtension>> GetInstalledExtensionsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<BrowserExtension>>(
            _extensions.Values.OrderBy(e => e.Name).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<BrowserExtension>> LoadUnpackedExtensionAsync(string directoryPath, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading unpacked extension from: {Path}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                return Result<BrowserExtension>.Failure("Extension directory not found", ErrorType.NotFound);
            }

            var manifestPath = Path.Combine(directoryPath, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return Result<BrowserExtension>.Failure("manifest.json not found", ErrorType.NotFound);
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath, ct);
            var manifest = JsonSerializer.Deserialize<ExtensionManifest>(manifestJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (manifest == null)
            {
                return Result<BrowserExtension>.Failure("Failed to parse manifest.json", ErrorType.Validation);
            }

            var extensionId = GenerateExtensionId(directoryPath);

            if (_extensions.ContainsKey(extensionId))
            {
                return Result<BrowserExtension>.Failure("Extension already loaded", ErrorType.Conflict);
            }

            var extension = new BrowserExtension
            {
                Id = extensionId,
                Name = manifest.Name,
                Version = manifest.Version,
                Description = manifest.Description,
                DirectoryPath = directoryPath,
                IsEnabled = true,
                IsPacked = false,
                Manifest = manifest,
                Permissions = manifest.Permissions,
                InstalledAt = _timeProvider.UtcNow,
                Icon = LoadExtensionIcon(directoryPath, manifest.Icons)
            };

            // Load content scripts
            foreach (var scriptDef in manifest.ContentScripts)
            {
                var contentScript = new ContentScript
                {
                    ExtensionId = extensionId,
                    ExtensionName = manifest.Name,
                    Matches = scriptDef.Matches,
                    RunAt = scriptDef.RunAt,
                    AllFrames = scriptDef.AllFrames
                };

                // Load JavaScript files
                if (scriptDef.Js.Count > 0)
                {
                    var jsContent = new List<string>();
                    foreach (var jsFile in scriptDef.Js)
                    {
                        var jsPath = Path.Combine(directoryPath, jsFile);
                        if (File.Exists(jsPath))
                        {
                            jsContent.Add(await File.ReadAllTextAsync(jsPath, ct));
                        }
                    }
                    contentScript.JavaScript = string.Join("\n", jsContent);
                }

                // Load CSS files
                if (scriptDef.Css.Count > 0)
                {
                    var cssContent = new List<string>();
                    foreach (var cssFile in scriptDef.Css)
                    {
                        var cssPath = Path.Combine(directoryPath, cssFile);
                        if (File.Exists(cssPath))
                        {
                            cssContent.Add(await File.ReadAllTextAsync(cssPath, ct));
                        }
                    }
                    contentScript.Css = string.Join("\n", cssContent);
                }

                extension.ContentScripts.Add(contentScript);
            }

            _extensions[extensionId] = extension;

            ExtensionLoaded?.Invoke(this, new ExtensionLoadedEventArgs { Extension = extension });

            _logger.LogInformation("Loaded extension: {Name} ({Id})", extension.Name, extension.Id);

            return Result<BrowserExtension>.Success(extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load extension from: {Path}", directoryPath);
            return Result<BrowserExtension>.Failure($"Extension load failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result> UnloadExtensionAsync(string extensionId, CancellationToken ct = default)
    {
        try
        {
            if (!_extensions.Remove(extensionId))
            {
                return Task.FromResult(Result.Failure("Extension not found", ErrorType.NotFound));
            }

            ExtensionUnloaded?.Invoke(this, new ExtensionUnloadedEventArgs { ExtensionId = extensionId });

            _logger.LogInformation("Unloaded extension: {Id}", extensionId);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload extension: {Id}", extensionId);
            return Task.FromResult(Result.Failure($"Extension unload failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SetExtensionEnabledAsync(string extensionId, bool enabled, CancellationToken ct = default)
    {
        try
        {
            if (!_extensions.TryGetValue(extensionId, out var extension))
            {
                return Task.FromResult(Result.Failure("Extension not found", ErrorType.NotFound));
            }

            extension.IsEnabled = enabled;

            _logger.LogInformation("Extension {Id} {Status}", extensionId, enabled ? "enabled" : "disabled");

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set extension state: {Id}", extensionId);
            return Task.FromResult(Result.Failure($"Failed to set extension state: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ContentScript>> GetContentScriptsForUrlAsync(string url, CancellationToken ct = default)
    {
        var scripts = new List<ContentScript>();

        foreach (var extension in _extensions.Values.Where(e => e.IsEnabled))
        {
            foreach (var script in extension.ContentScripts)
            {
                if (ShouldInjectScript(url, script))
                {
                    scripts.Add(script);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<ContentScript>>(scripts);
    }

    /// <inheritdoc />
    public Task<Result> InjectContentScriptsAsync(string pageUrl, IList<ContentScript> scripts, CancellationToken ct = default)
    {
        // This would be called by the browser control to actually inject scripts
        // The implementation depends on the specific browser engine being used
        _logger.LogDebug("Injecting {Count} content scripts into {Url}", scripts.Count, pageUrl);
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Loads popular pre-configured extensions.
    /// </summary>
    public async Task LoadPopularExtensionsAsync()
    {
        // These would be bundled with the application
        var popularExtensions = new[]
        {
            ("uBlock Origin", "ublock"),
            ("Dark Reader", "darkreader"),
            ("Privacy Badger", "privacybadger"),
            ("HTTPS Everywhere", "httpseverywhere")
        };

        foreach (var (name, id) in popularExtensions)
        {
            var extensionPath = Path.Combine(_extensionsDirectory, id);
            if (Directory.Exists(extensionPath))
            {
                await LoadUnpackedExtensionAsync(extensionPath);
            }
        }
    }

    private static bool ShouldInjectScript(string url, ContentScript script)
    {
        foreach (var pattern in script.Matches)
        {
            if (MatchPattern(url, pattern))
            {
                return true;
            }
        }
        return false;
    }

    private static bool MatchPattern(string url, string pattern)
    {
        // Simple pattern matching (would be more sophisticated in production)
        // Supports * wildcards
        if (pattern == "<all_urls>")
            return true;

        var regexPattern = pattern
            .Replace(".", "\\.")
            .Replace("*", ".*");

        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(url, regexPattern);
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateExtensionId(string path)
    {
        // Generate a deterministic ID from the path
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }

    private static ExtensionIcon? LoadExtensionIcon(string directoryPath, Dictionary<string, string> icons)
    {
        if (icons.Count == 0)
            return null;

        return new ExtensionIcon
        {
            Path16 = GetIconPath(directoryPath, icons, "16"),
            Path32 = GetIconPath(directoryPath, icons, "32"),
            Path48 = GetIconPath(directoryPath, icons, "48"),
            Path128 = GetIconPath(directoryPath, icons, "128")
        };
    }

    private static string GetIconPath(string directoryPath, Dictionary<string, string> icons, string size)
    {
        if (icons.TryGetValue(size, out var path))
        {
            return Path.Combine(directoryPath, path);
        }
        return string.Empty;
    }
}
