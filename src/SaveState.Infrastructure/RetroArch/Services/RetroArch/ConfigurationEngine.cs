using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using SaveState.Core.RetroArch.Models;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for managing RetroArch configuration.
/// </summary>
public partial class ConfigurationEngine : IConfigurationEngine
{
    private readonly ILogger<ConfigurationEngine> _logger;

    public ConfigurationEngine(ILogger<ConfigurationEngine> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<RetroArchConfig>> GetConfigAsync(string retroArchPath, CancellationToken ct = default)
    {
        try
        {
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            var configPath = Path.Combine(retroArchDir, "retroarch.cfg");

            if (!File.Exists(configPath))
            {
                LogConfigNotFound(_logger, configPath);
                return Result.Success(new RetroArchConfig());
            }

            var config = new RetroArchConfig();
            var lines = await File.ReadAllLinesAsync(configPath, ct);

            foreach (var line in lines)
            {
                if (line.StartsWith("savefile_directory"))
                    config.SavefileDirectory = ExtractConfigValue(line);
                else if (line.StartsWith("savestate_directory"))
                    config.SavestateDirectory = ExtractConfigValue(line);
                else if (line.StartsWith("system_directory"))
                    config.SystemDirectory = ExtractConfigValue(line);
                else if (line.StartsWith("netplay_enable"))
                    config.CloudSyncEnabled = ExtractConfigValue(line) == "true";
            }

            return Result.Success(config);
        }
        catch (IOException ex)
        {
            LogGetConfigError(_logger, ex);
            return Result.Failure<RetroArchConfig>($"Error reading config file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<RetroArchConfigInfo>> GetDetailedConfigAsync(string retroArchPath, CancellationToken ct = default)
    {
        try
        {
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            var configPath = Path.Combine(retroArchDir, "retroarch.cfg");

            if (!File.Exists(configPath))
            {
                LogConfigNotFound(_logger, configPath);
                return Result.Success(new RetroArchConfigInfo());
            }

            var config = new RetroArchConfigInfo();
            var lines = await File.ReadAllLinesAsync(configPath, ct);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                    continue;

                if (trimmedLine.StartsWith("savefile_directory"))
                    config.SavefileDirectory = ExtractConfigValue(trimmedLine);
                else if (trimmedLine.StartsWith("savestate_directory"))
                    config.SavestateDirectory = ExtractConfigValue(trimmedLine);
                else if (trimmedLine.StartsWith("system_directory"))
                    config.SystemDirectory = ExtractConfigValue(trimmedLine);
                else if (trimmedLine.StartsWith("core_assets_directory"))
                    config.CoreAssetsDirectory = ExtractConfigValue(trimmedLine);
                else if (trimmedLine.StartsWith("screenshot_directory"))
                    config.ScreenshotDirectory = ExtractConfigValue(trimmedLine);
                else if (trimmedLine.StartsWith("playlist_directory"))
                    config.PlaylistDirectory = ExtractConfigValue(trimmedLine);
                else if (trimmedLine.StartsWith("rgui_browser_directory"))
                    config.ContentDirectory = ExtractConfigValue(trimmedLine);
                else if (trimmedLine.StartsWith("netplay_enable"))
                    config.CloudSyncEnabled = ExtractConfigValue(trimmedLine) == "true";
                else if (trimmedLine.StartsWith("video_driver"))
                    config.Video.Driver = ParseVideoDriver(ExtractConfigValue(trimmedLine));
                else if (trimmedLine.StartsWith("input_driver"))
                    config.Input.Driver = ParseInputDriver(ExtractConfigValue(trimmedLine));
                else if (trimmedLine.StartsWith("audio_driver"))
                    config.Audio.Driver = ExtractConfigValue(trimmedLine) ?? "xaudio";
                else if (trimmedLine.StartsWith("video_fullscreen"))
                    config.Video.Fullscreen = ExtractConfigValue(trimmedLine) == "true";
                else if (trimmedLine.StartsWith("video_windowed_fullscreen"))
                    config.Video.WindowedFullscreen = ExtractConfigValue(trimmedLine) == "true";
                else if (trimmedLine.StartsWith("network_cmd_enable"))
                    config.Network.NetworkCommandEnable = ExtractConfigValue(trimmedLine) == "true";
                else if (trimmedLine.StartsWith("network_cmd_port"))
                {
                    if (int.TryParse(ExtractConfigValue(trimmedLine), out var port))
                        config.Network.NetworkCommandPort = port;
                }
            }

            return Result.Success(config);
        }
        catch (IOException ex)
        {
            LogGetConfigError(_logger, ex);
            return Result.Failure<RetroArchConfigInfo>($"Error reading config file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SetConfigValueAsync(string retroArchPath, string key, string value, CancellationToken ct = default)
    {
        try
        {
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            var configPath = Path.Combine(retroArchDir, "retroarch.cfg");

            if (!File.Exists(configPath))
            {
                return Result.Failure($"Config file not found: {configPath}");
            }

            var lines = (await File.ReadAllLinesAsync(configPath, ct)).ToList();
            var found = false;

            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith(key + " ") ||
                    lines[i].TrimStart().StartsWith(key + "="))
                {
                    lines[i] = $"{key} = \"{value}\"";
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                lines.Add($"{key} = \"{value}\"");
            }

            await File.WriteAllLinesAsync(configPath, lines, ct);
            LogConfigValueSet(_logger, key, value);

            return Result.Success();
        }
        catch (IOException ex)
        {
            LogSetConfigError(_logger, key, ex);
            return Result.Failure($"Error setting config value: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<string?>> GetConfigValueAsync(string retroArchPath, string key, CancellationToken ct = default)
    {
        try
        {
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            var configPath = Path.Combine(retroArchDir, "retroarch.cfg");

            if (!File.Exists(configPath))
            {
                return Result.Success<string?>(null);
            }

            var lines = await File.ReadAllLinesAsync(configPath, ct);

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith(key + " ") ||
                    line.TrimStart().StartsWith(key + "="))
                {
                    return Result.Success<string?>(ExtractConfigValue(line));
                }
            }

            return Result.Success<string?>(null);
        }
        catch (IOException ex)
        {
            LogGetConfigError(_logger, ex);
            return Result.Failure<string?>($"Error reading config value: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetSavefileDirectoryAsync(string retroArchPath, CancellationToken ct = default)
    {
        var configResult = await GetConfigAsync(retroArchPath, ct);
        if (configResult.IsFailure)
            return Result.Failure<string>(configResult.Error ?? "Failed to get config");

        var savefileDir = configResult.Value?.SavefileDirectory;
        if (string.IsNullOrEmpty(savefileDir))
        {
            // Use default RetroArch save directory
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            savefileDir = Path.Combine(retroArchDir, "saves");
        }

        return Result.Success(savefileDir);
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetSavestateDirectoryAsync(string retroArchPath, CancellationToken ct = default)
    {
        var configResult = await GetConfigAsync(retroArchPath, ct);
        if (configResult.IsFailure)
            return Result.Failure<string>(configResult.Error ?? "Failed to get config");

        var savestateDir = configResult.Value?.SavestateDirectory;
        if (string.IsNullOrEmpty(savestateDir))
        {
            // Use default RetroArch savestate directory
            var retroArchDir = Path.GetDirectoryName(retroArchPath)!;
            savestateDir = Path.Combine(retroArchDir, "states");
        }

        return Result.Success(savestateDir);
    }

    /// <inheritdoc />
    public string ExtractConfigValue(string line)
    {
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            return parts[1].Trim().Trim('"');
        }
        return string.Empty;
    }

    private static VideoDriver ParseVideoDriver(string? driver)
    {
        return driver?.ToLowerInvariant() switch
        {
            "d3d11" => VideoDriver.D3D11,
            "d3d12" => VideoDriver.D3D12,
            "gl" or "glcore" => VideoDriver.OpenGL,
            "vulkan" => VideoDriver.Vulkan,
            _ => VideoDriver.D3D11
        };
    }

    private static InputDriver ParseInputDriver(string? driver)
    {
        return driver?.ToLowerInvariant() switch
        {
            "dinput" => InputDriver.DInput,
            "xinput" => InputDriver.XInput,
            "raw" => InputDriver.Raw,
            "sdl2" => InputDriver.SDL,
            _ => InputDriver.DInput
        };
    }

    #region Logging

    [LoggerMessage(EventId = 301, Level = LogLevel.Warning, Message = "RetroArch config file not found: {Path}")]
    static partial void LogConfigNotFound(ILogger logger, string path);

    [LoggerMessage(EventId = 302, Level = LogLevel.Error, Message = "Error getting RetroArch config")]
    static partial void LogGetConfigError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 303, Level = LogLevel.Information, Message = "Config value set: {Key} = {Value}")]
    static partial void LogConfigValueSet(ILogger logger, string key, string value);

    [LoggerMessage(EventId = 304, Level = LogLevel.Error, Message = "Error setting config value: {Key}")]
    static partial void LogSetConfigError(ILogger logger, string key, Exception ex);

    #endregion
}
