using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using SaveState.Core.RetroArch.Models;
using System.Diagnostics;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for detecting RetroArch installation paths.
/// </summary>
public partial class PathDetectionEngine : IPathDetectionEngine
{
    private readonly ILogger<PathDetectionEngine> _logger;

    public PathDetectionEngine(ILogger<PathDetectionEngine> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<string>> DetectRetroArchPathAsync(RetroArchOptions options, CancellationToken ct = default)
    {
        try
        {
            // If already configured and valid, return it
            if (!string.IsNullOrEmpty(options.InstallPath) && File.Exists(options.InstallPath))
            {
                LogConfiguredPath(_logger, options.InstallPath);
                return Task.FromResult(Result.Success(options.InstallPath));
            }

            // If auto-detect is disabled and no path is set, fail
            if (!options.AutoDetect && string.IsNullOrEmpty(options.InstallPath))
            {
                return Task.FromResult(Result.Failure<string>("RetroArch path not configured and auto-detection is disabled."));
            }

            // Check common installation paths
            var possiblePaths = GetCommonInstallationPaths();

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    LogRetroArchDetected(_logger, path);
                    return Task.FromResult(Result.Success(path));
                }
            }

            LogNotFoundInCommonLocations(_logger);
            return Task.FromResult(Result.Failure<string>("RetroArch installation not found. Please install RetroArch or specify the path in Settings > RetroArch."));
        }
        catch (IOException ex)
        {
            LogPathDetectionError(_logger, ex);
            return Task.FromResult(Result.Failure<string>($"Error detecting RetroArch: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public bool IsValidRetroArchPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        // Check if the file is retroarch.exe
        var fileName = Path.GetFileName(path).ToLowerInvariant();
        return fileName is "retroarch.exe" or "retroarch";
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetCommonInstallationPaths()
    {
        return new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RetroArch", "retroarch.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "RetroArch", "retroarch.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetroArch", "retroarch.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RetroArch", "retroarch.exe"),
            @"C:\RetroArch-Win64\retroarch.exe",
            @"C:\RetroArch\retroarch.exe",
            @"D:\RetroArch\retroarch.exe",
            @"E:\RetroArch\retroarch.exe",
            @"C:\Program Files\RetroArch\retroarch.exe",
            @"C:\Program Files (x86)\RetroArch\retroarch.exe",
        };
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetVersionAsync(string retroArchPath, CancellationToken ct = default)
    {
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(retroArchPath);
            if (!string.IsNullOrEmpty(versionInfo.FileVersion))
            {
                return Result.Success(versionInfo.FileVersion);
            }

            // Try running retroarch with --version
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = retroArchPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (!string.IsNullOrWhiteSpace(output))
            {
                return Result.Success(output.Trim());
            }

            return Result.Failure<string>("Could not determine RetroArch version");
        }
        catch (Exception ex)
        {
            LogGetVersionError(_logger, ex);
            return Result.Failure<string>($"Error getting version: {ex.Message}");
        }
    }

    #region Logging

    [LoggerMessage(EventId = 701, Level = LogLevel.Information, Message = "Using configured RetroArch path: {Path}")]
    static partial void LogConfiguredPath(ILogger logger, string path);

    [LoggerMessage(EventId = 702, Level = LogLevel.Information, Message = "RetroArch detected at: {Path}")]
    static partial void LogRetroArchDetected(ILogger logger, string path);

    [LoggerMessage(EventId = 703, Level = LogLevel.Warning, Message = "RetroArch installation not found in common locations")]
    static partial void LogNotFoundInCommonLocations(ILogger logger);

    [LoggerMessage(EventId = 704, Level = LogLevel.Error, Message = "Error detecting RetroArch path")]
    static partial void LogPathDetectionError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 705, Level = LogLevel.Error, Message = "Error getting RetroArch version")]
    static partial void LogGetVersionError(ILogger logger, Exception ex);

    #endregion
}
