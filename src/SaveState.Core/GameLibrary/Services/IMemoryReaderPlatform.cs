using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Platform abstraction for memory reader implementations.
/// </summary>
public interface IMemoryReaderPlatform
{
    /// <summary>
    /// Gets the name of the platform.
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Gets whether this platform is supported on the current OS.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Creates a memory reader instance for this platform.
    /// </summary>
    /// <returns>Result containing the memory reader or error.</returns>
    Result<IGameMemoryReader> CreateReader();
}

/// <summary>
/// Supported platform types for memory reading.
/// </summary>
public enum PlatformType
{
    /// <summary>Windows platform.</summary>
    Windows,

    /// <summary>Linux platform (including Steam Deck).</summary>
    Linux,

    /// <summary>macOS platform.</summary>
    MacOS,

    /// <summary>Unknown or unsupported platform.</summary>
    Unknown
}

/// <summary>
/// Extension methods for PlatformType.
/// </summary>
public static class PlatformTypeExtensions
{
    /// <summary>
    /// Converts PlatformType to a display name.
    /// </summary>
    public static string ToDisplayName(this PlatformType platform)
    {
        return platform switch
        {
            PlatformType.Windows => "Windows",
            PlatformType.Linux => "Linux",
            PlatformType.MacOS => "macOS",
            PlatformType.Unknown => "Unknown",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Checks if the platform supports memory reading operations.
    /// </summary>
    public static bool SupportsMemoryReading(this PlatformType platform)
    {
        return platform is PlatformType.Windows or PlatformType.Linux;
    }
}
