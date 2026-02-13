using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for managing RetroArch cores.
/// </summary>
public partial class CoreManagementEngine : ICoreManagementEngine
{
    private readonly ILogger<CoreManagementEngine> _logger;

    // Popular cores list for GetAvailableCoresAsync
    private static readonly IReadOnlyList<RetroArchCore> PopularCores = new List<RetroArchCore>
    {
        new() { Name = "snes9x", DisplayName = "Snes9x (SNES)", Path = "", IsInstalled = false },
        new() { Name = "genesis_plus_gx", DisplayName = "Genesis Plus GX (Genesis/MD)", Path = "", IsInstalled = false },
        new() { Name = "mgba", DisplayName = "mGBA (Game Boy Advance)", Path = "", IsInstalled = false },
        new() { Name = "mupen64plus_next", DisplayName = "Mupen64Plus-Next (N64)", Path = "", IsInstalled = false },
        new() { Name = "pcsx_rearmed", DisplayName = "PCSX ReARMed (PlayStation)", Path = "", IsInstalled = false },
        new() { Name = "dolphin", DisplayName = "Dolphin (GameCube/Wii)", Path = "", IsInstalled = false },
        new() { Name = "ppsspp", DisplayName = "PPSSPP (PSP)", Path = "", IsInstalled = false },
        new() { Name = "nestopia", DisplayName = "Nestopia (NES)", Path = "", IsInstalled = false },
        new() { Name = "fceumm", DisplayName = "FCEUmm (NES)", Path = "", IsInstalled = false },
        new() { Name = "gambatte", DisplayName = "Gambatte (Game Boy/Color)", Path = "", IsInstalled = false },
        new() { Name = "desmume", DisplayName = "DeSmuME (Nintendo DS)", Path = "", IsInstalled = false },
        new() { Name = "melonDS", DisplayName = "melonDS (Nintendo DS)", Path = "", IsInstalled = false },
        new() { Name = "vice_x64", DisplayName = "VICE x64 (Commodore 64)", Path = "", IsInstalled = false },
        new() { Name = "atari800", DisplayName = "Atari800 (Atari 5200/800)", Path = "", IsInstalled = false },
        new() { Name = "stella", DisplayName = "Stella (Atari 2600)", Path = "", IsInstalled = false },
        new() { Name = "mednafen_pce_fast", DisplayName = "Mednafen PCE Fast (PC Engine)", Path = "", IsInstalled = false },
        new() { Name = "mednafen_psx", DisplayName = "Mednafen PSX (PlayStation)", Path = "", IsInstalled = false },
        new() { Name = "mednafen_psx_hw", DisplayName = "Mednafen PSX HW (PlayStation)", Path = "", IsInstalled = false },
        new() { Name = "fbneo", DisplayName = "FinalBurn Neo (Arcade)", Path = "", IsInstalled = false },
        new() { Name = "mame", DisplayName = "MAME (Arcade)", Path = "", IsInstalled = false },
    };

