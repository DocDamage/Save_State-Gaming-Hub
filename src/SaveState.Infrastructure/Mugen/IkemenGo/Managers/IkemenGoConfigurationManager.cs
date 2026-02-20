using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo.Managers;

/// <summary>
/// Manages IKEMEN GO configuration loading, saving, and validation.
/// </summary>
public sealed class IkemenGoConfigurationManager
{
    private readonly ILogger<IkemenGoConfigurationManager> _logger;
    private readonly ITimeProvider _timeProvider;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="IkemenGoConfigurationManager"/> class.
    /// </summary>
    public IkemenGoConfigurationManager(
        ILogger<IkemenGoConfigurationManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Loads IKEMEN GO configuration (config.json).
    /// </summary>
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
            var config = JsonSerializer.Deserialize<IkemenGoConfig>(json, ReadOptions);

            return Result<IkemenGoConfig>.Success(config ?? CreateDefaultConfig());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config");
            return Result<IkemenGoConfig>.Failure($"Load config failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Saves IKEMEN GO configuration.
    /// </summary>
    public async Task<Result> SaveConfigAsync(
        string configPath,
        IkemenGoConfig config,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Saving IKEMEN config to {Path}", configPath);

            var json = JsonSerializer.Serialize(config, WriteOptions);

            await File.WriteAllTextAsync(configPath, json, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save config");
            return Result.Failure($"Save config failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Updates specific configuration options.
    /// </summary>
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

            var config = loadResult.Value;
            var updatedKeys = new List<string>();
            var failedKeys = new List<string>();
            var validationErrors = new List<string>();

            foreach (var update in updates)
            {
                // Apply update based on key path
                if (TryUpdateConfigValue(ref config, update.Key, update.Value))
                {
                    updatedKeys.Add(update.Key);
                }
                else
                {
                    failedKeys.Add(update.Key);
                }
            }

            var saveResult = await SaveConfigAsync(configPath, config, ct);
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

    /// <summary>
    /// Validates IKEMEN GO configuration.
    /// </summary>
#pragma warning disable CA1502 // Cyclomatic complexity acceptable for validation method
    public Task<Result<IkemenGoConfigValidation>> ValidateConfigAsync(
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
            if (config.Video.FpsLimit < 30 || config.Video.FpsLimit > 240)
                warnings.Add("FPS limit outside typical range");

            // Validate audio settings
            if (config.Audio.MasterVolume < 0 || config.Audio.MasterVolume > 100)
                errors.Add("Master volume out of range");
            if (config.Audio.BgmVolume < 0 || config.Audio.BgmVolume > 100)
                errors.Add("BGM volume out of range");
            if (config.Audio.SfxVolume < 0 || config.Audio.SfxVolume > 100)
                errors.Add("SFX volume out of range");

            // Validate gameplay settings
            if (config.Gameplay.Difficulty < 1 || config.Gameplay.Difficulty > 8)
                warnings.Add("Difficulty setting may not be supported");
            if (config.Gameplay.GameSpeed < 0 || config.Gameplay.GameSpeed > 2)
                warnings.Add("Game speed setting may not be supported");
            if (config.Gameplay.RoundTime < 0 || config.Gameplay.RoundTime > 999)
                warnings.Add("Round time setting unusual");

            // Validate network settings
            if (config.Network.ListenPort < 1024 || config.Network.ListenPort > 65535)
                errors.Add("Listen port out of valid range");
            if (config.Network.MaxPing < 0 || config.Network.MaxPing > 1000)
                warnings.Add("Max ping setting unusual");

            // Validate rollback settings
            if (config.Network.Rollback.InputDelay < 0 || config.Network.Rollback.InputDelay > 10)
                warnings.Add("Input delay setting unusual");
            if (config.Network.Rollback.RollbackFrames < 0 || config.Network.Rollback.RollbackFrames > 15)
                warnings.Add("Rollback frames setting unusual");

            var result = new IkemenGoConfigValidation(
                errors.Count == 0,
                errors,
                warnings);

            return Task.FromResult(Result<IkemenGoConfigValidation>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate config");
            return Task.FromResult(Result<IkemenGoConfigValidation>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal));
        }
#pragma warning restore CA1502
    }

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public IkemenGoConfig CreateDefaultConfig()
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

    private bool TryUpdateConfigValue(ref IkemenGoConfig config, string key, object value)
    {
        try
        {
            var parts = key.Split('.');
            if (parts.Length < 2) return false;

            var section = parts[0];
            var property = parts[1];

            switch (section.ToLowerInvariant())
            {
                case "video":
                    config = config with
                    {
                        Video = UpdateVideoSettings(config.Video, property, value)
                    };
                    return true;

                case "audio":
                    config = config with
                    {
                        Audio = UpdateAudioSettings(config.Audio, property, value)
                    };
                    return true;

                case "gameplay":
                    config = config with
                    {
                        Gameplay = UpdateGameplaySettings(config.Gameplay, property, value)
                    };
                    return true;

                case "network":
                    config = config with
                    {
                        Network = UpdateNetworkSettings(config.Network, property, value)
                    };
                    return true;

                case "debug":
                    config = config with
                    {
                        Debug = UpdateDebugSettings(config.Debug, property, value)
                    };
                    return true;

                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private IkemenGoVideoSettings UpdateVideoSettings(IkemenGoVideoSettings settings, string property, object value)
    {
        return property.ToLowerInvariant() switch
        {
            "width" => settings with { Width = Convert.ToInt32(value) },
            "height" => settings with { Height = Convert.ToInt32(value) },
            "fullscreen" => settings with { Fullscreen = Convert.ToBoolean(value) },
            "vsync" => settings with { VSync = Convert.ToBoolean(value) },
            "fpslimit" => settings with { FpsLimit = Convert.ToInt32(value) },
            "renderer" => settings with { Renderer = value.ToString() ?? "OpenGL" },
            _ => settings
        };
    }

    private IkemenGoAudioSettings UpdateAudioSettings(IkemenGoAudioSettings settings, string property, object value)
    {
        return property.ToLowerInvariant() switch
        {
            "mastervolume" => settings with { MasterVolume = Convert.ToInt32(value) },
            "bgmvolume" => settings with { BgmVolume = Convert.ToInt32(value) },
            "sfxvolume" => settings with { SfxVolume = Convert.ToInt32(value) },
            "audioeffects" => settings with { AudioEffects = Convert.ToBoolean(value) },
            _ => settings
        };
    }

    private IkemenGoGameplaySettings UpdateGameplaySettings(IkemenGoGameplaySettings settings, string property, object value)
    {
        return property.ToLowerInvariant() switch
        {
            "difficulty" => settings with { Difficulty = Convert.ToInt32(value) },
            "gamespeed" => settings with { GameSpeed = Convert.ToInt32(value) },
            "roundtime" => settings with { RoundTime = Convert.ToInt32(value) },
            "roundcount" => settings with { RoundCount = Convert.ToInt32(value) },
            "autoguard" => settings with { AutoGuard = Convert.ToBoolean(value) },
            _ => settings
        };
    }

    private IkemenGoNetworkSettings UpdateNetworkSettings(IkemenGoNetworkSettings settings, string property, object value)
    {
        return property.ToLowerInvariant() switch
        {
            "playername" => settings with { PlayerName = value.ToString() ?? "Player" },
            "listenport" => settings with { ListenPort = Convert.ToInt32(value) },
            "maxping" => settings with { MaxPing = Convert.ToInt32(value) },
            "uselobby" => settings with { UseLobby = Convert.ToBoolean(value) },
            "lobbyserver" => settings with { LobbyServer = value.ToString() },
            _ => settings
        };
    }

    private IkemenGoDebugSettings UpdateDebugSettings(IkemenGoDebugSettings settings, string property, object value)
    {
        return property.ToLowerInvariant() switch
        {
            "debugmode" => settings with { DebugMode = Convert.ToBoolean(value) },
            "showfps" => settings with { ShowFps = Convert.ToBoolean(value) },
            "showinputs" => settings with { ShowInputs = Convert.ToBoolean(value) },
            "logtofile" => settings with { LogToFile = Convert.ToBoolean(value) },
            _ => settings
        };
    }
}
