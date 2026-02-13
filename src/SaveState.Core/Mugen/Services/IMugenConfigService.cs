namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;

/// <summary>
/// Service interface for reading and writing MUGEN/IKEMEN configuration files.
/// Handles config.json and system.def file parsing and modification.
/// </summary>
public interface IMugenConfigService
{
    /// <summary>
    /// Gets a configuration value from config.json.
    /// </summary>
    /// <param name="key">Configuration key (e.g., "GameMode.ActiveTag").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The configuration value as a string, or failure if key not found.</returns>
    Task<Result<string>> GetConfigValueAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Sets a configuration value in config.json.
    /// </summary>
    /// <param name="key">Configuration key (e.g., "GameMode.ActiveTag").</param>
    /// <param name="value">Value to set (will be converted to appropriate JSON type).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> SetConfigValueAsync(string key, object value, CancellationToken ct = default);

    /// <summary>
    /// Sets multiple configuration values in config.json atomically.
    /// </summary>
    /// <param name="settings">Dictionary of key-value pairs to set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> SetConfigValuesAsync(Dictionary<string, object> settings, CancellationToken ct = default);

    /// <summary>
    /// Reloads the configuration from disk.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> ReloadConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a backup of the current configuration files.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Path to the backup file, or failure.</returns>
    Task<Result<string>> CreateBackupAsync(CancellationToken ct = default);

    /// <summary>
    /// Restores configuration from a backup file.
    /// </summary>
    /// <param name="backupPath">Path to the backup file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> RestoreBackupAsync(string backupPath, CancellationToken ct = default);
}
