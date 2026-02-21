using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Enums;

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


