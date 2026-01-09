using SaveState.Core.Common;
using SaveState.Core.Configuration;

namespace SaveState.Application.RomManagement.Services;

/// <summary>
/// Service for managing ROM directory paths and configuration.
/// </summary>
public interface IRomPathManager
{
    /// <summary>
    /// Gets the current ROM scanning configuration.
    /// </summary>
    /// <returns>The ROM scanning options.</returns>
    RomScanningOptions GetConfiguration();

    /// <summary>
    /// Updates the ROM scanning configuration.
    /// </summary>
    /// <param name="options">The new configuration options.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdateConfigurationAsync(RomScanningOptions options);

    /// <summary>
    /// Adds a new ROM directory to the configuration.
    /// </summary>
    /// <param name="path">The directory path to add.</param>
    /// <param name="validatePath">Whether to validate the path exists.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AddRomDirectoryAsync(string path, bool validatePath = true);

    /// <summary>
    /// Removes a ROM directory from the configuration.
    /// </summary>
    /// <param name="path">The directory path to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RemoveRomDirectoryAsync(string path);

    /// <summary>
    /// Gets all configured and expanded ROM directory paths.
    /// </summary>
    /// <returns>A list of absolute directory paths.</returns>
    Task<IReadOnlyList<string>> GetRomDirectoriesAsync();

    /// <summary>
    /// Validates that a directory exists and is accessible.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <returns>True if the directory is valid and accessible.</returns>
    bool ValidateRomDirectory(string path);

    /// <summary>
    /// Expands environment variables and relative paths to absolute paths.
    /// </summary>
    /// <param name="path">The path to expand.</param>
    /// <returns>The expanded absolute path.</returns>
    string ExpandPath(string path);
}