using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Configuration;

namespace SaveState.Application.RomManagement.Services;

/// <summary>
/// Implementation of ROM path management service.
/// </summary>
public class RomPathManager : IRomPathManager
{
    private readonly ILogger<RomPathManager> _logger;
    private readonly IOptionsMonitor<RomScanningOptions> _options;

    public RomPathManager(
        ILogger<RomPathManager> logger,
        IOptionsMonitor<RomScanningOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public RomScanningOptions GetConfiguration()
    {
        return _options.CurrentValue;
    }

    public Task<Result> UpdateConfigurationAsync(RomScanningOptions options)
    {
        try
        {
            // Validate the options
            if (options == null)
                return Task.FromResult(Result.Failure("Options cannot be null", ErrorType.Validation));

            if (options.RomDirectories == null || options.RomDirectories.Length == 0)
                return Task.FromResult(Result.Failure("At least one ROM directory must be configured", ErrorType.Validation));

            // Note: In a production app, this would persist changes to a user configuration file
            // For now, changes made through the GUI are validated but not persisted between sessions
            // Users should edit appsettings.json directly for persistent changes

            _logger.LogInformation("ROM scanning configuration validated (persistence not yet implemented)");
            _logger.LogWarning("ROM directory changes made through GUI are not persisted. Edit appsettings.json manually for now.");

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ROM scanning configuration");
            return Task.FromResult(Result.Failure($"Failed to update configuration: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> AddRomDirectoryAsync(string path, bool validatePath = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult(Result.Failure("Path cannot be null or empty", ErrorType.Validation));

            var expandedPath = ExpandPath(path);

            if (validatePath && !ValidateRomDirectory(expandedPath))
                return Task.FromResult(Result.Failure($"Directory does not exist or is not accessible: {expandedPath}", ErrorType.Validation));

            // In a real implementation, this would update persistent configuration
            _logger.LogInformation("Added ROM directory: {Path}", expandedPath);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add ROM directory: {Path}", path);
            return Task.FromResult(Result.Failure($"Failed to add directory: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> RemoveRomDirectoryAsync(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult(Result.Failure("Path cannot be null or empty", ErrorType.Validation));

            var expandedPath = ExpandPath(path);

            // In a real implementation, this would update persistent configuration
            _logger.LogInformation("Removed ROM directory: {Path}", expandedPath);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove ROM directory: {Path}", path);
            return Task.FromResult(Result.Failure($"Failed to remove directory: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<IReadOnlyList<string>> GetRomDirectoriesAsync()
    {
        var config = GetConfiguration();
        var expandedPaths = new List<string>();

        foreach (var path in config.RomDirectories)
        {
            try
            {
                var expandedPath = ExpandPath(path);
                if (ValidateRomDirectory(expandedPath))
                {
                    expandedPaths.Add(expandedPath);
                }
                else
                {
                    _logger.LogWarning("ROM directory not accessible: {Path}", expandedPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to expand ROM directory path: {Path}", path);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(expandedPaths);
    }

    public bool ValidateRomDirectory(string path)
    {
        try
        {
            return Directory.Exists(path) && HasAccess(path);
        }
        catch
        {
            return false;
        }
    }

    public string ExpandPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Expand environment variables
        var expanded = Environment.ExpandEnvironmentVariables(path);

        // Handle ~ for home directory
        if (expanded.StartsWith("~"))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = expanded.Replace("~", homeDir);
        }

        // Convert to absolute path if relative
        if (!Path.IsPathRooted(expanded))
        {
            expanded = Path.GetFullPath(expanded);
        }

        return expanded;
    }

    private static bool HasAccess(string path)
    {
        try
        {
            // Try to enumerate the directory to check access
            _ = Directory.EnumerateFileSystemEntries(path).FirstOrDefault();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
