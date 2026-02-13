using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using SaveState.Core.RetroArch.Models;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for managing RetroArch configuration.
/// </summary>
public interface IConfigurationEngine
{
    /// <summary>
    /// Gets RetroArch configuration.
    /// </summary>
    Task<Result<RetroArchConfig>> GetConfigAsync(string retroArchPath, CancellationToken ct = default);

    /// <summary>
    /// Gets detailed RetroArch configuration.
    /// </summary>
    Task<Result<RetroArchConfigInfo>> GetDetailedConfigAsync(string retroArchPath, CancellationToken ct = default);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    Task<Result> SetConfigValueAsync(string retroArchPath, string key, string value, CancellationToken ct = default);

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    Task<Result<string?>> GetConfigValueAsync(string retroArchPath, string key, CancellationToken ct = default);

    /// <summary>
    /// Gets the savefile directory.
    /// </summary>
    Task<Result<string>> GetSavefileDirectoryAsync(string retroArchPath, CancellationToken ct = default);

    /// <summary>
    /// Gets the savestate directory.
    /// </summary>
    Task<Result<string>> GetSavestateDirectoryAsync(string retroArchPath, CancellationToken ct = default);

    /// <summary>
    /// Parses config line to extract value.
    /// </summary>
    string ExtractConfigValue(string line);
}
