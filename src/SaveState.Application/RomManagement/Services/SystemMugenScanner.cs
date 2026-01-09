using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SaveState.Application.RomManagement.Services;

/// <summary>
/// Implementation of system-wide MUGEN scanner.
/// </summary>
public class SystemMugenScanner : ISystemMugenScanner
{
    private readonly ILogger<SystemMugenScanner> _logger;

    public SystemMugenScanner(ILogger<SystemMugenScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlyList<DiscoveredMugenInstallation>>> ScanSystemAsync(
        MugenScanningOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting system-wide MUGEN scan");

            var discoveredInstallations = new List<DiscoveredMugenInstallation>();
            var totalPaths = options.KnownMugenPaths.Length + GetSystemPaths().Count();
            var scannedPaths = 0;

            // Scan known MUGEN paths
            foreach (var path in options.KnownMugenPaths)
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("MUGEN scan cancelled");
                    return Result.Success<IReadOnlyList<DiscoveredMugenInstallation>>(discoveredInstallations);
                }

                try
                {
                    var expandedPath = ExpandPath(path);
                    var found = await ScanDirectoryForMugenAsync(expandedPath, options, ct);
                    discoveredInstallations.AddRange(found);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan MUGEN path: {Path}", path);
                }

                scannedPaths++;
                progress?.Report(new ScanProgress(
                    FilesScanned: scannedPaths,
                    FilesTotal: totalPaths,
                    CurrentFile: path,
                    RomsFound: discoveredInstallations.Count));
            }