    public CoreManagementEngine(ILogger<CoreManagementEngine> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RetroArchCore>>> GetInstalledCoresAsync(
        string retroArchPath,
        string? coresPathOverride,
        CancellationToken ct = default)
    {
        try
        {
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            var coresDir = !string.IsNullOrEmpty(coresPathOverride)
                ? coresPathOverride
                : Path.Combine(retroArchDir, "cores");

            if (!Directory.Exists(coresDir))
            {
                LogCoresNotFound(_logger, coresDir);
                return Task.FromResult(Result.Success<IReadOnlyList<RetroArchCore>>(Array.Empty<RetroArchCore>()));
            }

            var cores = new List<RetroArchCore>();
            var coreFiles = Directory.GetFiles(coresDir, "*_libretro.dll");

            foreach (var coreFile in coreFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(coreFile);
                var coreName = fileName.Replace("_libretro", "");

                cores.Add(new RetroArchCore
                {
                    Name = coreName,
                    DisplayName = FormatCoreName(coreName),
                    Path = coreFile,
                    IsInstalled = true
                });
            }

            LogInstalledCoresFoundCount(_logger, cores.Count);
            return Task.FromResult(Result.Success<IReadOnlyList<RetroArchCore>>(cores));
        }
        catch (DirectoryNotFoundException ex)
        {
            LogGetInstalledCoresError(_logger, ex);
            return Task.FromResult(Result.Failure<IReadOnlyList<RetroArchCore>>($"Cores directory not found: {ex.Message}"));
        }
        catch (UnauthorizedAccessException ex)
        {
            LogGetInstalledCoresError(_logger, ex);
            return Task.FromResult(Result.Failure<IReadOnlyList<RetroArchCore>>($"Access denied to cores directory: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RetroArchCore>>> GetAvailableCoresAsync(CancellationToken ct = default)
    {
        LogFetchingAvailableCores(_logger);

        // For now, return a curated list of popular cores
        // In production, you'd download and parse the info.zip file
        return Task.FromResult(Result.Success((IReadOnlyList<RetroArchCore>)PopularCores));
    }

    /// <inheritdoc />
    public async Task<Result> InstallCoreAsync(string retroArchPath, string coreName, CancellationToken ct = default)
    {
        try
        {
            LogInstallingCore(_logger, coreName);

            // Use RetroArch's built-in core updater
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = retroArchPath,
                    Arguments = $"--updatecore {coreName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                LogCoreInstallSuccess(_logger, coreName);
                return Result.Success();
            }

            return Result.Failure($"Core installation failed with exit code: {process.ExitCode}");
        }
        catch (InvalidOperationException ex)
        {
            LogInstallCoreError(_logger, coreName, ex);
            return Result.Failure($"Error installing core: {ex.Message}");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            LogInstallCoreError(_logger, coreName, ex);
            return Result.Failure($"Error starting RetroArch: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<Result> UpdateCoreAsync(string retroArchPath, string coreName, CancellationToken ct = default)
    {
        // Same as install - RetroArch handles updates the same way
        return InstallCoreAsync(retroArchPath, coreName, ct);
    }

    /// <inheritdoc />
    public Task<Result> UninstallCoreAsync(string coresDirectory, string coreName, CancellationToken ct = default)
    {
        try
        {
            var coreFileName = $"{coreName}_libretro.dll";
            var corePath = Path.Combine(coresDirectory, coreFileName);
            var infoPath = Path.Combine(coresDirectory, $"{coreFileName}.info");

            if (File.Exists(corePath))
            {
                File.Delete(corePath);
            }

            if (File.Exists(infoPath))
            {
                File.Delete(infoPath);
            }

            LogCoreUninstalled(_logger, coreName);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            LogUninstallCoreError(_logger, coreName, ex);
            return Task.FromResult(Result.Failure($"Error uninstalling core: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result<string>> GetCoreInfoAsync(string coresDirectory, string coreName, CancellationToken ct = default)
    {
        try
        {
            var infoPath = Path.Combine(coresDirectory, $"{coreName}_libretro.dll.info");
            if (!File.Exists(infoPath))
            {
                return Task.FromResult(Result.Failure<string>($"Core info file not found: {infoPath}"));
            }

            var content = File.ReadAllText(infoPath);
            return Task.FromResult(Result.Success(content));
        }
        catch (Exception ex)
        {
            LogGetCoreInfoError(_logger, coreName, ex);
            return Task.FromResult(Result.Failure<string>($"Error reading core info: {ex.Message}"));
        }
    }

    private static string FormatCoreName(string coreName)
    {
        // Convert snake_case to Title Case
        return MyRegex().Replace(coreName, " ")
            .Split(' ')
            .Select(word => char.ToUpper(word[0]) + word[1..])
            .Aggregate((a, b) => $"{a} {b}");
    }

    [GeneratedRegex("_")]
    private static partial Regex MyRegex();

    #region Logging

    [LoggerMessage(EventId = 201, Level = LogLevel.Warning, Message = "RetroArch cores directory not found: {Path}")]
    static partial void LogCoresNotFound(ILogger logger, string path);

    [LoggerMessage(EventId = 202, Level = LogLevel.Information, Message = "Found {Count} installed RetroArch cores")]
    static partial void LogInstalledCoresFoundCount(ILogger logger, int count);

    [LoggerMessage(EventId = 203, Level = LogLevel.Error, Message = "Error getting installed cores")]
    static partial void LogGetInstalledCoresError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 204, Level = LogLevel.Information, Message = "Fetching available cores from RetroArch buildbot")]
    static partial void LogFetchingAvailableCores(ILogger logger);

    [LoggerMessage(EventId = 205, Level = LogLevel.Information, Message = "Installing RetroArch core: {CoreName}")]
    static partial void LogInstallingCore(ILogger logger, string coreName);

    [LoggerMessage(EventId = 206, Level = LogLevel.Information, Message = "Successfully installed core: {CoreName}")]
    static partial void LogCoreInstallSuccess(ILogger logger, string coreName);

    [LoggerMessage(EventId = 207, Level = LogLevel.Error, Message = "Error installing core: {CoreName}")]
    static partial void LogInstallCoreError(ILogger logger, string coreName, Exception ex);

    [LoggerMessage(EventId = 208, Level = LogLevel.Information, Message = "Successfully uninstalled core: {CoreName}")]
    static partial void LogCoreUninstalled(ILogger logger, string coreName);

    [LoggerMessage(EventId = 209, Level = LogLevel.Error, Message = "Error uninstalling core: {CoreName}")]
    static partial void LogUninstallCoreError(ILogger logger, string coreName, Exception ex);

    [LoggerMessage(EventId = 210, Level = LogLevel.Error, Message = "Error getting core info: {CoreName}")]
    static partial void LogGetCoreInfoError(ILogger logger, string coreName, Exception ex);

    #endregion
}
