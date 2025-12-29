namespace SaveState.Core.RomManagement;

/// <summary>
/// Registry for platform-specific file extensions.
/// </summary>
public interface IPlatformExtensionRegistry
{
    /// <summary>
    /// Gets the valid file extensions for a given platform.
    /// </summary>
    /// <param name="platformName">The name of the platform.</param>
    /// <returns>An array of file extensions (including the leading dot).</returns>
    string[] GetExtensions(string platformName);

    /// <summary>
    /// Checks if a file is a valid ROM for a given platform based on its extension.
    /// </summary>
    /// <param name="platformName">The name of the platform.</param>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>True if the file extension is valid for the platform.</returns>
    bool IsValidExtension(string platformName, string filePath);

    /// <summary>
    /// Attempts to detect the platform name based on the file extension.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The platform name if found, otherwise null.</returns>
    string? DetectPlatformName(string filePath);
}