            // Scan system paths for MUGEN executables
            foreach (var path in GetSystemPaths())
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("MUGEN scan cancelled");
                    return Result.Success<IReadOnlyList<DiscoveredMugenInstallation>>(discoveredInstallations);
                }

                try
                {
                    var found = await ScanDirectoryForMugenExecutablesAsync(path, options, ct);
                    discoveredInstallations.AddRange(found);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan system path for MUGEN: {Path}", path);
                }

                scannedPaths++;
                progress?.Report(new ScanProgress(
                    FilesScanned: scannedPaths,
                    FilesTotal: totalPaths,
                    CurrentFile: path,
                    RomsFound: discoveredInstallations.Count));
            }

            // Remove duplicates based on install path
            var uniqueInstallations = discoveredInstallations
                .GroupBy(m => m.InstallPath.ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("MUGEN scan completed. Found {Count} unique installations", uniqueInstallations.Count);
            return Result.Success<IReadOnlyList<DiscoveredMugenInstallation>>(uniqueInstallations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan system for MUGEN installations");
            return Result.Failure<IReadOnlyList<DiscoveredMugenInstallation>>(
                $"System scan failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    private async Task<IReadOnlyList<DiscoveredMugenInstallation>> ScanDirectoryForMugenAsync(
        string directoryPath,
        MugenScanningOptions options,
        CancellationToken ct)
    {
        var installations = new List<DiscoveredMugenInstallation>();

        if (!Directory.Exists(directoryPath))
            return installations;

        try
        {
            // Check if this directory itself is a MUGEN installation
            var installation = await AnalyzeMugenDirectoryAsync(directoryPath, options, ct);
            if (installation != null)
            {
                installations.Add(installation);
            }

            // Scan subdirectories if recursive scanning is enabled
            if (options.ScanRecursively)
            {
                var subdirs = Directory.GetDirectories(directoryPath);
                foreach (var subdir in subdirs)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    try
                    {
                        var subInstallation = await AnalyzeMugenDirectoryAsync(subdir, options, ct);
                        if (subInstallation != null)
                        {
                            installations.Add(subInstallation);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to analyze potential MUGEN directory: {Path}", subdir);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan directory for MUGEN: {Path}", directoryPath);
        }

        return installations;
    }

    private async Task<IReadOnlyList<DiscoveredMugenInstallation>> ScanDirectoryForMugenExecutablesAsync(
        string directoryPath,
        MugenScanningOptions options,
        CancellationToken ct)
    {
        var installations = new List<DiscoveredMugenInstallation>();

        if (!Directory.Exists(directoryPath))
            return installations;

        try
        {
            var searchOption = options.ScanRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            await foreach (var filePath in EnumerateMugenExecutablesAsync(directoryPath, options.MugenExecutables, searchOption, ct))
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var installPath = Path.GetDirectoryName(filePath);
                    if (installPath == null)
                        continue;

                    // Check if we've already analyzed this directory
                    if (installations.Any(i => i.InstallPath.Equals(installPath, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var installation = await AnalyzeMugenDirectoryAsync(installPath, options, ct);
                    if (installation != null)
                    {
                        installations.Add(installation);
                        _logger.LogDebug("Found MUGEN installation: {Name} at {Path}", installation.Name, installation.InstallPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze MUGEN installation at: {Path}", filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan directory for MUGEN executables: {Path}", directoryPath);
        }

        return installations;
    }

    private static async Task<DiscoveredMugenInstallation?> AnalyzeMugenDirectoryAsync(
        string directoryPath,
        MugenScanningOptions options,
        CancellationToken ct)
    {
        try
        {
            // Check for required directories
            var hasRequiredDirs = options.RequiredDirectories.All(dir =>
                Directory.Exists(Path.Combine(directoryPath, dir)));

            if (!hasRequiredDirs)
                return null;

            // Look for MUGEN executables in this directory
            var executables = Directory.GetFiles(directoryPath, "*.exe")
                .Where(f => options.MugenExecutables.Contains(Path.GetFileName(f).ToLowerInvariant()))
                .ToList();

            if (!executables.Any())
                return null;

            // Analyze the installation
            var mainExecutable = executables.First();
            var version = GetMugenVersionAsync(mainExecutable, ct);
            var engineType = DetermineEngineType(mainExecutable);
            var stats = await GetMugenStatsAsync(directoryPath, ct);

            return new DiscoveredMugenInstallation(
                Name: GetMugenFriendlyName(directoryPath, engineType),
                InstallPath: directoryPath,
                Version: version,
                EngineType: engineType,
                CharacterCount: stats.CharacterCount,
                StageCount: stats.StageCount,
                TotalSizeBytes: stats.TotalSizeBytes,
                InstallDate: Directory.GetCreationTime(directoryPath),
                IsValidInstallation: true);
        }
        catch
        {
            return null;
        }
    }

    private static MugenEngineType DetermineEngineType(string executablePath)
    {
        var fileName = Path.GetFileName(executablePath).ToLowerInvariant();

        if (fileName.Contains("ikemen_go") || fileName.Contains("ikemengo"))
            return MugenEngineType.IkemenGo;

        if (fileName.Contains("ikemen"))
            return MugenEngineType.IkemenGo; // Assume Ikemen GO for now

        if (fileName.Contains("mugen"))
            return MugenEngineType.Original;

        return MugenEngineType.Unknown;
    }

    private static string GetMugenFriendlyName(string installPath, MugenEngineType engineType)
    {
        var dirName = Path.GetFileName(installPath.TrimEnd(Path.DirectorySeparatorChar));

        return engineType switch
        {
            MugenEngineType.IkemenGo => $"Ikemen GO ({dirName})",
            MugenEngineType.Original => $"MUGEN ({dirName})",
            _ => dirName
        };
    }

    private static string? GetMugenVersionAsync(string executablePath, CancellationToken ct)
    {
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
            return versionInfo.ProductVersion ?? versionInfo.FileVersion;
        }
        catch
        {
            return null;
        }
    }

    private static Task<(int CharacterCount, int StageCount, long TotalSizeBytes)> GetMugenStatsAsync(
        string installPath,
        CancellationToken ct)
    {
        try
        {
            var charsDir = Path.Combine(installPath, "chars");
            var stagesDir = Path.Combine(installPath, "stages");

            var charCount = Directory.Exists(charsDir) ?
                Directory.GetFiles(charsDir, "*.def", SearchOption.AllDirectories).Length : 0;

            var stageCount = Directory.Exists(stagesDir) ?
                Directory.GetFiles(stagesDir, "*.def", SearchOption.AllDirectories).Length : 0;

            var totalSize = CalculateDirectorySizeAsync(installPath, ct);

            return Task.FromResult((charCount, stageCount, totalSize));
        }
        catch
        {
            return Task.FromResult((0, 0, 0L));
        }
    }

    private static long CalculateDirectorySizeAsync(string directoryPath, CancellationToken ct)
    {
        try
        {
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            long totalSize = 0;

            foreach (var file in files)
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    totalSize += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip files that can't be accessed
                }
            }

            return totalSize;
        }
        catch
        {
            return 0;
        }
    }

    private static IEnumerable<string> GetSystemPaths()
    {
        var paths = new List<string>();

        // Program Files directories
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));

        // Common application data
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

        // User directories
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        paths.Add(Path.Combine(userProfile, "Games"));
        paths.Add(Path.Combine(userProfile, "Documents"));
        paths.Add(Path.Combine(userProfile, "Desktop"));

        return paths.Where(Directory.Exists);
    }

    private static string ExpandPath(string path)
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

    private static async IAsyncEnumerable<string> EnumerateMugenExecutablesAsync(
        string directoryPath,
        string[] executableNames,
        SearchOption searchOption,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var patterns = executableNames.Select(name => $"*{name}").ToArray();

        foreach (var pattern in patterns)
        {
            var files = Directory.EnumerateFiles(directoryPath, pattern, searchOption);
            foreach (var file in files)
            {
                if (ct.IsCancellationRequested)
                    yield break;

                await Task.Yield();
                yield return file;
            }
        }
    }
}


