namespace SaveState.Core.Interfaces;

/// <summary>
/// Interface for application configuration service
/// </summary>
public interface IAppConfiguration
{
    /// <summary>
    /// Get API endpoint URL with environment override support
    /// </summary>
    string GetApiEndpoint(string serviceName, string defaultUrl);

    /// <summary>
    /// Get configuration value with environment override
    /// </summary>
    string GetValue(string key, string defaultValue = "");

    /// <summary>
    /// Get integer configuration value
    /// </summary>
    int GetIntValue(string key, int defaultValue = 0);

    /// <summary>
    /// Get boolean configuration value
    /// </summary>
    bool GetBoolValue(string key, bool defaultValue = false);
}
