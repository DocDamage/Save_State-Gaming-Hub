namespace SaveState.Infrastructure.Mugen;

using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Configuration;
using SaveState.Core.Mugen.Services;

/// <summary>
/// Implementation of the MUGEN configuration service.
/// Manages reading and writing IKEMEN config.json files.
/// </summary>
public class MugenConfigService : IMugenConfigService
{
    private readonly MugenOptions _options;
    private readonly ILogger<MugenConfigService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly string _configFilePath;
    private JsonNode? _cachedConfig;
    private DateTime _lastLoadTime;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public MugenConfigService(IOptions<MugenOptions> options, ILogger<MugenConfigService> logger, ITimeProvider timeProvider)
    {
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
        var exeDir = Path.GetDirectoryName(_options.ExecutablePath) ?? "engines/ikemen";
        _configFilePath = Path.Combine(exeDir, "data", "config.json");
    }

    public async Task<Result<string>> GetConfigValueAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _lock.WaitAsync(ct);
            try
            {
                var loadResult = await EnsureConfigLoadedAsync(ct);
                if (loadResult.IsFailure)
                    return Result.Failure<string>(loadResult.Error);

                if (_cachedConfig is null)
                {
                    return Result.Failure<string>("Configuration failed to load");
                }

                var value = GetNestedValue(_cachedConfig, key);
                if (value == null)
                    return Result.Failure<string>($"Configuration key '{key}' not found", ErrorType.NotFound);

                return Result.Success(value.ToJsonString());
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get config value for key: {Key}", key);
            return Result.Failure<string>($"Failed to read configuration: {ex.Message}");
        }
    }

    public async Task<Result> SetConfigValueAsync(string key, object value, CancellationToken ct = default)
    {
        return await SetConfigValuesAsync(new Dictionary<string, object> { { key, value } }, ct);
    }

    public async Task<Result> SetConfigValuesAsync(Dictionary<string, object> settings, CancellationToken ct = default)
    {
        try
        {
            await _lock.WaitAsync(ct);
            try
            {
                // Create backup before modification
                var backupResult = await CreateBackupAsync(ct);
                if (backupResult.IsFailure)
                {
                    _logger.LogWarning("Failed to create backup before modifying config: {Error}", backupResult.Error);
                }

                // Load current config
                var loadResult = await EnsureConfigLoadedAsync(ct);
                if (loadResult.IsFailure)
                    return loadResult;

                if (_cachedConfig is null)
                {
                    return Result.Failure("Configuration failed to load");
                }

                // Apply all settings
                foreach (var setting in settings)
                {
                    SetNestedValue(_cachedConfig, setting.Key, setting.Value);
                }

                // Write to disk
                var json = _cachedConfig.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_configFilePath, json, ct);
                _lastLoadTime = DateTime.UtcNow;

                _logger.LogInformation("Updated {Count} configuration settings", settings.Count);
                return Result.Success();
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set config values");
            return Result.Failure($"Failed to write configuration: {ex.Message}");
        }
    }

    public async Task<Result> ReloadConfigAsync(CancellationToken ct = default)
    {
        try
        {
            await _lock.WaitAsync(ct);
            try
            {
                _cachedConfig = null;
                _lastLoadTime = DateTime.MinValue;

                var loadResult = await EnsureConfigLoadedAsync(ct);
                return loadResult;
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload config");
            return Result.Failure($"Failed to reload configuration: {ex.Message}");
        }
    }

    public async Task<Result<string>> CreateBackupAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(_configFilePath))
                return Result.Failure<string>("Configuration file not found", ErrorType.NotFound);

            var configDir = Path.GetDirectoryName(_configFilePath) ?? "data";
            var backupDir = Path.Combine(configDir, "backups");
            Directory.CreateDirectory(backupDir);

            var timestamp = _timeProvider.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"config_{timestamp}.json");

            File.Copy(_configFilePath, backupPath, overwrite: false);

            // Keep only last 10 backups
            await CleanupOldBackupsAsync(backupDir, 10, ct);

            _logger.LogInformation("Created config backup: {BackupPath}", backupPath);
            return Result.Success(backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create config backup");
            return Result.Failure<string>($"Failed to create backup: {ex.Message}");
        }
    }

    public async Task<Result> RestoreBackupAsync(string backupPath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(backupPath))
                return Result.Failure("Backup file not found", ErrorType.NotFound);

            await _lock.WaitAsync(ct);
            try
            {
                // Validate backup file is valid JSON
                var backupContent = await File.ReadAllTextAsync(backupPath, ct);
                var testParse = JsonNode.Parse(backupContent);
                if (testParse == null)
                    return Result.Failure("Backup file is not valid JSON");

                // Copy backup to main config
                File.Copy(backupPath, _configFilePath, overwrite: true);

                // Reload cache
                _cachedConfig = null;
                _lastLoadTime = DateTime.MinValue;
                await EnsureConfigLoadedAsync(ct);

                _logger.LogInformation("Restored config from backup: {BackupPath}", backupPath);
                return Result.Success();
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore config from backup: {BackupPath}", backupPath);
            return Result.Failure($"Failed to restore backup: {ex.Message}");
        }
    }

    private async Task<Result> EnsureConfigLoadedAsync(CancellationToken ct)
    {
        // Check if config needs reload (cache is older than 5 minutes)
        if (_cachedConfig != null && (DateTime.UtcNow - _lastLoadTime).TotalMinutes < 5)
            return Result.Success();

        try
        {
            if (!File.Exists(_configFilePath))
            {
                // Create default config if it doesn't exist
                _cachedConfig = CreateDefaultConfig();
                var json = _cachedConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

                var dir = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                await File.WriteAllTextAsync(_configFilePath, json, ct);
                _lastLoadTime = DateTime.UtcNow;
                _logger.LogInformation("Created default config file: {Path}", _configFilePath);
                return Result.Success();
            }

            var content = await File.ReadAllTextAsync(_configFilePath, ct);
            _cachedConfig = JsonNode.Parse(content);
            _lastLoadTime = DateTime.UtcNow;

            if (_cachedConfig == null)
                return Result.Failure("Failed to parse config.json - invalid JSON format");

            return Result.Success();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in config file: {Path}", _configFilePath);
            return Result.Failure($"Invalid JSON format in config file: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config file: {Path}", _configFilePath);
            return Result.Failure($"Failed to load configuration: {ex.Message}");
        }
    }

    private static JsonNode? GetNestedValue(JsonNode root, string key)
    {
        var parts = key.Split('.');
        var current = root;

        foreach (var part in parts)
        {
            if (current is JsonObject obj && obj.TryGetPropertyValue(part, out var value))
            {
                current = value;
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private static void SetNestedValue(JsonNode root, string key, object value)
    {
        var parts = key.Split('.');
        var current = root;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            if (current is JsonObject obj)
            {
                if (!obj.TryGetPropertyValue(part, out var next))
                {
                    next = new JsonObject();
                    obj[part] = next;
                }
                current = next!;
            }
        }

        if (current is JsonObject targetObj)
        {
            var lastPart = parts[^1];
            targetObj[lastPart] = JsonValue.Create(value);
        }
    }

    private static JsonNode CreateDefaultConfig()
    {
        return new JsonObject
        {
            ["GameMode"] = new JsonObject
            {
                ["ActiveTag"] = true,
                ["SimulMode"] = false,
                ["DashCancel"] = false,
                ["GuardBreak"] = true,
                ["Clashing"] = false,
                ["ShadowAssist"] = false
            },
            ["Video"] = new JsonObject
            {
                ["DramaticZoom"] = false,
                ["RainbowEdition"] = false,
                ["AutoCamera"] = true,
                ["AttackDataDisplay"] = false
            },
            ["System"] = new JsonObject
            {
                ["RoundsToWin"] = 2,
                ["TimeLimit"] = 99,
                ["Difficulty"] = 4
            }
        };
    }

    private async Task CleanupOldBackupsAsync(string backupDir, int keepCount, CancellationToken ct)
    {
        try
        {
            var backups = Directory.GetFiles(backupDir, "config_*.json")
                .OrderByDescending(File.GetCreationTime)
                .Skip(keepCount)
                .ToList();

            foreach (var backup in backups)
            {
                File.Delete(backup);
                _logger.LogDebug("Deleted old backup: {Path}", backup);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backups");
        }
    }
}
