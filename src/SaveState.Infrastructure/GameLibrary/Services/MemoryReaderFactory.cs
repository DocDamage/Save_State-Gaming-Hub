using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Factory for creating platform-specific memory readers.
/// </summary>
public static class MemoryReaderFactory
{
    /// <summary>
    /// Creates a platform-specific memory reader based on the current operating system.
    /// </summary>
    /// <param name="services">The service provider for dependency resolution.</param>
    /// <returns>An implementation of <see cref="IGameMemoryReader"/> appropriate for the current platform.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    public static IGameMemoryReader Create(IServiceProvider services)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        if (OperatingSystem.IsWindows())
        {
            return new GameMemoryReader(
                loggerFactory.CreateLogger<GameMemoryReader>(),
                services.GetRequiredService<IMemoryPatternDatabase>());
        }
        else if (OperatingSystem.IsLinux())
        {
            return new LinuxMemoryReader(
                loggerFactory.CreateLogger<LinuxMemoryReader>(),
                services.GetRequiredService<ITimeProvider>());
        }
        else if (OperatingSystem.IsMacOS())
        {
            return new MacOSMemoryReader(
                loggerFactory.CreateLogger<MacOSMemoryReader>());
        }
        else
        {
            throw new PlatformNotSupportedException("Memory reading is only supported on Windows, Linux, and macOS platforms.");
        }
    }
}
