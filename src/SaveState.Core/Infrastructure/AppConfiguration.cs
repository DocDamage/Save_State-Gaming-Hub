using System;
using System.Collections.Generic;
using System.IO;

using SaveState.Core.Interfaces;

namespace SaveState.Core.Infrastructure;

/// <summary>
/// Centralized application configuration service
/// Provides configurable values with environment-specific overrides
/// </summary>
public class AppConfiguration : IAppConfiguration
{
    private readonly Dictionary<string, string> _configValues;

    public AppConfiguration()
    {
        _configValues = LoadConfiguration();
    }

    /// <summary>
    /// Get API endpoint URL with environment override support
    /// </summary>
    public string GetApiEndpoint(string serviceName, string defaultUrl)
    {
        var envKey = $"SAVESTATE_{serviceName.ToUpper()}_URL";
        var envValue = Environment.GetEnvironmentVariable(envKey);

        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        var configKey = $"ApiEndpoints:{serviceName}";
        if (_configValues.TryGetValue(configKey, out var configValue))
        {
            return configValue;
        }

        return defaultUrl;
    }

    /// <summary>
    /// Get configuration value with environment override
    /// </summary>
    public string GetValue(string key, string defaultValue = "")
    {
        var envKey = $"SAVESTATE_{key.ToUpper().Replace(":", "_")}";
        var envValue = Environment.GetEnvironmentVariable(envKey);

        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        if (_configValues.TryGetValue(key, out var configValue))
        {
            return configValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Get integer configuration value
    /// </summary>
    public int GetIntValue(string key, int defaultValue = 0)
    {
        var stringValue = GetValue(key);
        return int.TryParse(stringValue, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Get boolean configuration value
    /// </summary>
    public bool GetBoolValue(string key, bool defaultValue = false)
    {
        var stringValue = GetValue(key);
        return bool.TryParse(stringValue, out var result) ? result : defaultValue;
    }

    private Dictionary<string, string> LoadConfiguration()
    {
        var config = new Dictionary<string, string>();

        // Load from appsettings.json if it exists
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                // Simple JSON parsing for key-value pairs (basic implementation)
                LoadJsonConfig(json, config);
            }
            catch (Exception ex)
            {
                // Log but don't fail - use defaults
                Console.WriteLine($"Warning: Failed to load configuration from {configPath}: {ex.Message}");
            }
        }

        // Load from environment-specific config file
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var envConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"appsettings.{env}.json");
        if (File.Exists(envConfigPath))
        {
            try
            {
                var json = File.ReadAllText(envConfigPath);
                LoadJsonConfig(json, config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load environment configuration from {envConfigPath}: {ex.Message}");
            }
        }

        return config;
    }

    private void LoadJsonConfig(string json, Dictionary<string, string> config)
    {
        // Very basic JSON parsing for simple key-value pairs
        // In production, you'd use System.Text.Json or Newtonsoft.Json
        var lines = json.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Contains("://")) // Likely a URL
            {
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = trimmed.Substring(0, colonIndex).Trim('"', ' ');
                    var value = trimmed.Substring(colonIndex + 1).Trim(',', '"', ' ').TrimEnd('}');
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        config[key] = value;
                    }
                }
            }
        }
    }

    // Singleton instance
    private static AppConfiguration? _instance;
    public static AppConfiguration Instance => _instance ??= new AppConfiguration();
}
