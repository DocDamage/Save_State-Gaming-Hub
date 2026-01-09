using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SaveState.Application.RomManagement.Services;

/// <summary>
/// Implementation of system-wide emulator scanner.
/// </summary>
public class SystemEmulatorScanner : ISystemEmulatorScanner
{
    private readonly ILogger<SystemEmulatorScanner> _logger;

    public SystemEmulatorScanner(ILogger<SystemEmulatorScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlyList<DiscoveredEmulator>>> ScanSystemAsync(
        EmulatorScanningOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting system-wide emulator scan");

            var discoveredEmulators = new List<DiscoveredEmulator>();
            var totalPaths = options.KnownEmulatorPaths.Length + GetSystemPaths().Count();
            var scannedPaths = 0;

            // Scan known emulator paths
            foreach (var path in options.KnownEmulatorPaths)
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("Emulator scan cancelled");
                    return Result.Success<IReadOnlyList<DiscoveredEmulator>>(discoveredEmulators);
                }

                try
                {
                    var expandedPath = ExpandPath(path);
                    var found = await ScanDirectoryAsync(expandedPath, options, ct);
                    discoveredEmulators.AddRange(found);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan emulator path: {Path}", path);
                }

                scannedPaths++;
                progress?.Report(new ScanProgress(
                    FilesScanned: scannedPaths,
                    FilesTotal: totalPaths,
                    CurrentFile: path,
                    RomsFound: discoveredEmulators.Count));
            }

            // Scan system paths
            foreach (var path in GetSystemPaths())
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("Emulator scan cancelled");
                    return Result.Success<IReadOnlyList<DiscoveredEmulator>>(discoveredEmulators);
                }

                try
                {
                    var found = await ScanDirectoryAsync(path, options, ct);
                    discoveredEmulators.AddRange(found);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan system path: {Path}", path);
                }

                scannedPaths++;
                progress?.Report(new ScanProgress(
                    FilesScanned: scannedPaths,
                    FilesTotal: totalPaths,
                    CurrentFile: path,
                    RomsFound: discoveredEmulators.Count));
            }

            // Remove duplicates based on executable path
            var uniqueEmulators = discoveredEmulators
                .GroupBy(e => e.ExecutablePath.ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Emulator scan completed. Found {Count} unique emulators", uniqueEmulators.Count);
            return Result.Success<IReadOnlyList<DiscoveredEmulator>>(uniqueEmulators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan system for emulators");
            return Result.Failure<IReadOnlyList<DiscoveredEmulator>>(
                $"System scan failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    private async Task<IReadOnlyList<DiscoveredEmulator>> ScanDirectoryAsync(
        string directoryPath,
        EmulatorScanningOptions options,
        CancellationToken ct)
    {
        var emulators = new List<DiscoveredEmulator>();

        if (!Directory.Exists(directoryPath))
            return emulators;

        try
        {
            var searchOption = options.ScanRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            await foreach (var filePath in EnumerateFilesAsync(directoryPath, options.CommonEmulatorExecutables, searchOption, ct))
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var emulator = await AnalyzeExecutableAsync(filePath, ct);
                    if (emulator != null)
                    {
                        emulators.Add(emulator);
                        _logger.LogDebug("Found emulator: {Name} at {Path}", emulator.Name, emulator.ExecutablePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze potential emulator: {Path}", filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan directory: {Path}", directoryPath);
        }

        return emulators;
    }

    private static Task<DiscoveredEmulator?> AnalyzeExecutableAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);

            // Check file size
            if (fileInfo.Length < 1024 * 1024) // Less than 1MB
                return Task.FromResult<DiscoveredEmulator?>(null);

            // Check if it's actually an executable
            if (!IsExecutable(filePath))
                return Task.FromResult<DiscoveredEmulator?>(null);

            var name = Path.GetFileNameWithoutExtension(filePath);
            var version = GetVersionInfoAsync(filePath, ct);
            var publisher = GetPublisherInfoAsync(filePath, ct);

            var emulatorType = DetermineEmulatorType(name);

            return Task.FromResult<DiscoveredEmulator?>(new DiscoveredEmulator(
                Name: GetFriendlyName(name),
                ExecutablePath: filePath,
                Version: version,
                Publisher: publisher,
                InstallDate: fileInfo.CreationTime,
                SizeBytes: fileInfo.Length,
                Type: emulatorType));
        }
        catch
        {
            return Task.FromResult<DiscoveredEmulator?>(null);
        }
    }

    private static EmulatorType DetermineEmulatorType(string executableName)
    {
        var name = executableName.ToLowerInvariant();

        // Multi-system emulators
        if (name.Contains("retroarch") || name.Contains("mednafen") || name.Contains("openemu"))
            return EmulatorType.MultiSystem;

        // Single-system emulators
        if (name.Contains("mgba") || name.Contains("mesen") || name.Contains("fceux") ||
            name.Contains("snes9x") || name.Contains("zsnes") || name.Contains("project64") ||
            name.Contains("mupen64") || name.Contains("dolphin") || name.Contains("pcsx2") ||
            name.Contains("epsxe") || name.Contains("fusion"))
            return EmulatorType.SingleSystem;

        return EmulatorType.Unknown;
    }

    private static string GetFriendlyName(string executableName)
    {
        return executableName.ToLowerInvariant() switch
        {
            var name when name.Contains("retroarch") => "RetroArch",
            var name when name.Contains("mgba") => "mGBA",
            var name when name.Contains("mesen") => "Mesen",
            var name when name.Contains("fceux") => "FCEUX",
            var name when name.Contains("snes9x") => "Snes9x",
            var name when name.Contains("zsnes") => "ZSNES",
            var name when name.Contains("project64") => "Project64",
            var name when name.Contains("mupen64") => "Mupen64Plus",
            var name when name.Contains("dolphin") => "Dolphin",
            var name when name.Contains("pcsx2") => "PCSX2",
            var name when name.Contains("epsxe") => "ePSXe",
            var name when name.Contains("mednafen") => "Mednafen",
            var name when name.Contains("fusion") => "Fusion",
            _ => executableName
        };
    }

    private static string? GetVersionInfoAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            return versionInfo.ProductVersion ?? versionInfo.FileVersion;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetPublisherInfoAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            return versionInfo.CompanyName;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsExecutable(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".exe" || extension == ".app" || extension == ".cmd" || extension == ".bat";
        }
        catch
        {
            return false;
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

        // User application data
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        // Add common game/emulator directories
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        paths.Add(Path.Combine(userProfile, "Games"));
        paths.Add(Path.Combine(userProfile, "Emulators"));
        paths.Add(Path.Combine(userProfile, "RetroArch"));

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

    private static async IAsyncEnumerable<string> EnumerateFilesAsync(
        string directoryPath,
        string[] fileNames,
        SearchOption searchOption,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var patterns = fileNames.Select(name => $"*{name}").ToArray();

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


